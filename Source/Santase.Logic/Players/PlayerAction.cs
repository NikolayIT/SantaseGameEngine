namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;

    public class PlayerAction
    {
        public PlayerAction(
            PlayerActionType type,
            Card card,
            Announce announce)
        {
            this.Type = type;
            this.Card = card;
            this.Announce = announce;
        }

        public PlayerActionType Type { get; }

        public Card Card { get; }

        public Announce Announce { get; internal set; }
    }
}
