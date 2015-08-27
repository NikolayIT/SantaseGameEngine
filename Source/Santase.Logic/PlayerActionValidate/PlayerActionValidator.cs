namespace Santase.Logic.PlayerActionValidate
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayerActionValidator : IPlayerActionValidator
    {
        public bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards)
        {
            var announceValidator = new AnnounceValidator();
            action.Announce = announceValidator.GetPossibleAnnounce(
                playerCards,
                action.Card,
                context.TrumpCard,
                context.IsFirstPlayerTurn);

            switch (action.Type)
            {
                case PlayerActionType.PlayCard:
                    {
                        var playCardActionValidator = new PlayCardActionValidator();
                        var canPlayCard = playCardActionValidator.CanPlayCard(
                            context.IsFirstPlayerTurn,
                            action.Card,
                            context.FirstPlayedCard,
                            context.TrumpCard,
                            playerCards,
                            context.State.ShouldObserveRules);
                        return canPlayCard;
                    }

                case PlayerActionType.ChangeTrump:
                    {
                        var changeTrumpActionValidator = new ChangeTrumpActionValidator();
                        var canChangeTrump = changeTrumpActionValidator.CanChangeTrump(
                            context.IsFirstPlayerTurn,
                            context.State,
                            context.TrumpCard,
                            playerCards);
                        return canChangeTrump;
                    }

                case PlayerActionType.CloseGame:
                    {
                        var closeGameActionValidator = new CloseGameActionValidator();
                        var canCloseGame = closeGameActionValidator.CanCloseGame(
                            context.IsFirstPlayerTurn,
                            context.State);
                        return canCloseGame;
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
            }
        }
    }
}
