namespace Santase.Logic.Players
{
    using Santase.Logic.Cards;
    using Santase.Logic.RoundStates;

    public class PlayerTurnContext : IDeepCloneable<PlayerTurnContext>
    {
        public PlayerTurnContext(
            BaseRoundState state,
            Card trumpCard,
            int cardsLeftInDeck,
            int firstPlayerRoundPoints,
            int secondPlayerRoundPoints)
        {
            this.State = state;
            this.TrumpCard = trumpCard;
            this.CardsLeftInDeck = cardsLeftInDeck;
            this.FirstPlayerRoundPoints = firstPlayerRoundPoints;
            this.SecondPlayerRoundPoints = secondPlayerRoundPoints;
        }

        public BaseRoundState State { get; set; }

        public Card TrumpCard { get; set; }

        public int CardsLeftInDeck { get; }

        public Card FirstPlayedCard { get; set; }

        public Announce FirstPlayerAnnounce { get; set; }

        public int FirstPlayerRoundPoints { get; set; }

        public Card SecondPlayedCard { get; set; }

        public int SecondPlayerRoundPoints { get; set; }

        public bool IsFirstPlayerTurn => this.FirstPlayedCard == null;

        public PlayerTurnContext DeepClone()
        {
            // Creating new instance here seems to be faster than calling MemberwiseClone()
            var newPlayerTurnContext = new PlayerTurnContext(
                this.State,
                this.TrumpCard,
                this.CardsLeftInDeck,
                this.FirstPlayerRoundPoints,
                this.SecondPlayerRoundPoints)
            {
                FirstPlayedCard = this.FirstPlayedCard,
                SecondPlayedCard = this.SecondPlayedCard,
                FirstPlayerAnnounce = this.FirstPlayerAnnounce
            };

            return newPlayerTurnContext;
        }
    }
}
