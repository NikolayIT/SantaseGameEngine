namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.RoundStates;

    [TestFixture]
    public class TwoCardsLeftRoundStateTests
    {
        [Test]
        public void WhenTwoCardsAreLeftItIsPossibleToAnnounce20Or40()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.CanAnnounce20Or40);
        }

        [Test]
        public void WhenTwoCardsAreLeftItIsNotPossibleToCloseTheGame()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanClose);
        }

        [Test]
        public void WhenTwoCardsAreLeftItIsNotPossibleToChangeTheTrump()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanChangeTrump);
        }

        [Test]
        public void WhenTwoCardsAreLeftRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.ShouldObserveRules);
        }

        [Test]
        public void WhenTwoCardsAreLeftCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.ShouldDrawCard);
        }

        [Test]
        public void PlayHandWhenTwoCardsAreLeftShouldMoveTheGameToTheFinalRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);
            roundState.PlayHand(0);
            haveStateMock.Verify(x => x.SetState(It.IsAny<FinalRoundState>()), Times.Once);
        }

        [Test]
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
