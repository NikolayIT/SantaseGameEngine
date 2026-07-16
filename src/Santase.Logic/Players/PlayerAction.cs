namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;

    public sealed class PlayerAction
    {
        // ChangeTrump and CloseGame actions carry no card and their Announce is never
        // written (PlayerActionValidator only touches Announce on PlayCard actions), so a
        // single shared instance serves every call — per-turn "can I change the trump?"
        // probes stop allocating.
        private static readonly PlayerAction ChangeTrumpAction = new PlayerAction(PlayerActionType.ChangeTrump, null);

        private static readonly PlayerAction CloseGameAction = new PlayerAction(PlayerActionType.CloseGame, null);

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
            return ChangeTrumpAction;
        }

        public static PlayerAction CloseGame()
        {
            return CloseGameAction;
        }

        public override string ToString()
        {
            return $"Action: {this.Type}; Card: {this.Card}; Announce: {this.Announce}";
        }
    }
}
