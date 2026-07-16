namespace Santase.Logic.Tests.GameMechanics
{
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

    using Xunit;

    // Closed-game behavior through the real pipeline (Round -> RoundResult -> scoring),
    // driven by a player that closes at its first legal opportunity.
    public class RoundClosedGameTests
    {
        [Fact]
        public void ClosedRoundShouldStopDrawingAndScoreThroughTheRealPipeline()
        {
            const int NumberOfRounds = 200;

            IRoundWinnerPointsLogic pointsLogic = new RoundWinnerPointsPointsLogic();
            var closedRounds = 0;

            for (var i = 0; i < NumberOfRounds; i++)
            {
                var closer = new ClosingPlayer();
                var opponent = new ValidPlayerWithMethodsCallCounting();
                var round = new Round(closer, opponent, GameRulesProvider.Santase);

                var result = round.Play(0, 0);

                // Only the ClosingPlayer ever returns a CloseGame action.
                Assert.NotEqual(PlayerPosition.SecondPlayer, result.GameClosedBy);
                Assert.Equal(closer.HasClosed, result.GameClosedBy == PlayerPosition.FirstPlayer);

                if (result.GameClosedBy != PlayerPosition.FirstPlayer)
                {
                    continue;
                }

                closedRounds++;

                // No cards may be drawn after closing: the closer's draw count is frozen at
                // the moment of closing, and draws always come in pairs so the opponent's
                // count must match it.
                Assert.Equal(closer.DrawnCardsWhenClosing, closer.DrawnCardsCount);
                Assert.Equal(closer.DrawnCardsCount, opponent.AddCardCalledCount);

                // The +10 last-trick bonus is suspended in closed rounds: whatever
                // LastTrickWinner the real round produced must not influence the scoring.
                var scored = GetWinnerPoints(pointsLogic, result, result.LastTrickWinner);
                var scoredWithoutBonus = GetWinnerPoints(pointsLogic, result, PlayerPosition.NoOne);
                Assert.Equal(scoredWithoutBonus.Winner, scored.Winner);
                Assert.Equal(scoredWithoutBonus.Points, scored.Points);

                // Game-point award through SantaseGame.UpdatePoints: a failed close forfeits
                // a flat 3 to the opponent; a successful close wins 1..3 for the closer.
                var game = new SantaseGame(closer, opponent);
                game.UpdatePoints(result);
                if (result.FirstPlayer.RoundPoints < GameRulesProvider.Santase.RoundPointsForGoingOut)
                {
                    Assert.Equal(0, game.FirstPlayerTotalPoints);
                    Assert.Equal(3, game.SecondPlayerTotalPoints);
                }
                else
                {
                    Assert.InRange(game.FirstPlayerTotalPoints, 1, 3);
                    Assert.Equal(0, game.SecondPlayerTotalPoints);
                }
            }

            Assert.True(closedRounds > 0, "expected at least one round where the closing player got to close");
        }

        private static RoundWinnerPoints GetWinnerPoints(
            IRoundWinnerPointsLogic pointsLogic,
            RoundResult result,
            PlayerPosition lastTrickWinner)
        {
            return pointsLogic.GetWinnerPoints(
                result.FirstPlayer.RoundPoints,
                result.SecondPlayer.RoundPoints,
                result.GameClosedBy,
                result.NoTricksPlayer,
                lastTrickWinner,
                GameRulesProvider.Santase);
        }

        private sealed class ClosingPlayer : BasePlayer
        {
            public override string Name => "Closing player";

            public bool HasClosed { get; private set; }

            public int DrawnCardsCount { get; private set; }

            public int DrawnCardsWhenClosing { get; private set; }

            public override void AddCard(Card card)
            {
                this.DrawnCardsCount++;
                base.AddCard(card);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                if (!this.HasClosed && context.IsFirstPlayerTurn && context.State.CanClose)
                {
                    this.HasClosed = true;
                    this.DrawnCardsWhenClosing = this.DrawnCardsCount;
                    return PlayerAction.CloseGame();
                }

                var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(possibleCardsToPlay.First());
            }
        }
    }
}
