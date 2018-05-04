namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class ChangeTrumpActionValidatorTests
    {
        private static readonly Card TrumpThatCanBeChanged = Card.GetCard(CardSuit.Spade, CardType.Queen);

        private static readonly Card TrumpThatCannotBeChanged = Card.GetCard(CardSuit.Diamond, CardType.King);

        private static readonly ICollection<Card> PlayerCards = new List<Card>
                                                              {
                                                                  Card.GetCard(CardSuit.Club, CardType.Nine),
                                                                  Card.GetCard(CardSuit.Spade, CardType.Nine),
                                                                  Card.GetCard(CardSuit.Club, CardType.Ten),
                                                                  Card.GetCard(CardSuit.Spade, CardType.Ten),
                                                                  Card.GetCard(CardSuit.Diamond, CardType.Ace),
                                                                  Card.GetCard(CardSuit.Heart, CardType.Ace),
                                                              };

        [Fact]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsNotFirstButTheStatePermitsChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(false, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.False(canChangeTrump);
        }

        [Fact]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsFirstButTheStateDoesNotPermitChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.False(canChangeTrump);
        }

        [Fact]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsNotFirstAndTheStateDoesNotPermitChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(false, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.False(canChangeTrump);
        }

        [Fact]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsFirstAndTheStatePermitsChangingButNineOfTrumpsIsNotPresent()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCannotBeChanged, PlayerCards);
            Assert.False(canChangeTrump);
        }

        [Fact]
        public void CanChangeTrumpShouldReturnTrueWhenThePlayerIsFirstTheStatePermitsChangingAndNineOfTrumpsIsPresent()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.True(canChangeTrump);
        }
    }
}
