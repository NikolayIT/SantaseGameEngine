namespace Santase.Logic.PlayerActionValidate
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayerActionValidater : IPlayerActionValidater
    {
        public bool IsValid(PlayerAction action, PlayerTurnContext context, IList<Card> playerCards)
        {
            // TODO: Replace with announces validator
            if (!context.AmITheFirstPlayer)
            {
                action.Announce = Announce.None;
            }

            switch (action.Type)
            {
                case PlayerActionType.PlayCard:
                    var playCardActionValidator = new PlayCardActionValidator();
                    return playCardActionValidator.CanPlayCard(action, context, playerCards);
                case PlayerActionType.ChangeTrump:
                    var changeTrumpActionValidator = new ChangeTrumpActionValidator();
                    return changeTrumpActionValidator.CanChangeTrump(context, playerCards);
                case PlayerActionType.CloseGame:
                    var closeGameActionValidator = new CloseGameActionValidator();
                    return closeGameActionValidator.CanCloseGame(context.AmITheFirstPlayer, context.State);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
