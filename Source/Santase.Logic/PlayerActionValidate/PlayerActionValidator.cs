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
                context.AmITheFirstPlayer);

            switch (action.Type)
            {
                case PlayerActionType.PlayCard:
                    {
                        var playCardActionValidator = new PlayCardActionValidator();
                        var canPlayCard = playCardActionValidator.CanPlayCard(
                            context.AmITheFirstPlayer,
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
                            context.AmITheFirstPlayer,
                            context.State,
                            context.TrumpCard,
                            playerCards);
                        return canChangeTrump;
                    }

                case PlayerActionType.CloseGame:
                    {
                        var closeGameActionValidator = new CloseGameActionValidator();
                        var canCloseGame = closeGameActionValidator.CanCloseGame(
                            context.AmITheFirstPlayer,
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
