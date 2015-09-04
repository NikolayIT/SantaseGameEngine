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

        [Test]
        public void PlayCardShouldReturnPlayerActionWithTypePlayCard()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.PlayCardProxy(new Card(CardSuit.Heart, CardType.Ace));
            Assert.AreEqual(PlayerActionType.PlayCard, action.Type);
        }

        [Test]
        public void PlayCardShouldReturnPlayerActionWithPlayedCard()
        {
            var card = new Card(CardSuit.Heart, CardType.Ace);
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.PlayCardProxy(card);
            Assert.AreEqual(card, action.Card);
        }

        [Test]
        public void ChangeTrumpShouldReturnPlayerActionWithTypeChangeTrump()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.ChangeTrumpProxy(CardSuit.Diamond);
            Assert.AreEqual(PlayerActionType.ChangeTrump, action.Type);
        }

        [Test]
        public void ChangeTrumpShouldRemoveNineOfTrumpFromThePlayersCards()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.AddCard(new Card(CardSuit.Diamond, CardType.Nine));
            basePlayerImplementation.ChangeTrumpProxy(CardSuit.Diamond);
            Assert.AreEqual(0, basePlayerImplementation.CardsCount);
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

            public PlayerAction ChangeTrumpProxy(CardSuit trumpCardSuit)
            {
                return this.ChangeTrump(trumpCardSuit);
            }

            public PlayerAction PlayCardProxy(Card card)
            {
                return this.PlayCard(card);
            }
        }
    }
}
