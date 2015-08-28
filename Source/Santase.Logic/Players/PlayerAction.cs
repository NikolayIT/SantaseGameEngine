namespace Santase.Logic.Players
{
    using System;

    using Santase.Logic.Cards;

    public sealed class PlayerAction
    {
        private PlayerAction(PlayerActionType type, Card card)
        {
            this.Type = type;
            this.Card = card;
            this.Announce = Announce.None;
        }

        public PlayerActionType Type { get; }

        public Card Card { get; }

        public Announce Announce { get; internal set; }

        public static PlayerAction PlayCard(Card card)
        {
            return new PlayerAction(PlayerActionType.PlayCard, card);
        }

        public static PlayerAction ChangeTrump()
        {
            // TODO: Consider validation for 9 here?
            return new PlayerAction(PlayerActionType.ChangeTrump, null);
        }

        public static PlayerAction CloseGame()
        {
            return new PlayerAction(PlayerActionType.CloseGame, null);
        }
    }
}
