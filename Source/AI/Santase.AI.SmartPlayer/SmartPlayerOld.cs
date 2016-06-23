namespace Santase.AI.SmartPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    // Overall strategy can be based on the game score. When opponent is close to the winning the player should be riskier.
    internal class SmartPlayerOld : BasePlayer
    {
        private readonly CardTracker cardTracker = new CardTracker();

        public override string Name => "Smart Player Old";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is almost always a good move
            // Changing trump can be non-optimal when:
            // 1. Current player is planning to close the game and don't want to give additional points to his opponent
            // 2. The other player will close the game and you will give him additional points by giving him bigger trump card instead of 9
            // 3. Want to confuse the opponent
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                this.cardTracker.ChangeTrumpCard(context.TrumpCard);
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.CloseGame(context))
            {
                GlobalStats.GamesClosedByPlayer++;
                return this.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);

            this.cardTracker.Clear();
            foreach (var card in cards)
            {
                this.cardTracker.UnknownCards.Remove(card);
            }
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.cardTracker.UnknownCards.Remove(card);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            if (context.CardsLeftInDeck == 2)
            {
                this.cardTracker.UnknownCards.Add(context.TrumpCard);
            }

            this.cardTracker.CardPlayed(context.FirstPlayedCard);
            this.cardTracker.CardPlayed(context.SecondPlayedCard);
        }

        // TODO: Improve close game decision
        private bool CloseGame(PlayerTurnContext context)
        {
            if (!this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards))
            {
                return false;
            }

            if (this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5)
            {
                return true;
            }

            return false;
        }

        // TODO: Improve choosing best card to play
        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            this.cardTracker.TrumpCardSaw(context.TrumpCard);

            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);

            return context.State.ShouldObserveRules
                       ? (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesApply(context, possibleCardsToPlay))
                       : (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesDoNotApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesDoNotApply(context, possibleCardsToPlay));
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesDoNotApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            // Announce 40 or 20 if possible
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            // If the player is close to the win => play trump card which will surely win the trick
            var cardToWinTheGame = this.GetTrumpCardWhichWillSurelyWinTheGame(context.TrumpCard.Suit, context.FirstPlayerRoundPoints, possibleCardsToPlay);
            if (cardToWinTheGame != null)
            {
                return this.PlayCard(cardToWinTheGame);
            }

            // Smallest non-trump card from the shortest opponent suit
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => this.cardTracker.UnknownCards.Count(y => y.Suit == x.Suit))
                    .ThenBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            // Should never happen
            cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            // Find card that will surely win the trick
            var opponentHasTrump = this.cardTracker.UnknownCards.Any(x => x.Suit == context.TrumpCard.Suit);

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(
                context.TrumpCard.Suit,
                opponentHasTrump);
            if (trumpCard != null)
            {
                return this.PlayCard(trumpCard);
            }

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(
                    suit,
                    opponentHasTrump);
                if (possibleCard != null)
                {
                    return this.PlayCard(possibleCard);
                }
            }

            // Announce 40 or 20 if possible
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            // Smallest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            // Smallest card
            cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesDoNotApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            // If bigger card is available => play it
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                // Don't have Queen and King
                if (biggerCard.Type != CardType.Queen || !this.Cards.Contains(new Card(biggerCard.Suit, CardType.King)))
                {
                    if (biggerCard.Type != CardType.King || !this.Cards.Contains(new Card(biggerCard.Suit, CardType.Queen)))
                    {
                        return this.PlayCard(biggerCard);
                    }
                }
            }

            // When opponent plays Ace or Ten => play trump card
            if (context.FirstPlayedCard.Type == CardType.Ace || context.FirstPlayedCard.Type == CardType.Ten)
            {
                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Nine))
                    && context.TrumpCard.Type == CardType.Jack)
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Nine));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Jack)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Jack));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Queen))
                    && this.cardTracker.PlayedCards.Contains(new Card(context.TrumpCard.Suit, CardType.King)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Queen));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.King))
                    && this.cardTracker.PlayedCards.Contains(new Card(context.TrumpCard.Suit, CardType.Queen)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.King));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ten)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ten));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Ace)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Ace));
                }
            }

            // Smallest card
            var smallestCard = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(smallestCard);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            // If bigger card is available => play it
            var biggerCard =
                possibleCardsToPlay.Where(
                    x => x.Suit == context.FirstPlayedCard.Suit && x.GetValue() > context.FirstPlayedCard.GetValue())
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (biggerCard != null)
            {
                return this.PlayCard(biggerCard);
            }

            // Play smallest trump card?
            var smallestTrumpCard =
                possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (smallestTrumpCard != null)
            {
                return this.PlayCard(smallestTrumpCard);
            }

            // Smallest card
            var cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private Card GetTrumpCardWhichWillSurelyWinTheGame(
            CardSuit trumpSuit,
            int playerRoundPoints,
            ICollection<Card> possibleCardsToPlay)
        {
            var opponentBiggestTrumpCard =
                this.cardTracker.UnknownCards.Where(x => x.Suit == trumpSuit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            var myBiggestTrumpCards =
                possibleCardsToPlay.Where(x => x.Suit == trumpSuit).OrderByDescending(x => x.GetValue());

            var sumOfPoints = 0;
            foreach (var myTrumpCard in myBiggestTrumpCards)
            {
                sumOfPoints += myTrumpCard.GetValue();
                if (playerRoundPoints >= 66 - sumOfPoints)
                {
                    if (opponentBiggestTrumpCard == null
                        || myTrumpCard.GetValue() > opponentBiggestTrumpCard.GetValue())
                    {
                        return myTrumpCard;
                    }
                }
            }

            return null;
        }

        private Card GetCardWhichWillSurelyWinTheTrick(CardSuit suit, bool opponentHasTrump)
        {
            var myBiggestCard =
                this.Cards.Where(x => x.Suit == suit).OrderByDescending(x => x.GetValue()).FirstOrDefault();
            if (myBiggestCard == null)
            {
                return null;
            }

            var opponentBiggestCard =
                this.cardTracker.UnknownCards.Where(x => x.Suit == suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();

            if (!opponentHasTrump && opponentBiggestCard == null)
            {
                return myBiggestCard;
            }

            if (opponentBiggestCard != null && opponentBiggestCard.GetValue() < myBiggestCard.GetValue())
            {
                return myBiggestCard;
            }

            return null;
        }

        private PlayerAction TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            if (!context.State.CanAnnounce20Or40)
            {
                return null;
            }

            // Choose card with announce 40 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Forty)
                {
                    return this.PlayCard(card);
                }
            }

            // Choose card with announce 20 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Twenty)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }
    }
}
