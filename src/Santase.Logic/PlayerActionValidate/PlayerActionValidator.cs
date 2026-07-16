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
            if (action == null)
            {
                return false;
            }

            if (context.State.CanAnnounce20Or40)
            {
                // Melds are compulsory by design: the engine computes the announce itself and
                // overwrites whatever is on the action, so leading a King or Queen while holding
                // its marriage partner always declares the 20/40. Declining a meld (legal in
                // over-the-table play, never beneficial points-wise) is deliberately not modeled.
                action.Announce = this.announceValidator.GetPossibleAnnounce(
                    playerCards,
                    action.Card,
                    context.TrumpCard,
                    context.IsFirstPlayerTurn);
            }
            else
            {
                // States that forbid announcing (the first trick) must also clear any announce
                // already present on the action — Trick credits action.Announce unvalidated.
                action.Announce = Announce.None;
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

            // Iterate the concrete CardCollection (struct enumerator) when possible so the
            // per-turn legal-move scan does not box an IEnumerator on the heap.
            if (playerCards is CardCollection cardCollection)
            {
                var isFirst = context.IsFirstPlayerTurn;
                var firstPlayedCard = context.FirstPlayedCard;
                var trumpCard = context.TrumpCard;
                var shouldObserveRules = context.State.ShouldObserveRules;
                foreach (var card in cardCollection)
                {
                    if (PlayCardActionValidator.CanPlayCard(isFirst, card, firstPlayedCard, trumpCard, cardCollection, shouldObserveRules))
                    {
                        possibleCardsToPlay.Add(card);
                    }
                }

                return possibleCardsToPlay;
            }

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
