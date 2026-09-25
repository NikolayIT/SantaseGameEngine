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

            if (action.Type == PlayerActionType.PlayCard)
            {
                // Announce is computed for PlayCard actions only: ChangeTrump/CloseGame
                // actions are shared immutable instances (their Announce is always None and
                // is never read by the engine), so they must not be written to here.
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
            // Fast path for the engine's own hand type (every BasePlayer's Cards): iterate the
            // struct enumerator and return the legal cards as a new CardCollection — a single
            // small object instead of a List plus its backing array on every turn, with O(1)
            // Contains. The order is unchanged: the hand enumerates in ascending card hash, so the
            // List this used to return held the legal cards in that same order.
            if (playerCards is CardCollection cardCollection)
            {
                var isFirst = context.IsFirstPlayerTurn;
                var firstPlayedCard = context.FirstPlayedCard;
                var trumpCard = context.TrumpCard;
                var shouldObserveRules = context.State.ShouldObserveRules;
                long legalCards = 0;
                foreach (var card in cardCollection)
                {
                    if (PlayCardActionValidator.CanPlayCard(isFirst, card, firstPlayedCard, trumpCard, cardCollection, shouldObserveRules))
                    {
                        legalCards |= 1L << card.GetHashCode();
                    }
                }

                return new CardCollection(legalCards);
            }

            // Any other collection: a List in the caller's enumeration order.
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
