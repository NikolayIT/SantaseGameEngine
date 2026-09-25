namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Santase.Logic.Cards;

    /// <summary>
    /// Exact alpha-beta solver for the perfect-information Phase-2 endgame (deck empty, round not
    /// closed early, so the opponent's hand is exactly the unknown set). Shared by
    /// <see cref="ClaudePlayer"/>, <see cref="ClaudePlayerBaseline"/> and <see cref="ClaudePlayerNeural"/>,
    /// which used to carry three copies of a <see cref="Card"/>-based search.
    ///
    /// Speed comes from three things, none of which changes a decision:
    ///   * the position is a pair of card bitmasks plus integer scores and every card property is a
    ///     table read; move application is branch-light (side-selected by masks, trick winner from a
    ///     precomputed per-trump "beaters" mask);
    ///   * a transposition table. A position's minimax value is a pure function of (both hands,
    ///     both scores, whether each side has taken a trick, the led card, the side to move, the
    ///     trump suit, the evaluation), and all of that is the table key — so entries never go
    ///     stale and one small per-thread table is shared by every solver on the thread, across
    ///     moves, rounds and games. Successive moves of a round re-search subtrees of the previous
    ///     move's search, which is where most of the hits come from;
    ///   * inner nodes use the cheapest (bitmask) move order.
    ///
    /// Root moves are tried in the original searches' enumeration order, and a later move must be
    /// strictly better to replace an earlier one, so that order is the tie-break. Alpha-beta's result
    /// for each root move — exact when it beats the current best, a bound at or below it otherwise —
    /// holds regardless of inner move order and of which (correct) bounds the table supplies, so the
    /// chosen card is identical to the original search's.
    /// </summary>
    internal sealed class EndgameSolver
    {
        private const int RoundPointsToWin = 66;

        private const int NoCard = -1;

        // Eval magnitude per game-point of the round outcome (the GamePoints evaluation).
        private const int GamePointReward = 1000;

        // Terminal magnitudes of the Neural evaluation.
        private const int RoundWinReward = 1000;
        private const int HandOutReward = 500;

        // 4096 entries x 32 bytes = 128 KB per thread, allocated on the first solve.
        private const int TableBits = 12;

        private const byte ExactBound = 0;
        private const byte LowerBound = 1;
        private const byte UpperBound = 2;

        // Type order the original searches enumerated same-suit / trump candidates in. It is also
        // ascending card value, which is what the root order relies on.
        private static readonly CardType[] AscendingTypes =
        {
            CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace,
        };

        // Lookups by card hash (suit * 13 + type).
        private static readonly int[] ValueByHash = new int[53];
        private static readonly int[] SuitByHash = new int[53];

        // Mask of the six cards of each suit.
        private static readonly long[] SuitMask = new long[4];

        // For each card, the same-suit cards that beat it.
        private static readonly long[] HigherInSuitMask = new long[53];

        // For a King/Queen, the single-bit mask of its marriage partner; 0 for every other card.
        private static readonly long[] PartnerMask = new long[53];

        // Per trump suit, indexed [trump * 64 + card]: the replies that win against that lead
        // (higher same-suit cards, plus every trump when the lead is not a trump), and the value of
        // announcing that card's marriage (40 in trumps, 20 otherwise, 0 for non-K/Q cards).
        private static readonly long[] BeatersMask = new long[4 * 64];
        private static readonly int[] AnnounceValue = new int[4 * 64];

        [ThreadStatic]
        private static Entry[] table;

        private readonly Evaluation evaluation;

        private readonly int[] rootMoves = new int[12];

        private int trumpSuit;

        // Everything in a table key besides the hands and the per-node fields (trump, evaluation).
        private int keySalt;

        private Entry[] transpositions;

        static EndgameSolver()
        {
            for (var suit = 0; suit < 4; suit++)
            {
                foreach (var type in AscendingTypes)
                {
                    var card = Card.GetCard((CardSuit)suit, type);
                    var hash = card.GetHashCode();
                    ValueByHash[hash] = card.GetValue();
                    SuitByHash[hash] = suit;
                    SuitMask[suit] |= 1L << hash;
                }
            }

            for (var suit = 0; suit < 4; suit++)
            {
                foreach (var type in AscendingTypes)
                {
                    var hash = (suit * 13) + (int)type;
                    foreach (var other in AscendingTypes)
                    {
                        var otherHash = (suit * 13) + (int)other;
                        if (ValueByHash[otherHash] > ValueByHash[hash])
                        {
                            HigherInSuitMask[hash] |= 1L << otherHash;
                        }
                    }

                    if (type == CardType.King || type == CardType.Queen)
                    {
                        var partner = type == CardType.King ? CardType.Queen : CardType.King;
                        PartnerMask[hash] = 1L << ((suit * 13) + (int)partner);
                    }
                }
            }

            for (var trump = 0; trump < 4; trump++)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    foreach (var type in AscendingTypes)
                    {
                        var hash = (suit * 13) + (int)type;
                        BeatersMask[(trump * 64) + hash] = HigherInSuitMask[hash] | (suit != trump ? SuitMask[trump] : 0L);
                        AnnounceValue[(trump * 64) + hash] = PartnerMask[hash] == 0L ? 0 : (suit == trump ? 40 : 20);
                    }
                }
            }
        }

        public EndgameSolver(Evaluation evaluation)
        {
            this.evaluation = evaluation;
        }

        /// <summary>
        /// How terminal positions are scored — the two evaluations the original searches used.
        /// </summary>
        public enum Evaluation
        {
            /// <summary>
            /// <see cref="ClaudePlayer"/>: game points of the round outcome (schneider/schwarz via
            /// the loser's points and trick count, +10 last-trick bonus) times 1000, plus the
            /// round-point margin as a tie-break.
            /// </summary>
            GamePoints,

            /// <summary>
            /// <see cref="ClaudePlayerNeural"/>: +/-1000 for reaching 66, +/-500 for the higher
            /// score when the cards run out (no last-trick bonus), plus the round-point margin.
            /// </summary>
            Neural,
        }

        /// <summary>
        /// Solves the position with us to move and returns the hash of the best card (the first in
        /// the original enumeration order among equally good ones), or -1 when there is no move.
        /// <paramref name="ledHash"/> is the opponent's lead this trick, or -1 when we lead.
        /// </summary>
        public int FindBestMove(
            long myHand,
            long oppHand,
            int myPoints,
            int oppPoints,
            int ledHash,
            CardSuit trump,
            int myTricks,
            int oppTricks)
        {
            this.trumpSuit = (int)trump;
            this.keySalt = (this.trumpSuit << 27) | ((int)this.evaluation << 29);
            this.transpositions = table ??= new Entry[1 << TableBits];

            var root = new State
            {
                MyHand = myHand,
                OppHand = oppHand,
                MyPoints = myPoints,
                OppPoints = oppPoints,
                Led = ledHash,
                OppToMove = 0,
                MyTricks = myTricks,
                OppTricks = oppTricks,
            };

            var count = this.RootMoves(root);
            var best = NoCard;
            var bestValue = int.MinValue;
            var alpha = int.MinValue;
            for (var i = 0; i < count; i++)
            {
                var move = this.rootMoves[i];
                var child = root;
                this.Apply(ref child, move);
                var v = this.Search(ref child, alpha, int.MaxValue);
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

            return best;
        }

        private static int GamePointsForLoser(int loserPoints, int loserTricks)
        {
            if (loserTricks == 0)
            {
                return 3;
            }

            return loserPoints < 33 ? 2 : 1;
        }

        // The legal moves in the original enumeration order: a lead in card-hash order; a follow
        // as the first non-empty of {higher same-suit, any same-suit, trumps} in ascending value
        // order, else the whole hand in card-hash order.
        private int RootMoves(State state)
        {
            var hand = state.MyHand;
            if (state.Led == NoCard)
            {
                return this.AppendHashOrder(hand);
            }

            var higher = hand & HigherInSuitMask[state.Led];
            if (higher != 0L)
            {
                return this.AppendValueOrder(higher, SuitByHash[state.Led]);
            }

            var ledSuit = SuitByHash[state.Led];
            var sameSuit = hand & SuitMask[ledSuit];
            if (sameSuit != 0L)
            {
                return this.AppendValueOrder(sameSuit, ledSuit);
            }

            if (ledSuit != this.trumpSuit)
            {
                var trumps = hand & SuitMask[this.trumpSuit];
                if (trumps != 0L)
                {
                    return this.AppendValueOrder(trumps, this.trumpSuit);
                }
            }

            return this.AppendHashOrder(hand);
        }

        private int AppendHashOrder(long mask)
        {
            var count = 0;
            while (mask != 0L)
            {
                this.rootMoves[count++] = BitOperations.TrailingZeroCount((ulong)mask);
                mask &= mask - 1;
            }

            return count;
        }

        private int AppendValueOrder(long suitMask, int suit)
        {
            var count = 0;
            foreach (var type in AscendingTypes)
            {
                var hash = (suit * 13) + (int)type;
                if ((suitMask & (1L << hash)) != 0L)
                {
                    this.rootMoves[count++] = hash;
                }
            }

            return count;
        }

        // The legal replies to a lead, as a mask (same rule as the root, unordered). When the hand
        // holds nothing of the led suit, its beaters are exactly its trumps (none on a trump lead).
        private long FollowMask(long hand, int led)
        {
            var higher = hand & HigherInSuitMask[led];
            if (higher != 0L)
            {
                return higher;
            }

            var sameSuit = hand & SuitMask[SuitByHash[led]];
            if (sameSuit != 0L)
            {
                return sameSuit;
            }

            var trumps = hand & BeatersMask[(this.trumpSuit * 64) + led];
            return trumps != 0L ? trumps : hand;
        }

        private int Search(ref State state, int alpha, int beta)
        {
            if (state.MyPoints >= RoundPointsToWin || state.OppPoints >= RoundPointsToWin
                || (state.MyHand | state.OppHand) == 0L)
            {
                return this.Evaluate(ref state);
            }

            var hand = state.OppToMove == 0 ? state.MyHand : state.OppHand;
            var moves = state.Led == NoCard ? hand : this.FollowMask(hand, state.Led);
            if (moves == 0L)
            {
                return state.MyPoints - state.OppPoints;
            }

            // Only "has taken a trick" matters to the evaluation (the schwarz check), so that is
            // all of the trick counts that goes into the key.
            var key = this.keySalt | (state.Led + 1) | (state.OppToMove << 6)
                      | (state.MyTricks > 0 ? 1 << 7 : 0) | (state.OppTricks > 0 ? 1 << 8 : 0)
                      | (state.MyPoints << 9) | (state.OppPoints << 18);
            var mix = ((ulong)state.MyHand * 0x9E3779B97F4A7C15UL)
                      ^ ((ulong)state.OppHand * 0xC2B2AE3D27D4EB4FUL)
                      ^ ((ulong)key * 0x165667B19E3779F9UL);
            ref var entry = ref this.transpositions[(int)(mix >> (64 - TableBits))];
            if (entry.MyHand == state.MyHand && entry.OppHand == state.OppHand && entry.Key == key)
            {
                // The stored value is the true value (exact) or a bound on it; return it whenever
                // it settles this window, exactly as a fresh search would have.
                var stored = entry.Value;
                if (entry.Bound == ExactBound
                    || (entry.Bound == LowerBound && stored >= beta)
                    || (entry.Bound == UpperBound && stored <= alpha))
                {
                    return stored;
                }
            }

            var originalAlpha = alpha;
            var originalBeta = beta;
            int best;
            if (state.OppToMove == 0)
            {
                best = int.MinValue;
                while (moves != 0L)
                {
                    var move = BitOperations.TrailingZeroCount((ulong)moves);
                    moves &= moves - 1;
                    var child = state;
                    this.Apply(ref child, move);
                    var v = this.Search(ref child, alpha, beta);
                    if (v > best)
                    {
                        best = v;
                        if (best > alpha)
                        {
                            alpha = best;
                            if (alpha >= beta)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                best = int.MaxValue;
                while (moves != 0L)
                {
                    var move = BitOperations.TrailingZeroCount((ulong)moves);
                    moves &= moves - 1;
                    var child = state;
                    this.Apply(ref child, move);
                    var v = this.Search(ref child, alpha, beta);
                    if (v < best)
                    {
                        best = v;
                        if (best < beta)
                        {
                            beta = best;
                            if (alpha >= beta)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            // Always-replace: the recursion may have overwritten the slot, and this node's result is
            // the one most likely to be probed next.
            entry.MyHand = state.MyHand;
            entry.OppHand = state.OppHand;
            entry.Key = key;
            entry.Value = best;
            entry.Bound = best <= originalAlpha ? UpperBound : (best >= originalBeta ? LowerBound : ExactBound);
            return best;
        }

        private int Evaluate(ref State state)
        {
            if (this.evaluation == Evaluation.Neural)
            {
                var margin = state.MyPoints - state.OppPoints;
                if (state.MyPoints >= RoundPointsToWin)
                {
                    return RoundWinReward + margin;
                }

                if (state.OppPoints >= RoundPointsToWin)
                {
                    return -RoundWinReward + margin;
                }

                return margin > 0 ? HandOutReward + margin : (margin < 0 ? -HandOutReward + margin : 0);
            }

            // Mid-round 66: the round ends now, game points depend on the loser's state.
            if (state.MyPoints >= RoundPointsToWin)
            {
                return (GamePointsForLoser(state.OppPoints, state.OppTricks) * GamePointReward)
                       + state.MyPoints - state.OppPoints;
            }

            if (state.OppPoints >= RoundPointsToWin)
            {
                return (-GamePointsForLoser(state.MyPoints, state.MyTricks) * GamePointReward)
                       + state.MyPoints - state.OppPoints;
            }

            // Cards ran out: +10 to the last trick's winner (the next leader), applied before the
            // schneider check, matching the engine.
            var myFinal = state.MyPoints + (state.OppToMove == 0 ? 10 : 0);
            var oppFinal = state.OppPoints + (state.OppToMove == 0 ? 0 : 10);
            if (myFinal > oppFinal)
            {
                return (GamePointsForLoser(oppFinal, state.OppTricks) * GamePointReward) + myFinal - oppFinal;
            }

            if (myFinal < oppFinal)
            {
                return (-GamePointsForLoser(myFinal, state.MyTricks) * GamePointReward) + myFinal - oppFinal;
            }

            return 0;
        }

        // Plays one card. Side selection is by mask (0 = me, -1 = opponent) rather than branches,
        // since which side moves / wins is unpredictable inside the search.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Apply(ref State state, int move)
        {
            var bit = 1L << move;
            var mover = state.OppToMove;
            var moverMask = -(long)mover;
            if (state.Led == NoCard)
            {
                // Leading: a K/Q whose partner is still in hand announces 40 (trump) or 20.
                var moverHand = mover == 0 ? state.MyHand : state.OppHand;
                var announce = (moverHand & PartnerMask[move]) != 0L ? AnnounceValue[(this.trumpSuit * 64) + move] : 0;
                state.MyPoints += announce & ~(int)moverMask;
                state.OppPoints += announce & (int)moverMask;
                state.MyHand &= ~(bit & ~moverMask);
                state.OppHand &= ~(bit & moverMask);
                state.Led = move;
                state.OppToMove = mover ^ 1;
                return;
            }

            // Following: the follower wins with a higher card of the led suit or a trump on a
            // non-trump lead (CardWinnerLogic).
            var led = state.Led;
            var winner = (BeatersMask[(this.trumpSuit * 64) + led] & bit) != 0L ? mover : mover ^ 1;
            var winnerMask = -winner;
            var trickValue = ValueByHash[led] + ValueByHash[move];
            state.MyHand &= ~(bit & ~moverMask);
            state.OppHand &= ~(bit & moverMask);
            state.MyPoints += trickValue & ~winnerMask;
            state.OppPoints += trickValue & winnerMask;
            state.MyTricks += 1 & ~winnerMask;
            state.OppTricks += 1 & winnerMask;
            state.Led = NoCard;
            state.OppToMove = winner;
        }

        private struct Entry
        {
            public long MyHand;
            public long OppHand;
            public int Key;
            public int Value;
            public byte Bound;
        }

        private struct State
        {
            public long MyHand;
            public long OppHand;
            public int MyPoints;
            public int OppPoints;
            public int Led;
            public int MyTricks;
            public int OppTricks;

            // 0 = our move, 1 = the opponent's.
            public int OppToMove;
        }
    }
}
