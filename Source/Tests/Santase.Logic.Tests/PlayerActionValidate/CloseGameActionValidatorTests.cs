namespace Santase.Logic.Tests.PlayerActionValidate
{
    using Moq;

    using NUnit.Framework;

    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class CloseGameActionValidatorTests
    {
        [Test]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsNotFirstButTheStatePermitsClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(false, roundState);
            Assert.IsFalse(canCloseGame);
        }

        [Test]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsNotFirstAndTheStateDoesNotPermitClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new FinalRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(false, roundState);
            Assert.IsFalse(canCloseGame);
        }

        [Test]
        public void CanCloseGameShouldReturnFalseWhenThePlayerIsFirstButTheStateDoesNotPermitClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new TwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(true, roundState);
            Assert.IsFalse(canCloseGame);
        }

        [Test]
        public void CanCloseGameShouldReturnTrueWhenThePlayerIsFirsAndTheStatePermitsClosing()
        {
            var haveStateMock = new Mock<IStateManager>();
            var roundState = new MoreThanTwoCardsLeftRoundState(haveStateMock.Object);

            var canCloseGame = CloseGameActionValidator.CanCloseGame(true, roundState);
            Assert.IsTrue(canCloseGame);
        }
    }
}
