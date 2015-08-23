namespace Santase.Logic.Players
{
    using System;

    using Santase.Logic.Cards;

    public sealed class PlayerAction
    {
        private PlayerAction(PlayerActionType type, Card card, Announce announce)
        {
            this.Type = type;
            this.Card = card;
            this.Announce = announce;
        }

        public PlayerActionType Type { get; }

        public Card Card { get; }

        public Announce Announce { get; internal set; }

        public static PlayerAction PlayCard(Card card, Announce announce)
        {
            // TODO: Remove announces validation from other places
            if (announce != Announce.None && card.Type != CardType.Queen && card.Type != CardType.King)
            {
                throw new ArgumentException(
                    "When announcing twenty or fourty the card should be Queen or King.",
                    nameof(card));
            }

            return new PlayerAction(PlayerActionType.PlayCard, card, announce);
        }

        public static PlayerAction ChangeTrump()
        {
            // TODO: Consider validation for 9 here?
            return new PlayerAction(PlayerActionType.ChangeTrump, null, Announce.None);
        }

        public static PlayerAction CloseGame()
        {
            return new PlayerAction(PlayerActionType.CloseGame, null, Announce.None);
        }
    }
}
