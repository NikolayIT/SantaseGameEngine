namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Santase.AI.ClaudePlayer.Neural;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

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
    public class ClaudePlayerNeural : BasePlayer
    {
        private const int MaxSearchDepth = 14;
        private const int RoundWinReward = 1000;
        private const int HandOutReward = 500;

        private readonly Card[][] moveBuffers;
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

            this.moveBuffers = new Card[MaxSearchDepth][];
            for (var i = 0; i < MaxSearchDepth; i++)
            {
                this.moveBuffers[i] = new Card[6];
            }
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

        private static int EnumerateMoves(GameState state, Card[] buffer)
        {
            var hand = state.MyTurn ? state.MyHand : state.OppHand;

            if (state.LedCard == null)
            {
                return FillFromBitmask(hand, buffer);
            }

            var lead = state.LedCard;
            var leadSuit = lead.Suit;
            var leadVal = lead.GetValue();
            var trumpSuit = state.TrumpSuit;
            var allTypes = new[]
            {
                CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace,
            };

            var n = 0;

            foreach (var t in allTypes)
            {
                var hash = ((int)leadSuit * 13) + (int)t;
                if ((hand & (1L << hash)) != 0)
                {
                    var c = Card.Cards[hash];
                    if (c.GetValue() > leadVal)
                    {
                        buffer[n++] = c;
                    }
                }
            }

            if (n > 0)
            {
                return n;
            }

            foreach (var t in allTypes)
            {
                var hash = ((int)leadSuit * 13) + (int)t;
                if ((hand & (1L << hash)) != 0)
                {
                    buffer[n++] = Card.Cards[hash];
                }
            }

            if (n > 0)
            {
                return n;
            }

            if (leadSuit != trumpSuit)
            {
                foreach (var t in allTypes)
                {
                    var hash = ((int)trumpSuit * 13) + (int)t;
                    if ((hand & (1L << hash)) != 0)
                    {
                        buffer[n++] = Card.Cards[hash];
                    }
                }

                if (n > 0)
                {
                    return n;
                }
            }

            return FillFromBitmask(hand, buffer);
        }

        private static int FillFromBitmask(long hand, Card[] buffer)
        {
            var n = 0;
            while (hand != 0L)
            {
                var hash = BitOperations.TrailingZeroCount((ulong)hand);
                buffer[n++] = Card.Cards[hash];
                hand &= hand - 1;
            }

            return n;
        }

        private static GameState ApplyMove(GameState state, Card card)
        {
            var newState = state;
            var cardMask = 1L << card.GetHashCode();

            if (state.MyTurn)
            {
                newState.MyHand &= ~cardMask;
            }
            else
            {
                newState.OppHand &= ~cardMask;
            }

            if (state.LedCard == null)
            {
                var announce = 0;
                if (card.Type == CardType.King || card.Type == CardType.Queen)
                {
                    var partnerType = card.Type == CardType.King ? CardType.Queen : CardType.King;
                    var partnerHash = ((int)card.Suit * 13) + (int)partnerType;
                    var partnerMask = 1L << partnerHash;
                    var preHand = state.MyTurn ? state.MyHand : state.OppHand;
                    if ((preHand & partnerMask) != 0)
                    {
                        announce = card.Suit == state.TrumpSuit ? 40 : 20;
                    }
                }

                if (state.MyTurn)
                {
                    newState.MyPoints += announce;
                }
                else
                {
                    newState.OppPoints += announce;
                }

                newState.LedCard = card;
                newState.MyTurn = !state.MyTurn;
            }
            else
            {
                var leader = state.LedCard;
                var trickValue = leader.GetValue() + card.GetValue();

                // The follower (the card just played) wins iff it beats the led card.
                var followerWins =
                    CardWinnerLogic.GetWinner(leader, card, state.TrumpSuit) == PlayerPosition.SecondPlayer;

                var amWinningTrick = state.MyTurn == followerWins;

                if (amWinningTrick)
                {
                    newState.MyPoints += trickValue;
                }
                else
                {
                    newState.OppPoints += trickValue;
                }

                newState.LedCard = null;
                newState.MyTurn = amWinningTrick;
            }

            return newState;
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

            Card ledCard = null;
            if (!amLeader)
            {
                ledCard = context.FirstPlayedCard;
                if (ledCard != null)
                {
                    oppHand &= ~(1L << ledCard.GetHashCode());
                }
            }

            if (myHand == 0L || oppHand == 0L)
            {
                return null;
            }

            var rootState = new GameState
            {
                MyHand = myHand,
                OppHand = oppHand,
                MyPoints = amLeader ? context.FirstPlayerRoundPoints : context.SecondPlayerRoundPoints,
                OppPoints = amLeader ? context.SecondPlayerRoundPoints : context.FirstPlayerRoundPoints,
                LedCard = ledCard,
                MyTurn = true,
                TrumpSuit = context.TrumpCard.Suit,
            };

            var moves = this.moveBuffers[0];
            var count = EnumerateMoves(rootState, moves);
            if (count == 0)
            {
                return null;
            }

            Card best = null;
            var bestVal = int.MinValue;
            var alpha = int.MinValue;
            var beta = int.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var ns = ApplyMove(rootState, moves[i]);
                var v = this.Search(ns, alpha, beta, 1);
                if (v > bestVal)
                {
                    bestVal = v;
                    best = moves[i];
                }

                if (bestVal > alpha)
                {
                    alpha = bestVal;
                }
            }

            if (best != null && !possibleCards.Contains(best))
            {
                return null;
            }

            return best;
        }

        private int Search(GameState state, int alpha, int beta, int depth)
        {
            if (state.MyPoints >= 66)
            {
                return RoundWinReward + state.MyPoints - state.OppPoints;
            }

            if (state.OppPoints >= 66)
            {
                return -RoundWinReward + state.MyPoints - state.OppPoints;
            }

            if (state.MyHand == 0L && state.OppHand == 0L)
            {
                if (state.MyPoints > state.OppPoints)
                {
                    return HandOutReward + state.MyPoints - state.OppPoints;
                }

                if (state.MyPoints < state.OppPoints)
                {
                    return -HandOutReward + state.MyPoints - state.OppPoints;
                }

                return 0;
            }

            var moves = this.moveBuffers[depth];
            var count = EnumerateMoves(state, moves);
            if (count == 0)
            {
                return state.MyPoints - state.OppPoints;
            }

            if (state.MyTurn)
            {
                var best = int.MinValue;
                for (var i = 0; i < count; i++)
                {
                    var ns = ApplyMove(state, moves[i]);
                    var v = this.Search(ns, alpha, beta, depth + 1);
                    if (v > best)
                    {
                        best = v;
                    }

                    if (best > alpha)
                    {
                        alpha = best;
                    }

                    if (alpha >= beta)
                    {
                        break;
                    }
                }

                return best;
            }
            else
            {
                var best = int.MaxValue;
                for (var i = 0; i < count; i++)
                {
                    var ns = ApplyMove(state, moves[i]);
                    var v = this.Search(ns, alpha, beta, depth + 1);
                    if (v < best)
                    {
                        best = v;
                    }

                    if (best < beta)
                    {
                        beta = best;
                    }

                    if (alpha >= beta)
                    {
                        break;
                    }
                }

                return best;
            }
        }

        private struct GameState
        {
            public long MyHand;
            public long OppHand;
            public int MyPoints;
            public int OppPoints;
            public Card LedCard;
            public bool MyTurn;
            public CardSuit TrumpSuit;
        }
    }
}
