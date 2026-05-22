namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Santase.AI.ClaudePlayer.Neural;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    /// <summary>
    /// Shared scaffolding for determinized-search Santase players (currently <see cref="ClaudePlayerIsmcts"/>).
    /// It owns everything such a search needs in common:
    ///   * round bookkeeping (the <c>UnknownCards</c> belief, played cards, trick counts, whether
    ///     we closed) kept in lockstep with the engine callbacks, mirroring <see cref="ClaudePlayer"/>;
    ///   * the rule-based trump-swap / close-game gates and the exact alpha-beta solve for the
    ///     perfect-information Phase-2 endgame;
    ///   * a fast, allocation-light Santase simulator over a bitmask <see cref="SimState"/>
    ///     (legal-move generation, move application with draws/phase transitions, terminal scoring)
    ///     plus a strong perfect-information rollout policy;
    ///   * determinization: turning the current information set into concrete fully-observable worlds.
    /// The only thing subclasses provide is <see cref="RunSearch"/> — how they spend the time budget
    /// turning sampled worlds into a chosen card.
    /// </summary>
    public abstract class ClaudeSearchPlayerBase : BasePlayer
    {
        protected const int MaxHandSize = 6;

        protected const int RoundPointsToWin = 66;

        // Engine state-machine phases, reconstructed from the turn context so the simulator can
        // transition draws / rule-changes exactly like the real round does.
        protected const int PhaseStart = 0;
        protected const int PhaseMoreThanTwo = 1;
        protected const int PhaseTwoLeft = 2;
        protected const int PhaseFinal = 3;

        private const int MaxSearchDepth = 14;

        // Eval magnitude per game-point for the exact endgame solve (margin is the sub-unit tiebreak).
        private const int GamePointReward = 1000;

        private const int HalfRoundPoints = 33;

        // Type ranks in the order the engine enumerates them; only the six real Santase types.
        private static readonly CardType[] AllTypes =
        {
            CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace,
        };

        private static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        // Inverse lookups by card hash. The engine hash is suit*13 + type where type runs 1..13
        // (King = 13), so plain hash/13 and hash%13 do NOT recover (suit, type) — King hashes
        // (13, 26, 39, 52) would divide into the next suit. These tables hold the true values.
        private static readonly int[] ValueByHash = new int[53];
        private static readonly int[] SuitByHash = new int[53];
        private static readonly int[] TypeByHash = new int[53];

        // Bit mask of the six card hashes belonging to each suit (index = suit 0..3).
        private static readonly long[] SuitMask = new long[4];

        // For each card hash, the mask of same-suit cards that strictly beat it. Lets the hot path
        // test "is there a higher card of this suit in <hand>" with a single AND instead of a loop.
        private static readonly long[] HigherInSuitMask = new long[53];

        // For a King/Queen hash, the single-bit mask of its marriage partner (Queen/King of the same
        // suit); 0 for every other card. Reduces "do I hold the marriage partner" to one AND.
        private static readonly long[] PartnerMask = new long[53];

        // Per-depth scratch buffers for the recursive endgame solve (avoids per-call allocation).
        private readonly int[][] endgameMoveBuffers;

        // Reused move buffer for the rollout (and the subclass tree descent).
        private readonly int[] moveScratch = new int[MaxHandSize];

        private readonly int[] unknownPool = new int[24];
        private readonly int[] worldDeck = new int[13];

        // World-level immutables for the determinization currently being searched.
        private int worldTrumpSuit;
        private int worldDeckLength;
        private bool worldClosed;

        // Per-move determinization parameters (set by ConfigureMove, consumed by SampleWorld).
        private bool isLeader;
        private int ledHash;
        private int oppHandCount;
        private int faceDownDeck;
        private int rootPhase;
        private int trumpHash;
        private int rootMyPoints;
        private int rootOppPoints;
        private long myHandMask;
        private int poolCount;

        // Round bookkeeping (kept in lockstep with StartRound / AddCard / EndTurn).
        private bool iWasLeaderThisTrick;
        private int myTricksTakenInRound;
        private int oppTricksTakenInRound;
        private bool iClosedThisRound;

        static ClaudeSearchPlayerBase()
        {
            foreach (var suit in AllSuits)
            {
                foreach (var type in AllTypes)
                {
                    var card = Card.GetCard(suit, type);
                    var hash = card.GetHashCode();
                    ValueByHash[hash] = card.GetValue();
                    SuitByHash[hash] = (int)suit;
                    TypeByHash[hash] = (int)type;
                    SuitMask[(int)suit] |= 1L << hash;
                }
            }

            foreach (var suit in AllSuits)
            {
                foreach (var type in AllTypes)
                {
                    var hash = ((int)suit * 13) + (int)type;
                    var value = ValueByHash[hash];
                    long higher = 0L;
                    foreach (var other in AllTypes)
                    {
                        var otherHash = ((int)suit * 13) + (int)other;
                        if (ValueByHash[otherHash] > value)
                        {
                            higher |= 1L << otherHash;
                        }
                    }

                    HigherInSuitMask[hash] = higher;

                    if (type == CardType.King || type == CardType.Queen)
                    {
                        var partnerType = type == CardType.King ? CardType.Queen : CardType.King;
                        PartnerMask[hash] = 1L << (((int)suit * 13) + (int)partnerType);
                    }
                }
            }
        }

        protected ClaudeSearchPlayerBase()
        {
            this.endgameMoveBuffers = new int[MaxSearchDepth][];
            for (var i = 0; i < MaxSearchDepth; i++)
            {
                this.endgameMoveBuffers[i] = new int[MaxHandSize];
            }
        }

        /// <summary>
        /// Hard wall-clock budget per move, in milliseconds.
        /// </summary>
        public int TimeLimitMilliseconds { get; set; } = 100;

        /// <summary>
        /// UCB exploration constant; rewards are normalized to [0, 1].
        /// </summary>
        public double ExplorationConstant { get; set; } = 1.4;

        /// <summary>
        /// RNG for determinization shuffling. Per-instance (the simulator builds a fresh player per
        /// game and runs each game on one thread), so no contention; inject a seed for tests.
        /// </summary>
        public Random Rng { get; set; } = new Random();

        /// <summary>
        /// Optional sink for policy distillation: (features128, visitDistribution24). Fires once per
        /// searched card decision with the encoded position and the search's normalized root visit
        /// distribution over the dense 24-card policy space — the AlphaZero-style soft target for
        /// cloning this search into the MLP. Only the searched decisions are emitted (forced single
        /// moves and the exact deck-empty endgame solve route elsewhere and are skipped), matching
        /// the decision set <see cref="ClaudePlayerNeural"/> asks its net at inference. The arrays are
        /// freshly allocated per call, so the recorder may retain them. Null by default = zero
        /// production cost.
        /// </summary>
        public Action<float[], float[]> PolicyRecorder { get; set; }

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
            this.myTricksTakenInRound = 0;
            this.oppTricksTakenInRound = 0;
            this.iClosedThisRound = false;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.UnknownCards.Remove(card);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            // The face-up trump transitions from "on table" to "in a hand" as the deck reaches 2.
            if (context.CardsLeftInDeck == 2 && context.TrumpCard != null)
            {
                this.UnknownCards.Add(context.TrumpCard);
            }

            if (context.FirstPlayedCard != null && context.SecondPlayedCard != null)
            {
                var trumpSuit = context.TrumpCard.Suit;
                var first = context.FirstPlayedCard;
                var second = context.SecondPlayedCard;
                var firstWins = first.Suit == second.Suit
                    ? first.GetValue() > second.GetValue()
                    : second.Suit != trumpSuit;
                var iWon = this.iWasLeaderThisTrick == firstWins;
                if (iWon)
                {
                    this.myTricksTakenInRound++;
                }
                else
                {
                    this.oppTricksTakenInRound++;
                }
            }

            this.RecordPlayed(context.FirstPlayedCard);
            this.RecordPlayed(context.SecondPlayedCard);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.iWasLeaderThisTrick = context.IsFirstPlayerTurn;
            this.SyncTrumpCard(context.TrumpCard);

            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                var oldTrumpOnTable = context.TrumpCard;
                this.LastSeenTrumpCard = Card.GetCard(oldTrumpOnTable.Suit, CardType.Nine);
                return this.ChangeTrump(oldTrumpOnTable);
            }

            if (this.ShouldCloseGame(context))
            {
                this.iClosedThisRound = true;
                return this.CloseGame();
            }

            var possibleCards = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            var chosenHash = this.ChooseCard(context, possibleCards);
            return this.PlayCard(Card.Cards[chosenHash]);
        }

        /// <summary>
        /// The search strategy. Returns the chosen card's hash, or -1 to defer to the fallback pick
        /// (e.g. when the determinization invariants don't hold). The determinization helpers
        /// (<see cref="ConfigureMove"/> / <see cref="SampleWorld"/>) and the simulator are available.
        /// </summary>
        protected abstract int RunSearch(PlayerTurnContext context, ICollection<Card> possibleCards);

        protected static bool IsTerminal(SimState state)
        {
            return state.MyPoints >= RoundPointsToWin
                   || state.OppPoints >= RoundPointsToWin
                   || (state.MyHand == 0L && state.OppHand == 0L);
        }

        // Sets the per-move determinization parameters from the turn context. Returns false when the
        // belief state is inconsistent with the expected hand/deck counts (the search then defers).
        protected bool ConfigureMove(PlayerTurnContext context)
        {
            this.isLeader = context.IsFirstPlayerTurn;
            this.ledHash = this.isLeader ? -1 : context.FirstPlayedCard.GetHashCode();
            var myHandCount = this.Cards.Count;
            this.oppHandCount = this.isLeader ? myHandCount : myHandCount - 1;
            this.worldClosed = context.State.ShouldObserveRules;
            this.worldTrumpSuit = (int)context.TrumpCard.Suit;
            this.trumpHash = context.TrumpCard.GetHashCode();
            this.rootPhase = MapPhase(context.State);
            this.rootMyPoints = this.isLeader ? context.FirstPlayerRoundPoints : context.SecondPlayerRoundPoints;
            this.rootOppPoints = this.isLeader ? context.SecondPlayerRoundPoints : context.FirstPlayerRoundPoints;
            this.myHandMask = this.HandMask();
            this.poolCount = this.FillPool(this.ledHash);

            if (this.worldClosed)
            {
                // Closed game: the talon is dead; the rest of the unknown set never comes into play.
                if (this.poolCount < this.oppHandCount)
                {
                    return false;
                }

                this.faceDownDeck = 0;
            }
            else
            {
                this.faceDownDeck = context.CardsLeftInDeck - 1;
                if (this.faceDownDeck < 0 || this.poolCount != this.oppHandCount + this.faceDownDeck)
                {
                    return false;
                }
            }

            return true;
        }

        // Samples one fully-observable world consistent with the current information set: deals the
        // unknown cards to the opponent and the face-down talon (trump card on the bottom), then
        // returns the root simulator state. Call after ConfigureMove.
        protected SimState SampleWorld()
        {
            this.Shuffle(this.poolCount);

            long oppMask = 0L;
            for (var i = 0; i < this.oppHandCount; i++)
            {
                oppMask |= 1L << this.unknownPool[i];
            }

            if (this.worldClosed)
            {
                this.worldDeckLength = 0;
            }
            else
            {
                for (var i = 0; i < this.faceDownDeck; i++)
                {
                    this.worldDeck[i] = this.unknownPool[this.oppHandCount + i];
                }

                this.worldDeck[this.faceDownDeck] = this.trumpHash;
                this.worldDeckLength = this.faceDownDeck + 1;
            }

            return new SimState
            {
                MyHand = this.myHandMask,
                OppHand = oppMask,
                MyPoints = this.rootMyPoints,
                OppPoints = this.rootOppPoints,
                MyTricks = this.myTricksTakenInRound,
                OppTricks = this.oppTricksTakenInRound,
                LedHash = this.ledHash,
                MyTurn = true,
                Phase = this.rootPhase,
                DrawPtr = 0,
                LastTrickByMe = false,
            };
        }

        // The legal moves for the player to move, as a bit mask of card hashes.
        protected long GenMovesMask(SimState state)
        {
            var hand = state.MyTurn ? state.MyHand : state.OppHand;

            // Leading, or following in a phase where rules don't apply (Phase 1): any card is legal.
            if (state.LedHash < 0 || state.Phase != PhaseFinal)
            {
                return hand;
            }

            // Following under rules: overtake the led suit, else follow it low, else trump, else any.
            var higher = hand & HigherInSuitMask[state.LedHash];
            if (higher != 0L)
            {
                return higher;
            }

            var ledSuit = SuitByHash[state.LedHash];
            var sameSuit = hand & SuitMask[ledSuit];
            if (sameSuit != 0L)
            {
                return sameSuit;
            }

            if (ledSuit != this.worldTrumpSuit)
            {
                var trumps = hand & SuitMask[this.worldTrumpSuit];
                if (trumps != 0L)
                {
                    return trumps;
                }
            }

            return hand;
        }

        protected int GenMoves(SimState state, int[] buffer)
        {
            return FillFromMask(this.GenMovesMask(state), buffer);
        }

        protected SimState ApplyMove(SimState state, int move)
        {
            var newState = state;
            var moveMask = 1L << move;

            if (state.MyTurn)
            {
                newState.MyHand &= ~moveMask;
            }
            else
            {
                newState.OppHand &= ~moveMask;
            }

            if (state.LedHash < 0)
            {
                // Leading: auto-announce a marriage (engine forces it whenever legal).
                var announce = 0;
                if (state.Phase != PhaseStart)
                {
                    var handAfter = state.MyTurn ? newState.MyHand : newState.OppHand;
                    if (PartnerInHand(move, handAfter))
                    {
                        announce = SuitByHash[move] == this.worldTrumpSuit ? 40 : 20;
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

                newState.LedHash = move;
                newState.MyTurn = !state.MyTurn;
                return newState;
            }

            // Following: resolve the trick.
            var ledSuit = SuitByHash[state.LedHash];
            var ledValue = ValueByHash[state.LedHash];
            var moveSuit = SuitByHash[move];
            var moveValue = ValueByHash[move];
            var trickValue = ledValue + moveValue;

            bool followerWins = ledSuit == moveSuit
                ? moveValue > ledValue
                : moveSuit == this.worldTrumpSuit;
            var winnerIsMe = state.MyTurn == followerWins;

            if (winnerIsMe)
            {
                newState.MyPoints += trickValue;
                newState.MyTricks++;
            }
            else
            {
                newState.OppPoints += trickValue;
                newState.OppTricks++;
            }

            newState.LedHash = -1;
            newState.MyTurn = winnerIsMe;
            newState.LastTrickByMe = winnerIsMe;

            if (state.Phase != PhaseFinal)
            {
                if (newState.DrawPtr < this.worldDeckLength)
                {
                    var first = this.worldDeck[newState.DrawPtr++];
                    if (winnerIsMe)
                    {
                        newState.MyHand |= 1L << first;
                    }
                    else
                    {
                        newState.OppHand |= 1L << first;
                    }

                    if (newState.DrawPtr < this.worldDeckLength)
                    {
                        var second = this.worldDeck[newState.DrawPtr++];
                        if (winnerIsMe)
                        {
                            newState.OppHand |= 1L << second;
                        }
                        else
                        {
                            newState.MyHand |= 1L << second;
                        }
                    }
                }

                newState.Phase = NextPhase(state.Phase, this.worldDeckLength - newState.DrawPtr);
            }

            return newState;
        }

        // Plays the position out to a terminal with the strong perfect-information rollout policy and
        // returns the reward from our perspective in [0, 1] (1 = win 3 game-points, 0 = lose 3).
        protected double Rollout(SimState state)
        {
            while (true)
            {
                var bothEmpty = state.MyHand == 0L && state.OppHand == 0L;
                if (state.MyPoints >= RoundPointsToWin || state.OppPoints >= RoundPointsToWin || bothEmpty)
                {
                    var gp = this.SignedGamePoints(state, bothEmpty, out _);
                    var reward = 0.5 + (gp / 6d);
                    return reward < 0d ? 0d : (reward > 1d ? 1d : reward);
                }

                var move = this.RolloutPolicy(state);
                state = this.ApplyMove(state, move);
            }
        }

        // Emits one distillation sample for the just-searched decision: the encoded position plus the
        // normalized root visit distribution. moveHashes[i]/visitCounts[i] hold the engine card hash
        // and accumulated visits of root child i; each hash is mapped into the dense 24-card policy
        // index the net is trained on (NeuralFeatureEncoder.CardIndex). No-op unless a recorder is set.
        protected void RecordPolicy(PlayerTurnContext context, int[] moveHashes, int[] visitCounts, int count)
        {
            var recorder = this.PolicyRecorder;
            if (recorder == null)
            {
                return;
            }

            long totalVisits = 0;
            for (var i = 0; i < count; i++)
            {
                totalVisits += visitCounts[i];
            }

            if (totalVisits <= 0)
            {
                return;
            }

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, this.Cards, this.PlayedCards, this.UnknownCards);

            var distribution = new float[NeuralFeatureEncoder.CardCount];
            for (var i = 0; i < count; i++)
            {
                var index = NeuralFeatureEncoder.CardIndex(Card.Cards[moveHashes[i]]);
                distribution[index] += visitCounts[i] / (float)totalVisits;
            }

            recorder(features, distribution);
        }

        private static int FillFromMask(long hand, int[] buffer)
        {
            var n = 0;
            while (hand != 0L)
            {
                buffer[n++] = BitOperations.TrailingZeroCount((ulong)hand);
                hand &= hand - 1;
            }

            return n;
        }

        private static int NextPhase(int phase, int deckRemaining)
        {
            switch (phase)
            {
                case PhaseStart:
                    return PhaseMoreThanTwo;
                case PhaseMoreThanTwo:
                    return deckRemaining == 2 ? PhaseTwoLeft : PhaseMoreThanTwo;
                case PhaseTwoLeft:
                    return PhaseFinal;
                default:
                    return PhaseFinal;
            }
        }

        private static bool PartnerInHand(int cardHash, long hand)
        {
            // PartnerMask is 0 for non-K/Q cards, so this is false for them automatically.
            return (hand & PartnerMask[cardHash]) != 0L;
        }

        // Whether the other hand holds a card that beats this lead, given the follow rules in force.
        private static bool OppCanBeat(int leadHash, long otherHand, bool observeRules, int trump)
        {
            if ((otherHand & HigherInSuitMask[leadHash]) != 0L)
            {
                return true;
            }

            var leadSuit = SuitByHash[leadHash];
            if (leadSuit == trump)
            {
                return false;
            }

            if ((otherHand & SuitMask[trump]) == 0L)
            {
                return false;
            }

            // Phase 2 forces following suit, so the opponent can only trump when void of the led
            // suit; in Phase 1 it may trump anything.
            return !observeRules || (otherHand & SuitMask[leadSuit]) == 0L;
        }

        private static bool PossibleCardsContain(ICollection<Card> possibleCards, int hash)
        {
            foreach (var c in possibleCards)
            {
                if (c.GetHashCode() == hash)
                {
                    return true;
                }
            }

            return false;
        }

        private static int MapPhase(BaseRoundState state)
        {
            if (state.ShouldObserveRules)
            {
                return PhaseFinal;
            }

            if (!state.CanAnnounce20Or40)
            {
                return PhaseStart;
            }

            return state.CanClose ? PhaseMoreThanTwo : PhaseTwoLeft;
        }

        private int ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            // Resolved by the engine validator already, but guard against a single forced move.
            if (possibleCards.Count == 1)
            {
                foreach (var c in possibleCards)
                {
                    return c.GetHashCode();
                }
            }

            // Perfect-information endgame: solve exactly.
            if (context.State.ShouldObserveRules && context.CardsLeftInDeck == 0)
            {
                var exact = this.RunEndgameSolve(context, possibleCards);
                if (exact >= 0)
                {
                    return exact;
                }
            }

            var chosen = this.RunSearch(context, possibleCards);
            return chosen >= 0 ? chosen : this.FallbackPick(context, possibleCards);
        }

        private int RolloutPolicy(SimState state)
        {
            var count = this.GenMoves(state, this.moveScratch);
            if (count == 1)
            {
                return this.moveScratch[0];
            }

            var trump = this.worldTrumpSuit;
            var observeRules = state.Phase == PhaseFinal;
            var moverHand = state.MyTurn ? state.MyHand : state.OppHand;
            var otherHand = state.MyTurn ? state.OppHand : state.MyHand;
            var moverPoints = state.MyTurn ? state.MyPoints : state.OppPoints;
            var otherPoints = state.MyTurn ? state.OppPoints : state.MyPoints;

            if (state.LedHash < 0)
            {
                return this.RolloutLead(count, trump, observeRules, moverHand, otherHand, moverPoints, state.Phase != PhaseStart);
            }

            // Following: classify each legal move as a winner or loser of this trick, keeping the
            // cheapest (by preservation score) of each.
            var ledSuit = SuitByHash[state.LedHash];
            var ledValue = ValueByHash[state.LedHash];
            var bestWinner = -1;
            var bestWinnerScore = int.MaxValue;
            var bestLoser = -1;
            var bestLoserScore = int.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var m = this.moveScratch[i];
                var suit = SuitByHash[m];
                var wins = suit == ledSuit ? ValueByHash[m] > ledValue : suit == trump;
                var sc = MoveScore(m, moverHand, trump);
                if (wins)
                {
                    if (sc < bestWinnerScore)
                    {
                        bestWinner = m;
                        bestWinnerScore = sc;
                    }
                }
                else if (sc < bestLoserScore)
                {
                    bestLoser = m;
                    bestLoserScore = sc;
                }
            }

            if (bestWinner < 0)
            {
                return bestLoser;
            }

            if (bestLoser < 0)
            {
                return bestWinner;
            }

            var winnerValue = ValueByHash[bestWinner];
            var trickValue = ledValue + winnerValue;

            // Take when it wins the round; when ducking would instead let the leader reach 66 (block);
            // when capturing a fat (Ten/Ace) lead; or when the pot beats the invested card by >= 4
            // (a tempo rule from the heuristic player). Otherwise duck with the cheapest card.
            var worthTaking = moverPoints + trickValue >= RoundPointsToWin
                              || otherPoints + ledValue + ValueByHash[bestLoser] >= RoundPointsToWin
                              || ledValue >= 10
                              || trickValue >= winnerValue + 4;
            return worthTaking ? bestWinner : bestLoser;
        }

        private int RolloutLead(int count, int trump, bool observeRules, long moverHand, long otherHand, int moverPoints, bool canAnnounce)
        {
            // 1. A marriage announce (or a guaranteed winner) that reaches 66 ends the round now.
            var bestGuaranteed = -1;
            var bestGuaranteedValue = -1;
            for (var i = 0; i < count; i++)
            {
                var m = this.moveScratch[i];
                var announce = canAnnounce && PartnerInHand(m, moverHand)
                    ? (SuitByHash[m] == trump ? 40 : 20)
                    : 0;
                if (moverPoints + announce >= RoundPointsToWin)
                {
                    return m;
                }

                if (!OppCanBeat(m, otherHand, observeRules, trump))
                {
                    if (moverPoints + announce + ValueByHash[m] >= RoundPointsToWin)
                    {
                        return m;
                    }

                    if (ValueByHash[m] > bestGuaranteedValue)
                    {
                        bestGuaranteed = m;
                        bestGuaranteedValue = ValueByHash[m];
                    }
                }
            }

            // 2. Cash a fat guaranteed winner (Ten / Ace the opponent cannot beat in this world).
            if (bestGuaranteed >= 0 && bestGuaranteedValue >= 10)
            {
                return bestGuaranteed;
            }

            // 3. Bank a marriage by leading its Queen (announces, keeps the higher King in hand).
            if (canAnnounce)
            {
                var bestQueen = -1;
                var bestQueenRank = -1;
                for (var i = 0; i < count; i++)
                {
                    var m = this.moveScratch[i];
                    if (TypeByHash[m] == (int)CardType.Queen && PartnerInHand(m, moverHand))
                    {
                        var rank = SuitByHash[m] == trump ? 1 : 0;
                        if (rank > bestQueenRank)
                        {
                            bestQueen = m;
                            bestQueenRank = rank;
                        }
                    }
                }

                if (bestQueen >= 0)
                {
                    return bestQueen;
                }
            }

            // 4. Lead the lowest-value safe card (preserves trumps and marriages).
            var lead = this.moveScratch[0];
            var leadScore = MoveScore(lead, moverHand, trump);
            for (var i = 1; i < count; i++)
            {
                var sc = MoveScore(this.moveScratch[i], moverHand, trump);
                if (sc < leadScore)
                {
                    lead = this.moveScratch[i];
                    leadScore = sc;
                }
            }

            return lead;
        }

        private static int MoveScore(int cardHash, long moverHand, int trump)
        {
            var score = ValueByHash[cardHash] * 2;
            if (SuitByHash[cardHash] == trump)
            {
                score += 30;
            }

            if (PartnerInHand(cardHash, moverHand))
            {
                score += 18;
            }

            return score;
        }

        private int RunEndgameSolve(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            var leader = context.IsFirstPlayerTurn;
            var led = leader ? -1 : context.FirstPlayedCard.GetHashCode();

            var myHand = this.HandMask();
            long oppHand = 0L;
            foreach (var c in this.UnknownCards)
            {
                oppHand |= 1L << c.GetHashCode();
            }

            if (!leader && led >= 0)
            {
                oppHand &= ~(1L << led);
            }

            if (myHand == 0L || oppHand == 0L)
            {
                return -1;
            }

            this.worldClosed = false;
            this.worldDeckLength = 0;
            this.worldTrumpSuit = (int)context.TrumpCard.Suit;

            var root = new SimState
            {
                MyHand = myHand,
                OppHand = oppHand,
                MyPoints = leader ? context.FirstPlayerRoundPoints : context.SecondPlayerRoundPoints,
                OppPoints = leader ? context.SecondPlayerRoundPoints : context.FirstPlayerRoundPoints,
                MyTricks = this.myTricksTakenInRound,
                OppTricks = this.oppTricksTakenInRound,
                LedHash = led,
                MyTurn = true,
                Phase = PhaseFinal,
                DrawPtr = 0,
                LastTrickByMe = false,
            };

            var moves = this.endgameMoveBuffers[0];
            var count = this.GenMoves(root, moves);
            if (count == 0)
            {
                return -1;
            }

            var best = -1;
            var bestValue = int.MinValue;
            var alpha = int.MinValue;
            for (var i = 0; i < count; i++)
            {
                var move = moves[i];
                var v = this.Solve(this.ApplyMove(root, move), alpha, int.MaxValue, 1);
                if (v > bestValue)
                {
                    bestValue = v;
                    best = move;
                }

                if (bestValue > alpha)
                {
                    alpha = bestValue;
                }
            }

            if (best < 0 || !PossibleCardsContain(possibleCards, best))
            {
                return -1;
            }

            return best;
        }

        private int Solve(SimState state, int alpha, int beta, int depth)
        {
            var bothEmpty = state.MyHand == 0L && state.OppHand == 0L;
            if (state.MyPoints >= RoundPointsToWin || state.OppPoints >= RoundPointsToWin || bothEmpty)
            {
                var gp = this.SignedGamePoints(state, bothEmpty, out var margin);
                return (gp * GamePointReward) + margin;
            }

            if (depth >= MaxSearchDepth)
            {
                return state.MyPoints - state.OppPoints;
            }

            var moves = this.endgameMoveBuffers[depth];
            var count = this.GenMoves(state, moves);
            if (count == 0)
            {
                return state.MyPoints - state.OppPoints;
            }

            if (state.MyTurn)
            {
                var best = int.MinValue;
                for (var i = 0; i < count; i++)
                {
                    var v = this.Solve(this.ApplyMove(state, moves[i]), alpha, beta, depth + 1);
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
                    var v = this.Solve(this.ApplyMove(state, moves[i]), alpha, beta, depth + 1);
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

        private int SignedGamePoints(SimState state, bool bothEmpty, out int margin)
        {
            var myPoints = state.MyPoints;
            var opponentPoints = state.OppPoints;
            if (bothEmpty && !this.worldClosed)
            {
                if (state.LastTrickByMe)
                {
                    myPoints += 10;
                }
                else
                {
                    opponentPoints += 10;
                }
            }

            margin = myPoints - opponentPoints;

            if (this.worldClosed)
            {
                if (this.iClosedThisRound && myPoints < RoundPointsToWin)
                {
                    return -3;
                }

                if (!this.iClosedThisRound && opponentPoints < RoundPointsToWin)
                {
                    return 3;
                }
            }

            if (myPoints == opponentPoints)
            {
                return 0;
            }

            if (myPoints < RoundPointsToWin && opponentPoints < RoundPointsToWin)
            {
                return myPoints > opponentPoints ? 1 : -1;
            }

            if (myPoints > opponentPoints)
            {
                if (opponentPoints >= HalfRoundPoints)
                {
                    return 1;
                }

                return state.OppTricks == 0 ? 3 : 2;
            }

            if (myPoints >= HalfRoundPoints)
            {
                return -1;
            }

            return state.MyTricks == 0 ? -3 : -2;
        }

        private int FallbackPick(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            // Determinization invariants failed (should not happen) — pick a reasonable legal move
            // without any search so the engine still gets a valid action.
            var trump = (int)context.TrumpCard.Suit;
            var myHand = this.HandMask();

            if (context.IsFirstPlayerTurn)
            {
                var best = -1;
                var bestScore = int.MaxValue;
                foreach (var c in possibleCards)
                {
                    var sc = MoveScore(c.GetHashCode(), myHand, trump);
                    if (sc < bestScore)
                    {
                        bestScore = sc;
                        best = c.GetHashCode();
                    }
                }

                return best;
            }

            var ledSuit = (int)context.FirstPlayedCard.Suit;
            var ledValue = context.FirstPlayedCard.GetValue();
            var bestWinner = -1;
            var bestWinnerScore = int.MaxValue;
            var bestLoser = -1;
            var bestLoserScore = int.MaxValue;
            foreach (var c in possibleCards)
            {
                var hash = c.GetHashCode();
                var suit = SuitByHash[hash];
                var wins = suit == ledSuit ? ValueByHash[hash] > ledValue : suit == trump;
                var sc = MoveScore(hash, myHand, trump);
                if (wins)
                {
                    if (sc < bestWinnerScore)
                    {
                        bestWinnerScore = sc;
                        bestWinner = hash;
                    }
                }
                else if (sc < bestLoserScore)
                {
                    bestLoserScore = sc;
                    bestLoser = hash;
                }
            }

            return bestLoser >= 0 ? bestLoser : bestWinner;
        }

        private int FillPool(int excludeHash)
        {
            var n = 0;
            foreach (var card in this.UnknownCards)
            {
                var hash = card.GetHashCode();
                if (hash == excludeHash)
                {
                    continue;
                }

                this.unknownPool[n++] = hash;
            }

            return n;
        }

        private void Shuffle(int count)
        {
            for (var i = count - 1; i > 0; i--)
            {
                var j = this.Rng.Next(i + 1);
                (this.unknownPool[i], this.unknownPool[j]) = (this.unknownPool[j], this.unknownPool[i]);
            }
        }

        private long HandMask()
        {
            long mask = 0L;
            foreach (var c in this.Cards)
            {
                mask |= 1L << c.GetHashCode();
            }

            return mask;
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
            var myPoints = context.FirstPlayerRoundPoints;

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

            var hasTrumpKing = this.Cards.Contains(Card.GetCard(trumpSuit, CardType.King));
            var hasTrumpQueen = this.Cards.Contains(Card.GetCard(trumpSuit, CardType.Queen));
            var hasTrumpMarriage = hasTrumpKing && hasTrumpQueen;

            if (trumpCount >= 4 && hasTrumpMarriage)
            {
                var oppCouldHaveAce = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ace));
                var oppCouldHaveTen = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ten));
                if (!oppCouldHaveAce || !oppCouldHaveTen)
                {
                    return true;
                }
            }

            if (hasTrumpMarriage && myPoints >= 26)
            {
                return true;
            }

            foreach (var s in AllSuits)
            {
                if (s == trumpSuit)
                {
                    continue;
                }

                if (myPoints >= 46
                    && this.Cards.Contains(Card.GetCard(s, CardType.King))
                    && this.Cards.Contains(Card.GetCard(s, CardType.Queen)))
                {
                    return true;
                }
            }

            return false;
        }

        protected struct SimState
        {
            public long MyHand;
            public long OppHand;
            public int MyPoints;
            public int OppPoints;
            public int MyTricks;
            public int OppTricks;
            public int LedHash;
            public bool MyTurn;
            public int Phase;
            public int DrawPtr;
            public bool LastTrickByMe;
        }
    }
}
