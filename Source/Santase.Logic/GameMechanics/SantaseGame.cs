namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    public class SantaseGame : ISantaseGame
    {
        private readonly IPlayer firstPlayer;

        private readonly IPlayer secondPlayer;

        private readonly ILogger logger;

        private PlayerPosition firstToPlay;

        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer, PlayerPosition firstToPlay, ILogger logger)
        {
            this.FirstPlayerTotalPoints = 0;
            this.SecondPlayerTotalPoints = 0;
            this.RoundsPlayed = 0;
            this.firstPlayer = firstPlayer;
            this.secondPlayer = secondPlayer;
            this.firstToPlay = firstToPlay;
            this.logger = logger;
        }

        public SantaseGame(
            IPlayer firstPlayer,
            IPlayer secondPlayer,
            PlayerPosition firstToPlay = PlayerPosition.FirstPlayer)
            : this(firstPlayer, secondPlayer, firstToPlay, new NoLogger())
        {
        }

        public int FirstPlayerTotalPoints { get; private set; }

        public int SecondPlayerTotalPoints { get; private set; }

        public int RoundsPlayed { get; private set; }

        public void Start()
        {
            while (!this.IsGameFinished())
            {
                this.PlayRound();
                this.RoundsPlayed++;
            }
        }

        private void PlayRound()
        {
            var round = this.firstToPlay == PlayerPosition.SecondPlayer
                            ? new Round(this.secondPlayer, this.firstPlayer)
                            : new Round(this.firstPlayer, this.secondPlayer);

            var roundResult = round.Play();
            this.logger.LogLine($"{roundResult.FirstPlayer.RoundPoints} - {roundResult.SecondPlayer.RoundPoints}");

            // TODO: Update this.firstToPlay
            // TODO: this.UpdatePoints(roundResult);
        }

        /*
        private void UpdatePoints(RoundResult round)
        {
            if (round.ClosedByPlayer == PlayerPosition.FirstPlayer)
            {
                if (round.FirstPlayerPoints < 66)
                {
                    this.SecondPlayerTotalPoints += 3;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                    return;
                }
            }

            if (round.ClosedByPlayer == PlayerPosition.SecondPlayer)
            {
                if (round.SecondPlayerPoints < 66)
                {
                    this.FirstPlayerTotalPoints += 3;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                    return;
                }
            }

            if (round.FirstPlayerPoints < 66 && round.SecondPlayerPoints < 66)
            {
                var winner = round.LastHandInPlayer;
                if (winner == PlayerPosition.FirstPlayer)
                {
                    this.FirstPlayerTotalPoints += 1;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                    return;
                }
                else
                {
                    this.SecondPlayerTotalPoints += 1;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                    return;
                }
            }

            if (round.FirstPlayerPoints > round.SecondPlayerPoints)
            {
                if (round.SecondPlayerPoints >= 33)
                {
                    this.FirstPlayerTotalPoints += 1;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                }
                else if (round.SecondPlayerHasHand)
                {
                    this.FirstPlayerTotalPoints += 2;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                }
                else
                {
                    this.FirstPlayerTotalPoints += 3;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                }
            }
            else if (round.SecondPlayerPoints > round.FirstPlayerPoints)
            {
                if (round.FirstPlayerPoints >= 33)
                {
                    this.SecondPlayerTotalPoints += 1;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                }
                else if (round.FirstPlayerHasHand)
                {
                    this.SecondPlayerTotalPoints += 2;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                }
                else
                {
                    this.SecondPlayerTotalPoints += 3;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                }
            }
            else
            {
                // Equal points => 0 points to each
            }
        }
        */
        private bool IsGameFinished()
        {
            return this.FirstPlayerTotalPoints >= 11 || this.SecondPlayerTotalPoints >= 11;
        }
    }
}
