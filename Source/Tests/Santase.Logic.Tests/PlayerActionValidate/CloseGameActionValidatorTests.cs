namespace Santase.Logic.Tests.PlayerActionValidate
{
    using Moq;

    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class CloseGameActionValidatorTests
    {
        [Fact]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsNotFirstButTheStatePermitsClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(false, roundState);
            Assert.False(canCloseGame);
        }

        [Fact]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsNotFirstAndTheStateDoesNotPermitClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(false, roundState);
            Assert.False(canCloseGame);
        }

        [Fact]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsFirstButTheStateDoesNotPermitClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(true, roundState);
            Assert.False(canCloseGame);
        }

        [Fact]
        public void CanCloseGameShouldReturnTrueWhenThePlayerIsFirsAndTheStatePermitsClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(true, roundState);
            Assert.True(canCloseGame);
        }
    }
}
