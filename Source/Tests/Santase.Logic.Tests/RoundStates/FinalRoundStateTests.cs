namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.RoundStates;

    [TestFixture]
    public class FinalRoundStateTests
    {
        [Test]
        public void WhenInFinalStateItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.CanAnnounce20Or40);
        }

        [Test]
        public void WhenInFinalStateItIsNotPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanClose);
        }

        [Test]
        public void WhenInFinalStateItIsNotPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanChangeTrump);
        }

        [Test]
        public void WhenInFinalStateRulesShouldBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.ShouldObserveRules);
        }

        [Test]
        public void WhenInFinalStateNoMoreCardsShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.ShouldDrawCard);
        }

        [Test]
        public void PlayHandWhenInFinalStateShouldNotChangeGameState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);
            roundState.PlayHand(0);
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }

        [Test]
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
