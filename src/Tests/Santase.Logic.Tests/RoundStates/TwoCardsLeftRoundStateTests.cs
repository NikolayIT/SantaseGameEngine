namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using Santase.Logic.RoundStates;

    using Xunit;

    public class TwoCardsLeftRoundStateTests
    {
        [Fact]
        public void WhenTwoCardsAreLeftItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.CanAnnounce20Or40);
        }

        [Fact]
        public void WhenTwoCardsAreLeftItIsNotPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.False(roundState.CanClose);
        }

        [Fact]
        public void WhenTwoCardsAreLeftItIsNotPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.False(roundState.CanChangeTrump);
        }

        [Fact]
        public void WhenTwoCardsAreLeftRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.False(roundState.ShouldObserveRules);
        }

        [Fact]
        public void WhenTwoCardsAreLeftCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.ShouldDrawCard);
        }

        [Fact]
        public void PlayHandWhenTwoCardsAreLeftShouldMoveTheGameToTheFinalRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(0);
            haveStateMock.Verify(x => x.SetState(It.IsAny<FinalRoundState>()), Times.Once);
        }

        [Fact]
        public void CloseShouldNotChangeGameState()
        {
            // It is not allowed to close the game in this state
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            roundState.Close();
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }
    }
}
