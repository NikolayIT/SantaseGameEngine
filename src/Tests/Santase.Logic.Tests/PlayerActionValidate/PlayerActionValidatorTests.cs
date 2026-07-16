namespace Santase.Logic.Tests.PlayerActionValidate
{
    using System.Collections.Generic;
    using System.Linq;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class PlayerActionValidatorTests
    {
        [Fact]
        public void IsValidShouldReturnFalseForNullActionWhenTheStateForbidsAnnouncing()
        {
            var context = CreateContext(StartState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Heart, CardType.King) };

            var isValid = PlayerActionValidator.Instance.IsValid(null, context, playerCards);

            Assert.False(isValid);
        }

        [Fact]
        public void IsValidShouldReturnFalseForNullActionWhenTheStateAllowsAnnouncing()
        {
            // Regression: the null check used to run after the announce computation, so a
            // null action in an announce-capable state crashed instead of being rejected.
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Heart, CardType.King) };

            var isValid = PlayerActionValidator.Instance.IsValid(null, context, playerCards);

            Assert.False(isValid);
        }

        [Fact]
        public void IsValidShouldComputeTwentyWhenLeadingANonTrumpMarriageCard()
        {
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Heart, CardType.Queen),
                                      Card.GetCard(CardSuit.Spade, CardType.Ace),
                                  };
            var action = PlayerAction.PlayCard(Card.GetCard(CardSuit.Heart, CardType.King));

            var isValid = PlayerActionValidator.Instance.IsValid(action, context, playerCards);

            Assert.True(isValid);
            Assert.Equal(Announce.Twenty, action.Announce);
        }

        [Fact]
        public void IsValidShouldComputeFortyWhenLeadingATrumpMarriageCard()
        {
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Club, CardType.King),
                                      Card.GetCard(CardSuit.Club, CardType.Queen),
                                  };
            var action = PlayerAction.PlayCard(Card.GetCard(CardSuit.Club, CardType.Queen));

            var isValid = PlayerActionValidator.Instance.IsValid(action, context, playerCards);

            Assert.True(isValid);
            Assert.Equal(Announce.Forty, action.Announce);
        }

        [Fact]
        public void IsValidShouldOverwriteASmuggledAnnounceWhenNoMarriageIsHeld()
        {
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Spade, CardType.Ace),
                                  };
            var action = PlayerAction.PlayCard(Card.GetCard(CardSuit.Heart, CardType.King));
            action.Announce = Announce.Forty;

            var isValid = PlayerActionValidator.Instance.IsValid(action, context, playerCards);

            Assert.True(isValid);
            Assert.Equal(Announce.None, action.Announce);
        }

        [Fact]
        public void IsValidShouldClearASmuggledAnnounceWhenTheStateForbidsAnnouncing()
        {
            // Regression: on the first trick (announcing forbidden) the action's announce
            // used to pass through unvalidated and Trick would have credited it.
            var context = CreateContext(StartState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Heart, CardType.Queen),
                                  };
            var action = PlayerAction.PlayCard(Card.GetCard(CardSuit.Heart, CardType.King));
            action.Announce = Announce.Forty;

            var isValid = PlayerActionValidator.Instance.IsValid(action, context, playerCards);

            Assert.True(isValid);
            Assert.Equal(Announce.None, action.Announce);
        }

        [Fact]
        public void IsValidShouldNotComputeAnAnnounceForTheSecondPlayer()
        {
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            context.FirstPlayedCard = Card.GetCard(CardSuit.Spade, CardType.Ace);
            var playerCards = new List<Card>
                                  {
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Heart, CardType.Queen),
                                  };
            var action = PlayerAction.PlayCard(Card.GetCard(CardSuit.Heart, CardType.King));

            var isValid = PlayerActionValidator.Instance.IsValid(action, context, playerCards);

            Assert.True(isValid);
            Assert.Equal(Announce.None, action.Announce);
        }

        [Fact]
        public void IsValidShouldAllowChangeTrumpWhenStateAndHandPermit()
        {
            var trumpCard = Card.GetCard(CardSuit.Spade, CardType.Queen);
            var context = CreateContext(MidRoundState(), trumpCard);
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Spade, CardType.Nine) };

            var isValid = PlayerActionValidator.Instance.IsValid(PlayerAction.ChangeTrump(), context, playerCards);

            Assert.True(isValid);
        }

        [Fact]
        public void IsValidShouldRejectChangeTrumpWhenTheStateForbidsIt()
        {
            var trumpCard = Card.GetCard(CardSuit.Spade, CardType.Queen);
            var context = CreateContext(FinalState(), trumpCard);
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Spade, CardType.Nine) };

            var isValid = PlayerActionValidator.Instance.IsValid(PlayerAction.ChangeTrump(), context, playerCards);

            Assert.False(isValid);
        }

        [Fact]
        public void IsValidShouldRejectChangeTrumpWithoutTheNineOfTrumps()
        {
            var trumpCard = Card.GetCard(CardSuit.Spade, CardType.Queen);
            var context = CreateContext(MidRoundState(), trumpCard);
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Heart, CardType.Nine) };

            var isValid = PlayerActionValidator.Instance.IsValid(PlayerAction.ChangeTrump(), context, playerCards);

            Assert.False(isValid);
        }

        [Fact]
        public void IsValidShouldAllowCloseGameOnlyForTheLeaderInAPermittingState()
        {
            var trumpCard = Card.GetCard(CardSuit.Spade, CardType.Queen);
            var playerCards = new List<Card> { Card.GetCard(CardSuit.Heart, CardType.Nine) };

            var leaderContext = CreateContext(MidRoundState(), trumpCard);
            Assert.True(PlayerActionValidator.Instance.IsValid(PlayerAction.CloseGame(), leaderContext, playerCards));

            var followerContext = CreateContext(MidRoundState(), trumpCard);
            followerContext.FirstPlayedCard = Card.GetCard(CardSuit.Spade, CardType.Ace);
            Assert.False(PlayerActionValidator.Instance.IsValid(PlayerAction.CloseGame(), followerContext, playerCards));

            var twoCardsLeftContext = CreateContext(TwoCardsLeftState(), trumpCard);
            Assert.False(PlayerActionValidator.Instance.IsValid(PlayerAction.CloseGame(), twoCardsLeftContext, playerCards));
        }

        [Fact]
        public void GetPossibleCardsToPlayShouldReturnAllCardsWhenRulesDoNotApply()
        {
            var context = CreateContext(MidRoundState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            context.FirstPlayedCard = Card.GetCard(CardSuit.Spade, CardType.Ace);
            var playerCards = new CardCollection
                                  {
                                      Card.GetCard(CardSuit.Spade, CardType.Nine),
                                      Card.GetCard(CardSuit.Heart, CardType.King),
                                      Card.GetCard(CardSuit.Diamond, CardType.Ten),
                                      Card.GetCard(CardSuit.Club, CardType.Jack),
                                  };

            var possibleCards = PlayerActionValidator.Instance.GetPossibleCardsToPlay(context, playerCards);

            Assert.Equal(4, possibleCards.Count);
        }

        [Fact]
        public void GetPossibleCardsToPlayShouldReturnTheSameLegalMovesForBothCollectionImplementations()
        {
            // Second phase, follower holds A♦ 9♦ A♠ 9♣ against a led 10♦ (trump ♣):
            // the single legal move is A♦ (must beat in the led suit; the lower 9♦, the
            // off-suit A♠ and the trump 9♣ are all illegal while a higher ♦ is held).
            var context = CreateContext(FinalState(), Card.GetCard(CardSuit.Club, CardType.Nine));
            context.FirstPlayedCard = Card.GetCard(CardSuit.Diamond, CardType.Ten);

            var cards = new[]
                            {
                                Card.GetCard(CardSuit.Diamond, CardType.Ace),
                                Card.GetCard(CardSuit.Diamond, CardType.Nine),
                                Card.GetCard(CardSuit.Spade, CardType.Ace),
                                Card.GetCard(CardSuit.Club, CardType.Nine),
                            };

            var fastPathCards = new CardCollection();
            foreach (var card in cards)
            {
                fastPathCards.Add(card);
            }

            var slowPathCards = new List<Card>(cards);

            var fastPathResult = PlayerActionValidator.Instance.GetPossibleCardsToPlay(context, fastPathCards);
            var slowPathResult = PlayerActionValidator.Instance.GetPossibleCardsToPlay(context, slowPathCards);

            Assert.Single(fastPathResult);
            Assert.Contains(Card.GetCard(CardSuit.Diamond, CardType.Ace), fastPathResult);
            Assert.Equal(fastPathResult.OrderBy(x => x.GetHashCode()), slowPathResult.OrderBy(x => x.GetHashCode()));
        }

        private static PlayerTurnContext CreateContext(BaseRoundState state, Card trumpCard)
        {
            return new PlayerTurnContext(state, trumpCard, 12, 0, 0);
        }

        private static BaseRoundState StartState()
        {
            return new StartRoundState(new Mock<IStateManager>().Object);
        }

        private static BaseRoundState MidRoundState()
        {
            return new MoreThanTwoCardsLeftRoundState(new Mock<IStateManager>().Object);
        }

        private static BaseRoundState TwoCardsLeftState()
        {
            return new TwoCardsLeftRoundState(new Mock<IStateManager>().Object);
        }

        private static BaseRoundState FinalState()
        {
            return new FinalRoundState(new Mock<IStateManager>().Object);
        }
    }
}
