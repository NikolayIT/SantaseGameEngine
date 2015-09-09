namespace Santase.Logic.Tests.Players
{
    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

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
        public void AddCardShouldWorkCorrectly()
        {
            var basePlayerImplementation = new BasePlayerImpl();

            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Ace));
            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Ten));
            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.King));
            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Queen));
            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Jack));
            basePlayerImplementation.AddCard(new Card(CardSuit.Club, CardType.Nine));

            Assert.AreEqual(6, basePlayerImplementation.CardsCount);
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

        [Test]
        public void EndTurnShouldNotThrowExceptions()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(null),
                new Card(CardSuit.Club, CardType.Ace),
                0, 0, 0);
            basePlayerImplementation.EndTurn(playerTurnContext);
        }

        [Test]
        public void EndGameShouldNotThrowExceptions()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.EndGame(true);
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
