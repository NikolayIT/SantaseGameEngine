namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    // TODO: Unit test this class
    public class Round
    {
        private readonly IDeck deck;

        private readonly IStateManager stateManager;

        private readonly RoundPlayerInfo firstPlayer;

        private readonly RoundPlayerInfo secondPlayer;

        private PlayerPosition lastTrickWinner;

        public Round(IPlayer firstPlayer, IPlayer secondPlayer)
        {
            this.deck = new Deck();
            this.stateManager = new StateManagerManager();

            this.firstPlayer = new RoundPlayerInfo(firstPlayer);
            this.secondPlayer = new RoundPlayerInfo(secondPlayer);

            this.lastTrickWinner = PlayerPosition.NoOne;
        }

        public RoundResult Play()
        {
            this.DealFirstCards();

            // TODO: !!! When player announces something he may immediately become round winner !!!
            while (!this.IsFinished())
            {
                this.PlayTrick();
            }

            this.firstPlayer.Player.EndRound();
            this.secondPlayer.Player.EndRound();

            return new RoundResult(this.firstPlayer, this.secondPlayer);
        }

        private void PlayTrick()
        {
            var trick = this.lastTrickWinner == PlayerPosition.SecondPlayer
                            ? new Trick(this.secondPlayer, this.firstPlayer, this.stateManager, this.deck)
                            : new Trick(this.firstPlayer, this.secondPlayer, this.stateManager, this.deck);

            var trickResult = trick.Play();
            this.lastTrickWinner = trickResult.Winner == this.firstPlayer
                                       ? PlayerPosition.FirstPlayer
                                       : PlayerPosition.SecondPlayer;

            if (this.stateManager.State.ShouldDrawCard)
            {
                // TODO: Should the second player take card first when win last trick?
                this.GiveCardToPlayer(this.firstPlayer);
                this.GiveCardToPlayer(this.secondPlayer);
            }

            this.stateManager.State.PlayHand(this.deck.CardsLeft);
        }

        private bool IsFinished()
        {
            if (this.firstPlayer.RoundPoints >= 66)
            {
                return true;
            }

            if (this.secondPlayer.RoundPoints >= 66)
            {
                return true;
            }

            // No cards left => round over
            return this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0;
        }

        private void DealFirstCards()
        {
            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToPlayer(this.firstPlayer);
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToPlayer(this.secondPlayer);
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToPlayer(this.firstPlayer);
            }

            for (var i = 0; i < 3; i++)
            {
                this.GiveCardToPlayer(this.secondPlayer);
            }
        }

        private void GiveCardToPlayer(RoundPlayerInfo player)
        {
            // We are adding the card in two different places to control what an AI player can play
            var card = this.deck.GetNextCard();

            // TODO: Add method "AddCard" in RoundPlayerInfo
            player.Cards.Add(card);
            player.Player.AddCard(card);
        }
    }
}
