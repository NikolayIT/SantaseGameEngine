namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Santase.AI.ClaudePlayer.Neural;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// Santase player. Stays well under 0.01 seconds per turn.
    /// Strategy in two layers:
    ///   * Alpha-beta minimax in non-closed Phase 2 - the moment the deck is empty and the round
    ///     wasn't closed early, the opponent's hand is exactly our <c>UnknownCards</c> set, so
    ///     the rest of the round is a small perfect-information subgame (~6 cards each, branching
    ///     factor 1-3 with the validator constraints). We search it to terminal.
    ///   * Heuristic fallback for Phase 1 and closed-Phase-2 turns where the opponent's hand is
    ///     uncertain. The heuristic prefers marriage preservation, leads the Q (not K) when
    ///     announcing, drains opponent trumps before announcing in Phase 2 leads, has a two-trick
    ///     "trump-now-then-announce" lookahead, and only closes on 5+ trumps (or 4 trumps + the
    ///     trump marriage when opponent doesn't hold both A and 10 of trump).
    /// </summary>
    public class ClaudePlayer : BasePlayer
    {
        private const int MaxSearchDepth = 14;

        // Eval magnitude per game-point of the round outcome.
        // The 1/2/3 game-point engine reward (RoundWinnerPointsPointsLogic) is the actual win
        // condition, so the minimax weights its terminal by it: a "schwarz" (opp 0 tricks → 3
        // game-points) is worth 3× a normal 1-game-point win. Round-points margin stays as a
        // tie-breaker below the game-point signal.
        private const int GamePointReward = 1000;

        private static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        private static readonly CardType[] AllTypes =
        {
            CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace,
        };

        // Per-instance scratch buffers indexed by recursion depth, to avoid Card[] allocations
        // on every Search call. Each slot holds at most 6 cards (max hand size).
        private readonly Card[][] moveBuffers;

        private float[] recordingFeatures;

        // Trick-count bookkeeping: updated in EndTurn so the minimax can seed its root state
        // with the true (pre-search) trick counts. The schneider/schwarz game-point scoring
        // depends on the loser's final trick count, so we have to carry this across moves.
        private bool iWasLeaderThisTrick;

        private int myTricksTakenInRound;

        private int oppTricksTakenInRound;

        public ClaudePlayer()
        {
            this.moveBuffers = new Card[MaxSearchDepth][];
            for (var i = 0; i < MaxSearchDepth; i++)
            {
                this.moveBuffers[i] = new Card[6];
            }
        }

        public override string Name => "Claude Player";

        /// <summary>
        /// Optional sink for training data. When set, every heuristic-path move (i.e. when the
        /// alpha-beta minimax did NOT decide the move) is recorded as (features, card_index).
        /// Default is null, in which case this property has zero runtime cost.
        /// The buffer passed to the recorder is reused — copy if you need to retain it.
        /// </summary>
        public Action<float[], int> TrainingRecorder { get; set; }

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
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.UnknownCards.Remove(card);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            // The face-up trump card transitions from "on table" to "in some hand" once the deck reaches 2 cards.
            // Add it back to unknown so post-draw bookkeeping is correct (AddCard will remove it again if I drew it).
            if (context.CardsLeftInDeck == 2 && context.TrumpCard != null)
            {
                this.UnknownCards.Add(context.TrumpCard);
            }

            // Trick winner counting. Both cards present => normal trick resolution; only first
            // present => round ended on a winning announce (no actual trick completed), skip.
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

            // Trump change: trade the 9 of trump for the visible (higher) trump card. Almost always positive.
            // The face-up card was already removed from UnknownCards by SyncTrumpCard above; we just
            // need to update LastSeenTrumpCard to the 9 we're putting on the table.
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
                // Leading: any card in hand is legal.
                return FillFromBitmask(hand, buffer);
            }

            // Following: validator-style restrictions.
            var lead = state.LedCard;
            var leadSuit = lead.Suit;
            var leadVal = lead.GetValue();
            var trumpSuit = state.TrumpSuit;

            var n = 0;

            // 1. Same-suit higher cards (must overtake when possible).
            foreach (var t in AllTypes)
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

            // 2. Same-suit lower cards (when no higher exists).
            foreach (var t in AllTypes)
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

            // 3. Trumps (when void of lead suit).
            if (leadSuit != trumpSuit)
            {
                foreach (var t in AllTypes)
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

            // 4. Anything goes (void of lead suit, no trumps).
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
                // Leading. Compute marriage announce against the PRE-removal hand (the played
                // card itself isn't the partner being checked).
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
                // Following. Resolve trick (winner gets both card values).
                var leader = state.LedCard;
                var trickValue = leader.GetValue() + card.GetValue();

                bool followerWins;
                if (leader.Suit == card.Suit)
                {
                    followerWins = card.GetValue() > leader.GetValue();
                }
                else
                {
                    followerWins = card.Suit == state.TrumpSuit;
                }

                // Current player (about to play) is the follower.
                // followerWins == true => current player wins; false => the other (leader) wins.
                // I win iff "current is me" matches "current wins" (XNOR).
                var amWinningTrick = state.MyTurn == followerWins;

                if (amWinningTrick)
                {
                    newState.MyPoints += trickValue;
                    newState.MyTricksTaken++;
                }
                else
                {
                    newState.OppPoints += trickValue;
                    newState.OppTricksTaken++;
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

            // The trump card on the table differs from what we last observed.
            // First observation: just remove the visible card from the unknown set.
            // Opponent swapped trump: the old trump (was on table = visible) is now in opp's hand,
            // so add it back to unknown; the new trump (the 9 they put down) is now visible, so
            // remove it. Without this, UnknownCards drifts undercount-by-one each opp trump-swap
            // and corrupts every downstream "could opp have X" deduction.
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

            // Close is only legal as the trick leader (CloseGameActionValidator), so my points
            // are the leader's points in this context.
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

            // 1. Trump dominance: 5+ trumps (SmartPlayer's baseline).
            if (trumpCount >= 5)
            {
                return true;
            }

            var hasTrumpKing = this.Cards.Contains(Card.GetCard(trumpSuit, CardType.King));
            var hasTrumpQueen = this.Cards.Contains(Card.GetCard(trumpSuit, CardType.Queen));
            var hasTrumpMarriage = hasTrumpKing && hasTrumpQueen;

            // 2. 4 trumps + trump marriage when opp can't hold both top trumps - we control
            //    trump suit plus the +40 announce.
            if (trumpCount >= 4 && hasTrumpMarriage)
            {
                var oppCouldHaveAce = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ace));
                var oppCouldHaveTen = this.UnknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ten));
                if (!oppCouldHaveAce || !oppCouldHaveTen)
                {
                    return true;
                }
            }

            // 3. Trump marriage + already at 26+ round-points: closing then leading K or Q with
            //    the +40 announce takes me to >= 66 BEFORE opp can respond (engine checks
            //    RoundPoints right after the announce is registered in Trick.Play). Provably
            //    safe - the announce alone delivers the win, independent of which cards opp
            //    holds or how the rest of the closed game plays out.
            if (hasTrumpMarriage && myPoints >= 26)
            {
                return true;
            }

            // 4. Any non-trump K+Q marriage + 46+ round-points: announce 20 takes me to >= 66
            //    instantly. Provably safe, symmetric to condition 3.
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

            // Probabilistic close conditions (trump marriage + Ace + 3 trumps; 20-marriage +
            // heavy trump control; hand-value-sum gate) were tested and regressed against
            // strong heuristic opponents (SmartPlayer / NinjaPlayer): the ~3 game-point cost
            // of a failed close outweighed the wins against weaker opponents.

            return false;
        }

        private Card ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            // Alpha-beta minimax for normal Phase 2 (rules apply, deck fully drawn, not closed
            // mid-Phase-1). Otherwise UnknownCards is wider than the opponent's hand and a
            // perfect-info search would be misled, so we fall through to the heuristic policy.
            if (context.State.ShouldObserveRules && context.CardsLeftInDeck == 0)
            {
                var move = this.RunMinimax(context, possibleCards);
                if (move != null)
                {
                    return move;
                }
            }

            var chosen = context.IsFirstPlayerTurn
                ? this.ChooseLeadCard(context, possibleCards)
                : this.ChooseFollowCard(context, possibleCards);

            if (this.TrainingRecorder != null)
            {
                if (this.recordingFeatures == null)
                {
                    this.recordingFeatures = new float[NeuralFeatureEncoder.FeatureCount];
                }

                NeuralFeatureEncoder.Encode(this.recordingFeatures, context, this.Cards, this.PlayedCards, this.UnknownCards);
                this.TrainingRecorder(this.recordingFeatures, NeuralFeatureEncoder.CardIndex(chosen));
            }

            return chosen;
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
                    // Opponent's lead card is still in UnknownCards (EndTurn hasn't fired for this
                    // trick yet); subtract it so OppHand reflects what they have left to play.
                    oppHand &= ~(1L << ledCard.GetHashCode());
                }
            }

            // Sanity: minimax assumes opp hand size matches what we expect from a non-closed Phase 2.
            // If something is off, decline and let the heuristic handle it.
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
                MyTricksTaken = this.myTricksTakenInRound,
                OppTricksTaken = this.oppTricksTakenInRound,
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

            // Defensive: if minimax somehow picked a move the validator would reject, defer.
            if (best != null && !possibleCards.Contains(best))
            {
                return null;
            }

            return best;
        }

        private int Search(GameState state, int alpha, int beta, int depth)
        {
            // Mid-round 66-reach: round ends now, no +10 last-trick bonus (hands not both empty).
            // Game-points to the winner depend on the loser's state at this instant.
            if (state.MyPoints >= 66)
            {
                return (GamePointsForLoser(state.OppPoints, state.OppTricksTaken) * GamePointReward)
                       + state.MyPoints - state.OppPoints;
            }

            if (state.OppPoints >= 66)
            {
                return (-GamePointsForLoser(state.MyPoints, state.MyTricksTaken) * GamePointReward)
                       + state.MyPoints - state.OppPoints;
            }

            // Both hands empty without reaching 66: +10 last-trick bonus to whoever won the
            // last trick (state.MyTurn after trick resolution = trick winner = next leader).
            // The bonus is applied BEFORE the schneider check, matching the engine.
            if (state.MyHand == 0L && state.OppHand == 0L)
            {
                var myFinal = state.MyPoints + (state.MyTurn ? 10 : 0);
                var oppFinal = state.OppPoints + (state.MyTurn ? 0 : 10);

                if (myFinal > oppFinal)
                {
                    return (GamePointsForLoser(oppFinal, state.OppTricksTaken) * GamePointReward)
                           + myFinal - oppFinal;
                }

                if (myFinal < oppFinal)
                {
                    return (-GamePointsForLoser(myFinal, state.MyTricksTaken) * GamePointReward)
                           + myFinal - oppFinal;
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

        private Card ChooseLeadCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            var trumpSuit = context.TrumpCard.Suit;
            var myPoints = context.FirstPlayerRoundPoints;

            // 1. If a single play (with announce or guaranteed trick) wins the round, do it.
            var instantWin = this.TryFindRoundEndingLead(context, possibleCards, trumpSuit, myPoints);
            if (instantWin != null)
            {
                return instantWin;
            }

            // 1b. Two-trick sequence: lead a guaranteed-winning trump now, then announce a marriage
            //     next trick to reach 66. This is SmartPlayer's "play trump first then announce" trick.
            if (context.State.CanAnnounce20Or40)
            {
                var bestAnnounceBonus = 0;
                foreach (var s in AllSuits)
                {
                    if (this.Cards.Contains(Card.GetCard(s, CardType.King))
                        && this.Cards.Contains(Card.GetCard(s, CardType.Queen)))
                    {
                        var bonus = s == trumpSuit ? 40 : 20;
                        if (bonus > bestAnnounceBonus)
                        {
                            bestAnnounceBonus = bonus;
                        }
                    }
                }

                if (bestAnnounceBonus > 0)
                {
                    Card guaranteedTrump = null;
                    var guaranteedTrumpVal = -1;
                    foreach (var c in possibleCards)
                    {
                        if (c.Suit == trumpSuit && this.IsGuaranteedWinner(c, context, trumpSuit)
                            && c.GetValue() > guaranteedTrumpVal)
                        {
                            guaranteedTrump = c;
                            guaranteedTrumpVal = c.GetValue();
                        }
                    }

                    if (guaranteedTrump != null
                        && myPoints + guaranteedTrumpVal + bestAnnounceBonus >= 66)
                    {
                        return guaranteedTrump;
                    }
                }
            }

            // 2. In Phase 2 (rules apply), lead a guaranteed winner before announcing -
            //    these are use-it-or-lose-it now since the deck won't refresh anyone's hand,
            //    and announces can usually still happen in subsequent tricks while we hold the lead.
            if (context.State.ShouldObserveRules)
            {
                var phase2Guaranteed = this.FindBestGuaranteedWinner(context, possibleCards, trumpSuit);
                if (phase2Guaranteed != null)
                {
                    return phase2Guaranteed;
                }
            }

            // 3. Lead the Q of trump for a +40 announce when we have the trump marriage.
            //    Q (3 points) is sacrificed first; K (4 points, beats Q in same suit) stays in hand.
            if (context.State.CanAnnounce20Or40)
            {
                var trumpKing = Card.GetCard(trumpSuit, CardType.King);
                var trumpQueen = Card.GetCard(trumpSuit, CardType.Queen);
                if (this.Cards.Contains(trumpKing) && this.Cards.Contains(trumpQueen))
                {
                    return trumpQueen;
                }

                // 4. Lead the Q of any non-trump marriage for +20.
                foreach (var s in AllSuits)
                {
                    if (s == trumpSuit)
                    {
                        continue;
                    }

                    var k = Card.GetCard(s, CardType.King);
                    var q = Card.GetCard(s, CardType.Queen);
                    if (this.Cards.Contains(k) && this.Cards.Contains(q))
                    {
                        return q;
                    }
                }
            }

            // 5. In Phase 1, also cash in any guaranteed-winning lead (typically a top trump).
            //    Saving it for later rarely pays off: opponents almost never lead high cards
            //    that would let us catch them with our saved high card.
            var guaranteed = this.FindBestGuaranteedWinner(context, possibleCards, trumpSuit);
            if (guaranteed != null)
            {
                return guaranteed;
            }

            // 6. Otherwise lead a low-value safe card.
            return this.SelectSafeLead(context, possibleCards, trumpSuit);
        }

        private Card TryFindRoundEndingLead(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit, int myPoints)
        {
            foreach (var c in possibleCards)
            {
                var announceBonus = this.AnnounceBonusFor(c, context, trumpSuit);

                // Engine ends the trick the moment my announce pushes me past 66 (round-end check before opponent plays).
                if (myPoints + announceBonus >= 66)
                {
                    return c;
                }

                // Pessimistic: assume opponent contributes 0. If guaranteed to win + my points reach 66, take it.
                if (this.IsGuaranteedWinner(c, context, trumpSuit)
                    && myPoints + announceBonus + c.GetValue() >= 66)
                {
                    return c;
                }
            }

            return null;
        }

        private int AnnounceBonusFor(Card card, PlayerTurnContext context, CardSuit trumpSuit)
        {
            if (!context.State.CanAnnounce20Or40)
            {
                return 0;
            }

            if (card.Type != CardType.King && card.Type != CardType.Queen)
            {
                return 0;
            }

            var partnerType = card.Type == CardType.King ? CardType.Queen : CardType.King;
            if (!this.Cards.Contains(Card.GetCard(card.Suit, partnerType)))
            {
                return 0;
            }

            return card.Suit == trumpSuit ? 40 : 20;
        }

        private Card FindBestGuaranteedWinner(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            // In Phase 2, prefer leading guaranteed-winning trumps first (drains opponent's trumps,
            // which makes later non-trump leads safer too). In Phase 1, just pick the highest-value
            // guaranteed winner regardless of suit.
            if (context.State.ShouldObserveRules)
            {
                Card bestTrump = null;
                var bestTrumpValue = -1;
                foreach (var c in possibleCards)
                {
                    if (c.Suit != trumpSuit)
                    {
                        continue;
                    }

                    if (this.IsGuaranteedWinner(c, context, trumpSuit) && c.GetValue() > bestTrumpValue)
                    {
                        bestTrump = c;
                        bestTrumpValue = c.GetValue();
                    }
                }

                if (bestTrump != null)
                {
                    return bestTrump;
                }
            }

            Card best = null;
            var bestValue = -1;
            foreach (var c in possibleCards)
            {
                if (!this.IsGuaranteedWinner(c, context, trumpSuit))
                {
                    continue;
                }

                if (c.GetValue() > bestValue)
                {
                    best = c;
                    bestValue = c.GetValue();
                }
            }

            return best;
        }

        private bool IsGuaranteedWinner(Card lead, PlayerTurnContext context, CardSuit trumpSuit)
        {
            // Trump leads need no higher trump remaining outside my hand.
            if (lead.Suit == trumpSuit)
            {
                return !this.UnknownHasHigherSameSuit(lead);
            }

            // Non-trump lead: any higher card of the suit beats us.
            if (this.UnknownHasHigherSameSuit(lead))
            {
                return false;
            }

            var oppCouldHaveTrump = false;
            var oppCouldHaveLeadSuit = false;
            foreach (var c in this.UnknownCards)
            {
                if (c.Suit == trumpSuit)
                {
                    oppCouldHaveTrump = true;
                }

                if (c.Suit == lead.Suit)
                {
                    oppCouldHaveLeadSuit = true;
                }
            }

            // No trump unaccounted-for => nobody can trump us.
            if (!oppCouldHaveTrump)
            {
                return true;
            }

            if (context.State.ShouldObserveRules)
            {
                // Phase 2 with closed game: UnknownCards includes the abandoned deck, so we can't
                // be sure whether opponent has the lead suit. Decline the "guaranteed" claim.
                if (context.CardsLeftInDeck > 0)
                {
                    return false;
                }

                // Phase 2 normal: UnknownCards == opponent's hand. If they have any of the lead
                // suit, they must follow and can't trump us.
                return oppCouldHaveLeadSuit;
            }

            // Phase 1: opponent isn't forced to follow; they can trump whenever they like.
            return false;
        }

        private bool UnknownHasHigherSameSuit(Card lead)
        {
            foreach (var t in AllTypes)
            {
                var c = Card.GetCard(lead.Suit, t);
                if (c.GetValue() > lead.GetValue() && this.UnknownCards.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }

        private Card SelectSafeLead(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            var unknownPerSuit = new int[4];
            foreach (var c in this.UnknownCards)
            {
                unknownPerSuit[(int)c.Suit]++;
            }

            // Tier 1: lowest-value non-trump, non-marriage card from the shortest opponent suit.
            // Matches SmartPlayer's heuristic: non-trump first, then by suit shortness, then by value.
            Card bestNonTrump = null;
            var bestNonTrumpSuitCount = int.MaxValue;
            var bestNonTrumpValue = int.MaxValue;
            foreach (var c in possibleCards)
            {
                if (c.Suit == trumpSuit)
                {
                    continue;
                }

                if (context.State.CanAnnounce20Or40 && this.IsHalfOfMyMarriage(c))
                {
                    continue;
                }

                var suitCount = unknownPerSuit[(int)c.Suit];
                var val = c.GetValue();
                if (suitCount < bestNonTrumpSuitCount
                    || (suitCount == bestNonTrumpSuitCount && val < bestNonTrumpValue))
                {
                    bestNonTrump = c;
                    bestNonTrumpSuitCount = suitCount;
                    bestNonTrumpValue = val;
                }
            }

            if (bestNonTrump != null)
            {
                return bestNonTrump;
            }

            // Tier 2: lowest-value non-marriage card (might be a trump).
            Card bestNonMarriage = null;
            var bestNonMarriageValue = int.MaxValue;
            foreach (var c in possibleCards)
            {
                if (context.State.CanAnnounce20Or40 && this.IsHalfOfMyMarriage(c))
                {
                    continue;
                }

                if (c.GetValue() < bestNonMarriageValue)
                {
                    bestNonMarriage = c;
                    bestNonMarriageValue = c.GetValue();
                }
            }

            if (bestNonMarriage != null)
            {
                return bestNonMarriage;
            }

            // Tier 3: forced to break a marriage; pick the smallest card overall.
            Card best = null;
            var bestValue = int.MaxValue;
            foreach (var c in possibleCards)
            {
                if (c.GetValue() < bestValue)
                {
                    best = c;
                    bestValue = c.GetValue();
                }
            }

            return best;
        }

        private bool IsHalfOfMyMarriage(Card c)
        {
            if (c.Type != CardType.King && c.Type != CardType.Queen)
            {
                return false;
            }

            var partner = Card.GetCard(c.Suit, c.Type == CardType.King ? CardType.Queen : CardType.King);
            return this.Cards.Contains(partner);
        }

        private Card ChooseFollowCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            var trumpSuit = context.TrumpCard.Suit;
            return context.State.ShouldObserveRules
                ? this.ChooseFollowPhase2(context, possibleCards, trumpSuit)
                : this.ChooseFollowPhase1(context, possibleCards, trumpSuit);
        }

        private Card ChooseFollowPhase1(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            var ledCard = context.FirstPlayedCard;
            var myPoints = context.SecondPlayerRoundPoints;
            var oppPoints = context.FirstPlayerRoundPoints;

            // Smallest sufficient overtake of the same suit (preserves higher cards for future).
            Card smallestSameSuitWinner = null;
            var sameSuitVal = int.MaxValue;

            // Biggest same-suit overtake (used when game-critical, e.g., to win/block the round).
            Card biggestSameSuitWinner = null;
            var biggestSameSuitVal = -1;

            foreach (var c in possibleCards)
            {
                if (c.Suit != ledCard.Suit || c.GetValue() <= ledCard.GetValue())
                {
                    continue;
                }

                if (c.GetValue() < sameSuitVal)
                {
                    smallestSameSuitWinner = c;
                    sameSuitVal = c.GetValue();
                }

                if (c.GetValue() > biggestSameSuitVal)
                {
                    biggestSameSuitWinner = c;
                    biggestSameSuitVal = c.GetValue();
                }
            }

            // Smallest non-marriage trump (only useful when leading suit is non-trump).
            Card smallestTrump = null;
            var smallestTrumpVal = int.MaxValue;
            Card biggestTrump = null;
            var biggestTrumpVal = -1;
            if (ledCard.Suit != trumpSuit)
            {
                foreach (var c in possibleCards)
                {
                    if (c.Suit != trumpSuit)
                    {
                        continue;
                    }

                    if (c.GetValue() > biggestTrumpVal)
                    {
                        biggestTrump = c;
                        biggestTrumpVal = c.GetValue();
                    }

                    if (this.IsHalfOfMyMarriage(c))
                    {
                        continue;
                    }

                    if (c.GetValue() < smallestTrumpVal)
                    {
                        smallestTrump = c;
                        smallestTrumpVal = c.GetValue();
                    }
                }
            }

            var dump = this.SelectDump(possibleCards, trumpSuit);
            var dumpValue = dump.GetValue();

            // Round-winning take with the cheapest sufficient winner.
            if (smallestSameSuitWinner != null && myPoints + ledCard.GetValue() + sameSuitVal >= 66)
            {
                return smallestSameSuitWinner;
            }

            if (biggestTrump != null && myPoints + ledCard.GetValue() + biggestTrumpVal >= 66)
            {
                return biggestTrump;
            }

            // Block: if dumping would let opponent reach 66, take with whatever wins.
            if (oppPoints + ledCard.GetValue() + dumpValue >= 66)
            {
                if (biggestSameSuitWinner != null)
                {
                    return biggestSameSuitWinner;
                }

                if (biggestTrump != null)
                {
                    return biggestTrump;
                }
            }

            // Defensive trump-up. Opp is one strong trick away from schneider-ing me: their
            // round-points plus the lead value is already at 50, and I am still under 33. If
            // I dump, opp pockets lead + dump and gets even closer to 66 while I stay in the
            // 2-game-point penalty zone. Winning the trick denies them the points and pulls
            // me closer to the 33 boundary - even a small trump pays for itself here, since
            // letting the round end with me under 33 costs an extra game-point. Preserve the
            // announce by skipping marriage halves, and preserve top trumps by using the
            // smallest non-marriage trump available.
            if (ledCard.Suit != trumpSuit
                && oppPoints + ledCard.GetValue() >= 50
                && myPoints < 33)
            {
                Card smallestNonMarriageSameSuitWinner = null;
                var smallestNonMarriageSameSuitVal = int.MaxValue;
                foreach (var c in possibleCards)
                {
                    if (c.Suit == ledCard.Suit
                        && c.GetValue() > ledCard.GetValue()
                        && !this.IsHalfOfMyMarriage(c)
                        && c.GetValue() < smallestNonMarriageSameSuitVal)
                    {
                        smallestNonMarriageSameSuitWinner = c;
                        smallestNonMarriageSameSuitVal = c.GetValue();
                    }
                }

                if (smallestNonMarriageSameSuitWinner != null)
                {
                    return smallestNonMarriageSameSuitWinner;
                }

                if (smallestTrump != null)
                {
                    return smallestTrump;
                }
            }

            // Routine same-suit overtake. Take with the BIGGEST non-marriage higher card -
            // empirically this matches SmartPlayer's behavior and reduces overall losses, since
            // burning high cards on overtakes makes the late-round hand safer to lead from.
            Card biggestNonMarriageWinner = null;
            var biggestNonMarriageVal = -1;
            foreach (var c in possibleCards)
            {
                if (c.Suit == ledCard.Suit && c.GetValue() > ledCard.GetValue()
                    && !this.IsHalfOfMyMarriage(c) && c.GetValue() > biggestNonMarriageVal)
                {
                    biggestNonMarriageWinner = c;
                    biggestNonMarriageVal = c.GetValue();
                }
            }

            if (biggestNonMarriageWinner != null)
            {
                return biggestNonMarriageWinner;
            }

            // Trump high-value non-trump leads (10 or A) with the smallest non-marriage trump.
            // Empirically, trumping smaller leads loses more from the wasted trump than it gains
            // in points - the opponent then leads back and we've spent ammunition for little.
            if (ledCard.Suit != trumpSuit
                && (ledCard.Type == CardType.Ace || ledCard.Type == CardType.Ten)
                && smallestTrump != null)
            {
                return smallestTrump;
            }

            return dump;
        }

        private Card ChooseFollowPhase2(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            var ledCard = context.FirstPlayedCard;
            var myPoints = context.SecondPlayerRoundPoints;
            var oppPoints = context.FirstPlayerRoundPoints;

            // Single pass: pick the cheapest winner and the cheapest loser using value/marriage/trump
            // scoring (lower is better).
            Card bestWinner = null;
            var bestWinnerScore = int.MaxValue;
            Card bestLoser = null;
            var bestLoserScore = int.MaxValue;
            foreach (var c in possibleCards)
            {
                var wins = (c.Suit == ledCard.Suit && c.GetValue() > ledCard.GetValue())
                           || (c.Suit == trumpSuit && ledCard.Suit != trumpSuit);
                var score = this.PreservationScore(c, trumpSuit);
                if (wins)
                {
                    if (score < bestWinnerScore)
                    {
                        bestWinner = c;
                        bestWinnerScore = score;
                    }
                }
                else
                {
                    if (score < bestLoserScore)
                    {
                        bestLoser = c;
                        bestLoserScore = score;
                    }
                }
            }

            // Forced moves first (validator usually constrains us to one category in Phase 2).
            if (bestWinner != null && bestLoser == null)
            {
                return bestWinner;
            }

            if (bestWinner == null && bestLoser != null)
            {
                return bestLoser;
            }

            // Rare: both options legal. Prefer winning when round-decisive or when trick is fat enough.
            var trickValue = ledCard.GetValue() + bestWinner.GetValue();

            if (myPoints + trickValue >= 66)
            {
                return bestWinner;
            }

            if (oppPoints + ledCard.GetValue() + bestLoser.GetValue() >= 66)
            {
                return bestWinner;
            }

            if (trickValue >= bestWinner.GetValue() + 4)
            {
                return bestWinner;
            }

            return bestLoser;
        }

        private int PreservationScore(Card c, CardSuit trumpSuit)
        {
            var score = c.GetValue() * 2;
            if (c.Suit == trumpSuit)
            {
                score += 30;
            }

            if (this.IsHalfOfMyMarriage(c))
            {
                score += 18;
            }

            return score;
        }

        private Card SelectDump(ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            var unknownPerSuit = new int[4];
            foreach (var c in this.UnknownCards)
            {
                unknownPerSuit[(int)c.Suit]++;
            }

            // Lower score is better. Tiebreaker: prefer dumping from a long opponent suit
            // (preserves our presence in short suits, which gives leverage later).
            Card best = null;
            var bestScore = int.MaxValue;
            var bestSuitCount = -1;
            foreach (var c in possibleCards)
            {
                var score = c.GetValue() * 2;
                if (c.Suit == trumpSuit)
                {
                    score += 30;
                }

                if (this.IsHalfOfMyMarriage(c))
                {
                    score += 18;
                }

                var suitCount = unknownPerSuit[(int)c.Suit];
                if (score < bestScore || (score == bestScore && suitCount > bestSuitCount))
                {
                    bestScore = score;
                    bestSuitCount = suitCount;
                    best = c;
                }
            }

            return best;
        }

        // Engine scoring (RoundWinnerPointsPointsLogic): loser with 0 tricks → 3 game-points
        // to the winner (schneider schwarz); loser under 33 round-points → 2; else → 1.
        private static int GamePointsForLoser(int loserPoints, int loserTricks)
        {
            if (loserTricks == 0)
            {
                return 3;
            }

            if (loserPoints < 33)
            {
                return 2;
            }

            return 1;
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
            public int MyTricksTaken;
            public int OppTricksTaken;
        }
    }
}
