namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    internal class Round
    {
        private readonly IGameRules gameRules;

        private readonly IDeck deck;

        private readonly IStateManager stateManager;

        private readonly RoundPlayerInfo firstPlayer;

        private readonly RoundPlayerInfo secondPlayer;

        private PlayerPosition firstToPlay;

        public Round(IPlayer firstPlayer, IPlayer secondPlayer, IGameRules gameRules)
        {
            this.gameRules = gameRules;
            this.deck = new Deck();
            this.stateManager = new StateManager();

            this.firstPlayer = new RoundPlayerInfo(firstPlayer);
            this.secondPlayer = new RoundPlayerInfo(secondPlayer);

            this.firstToPlay = PlayerPosition.FirstPlayer;
        }

        public RoundResult Play()
        {
            this.DealFirstCards();

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
            var trick = this.firstToPlay == PlayerPosition.FirstPlayer
                ? new Trick(this.firstPlayer, this.secondPlayer, this.stateManager, this.deck, this.gameRules)
                : new Trick(this.secondPlayer, this.firstPlayer, this.stateManager, this.deck, this.gameRules);

            var trickWinner = trick.Play();

            // The one who wins the trick should play first
            this.firstToPlay = trickWinner == this.firstPlayer
                                   ? PlayerPosition.FirstPlayer
                                   : PlayerPosition.SecondPlayer;

            if (this.stateManager.State.ShouldDrawCard)
            {
                // The player who wins last trick takes card first
                if (this.firstToPlay == PlayerPosition.FirstPlayer)
                {
                    this.GiveCardToPlayer(this.firstPlayer);
                    this.GiveCardToPlayer(this.secondPlayer);
                }
                else
                {
                    this.GiveCardToPlayer(this.secondPlayer);
                    this.GiveCardToPlayer(this.firstPlayer);
                }
            }

            this.stateManager.State.PlayHand(this.deck.CardsLeft);
        }

        private bool IsFinished()
        {
            if (this.firstPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                return true;
            }

            if (this.secondPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut)
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
            var card = this.deck.GetNextCard();
            player.AddCard(card);
        }
    }
}
