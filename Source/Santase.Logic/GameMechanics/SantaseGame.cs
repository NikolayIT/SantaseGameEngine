namespace Santase.Logic.GameMechanics
{
    using Santase.Logic.Logger;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

    public class SantaseGame : ISantaseGame
    {
        private readonly IGameRules gameRules;

        private readonly IPlayer firstPlayer;

        private readonly IPlayer secondPlayer;

        private readonly ILogger logger;

        private PlayerPosition firstToPlay = PlayerPosition.NoOne;

        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer)
            : this(firstPlayer, secondPlayer, GameRulesProvider.Santase, new NoLogger())
        {
        }

        public SantaseGame(IPlayer firstPlayer, IPlayer secondPlayer, IGameRules gameRules, ILogger logger)
        {
            this.RestartGame();
            this.firstPlayer = firstPlayer;
            this.secondPlayer = secondPlayer;
            this.gameRules = gameRules;
            this.logger = logger;
        }

        public int FirstPlayerTotalPoints { get; private set; }

        public int SecondPlayerTotalPoints { get; private set; }

        public int RoundsPlayed { get; private set; }

        public PlayerPosition Start(PlayerPosition firstToPlay = PlayerPosition.FirstPlayer)
        {
            this.firstToPlay = firstToPlay;
            this.RestartGame();

            // Inform players
            this.firstPlayer.StartGame(this.secondPlayer.Name);
            this.secondPlayer.StartGame(this.firstPlayer.Name);

            while (this.GameWinner() == PlayerPosition.NoOne)
            {
                this.PlayRound();
                this.RoundsPlayed++;
            }

            var gameWinner = this.GameWinner();

            this.firstPlayer.EndGame(gameWinner == PlayerPosition.FirstPlayer);
            this.secondPlayer.EndGame(gameWinner == PlayerPosition.SecondPlayer);

            return gameWinner;
        }

        private void RestartGame()
        {
            this.FirstPlayerTotalPoints = 0;
            this.SecondPlayerTotalPoints = 0;
            this.RoundsPlayed = 0;
        }

        private void PlayRound()
        {
            var round = this.firstToPlay == PlayerPosition.FirstPlayer
                            ? new Round(this.firstPlayer, this.secondPlayer, this.gameRules)
                            : new Round(this.secondPlayer, this.firstPlayer, this.gameRules);

            var roundResult = round.Play();

            this.logger.LogLine(
                this.firstToPlay == PlayerPosition.FirstPlayer
                    ? $"{roundResult.FirstPlayer.RoundPoints} - {roundResult.SecondPlayer.RoundPoints}"
                    : $"{roundResult.SecondPlayer.RoundPoints} - {roundResult.FirstPlayer.RoundPoints}");

            this.UpdatePoints(roundResult);
        }

        private void UpdatePoints(RoundResult roundResult)
        {
            IRoundWinnerPointsLogic roundWinnerPointsPointsLogic = new RoundWinnerPointsPointsLogic();
            var roundWinnerPoints = roundWinnerPointsPointsLogic.GetWinnerPoints(
                roundResult.FirstPlayer.RoundPoints,
                roundResult.SecondPlayer.RoundPoints,
                roundResult.GameClosedBy,
                roundResult.NoTricksPlayer,
                this.gameRules);

            if (roundWinnerPoints.Winner == PlayerPosition.NoOne)
            {
                return;
            }

            if (this.firstToPlay == PlayerPosition.FirstPlayer)
            {
                if (roundWinnerPoints.Winner == PlayerPosition.FirstPlayer)
                {
                    this.FirstPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                }
                else
                {
                    this.SecondPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                }
            }
            else
            {
                if (roundWinnerPoints.Winner == PlayerPosition.FirstPlayer)
                {
                    // It is actually our second player
                    this.SecondPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                }
                else
                {
                    this.FirstPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                }
            }
        }

        private PlayerPosition GameWinner()
        {
            if (this.FirstPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin)
            {
                return PlayerPosition.FirstPlayer;
            }

            if (this.SecondPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin)
            {
                return PlayerPosition.SecondPlayer;
            }

            return PlayerPosition.NoOne;
        }
    }
}
