namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.RoundStates;

    [TestFixture]
    public class MoreThanTwoCardsLeftRoundStateTests
    {
        [Test]
        public void InTheMiddleOfTheGameItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.CanAnnounce20Or40);
        }

        [Test]
        public void InTheMiddleOfTheGameItIsPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.CanClose);
        }

        [Test]
        public void InTheMiddleOfTheGameItIsPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.CanChangeTrump);
        }

        [Test]
        public void InTheMiddleOfTheGameRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.ShouldObserveRules);
        }

        [Test]
        public void InTheMiddleOfTheGameCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.ShouldDrawCard);
        }

        [Test]
        public void PlayHandWithMoreThan2CardsLeftShouldNotChangeTheState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(4);
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }

        [Test]
        public void PlayHandWith2CardsLeftShouldChangeTheStateToTwoCardsLeftRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(2);
            haveStateMock.Verify(x => x.SetState(It.IsAny<TwoCardsLeftRoundState>()), Times.Once);
        }

        [Test]
        public void CloseShouldMoveTheGameToTheFinalRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);
            roundState.Close();
            haveStateMock.Verify(x => x.SetState(It.IsAny<FinalRoundState>()), Times.Once);
        }
    }
}
