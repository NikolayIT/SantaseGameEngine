namespace Santase.Logic.Tests
{
    using Xunit;

    public class SantaseGameRulesTests
    {
        [Fact]
        public void SantaseRulesShouldUseTheStandardBulgarianConstants()
        {
            // Anchors for the load-bearing rule constants: 66 to go out, 33 for the
            // Schneider bracket, game played to 11 (the Bulgarian convention — German 66
            // plays to 7), 6 cards dealt.
            var rules = GameRulesProvider.Santase;

            Assert.Equal(66, rules.RoundPointsForGoingOut);
            Assert.Equal(33, rules.HalfRoundPoints);
            Assert.Equal(11, rules.GamePointsNeededForWin);
            Assert.Equal(6, rules.CardsAtStartOfTheRound);
        }

        [Fact]
        public void GameRulesProviderShouldExposeASingletonSantaseRulesInstance()
        {
            Assert.IsType<SantaseGameRules>(GameRulesProvider.Santase);
            Assert.Same(GameRulesProvider.Santase, GameRulesProvider.Santase);
        }
    }
}
