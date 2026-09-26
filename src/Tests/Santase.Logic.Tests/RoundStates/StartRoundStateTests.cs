namespace Santase.Logic.Tests.RoundStates
{
    using Moq;

    using Santase.Logic.RoundStates;

    using Xunit;

    public class StartRoundStateTests
    {
        [Fact]
        public void OnFirstHandCannotAnnounce20Or40()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#marriages
            // "The only exception is that during the first hand no marriage can be announced."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.False(roundState.CanAnnounce20Or40);
        }

        [Fact]
        public void GameCannotBeClosedOnFirstHand()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#closing
            // "It is not possible to close the game before any cards are played."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.False(roundState.CanClose);
        }

        [Fact]
        public void OnFirstHandCannotChangeTrump()
        {
            // https://github.com/NikolayIT/SantaseGameEngine/blob/master/Documentation/Rules.md#nine-of-trumps
            // "... provided that he has already won at least one trick."
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.False(roundState.CanChangeTrump);
        }

        [Fact]
        public void OnFirstHandRulesShouldNotBeObserved()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.False(roundState.ShouldObserveRules);
        }

        [Fact]
        public void AfterFirstHandCardShouldBeDrawn()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            Assert.True(roundState.ShouldDrawCard);
        }

        [Fact]
        public void PlayHandShouldChangeTheStateToMoreThanTwoCardsLeftRoundState()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            roundState.PlayHand(8);
            haveStateMock.Verify(x => x.SetState(It.IsAny<MoreThanTwoCardsLeftRoundState>()), Times.Once);
        }

        // A variant that deals more cards (IGameRules.CardsAtStartOfTheRound 10 or 11) has 2 or no
        // cards left after the first trick.
        [Fact]
        public void PlayHandShouldChangeTheStateToTwoCardsLeftRoundStateWhenTwoCardsAreLeft()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            roundState.PlayHand(2);
            haveStateMock.Verify(x => x.SetState(It.IsAny<TwoCardsLeftRoundState>()), Times.Once);
        }

        [Fact]
        public void PlayHandShouldChangeTheStateToFinalRoundStateWhenNoCardsAreLeft()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);
            roundState.PlayHand(0);
            haveStateMock.Verify(x => x.SetState(It.IsAny<FinalRoundState>()), Times.Once);
        }

        [Fact]
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
