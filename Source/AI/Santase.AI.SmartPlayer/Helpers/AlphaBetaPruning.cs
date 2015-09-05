namespace Santase.AI.SmartPlayer.Helpers
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public class AlphaBetaPruning
    {
        private readonly IPlayerActionValidator playerActionValidator;

        private readonly ICardWinnerLogic cardWinnerLogic;

        private readonly IRoundWinnerPointsLogic roundWinnerPointsLogic;

        public AlphaBetaPruning(IPlayerActionValidator playerActionValidator, ICardWinnerLogic cardWinnerLogic, IRoundWinnerPointsLogic roundWinnerPointsLogic)
        {
            this.playerActionValidator = playerActionValidator;
            this.cardWinnerLogic = cardWinnerLogic;
            this.roundWinnerPointsLogic = roundWinnerPointsLogic;
        }

        public IDictionary<Card, int> GetBestCard(
            IList<Card> firstPlayerCards,
            IList<Card> secondPlayerCards,
            PlayerTurnContext context,
            int firstPlayerPoints,
            int secondPlayerPoints)
        {
            var options = new Dictionary<Card, int>();
            this.GetBestCardRecursive(
                firstPlayerCards,
                secondPlayerCards,
                context.Clone() as PlayerTurnContext,
                firstPlayerPoints,
                secondPlayerPoints,
                null,
                options);
            return options;
        }


        private void GetBestCardRecursive(
            IList<Card> firstPlayerCards,
            IList<Card> secondPlayerCards,
            PlayerTurnContext context,
            int firstPlayerPoints,
            int secondPlayerPoints,
            Card firstCard,
            IDictionary<Card, int> options)
        {
            if ((firstPlayerCards.Count == 0 && secondPlayerCards.Count == 0)
                || firstPlayerPoints >= 66 || secondPlayerPoints >= 66)
            {
                // if (this.gameWinnerLogic.UpdatePointsAndGetFirstToPlay(new RoundResult(new RoundPlayerInfo(), ), ))
                options[firstCard]++;
                return;
            }

            var playerCards = context.IsFirstPlayerTurn ? firstPlayerCards : secondPlayerCards;
            foreach (var playerCard in playerCards)
            {
                var action = PlayerAction.PlayCard(playerCard);
                if (this.playerActionValidator.IsValid(action, context, playerCards))
                {
                    playerCards.Remove(playerCard);
                    if (context.IsFirstPlayerTurn)
                    {
                        context.FirstPlayedCard = playerCard;
                        context.FirstPlayerAnnounce = action.Announce;
                        firstPlayerPoints += (int)action.Announce;
                    }
                    else
                    {
                        context.SecondPlayedCard = playerCard;
                        var winner = this.cardWinnerLogic.Winner(
                            context.FirstPlayedCard,
                            context.SecondPlayedCard,
                            context.TrumpCard.Suit);
                        if (winner == PlayerPosition.FirstPlayer)
                        {
                            firstPlayerPoints += context.FirstPlayedCard?.GetValue() ?? 0;
                            secondPlayerPoints += context.SecondPlayedCard?.GetValue() ?? 0;
                        }
                        
                        context.FirstPlayerAnnounce = Announce.None;
                        context.FirstPlayedCard = null;
                        context.SecondPlayedCard = null;
                    }

                    if (firstCard == null)
                    {
                        firstCard = playerCard;
                    }

                    this.GetBestCardRecursive(
                        firstPlayerCards,
                        secondPlayerCards,
                        context,
                        firstPlayerPoints,
                        secondPlayerPoints,
                        firstCard,
                        options);

                    playerCards.Add(playerCard);
                }
            }
        }
    }
}
