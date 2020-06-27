namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using Santase.Logic.RoundStates;

    using Xunit;

    public class FinalRoundStateTests
    {
        [Fact]
        public void WhenInFinalStateItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.True(roundState.CanAnnounce20Or40);
        }

        [Fact]
        public void WhenInFinalStateItIsNotPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.False(roundState.CanClose);
        }

        [Fact]
        public void WhenInFinalStateItIsNotPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.False(roundState.CanChangeTrump);
        }

        [Fact]
        public void WhenInFinalStateRulesShouldBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.True(roundState.ShouldObserveRules);
        }

        [Fact]
        public void WhenInFinalStateNoMoreCardsShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.False(roundState.ShouldDrawCard);
        }

        [Fact]
        public void PlayHandWhenInFinalStateShouldNotChangeGameState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            roundState.PlayHand(0);
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }

        [Fact]
        public void CloseShouldNotChangeGameState()
        {
            // It is not allowed to close the game in this state
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            roundState.Close();
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }
    }
}
