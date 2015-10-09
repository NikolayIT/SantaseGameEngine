namespace Santase.Logic.PlayerActionValidate
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class PlayerActionValidator : IPlayerActionValidator
    {
        private static readonly Lazy<PlayerActionValidator> Lazy =
            new Lazy<PlayerActionValidator>(() => new PlayerActionValidator());

        private readonly AnnounceValidator announceValidator = new AnnounceValidator();

        public static PlayerActionValidator Instance => Lazy.Value;

        public bool IsValid(PlayerAction action, PlayerTurnContext context, ICollection<Card> playerCards)
        {
            if (context.State.CanAnnounce20Or40)
            {
                action.Announce = this.announceValidator.GetPossibleAnnounce(
                    playerCards,
                    action.Card,
                    context.TrumpCard,
                    context.IsFirstPlayerTurn);
            }

            if (action == null)
            {
                return false;
            }

            if (action.Type == PlayerActionType.PlayCard)
            {
                var canPlayCard = PlayCardActionValidator.CanPlayCard(
                    context.IsFirstPlayerTurn,
                    action.Card,
                    context.FirstPlayedCard,
                    context.TrumpCard,
                    playerCards,
                    context.State.ShouldObserveRules);
                return canPlayCard;
            }

            if (action.Type == PlayerActionType.ChangeTrump)
            {
                var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(
                    context.IsFirstPlayerTurn,
                    context.State,
                    context.TrumpCard,
                    playerCards);
                return canChangeTrump;
            }

            // action.Type == PlayerActionType.CloseGame
            var canCloseGame = CloseGameActionValidator.CanCloseGame(context.IsFirstPlayerTurn, context.State);
            return canCloseGame;
        }

        public ICollection<Card> GetPossibleCardsToPlay(PlayerTurnContext context, ICollection<Card> playerCards)
        {
            var possibleCardsToPlay = new List<Card>(playerCards.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery (performance)
            foreach (var card in playerCards)
            {
                if (PlayCardActionValidator.CanPlayCard(
                    context.IsFirstPlayerTurn,
                    card,
                    context.FirstPlayedCard,
                    context.TrumpCard,
                    playerCards,
                    context.State.ShouldObserveRules))
                {
                    possibleCardsToPlay.Add(card);
                }
            }

            return possibleCardsToPlay;
        }
    }
}
