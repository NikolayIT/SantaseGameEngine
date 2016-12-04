namespace Santase.AI.SmartPlayer.Helpers
{
    using System;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;

    public class BestCardProviderWhenAllCardsAreKnown
    {
        private readonly IPlayerActionValidator playerActionValidator;

        private readonly IAnnounceValidator announceValidator;

        private readonly ICardWinnerLogic cardWinnerLogic;

        public BestCardProviderWhenAllCardsAreKnown()
        {
            this.playerActionValidator = new PlayerActionValidator();
            this.announceValidator = new AnnounceValidator();
            this.cardWinnerLogic = new CardWinnerLogic();
        }

        /// <summary>
        /// Finds the best card to be played when all cards are known and rules apply
        /// </summary>
        /// <param name="context">The current game state context</param>
        /// <param name="firstPlayerCards">First to play player's cards</param>
        /// <param name="secondPlayerCards">Second to play player's cards</param>
        /// <returns>Best card to be played by the first player</returns>
        public Card FindBestCard(
            PlayerTurnContext context,
            CardCollection firstPlayerCards,
            CardCollection secondPlayerCards)
        {
            if (context.GetType() != typeof(FinalRoundState))
            {
                throw new Exception(
                          $"Invalid state of the game. {nameof(this.FindBestCard)} method can only work in {nameof(FinalRoundState)}.");
            }

            //// TODO: If context.FirstPlayedCard != null?? // maximizingPlayer = false?

            var card = this.FindBestCardRecursively(context.DeepClone(), firstPlayerCards, secondPlayerCards, true);
            return card;
        }

        private Card FindBestCardRecursively(
            PlayerTurnContext context,
            CardCollection firstPlayerCards,
            CardCollection secondPlayerCards,
            bool maximizingPlayer)
        {
            foreach (var firstPlayerCard in firstPlayerCards)
            {
                context.FirstPlayedCard = firstPlayerCard;
                if (this.announceValidator.GetPossibleAnnounce(
                    firstPlayerCards,
                    firstPlayerCard,
                    context.TrumpCard,
                    true) == Announce.Twenty)
                {
                    context.FirstPlayerAnnounce = Announce.Twenty;
                }
                else if (this.announceValidator.GetPossibleAnnounce(
                        firstPlayerCards,
                        firstPlayerCard,
                        context.TrumpCard,
                        true) == Announce.Twenty)
                {
                    context.FirstPlayerAnnounce = Announce.Forty;
                }
                else
                {
                    context.FirstPlayerAnnounce = Announce.None;
                }

                var secondPlayerPossibleCards = this.playerActionValidator.GetPossibleCardsToPlay(context, secondPlayerCards);
                foreach (var secondPlayerCard in secondPlayerPossibleCards)
                {
                    var winnerPosition = this.cardWinnerLogic.Winner(firstPlayerCard, secondPlayerCard, context.TrumpCard.Suit);
                    var pointsFromCards = (int)firstPlayerCard.Type + (int)secondPlayerCard.Type;

                    context.FirstPlayerRoundPoints += (int)context.FirstPlayerAnnounce;
                    if (winnerPosition == PlayerPosition.FirstPlayer)
                    {
                        context.FirstPlayerRoundPoints += pointsFromCards;
                    }
                    else
                    {
                        context.SecondPlayerRoundPoints += pointsFromCards;
                    }

                    firstPlayerCards.Remove(firstPlayerCard);
                    secondPlayerCards.Remove(secondPlayerCard);

                    // TODO: Call recursively
                    var newContext = context.DeepClone();
                    //// TODO: Revert
                    newContext.FirstPlayerRoundPoints = newContext.SecondPlayerRoundPoints;
                    this.FindBestCardRecursively(newContext, firstPlayerCards, secondPlayerCards, !maximizingPlayer);

                    // TODO: Restore
                    firstPlayerCards.Add(firstPlayerCard);
                    secondPlayerCards.Add(secondPlayerCard);
                    context.FirstPlayerRoundPoints += (int)context.FirstPlayerAnnounce;
                    if (winnerPosition == PlayerPosition.FirstPlayer)
                    {
                        context.FirstPlayerRoundPoints -= pointsFromCards;
                    }
                    else
                    {
                        context.SecondPlayerRoundPoints -= pointsFromCards;
                    }
                }
            }

            return null;
        }
    }
}
