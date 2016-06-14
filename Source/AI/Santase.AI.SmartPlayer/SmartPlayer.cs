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
    public class SmartPlayer : BasePlayer
    {
        private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly OpponentSuitCardsProvider opponentSuitCardsProvider = new OpponentSuitCardsProvider();

        public override string Name => "Smart Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is almost always a good move
            // Changing trump can be non-optimal when:
            // 1. Current player is planning to close the game and don't want to give additional points to his opponent
            // 2. The player will close the game and you will give him additional points by giving him bigger trump card instead of 9
            // 3. Want to confuse the opponent
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.CloseGame(context))
            {
                return this.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void EndRound()
        {
            this.playedCards.Clear();
            base.EndRound();
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
        }

        // TODO: Improve close game decision
        private bool CloseGame(PlayerTurnContext context)
        {
            var shouldCloseGame = this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards)
                                  && this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5;
            if (shouldCloseGame)
            {
                GlobalStats.GamesClosedByPlayer++;
            }

            return shouldCloseGame;
        }

        // TODO: Improve choosing best card to play
        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
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
            var cardToWinTheGame = this.GetTrumpCardWhichWillSurelyWinTheGame(context.TrumpCard, context.FirstPlayerRoundPoints, possibleCardsToPlay);
            if (cardToWinTheGame != null)
            {
                return this.PlayCard(cardToWinTheGame);
            }

            // Smallest non-trump card from the shortest opponent suit
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(
                        x =>
                        this.opponentSuitCardsProvider.GetOpponentCards(
                            this.Cards,
                            this.playedCards,
                            context.TrumpCard,
                            x.Suit).Count)
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
            var opponentHasTrump =
                this.opponentSuitCardsProvider.GetOpponentCards(
                    this.Cards,
                    this.playedCards,
                    context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
                    context.TrumpCard.Suit).Any();

            var trumpCard = this.GetCardWhichWillSurelyWinTheTrick(
                context.TrumpCard.Suit,
                context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
                opponentHasTrump);
            if (trumpCard != null)
            {
                return this.PlayCard(trumpCard);
            }

            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                var possibleCard = this.GetCardWhichWillSurelyWinTheTrick(
                    suit,
                    context.CardsLeftInDeck == 0 ? null : context.TrumpCard,
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

        private Card GetTrumpCardWhichWillSurelyWinTheGame(
            Card trumpCard,
            int playerRoundPoints,
            ICollection<Card> possibleCardsToPlay)
        {
            var opponentBiggestTrumpCard =
                this.opponentSuitCardsProvider.GetOpponentCards(this.Cards, this.playedCards, trumpCard, trumpCard.Suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            var myBiggestTrumpCards =
                possibleCardsToPlay.Where(x => x.Suit == trumpCard.Suit).OrderByDescending(x => x.GetValue());

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

        private Card GetCardWhichWillSurelyWinTheTrick(CardSuit suit, Card trumpCard, bool opponentHasTrump)
        {
            var myBiggestCard =
                this.Cards.Where(x => x.Suit == suit).OrderByDescending(x => x.GetValue()).FirstOrDefault();
            if (myBiggestCard == null)
            {
                return null;
            }

            var opponentBiggestCard =
                this.opponentSuitCardsProvider.GetOpponentCards(this.Cards, this.playedCards, trumpCard, suit)
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
                    if (biggerCard.Type != CardType.King
                        || !this.Cards.Contains(new Card(biggerCard.Suit, CardType.Queen)))
                    {
                        return this.PlayCard(biggerCard);
                    }
                }
            }

            // When opponent plays Ace or Ten => play trump card
            if (context.FirstPlayedCard.Type == CardType.Ace || context.FirstPlayedCard.Type == CardType.Ten)
            {
                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Jack)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Jack));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Nine))
                    && context.TrumpCard.Type == CardType.Jack)
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Nine));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Queen))
                    && this.playedCards.Contains(new Card(context.TrumpCard.Suit, CardType.King)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Queen));
                }

                if (possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.King))
                    && this.playedCards.Contains(new Card(context.TrumpCard.Suit, CardType.Queen)))
                {
                    return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.King));
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
