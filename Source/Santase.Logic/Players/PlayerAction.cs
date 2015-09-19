namespace Santase.Logic.Players
{
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

        internal Announce Announce { get; set; }

        public static PlayerAction PlayCard(Card card)
        {
            return new PlayerAction(PlayerActionType.PlayCard, card);
        }

        public static PlayerAction ChangeTrump()
        {
            return new PlayerAction(PlayerActionType.ChangeTrump, null);
        }

        public static PlayerAction CloseGame()
        {
            return new PlayerAction(PlayerActionType.CloseGame, null);
        }

        public override string ToString()
        {
            return $"Action: {this.Type}; Card: {this.Card}; Announce: {this.Announce}";
        }
    }
}
