namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;

    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class ChangeTrumpActionValidatorTests
    {
        private static readonly Card TrumpThatCanBeChanged = new Card(CardSuit.Spade, CardType.Queen);

        private static readonly Card TrumpThatCannotBeChanged = new Card(CardSuit.Diamond, CardType.King);

        private static readonly ICollection<Card> PlayerCards = new List<Card>
                                                              {
                                                                  new Card(CardSuit.Club, CardType.Nine),
                                                                  new Card(CardSuit.Spade, CardType.Nine),
                                                                  new Card(CardSuit.Club, CardType.Ten),
                                                                  new Card(CardSuit.Spade, CardType.Ten),
                                                                  new Card(CardSuit.Diamond, CardType.Ace),
                                                                  new Card(CardSuit.Heart, CardType.Ace)
                                                              };

        [Test]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsNotFirstButTheStatePermitsChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(false, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.IsFalse(canChangeTrump);
        }

        [Test]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsFirstButTheStateDoesNotPermitChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.IsFalse(canChangeTrump);
        }

        [Test]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsNotFirstAndTheStateDoesNotPermitChanging()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new StartRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(false, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.IsFalse(canChangeTrump);
        }

        [Test]
        public void CanChangeTrumpShouldReturnFalseWhenThePlayerIsFirstAndTheStatePermitsChangingButNineOfTrumpsIsNotPresent()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCannotBeChanged, PlayerCards);
            Assert.IsFalse(canChangeTrump);
        }

        [Test]
        public void CanChangeTrumpShouldReturnTrueWhenThePlayerIsFirstTheStatePermitsChangingAndNineOfTrumpsIsPresent()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canChangeTrump = ChangeTrumpActionValidator.CanChangeTrump(true, roundState, TrumpThatCanBeChanged, PlayerCards);
            Assert.IsTrue(canChangeTrump);
        }
    }
}
