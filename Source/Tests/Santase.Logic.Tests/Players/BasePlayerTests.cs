namespace Santase.Logic.Tests.Players
{
    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    [TestFixture]
    public class BasePlayerTests
    {
        [Test]
        public void CardsShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.IsTrue(basePlayerImplementation.ListIsNotNull);
        }

        [Test]
        public void AnnounceValidatorShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.IsTrue(basePlayerImplementation.AnnounceValidatorIsNotNull);
        }

        [Test]
        public void AddCardShouldNotBeNull()
        {
            const int CardsCount = 5;
            var basePlayerImplementation = new BasePlayerImpl();
            for (var i = 0; i < CardsCount; i++)
            {
                basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Ace));
            }

            Assert.AreEqual(CardsCount, basePlayerImplementation.CardsCount);
        }

        [Test]
        public void EndRoundShouldClearCards()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.EndRound();
            Assert.AreEqual(0, basePlayerImplementation.CardsCount);
        }

        [Test]
        public void PlayerActionValidatorShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.IsTrue(basePlayerImplementation.PlayerActionValidatorIsNotNull);
        }

        private class BasePlayerImpl : BasePlayer
        {
            public bool ListIsNotNull => this.Cards != null;

            public bool AnnounceValidatorIsNotNull => this.AnnounceValidator != null;

            public bool PlayerActionValidatorIsNotNull => this.PlayerActionValidator != null;

            public int CardsCount => this.Cards.Count;

            public override string Name => string.Empty;

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                throw new System.NotImplementedException();
            }

            public override void EndTurn(PlayerTurnContext context)
            {
            }
        }
    }
}
