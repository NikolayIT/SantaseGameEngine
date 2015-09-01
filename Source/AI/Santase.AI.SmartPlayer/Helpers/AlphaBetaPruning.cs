namespace Santase.AI.SmartPlayer.Helpers
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class AlphaBetaPruning
    {
        private readonly IPlayerActionValidator playerActionValidator;

        private readonly ICardWinnerLogic cardWinnerLogic;

        public AlphaBetaPruning(IPlayerActionValidator playerActionValidator, ICardWinnerLogic cardWinnerLogic)
        {
            this.playerActionValidator = playerActionValidator;
            this.cardWinnerLogic = cardWinnerLogic;
        }

        public void GetBestCard(
            IList<Card> firstPlayerCards,
            IList<Card> secondPlayerCards,
            PlayerTurnContext context,
            int firstPlayerPoints,
            int secondPlayerPoints)
        {
            var playerCards = context.IsFirstPlayerTurn ? firstPlayerCards : secondPlayerCards;
            foreach (var playerCard in playerCards)
            {
                if (this.playerActionValidator.IsValid(PlayerAction.PlayCard(playerCard), context, playerCards))
                {
                    playerCards.Remove(playerCard);
                    
                    this.GetBestCard(
                        firstPlayerCards,
                        secondPlayerCards,
                        context,
                        firstPlayerPoints,
                        secondPlayerPoints);

                    playerCards.Add(playerCard);
                }
            }

            throw new NotImplementedException();
        }
    }
}
