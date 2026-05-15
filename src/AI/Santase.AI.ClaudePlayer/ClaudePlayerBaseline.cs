namespace Santase.AI.ClaudePlayer
{
    using System.Collections.Generic;
    using System.Numerics;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// FROZEN reference snapshot of <see cref="ClaudePlayer"/>. DO NOT MODIFY this file when
    /// iterating on improvements - it exists so head-to-head simulations and regression tests
    /// can compare a candidate ClaudePlayer against a stable baseline. Update only after a
    /// proven improvement has shipped, so further gains stay measurable.
    /// </summary>
    public class ClaudePlayerBaseline : BasePlayer
    {
        private const int MaxSearchDepth = 14;

        // Eval magnitudes for terminal positions in the minimax.
        private const int RoundWinReward = 1000;
        private const int HandOutReward = 500;

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

        public ClaudePlayerBaseline()
        {
            this.moveBuffers = new Card[MaxSearchDepth][];
            for (var i = 0; i < MaxSearchDepth; i++)
            {
                this.moveBuffers[i] = new Card[6];
            }
        }

        public override string Name => "Claude Player (Baseline)";

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
            // The face-up trump card transitions from "on table" to "in some hand" once the deck reaches 2 cards.
            // Add it back to unknown so post-draw bookkeeping is correct (AddCard will remove it again if I drew it).
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

            var trumpSuit = context.TrumpCard.Suit;
            var trumpCount = 0;
            foreach (var c in this.Cards)
            {
                if (c.Suit == trumpSuit)
                {
                    trumpCount++;
                }
            }

            // Strong condition: 5+ trumps (SmartPlayer's baseline).
            if (trumpCount >= 5)
            {
                return true;
            }

            // Also close when we hold 4 trumps including the trump marriage AND opponent doesn't
            // have both top trumps (A and 10): we control most trump tricks plus +40 announce.
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

            return context.IsFirstPlayerTurn
                ? this.ChooseLeadCard(context, possibleCards)
                : this.ChooseFollowCard(context, possibleCards);
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
