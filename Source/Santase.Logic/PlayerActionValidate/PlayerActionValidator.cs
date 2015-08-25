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
            // TODO: Replace with AnnounceValidator
            if (!context.AmITheFirstPlayer)
            {
                action.Announce = Announce.None;
            }

            if (action.Announce != Announce.None)
            {
                if (action.Card.Type != CardType.Queen && action.Card.Type != CardType.King)
                {
                    action.Announce = Announce.None;
                }

                // TODO: Check for another card
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
