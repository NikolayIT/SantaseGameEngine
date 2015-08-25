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
                        return playCardActionValidator.CanPlayCard(action, context, playerCards);
                    }

                case PlayerActionType.ChangeTrump:
                    {
                        var changeTrumpActionValidator = new ChangeTrumpActionValidator();
                        return changeTrumpActionValidator.CanChangeTrump(
                            context.AmITheFirstPlayer,
                            context.State,
                            context.TrumpCard,
                            playerCards);
                    }

                case PlayerActionType.CloseGame:
                    {
                        var closeGameActionValidator = new CloseGameActionValidator();
                        return closeGameActionValidator.CanCloseGame(context.AmITheFirstPlayer, context.State);
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
            }
        }
    }
}
