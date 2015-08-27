namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.RoundStates;

    [TestFixture]
    public class StartRoundStateTests
    {
        [Test]
        public void OnFirstHandCannotAnnounce20Or40()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#marriages
            // "The only exception is that during the first hand no marriage can be announced."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanAnnounce20Or40);
        }

        [Test]
        public void GameCannotBeClosedOnFirstHand()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#closing
            // "It is not possible to close the game before any cards are played."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanClose);
        }

        [Test]
        public void OnFirstHandCannotChangeTrump()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#nine-of-trumps
            // "... provided that he has already won at least one trick."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.CanChangeTrump);
        }

        [Test]
        public void OnFirstHandRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.IsFalse(roundState.ShouldObserveRules);
        }

        [Test]
        public void AfterFirstHandCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.IsTrue(roundState.ShouldDrawCard);
        }

        [Test]
        public void PlayHandShouldChangeTheStateToMoreThanTwoCardsLeftRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            roundState.PlayHand(8);
            haveStateMock.Verify(x => x.SetState(It.IsAny<MoreThanTwoCardsLeftRoundState>()), Times.Once);
        }

        [Test]
        public void CloseShouldNotChangeGameState()
        {
            // It is not allowed to close the game in this state
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            roundState.Close();
            haveStateMock.Verify(x => x.SetState(It.IsAny<BaseRoundState>()), Times.Never);
        }
    }
}
