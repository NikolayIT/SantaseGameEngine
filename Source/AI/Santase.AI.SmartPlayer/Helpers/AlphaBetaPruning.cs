namespace Santase.AI.SmartPlayer.Helpers
{
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

    // ReSharper disable once UnusedMember.Global
    public class AlphaBetaPruning
    {
        private readonly IPlayerActionValidator playerActionValidator;

        private readonly ICardWinnerLogic cardWinnerLogic;

        private readonly IRoundWinnerPointsLogic roundWinnerPointsLogic;

        private readonly IGameRules gameRules;

        public AlphaBetaPruning(IPlayerActionValidator playerActionValidator, ICardWinnerLogic cardWinnerLogic, IRoundWinnerPointsLogic roundWinnerPointsLogic, IGameRules gameRules)
        {
            this.playerActionValidator = playerActionValidator;
            this.cardWinnerLogic = cardWinnerLogic;
            this.roundWinnerPointsLogic = roundWinnerPointsLogic;
            this.gameRules = gameRules;
        }

        // ReSharper disable once UnusedMember.Global
        public IDictionary<Card, int> GetBestCard(
            ICollection<Card> firstPlayerCards,
            ICollection<Card> secondPlayerCards,
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
            ICollection<Card> firstPlayerCards,
            ICollection<Card> secondPlayerCards,
            PlayerTurnContext context,
            int firstPlayerPoints,
            int secondPlayerPoints,
            Card firstCard,
            IDictionary<Card, int> options)
        {
            if ((firstPlayerCards.Count == 0 && secondPlayerCards.Count == 0)
                || firstPlayerPoints >= this.gameRules.RoundPointsForGoingOut
                || secondPlayerPoints >= this.gameRules.RoundPointsForGoingOut)
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
