namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using Santase.Logic.RoundStates;

    using Xunit;

    public class MoreThanTwoCardsLeftRoundStateTests
    {
        [Fact]
        public void InTheMiddleOfTheGameItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.CanAnnounce20Or40);
        }

        [Fact]
        public void InTheMiddleOfTheGameItIsPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.CanClose);
        }

        [Fact]
        public void InTheMiddleOfTheGameItIsPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.CanChangeTrump);
        }

        [Fact]
        public void InTheMiddleOfTheGameRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.False(roundState.ShouldObserveRules);
        }

        [Fact]
        public void InTheMiddleOfTheGameCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.True(roundState.ShouldDrawCard);
        }

        [Fact]
        public void PlayHandWithMoreThan2CardsLeftShouldNotChangeTheState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(4);
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }

        [Fact]
        public void PlayHandWith2CardsLeftShouldChangeTheStateToTwoCardsLeftRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(2);
            haveStateMock.Verify(x => x.SetState(It.IsAny<TwoCardsLeftRoundState>()), Times.Once);
        }

        [Fact]
        public void CloseShouldMoveTheGameToTheFinalRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.Close();
            haveStateMock.Verify(x => x.SetState(It.IsAny<FinalRoundState>()), Times.Once);
        }
    }
}
