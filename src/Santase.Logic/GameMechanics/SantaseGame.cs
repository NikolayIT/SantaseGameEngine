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

        public PlayerPosition Start(PlayerPosition firstToPlayInFirstRound = PlayerPosition.FirstPlayer)
        {
            this.firstToPlay = firstToPlayInFirstRound;
            this.RestartGame();

            // Inform players
            this.firstPlayer.StartGame(this.secondPlayer.Name);
            this.secondPlayer.StartGame(this.firstPlayer.Name);

            // Play rounds until game winner is determined
            while (this.GameWinner() == PlayerPosition.NoOne)
            {
                this.PlayRound();
                this.RoundsPlayed++;
            }

            var gameWinner = this.GameWinner();

            // Inform players
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
            var round = new Round(this.firstPlayer, this.secondPlayer, this.gameRules, this.firstToPlay);
            var roundResult = round.Play(this.FirstPlayerTotalPoints, this.SecondPlayerTotalPoints);
            this.UpdatePoints(roundResult);

            this.logger.LogLine($"{roundResult.FirstPlayer.RoundPoints} - {roundResult.SecondPlayer.RoundPoints}");
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

            switch (roundWinnerPoints.Winner)
            {
                case PlayerPosition.FirstPlayer:
                    this.FirstPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.SecondPlayer;
                    break;
                case PlayerPosition.SecondPlayer:
                    this.SecondPlayerTotalPoints += roundWinnerPoints.Points;
                    this.firstToPlay = PlayerPosition.FirstPlayer;
                    break;
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
