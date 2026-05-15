namespace Santase.AI.ClaudePlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// Heuristic Santase player. Stays well under 0.01 seconds per turn.
    /// Used as the fallback policy by the minimax-based <see cref="ClaudePlayer"/> when full
    /// minimax doesn't apply (Phase 1 or closed Phase 2).
    /// Key ideas vs SmartPlayer:
    ///   * Marriage preservation - never breaks a Q+K marriage to dump or routinely overtake;
    ///     leads the Q (not the K) when announcing, keeping the K for future trick-taking.
    ///   * Phase-aware lead order - Phase 2 (rules apply) prefers a guaranteed-winning trump
    ///     before announcing (drains opponent's trumps); Phase 1 keeps announce first.
    ///   * Two-trick lookahead - if a guaranteed trump now plus a marriage announce next reaches 66,
    ///     play the trump first (matches SmartPlayer's "trump-then-announce" pattern).
    ///   * Conservative closing - only closes on 5+ trumps, or 4 trumps + trump marriage when
    ///     opponent doesn't hold both A and 10 of trump.
    ///   * Sound deduction - tracks unknown cards as a CardCollection, conservatively treats
    ///     non-trump leads in closed Phase 2 as not-guaranteed (because the abandoned deck
    ///     pollutes the "opp could have lead suit" signal).
    /// </summary>
    public class ClaudePlayerHeuristic : BasePlayer
    {
        protected static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        protected static readonly CardType[] AllTypes =
        {
            CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace,
        };

        protected CardCollection unknownCards = new CardCollection(CardCollection.AllSantaseCardsBitMask);

        protected CardCollection playedCards = new CardCollection();

        protected Card lastSeenTrumpCard;

        public override string Name => "Claude Player (Heuristic)";

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);

            this.unknownCards = new CardCollection(CardCollection.AllSantaseCardsBitMask);
            foreach (var c in cards)
            {
                this.unknownCards.Remove(c);
            }

            this.playedCards = new CardCollection();
            this.lastSeenTrumpCard = null;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.unknownCards.Remove(card);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            // The face-up trump card transitions from "on table" to "in some hand" once the deck reaches 2 cards.
            // Add it back to unknown so post-draw bookkeeping is correct (AddCard will remove it again if I drew it).
            if (context.CardsLeftInDeck == 2 && context.TrumpCard != null)
            {
                this.unknownCards.Add(context.TrumpCard);
            }

            this.RecordPlayed(context.FirstPlayedCard);
            this.RecordPlayed(context.SecondPlayedCard);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.SyncTrumpCard(context.TrumpCard);

            // Trump change: trade the 9 of trump for the visible (higher) trump card. Almost always positive.
            // The face-up card was already removed from unknownCards by SyncTrumpCard above; we just
            // need to update lastSeenTrumpCard to the 9 we're putting on the table.
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                var oldTrumpOnTable = context.TrumpCard;
                this.lastSeenTrumpCard = Card.GetCard(oldTrumpOnTable.Suit, CardType.Nine);
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

        protected void SyncTrumpCard(Card current)
        {
            if (Card.Equals(current, this.lastSeenTrumpCard))
            {
                return;
            }

            // Either first observation, or opponent changed the trump card. Either way, the new trump card
            // is now visible on the table, so it should not be in our "unknown" set.
            this.unknownCards.Remove(current);
            this.lastSeenTrumpCard = current;
        }

        private void RecordPlayed(Card card)
        {
            if (card == null)
            {
                return;
            }

            this.unknownCards.Remove(card);
            this.playedCards.Add(card);
        }

        protected virtual bool ShouldCloseGame(PlayerTurnContext context)
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
                var oppCouldHaveAce = this.unknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ace));
                var oppCouldHaveTen = this.unknownCards.Contains(Card.GetCard(trumpSuit, CardType.Ten));
                if (!oppCouldHaveAce || !oppCouldHaveTen)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual Card ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            return context.IsFirstPlayerTurn
                ? this.ChooseLeadCard(context, possibleCards)
                : this.ChooseFollowCard(context, possibleCards);
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
            foreach (var c in this.unknownCards)
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
                // Phase 2 with closed game: unknownCards includes the abandoned deck, so we can't
                // be sure whether opponent has the lead suit. Decline the "guaranteed" claim.
                if (context.CardsLeftInDeck > 0)
                {
                    return false;
                }

                // Phase 2 normal: unknownCards == opponent's hand. If they have any of the lead
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
                if (c.GetValue() > lead.GetValue() && this.unknownCards.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }

        private Card SelectSafeLead(PlayerTurnContext context, ICollection<Card> possibleCards, CardSuit trumpSuit)
        {
            var unknownPerSuit = new int[4];
            foreach (var c in this.unknownCards)
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

            // Smallest non-marriage same-suit overtake (preferred for routine takes).
            Card smallestNonMarriageWinner = null;
            var smallestNonMarriageVal = int.MaxValue;

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

                if (!this.IsHalfOfMyMarriage(c) && c.GetValue() < smallestNonMarriageVal)
                {
                    smallestNonMarriageWinner = c;
                    smallestNonMarriageVal = c.GetValue();
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
            // scoring (lower is better). Avoids allocating two intermediate Lists per turn.
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
            foreach (var c in this.unknownCards)
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
    }
}
