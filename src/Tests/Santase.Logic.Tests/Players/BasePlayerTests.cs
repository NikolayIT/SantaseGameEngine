namespace Santase.Logic.Tests.Players
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class BasePlayerTests
    {
        [Fact]
        public void CardsShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.True(basePlayerImplementation.ListIsNotNull);
        }

        [Fact]
        public void AnnounceValidatorShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.True(basePlayerImplementation.AnnounceValidatorIsNotNull);
        }

        [Fact]
        public void AddCardShouldWorkCorrectly()
        {
            var basePlayerImplementation = new BasePlayerImpl();

            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.Ace));
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.Ten));
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.King));
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.Queen));
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.Jack));
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Club, CardType.Nine));

            Assert.Equal(6, basePlayerImplementation.CardsCollection.Count);
        }

        [Fact]
        public void EndRoundShouldClearCards()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.EndRound();
            Assert.Equal(0, basePlayerImplementation.CardsCollection.Count);
        }

        [Fact]
        public void PlayerActionValidatorShouldNotBeNull()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            Assert.True(basePlayerImplementation.PlayerActionValidatorIsNotNull);
        }

        [Fact]
        public void PlayCardShouldReturnPlayerActionWithTypePlayCard()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.PlayCardProxy(Card.GetCard(CardSuit.Heart, CardType.Ace));
            Assert.Equal(PlayerActionType.PlayCard, action.Type);
        }

        [Fact]
        public void PlayCardShouldReturnPlayerActionWithPlayedCard()
        {
            var card = Card.GetCard(CardSuit.Heart, CardType.Ace);
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.PlayCardProxy(card);
            Assert.Equal(card, action.Card);
        }

        [Fact]
        public void ChangeTrumpShouldReturnPlayerActionWithTypeChangeTrump()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var action = basePlayerImplementation.ChangeTrumpProxy(Card.GetCard(CardSuit.Diamond, CardType.King));
            Assert.Equal(PlayerActionType.ChangeTrump, action.Type);
        }

        [Fact]
        public void ChangeTrumpShouldRemoveNineOfTrumpFromThePlayersCards()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Nine));

            basePlayerImplementation.ChangeTrumpProxy(Card.GetCard(CardSuit.Diamond, CardType.King));

            Assert.False(
                basePlayerImplementation.CardsCollection.Contains(Card.GetCard(CardSuit.Diamond, CardType.Nine)),
                "Trump card for changing found in player cards after changing the trump");
        }

        [Fact]
        public void ChangeTrumpShouldAddTrumpCardToPlayerCards()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            basePlayerImplementation.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Nine));

            basePlayerImplementation.ChangeTrumpProxy(Card.GetCard(CardSuit.Diamond, CardType.King));

            Assert.True(
                basePlayerImplementation.CardsCollection.Contains(Card.GetCard(CardSuit.Diamond, CardType.King)),
                "Trump card not found in player cards after changing the trump");
            Assert.Equal(1, basePlayerImplementation.CardsCollection.Count);
        }

        [Fact]
        public void EndTurnShouldNotThrowExceptions()
        {
            var basePlayerImplementation = new BasePlayerImpl();
            var playerTurnContext = new PlayerTurnContext(
                new FinalRoundState(null),
                Card.GetCard(CardSuit.Club, CardType.Ace),
                0,
                0,
                0);
            basePlayerImplementation.EndTurn(playerTurnContext);
        }

        [Fact]
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

            public ICollection<Card> CardsCollection => this.Cards;

            public override string Name => string.Empty;

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                throw new System.NotImplementedException();
            }

            public PlayerAction ChangeTrumpProxy(Card trumpCard)
            {
                return this.ChangeTrump(trumpCard);
            }

            public PlayerAction PlayCardProxy(Card card)
            {
                return this.PlayCard(card);
            }
        }
    }
}
