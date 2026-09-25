namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;

    using Santase.AI.ClaudePlayer.Neural;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    /// <summary>
    /// Variant of <see cref="ClaudePlayer"/> where the heuristic card-choice path is replaced
    /// by a multilayer perceptron policy network. Strategy:
    ///   * Same alpha-beta minimax in non-closed Phase 2 (perfect-info subgame).
    ///   * For every other turn, encode the position into a fixed feature vector, run a
    ///     forward pass through <see cref="NeuralNetwork"/>, mask the 24-logit output to
    ///     legal moves only, and play the argmax. The whole heuristic helper tree
    ///     (lead/follow phase 1/2, marriage preservation, guaranteed-winner search, etc.)
    ///     is gone.
    /// The trump-swap and close-game gates are kept rule-based — they're discrete tactical
    /// gates, not scoring decisions, and keeping them avoids fighting the engine validators.
    /// </summary>
    public class ClaudePlayerNeural : BasePlayer, IRestorablePlayer
    {
        // Exact solver for the perfect-information Phase-2 endgame (see ChooseCard).
        private readonly EndgameSolver endgameSolver = new EndgameSolver(EndgameSolver.Evaluation.Neural);

        private readonly NeuralNetwork network;
        private readonly float[] features;
        private readonly float[] logits;
        private readonly float[] sampleProbs;

        public ClaudePlayerNeural()
            : this(NeuralWeightsLoader.Load())
        {
        }

        public ClaudePlayerNeural(NeuralNetwork network)
        {
            this.network = network;
            this.features = new float[NeuralFeatureEncoder.FeatureCount];
            this.logits = new float[NeuralNetwork.OutputSize];
            this.sampleProbs = new float[NeuralNetwork.OutputSize];
        }

        public override string Name => "Claude Player (Neural)";

        /// <summary>
        /// Softmax sampling temperature. 0 (default) plays the argmax over legal moves
        /// (deterministic, used in production / regression). A positive value samples from the
        /// softmax-with-temperature distribution restricted to legal cards — required for
        /// REINFORCE-style training where the trainee needs to explore.
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// Optional sink for PPO training: (features, sampledAction, legalMask24bit, oldLogProb).
        /// oldLogProb is log of the masked-softmax probability of the sampled action under the
        /// behavior policy. Only fires on sampled NN decisions (<see cref="Temperature"/> &gt; 0);
        /// for PPO correctness run with Temperature = 1 so behavior == training distribution.
        /// Null by default = zero production cost.
        /// </summary>
        public Action<float[], int, int, float> PpoRecorder { get; set; }

        /// <summary>
        /// RNG used when <see cref="Temperature"/> &gt; 0. Defaults to <see cref="Random.Shared"/>
        /// (thread-safe), but a deterministic seed can be injected for tests.
        /// </summary>
        public Random Rng { get; set; } = Random.Shared;

        private CardCollection UnknownCards { get; set; } = new CardCollection(CardCollection.AllSantaseCardsBitMask);

        private CardCollection PlayedCards { get; set; } = new CardCollection();

        private Card LastSeenTrumpCard { get; set; }

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);

            this.UnknownCards = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            foreach (var c in cards)
            {
                this.UnknownCards.Remove(c);
            }

            this.PlayedCards = new CardCollection();
            this.LastSeenTrumpCard = null;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.UnknownCards.Remove(card);
        }

        public void Restore(SantaseSeatView view)
        {
            this.RestoreHand(view);
            this.UnknownCards = SeatViewMemory.UnknownCards(view);
            this.PlayedCards = view.GetPlayedCards();
            this.LastSeenTrumpCard = view.TrumpCard;
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            if (context.CardsLeftInDeck == 2 && context.TrumpCard != null)
            {
                this.UnknownCards.Add(context.TrumpCard);
            }

            this.RecordPlayed(context.FirstPlayedCard);
            this.RecordPlayed(context.SecondPlayedCard);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.SyncTrumpCard(context.TrumpCard);

            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                var oldTrumpOnTable = context.TrumpCard;
                this.LastSeenTrumpCard = Card.GetCard(oldTrumpOnTable.Suit, CardType.Nine);
                return this.ChangeTrump(oldTrumpOnTable);
            }

            if (this.ShouldCloseGame(context))
            {
                return this.CloseGame();
            }

            var possibleCards = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            var chosen = this.ChooseCard(context, possibleCards);
            return this.PlayCard(chosen);
        }

        private void SyncTrumpCard(Card current)
        {
            if (Card.Equals(current, this.LastSeenTrumpCard))
            {
                return;
            }

            if (this.LastSeenTrumpCard != null)
            {
                this.UnknownCards.Add(this.LastSeenTrumpCard);
            }

            this.UnknownCards.Remove(current);
            this.LastSeenTrumpCard = current;
        }

        private void RecordPlayed(Card card)
        {
            if (card == null)
            {
                return;
            }

            this.UnknownCards.Remove(card);
            this.PlayedCards.Add(card);
        }

        private bool ShouldCloseGame(PlayerTurnContext context)
        {
            if (!this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards))
            {
                return false;
            }

            var trumpSuit = context.TrumpCard.Suit;
            var trumpCount = 0;
            foreach (var c in this.Cards)
            {
                if (c.Suit == trumpSuit)
                {
                    trumpCount++;
                }
            }

            if (trumpCount >= 5)
            {
                return true;
            }

            if (trumpCount >= 4
                && this.Cards.Contains(Card.GetCard(trumpSuit, CardType.King))
                && this.Cards.Contains(Card.GetCard(trumpSuit, CardType.Queen)))
            {
                var oppCouldHaveAce = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ace));
                var oppCouldHaveTen = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ten));
                if (!oppCouldHaveAce || !oppCouldHaveTen)
                {
                    return true;
                }
            }

            return false;
        }

        private Card ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            if (context.State.ShouldObserveRules && context.CardsLeftInDeck == 0)
            {
                var move = this.RunMinimax(context, possibleCards);
                if (move != null)
                {
                    return move;
                }
            }

            return this.ChooseByPolicy(context, possibleCards);
        }

        private Card ChooseByPolicy(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            NeuralFeatureEncoder.Encode(this.features, context, this.Cards, this.PlayedCards, this.UnknownCards);
            this.network.Forward(this.features, this.logits);

            var sampling = this.Temperature > 0f;
            var chosenProb = 0f;
            var chosen = sampling
                ? this.SampleLegalCard(possibleCards, out chosenProb)
                : this.ArgmaxLegalCard(possibleCards);

            if (sampling && this.PpoRecorder != null)
            {
                var legalMask = 0;
                foreach (var c in possibleCards)
                {
                    legalMask |= 1 << NeuralFeatureEncoder.CardIndex(c);
                }

                var p = chosenProb > 1e-12f ? chosenProb : 1e-12f;
                this.PpoRecorder(this.features, NeuralFeatureEncoder.CardIndex(chosen), legalMask, MathF.Log(p));
            }

            return chosen;
        }

        private Card ArgmaxLegalCard(ICollection<Card> possibleCards)
        {
            Card best = null;
            var bestLogit = float.NegativeInfinity;
            foreach (var c in possibleCards)
            {
                var v = this.logits[NeuralFeatureEncoder.CardIndex(c)];
                if (v > bestLogit)
                {
                    bestLogit = v;
                    best = c;
                }
            }

            if (best == null)
            {
                foreach (var c in possibleCards)
                {
                    return c;
                }
            }

            return best;
        }

        private Card SampleLegalCard(ICollection<Card> possibleCards, out float chosenProb)
        {
            // Numerically stable softmax over legal cards only, then sample one.
            var temp = this.Temperature;
            var maxScaled = float.NegativeInfinity;
            foreach (var c in possibleCards)
            {
                var scaled = this.logits[NeuralFeatureEncoder.CardIndex(c)] / temp;
                if (scaled > maxScaled)
                {
                    maxScaled = scaled;
                }
            }

            Array.Clear(this.sampleProbs, 0, this.sampleProbs.Length);
            var sumExp = 0f;
            foreach (var c in possibleCards)
            {
                var idx = NeuralFeatureEncoder.CardIndex(c);
                var w = MathF.Exp((this.logits[idx] / temp) - maxScaled);
                this.sampleProbs[idx] = w;
                sumExp += w;
            }

            if (sumExp <= 0f || float.IsNaN(sumExp) || float.IsInfinity(sumExp))
            {
                // Degenerate distribution — fall back to argmax (still a legal move).
                chosenProb = 1f;
                return this.ArgmaxLegalCard(possibleCards);
            }

            var u = (float)this.Rng.NextDouble() * sumExp;
            var cumulative = 0f;
            Card last = null;
            foreach (var c in possibleCards)
            {
                last = c;
                cumulative += this.sampleProbs[NeuralFeatureEncoder.CardIndex(c)];
                if (cumulative >= u)
                {
                    chosenProb = this.sampleProbs[NeuralFeatureEncoder.CardIndex(c)] / sumExp;
                    return c;
                }
            }

            chosenProb = last != null
                ? this.sampleProbs[NeuralFeatureEncoder.CardIndex(last)] / sumExp
                : 1f;
            return last;
        }

        private Card RunMinimax(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            var amLeader = context.IsFirstPlayerTurn;

            long myHand = 0L;
            foreach (var c in this.Cards)
            {
                myHand |= 1L << c.GetHashCode();
            }

            long oppHand = 0L;
            foreach (var c in this.UnknownCards)
            {
                oppHand |= 1L << c.GetHashCode();
            }

            var ledHash = -1;
            if (!amLeader)
            {
                // Opponent's lead card is still in UnknownCards (EndTurn hasn't fired for this
                // trick yet); subtract it so OppHand reflects what they have left to play.
                ledHash = context.FirstPlayedCard.GetHashCode();
                oppHand &= ~(1L << ledHash);
            }

            if (myHand == 0L || oppHand == 0L)
            {
                return null;
            }

            var bestHash = this.endgameSolver.FindBestMove(
                myHand,
                oppHand,
                amLeader ? context.FirstPlayerRoundPoints : context.SecondPlayerRoundPoints,
                amLeader ? context.SecondPlayerRoundPoints : context.FirstPlayerRoundPoints,
                ledHash,
                context.TrumpCard.Suit,
                0,
                0);
            if (bestHash < 0)
            {
                return null;
            }

            var best = Card.Cards[bestHash];
            return possibleCards.Contains(best) ? best : null;
        }
    }
}
