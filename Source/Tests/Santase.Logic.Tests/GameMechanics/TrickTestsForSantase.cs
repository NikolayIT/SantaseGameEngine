namespace Santase.Logic.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class TrickTestsForSantase
    {
        [Test]
        public void PlayShouldCallGetTurnAndEndTurnForBothPlayers()
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            var deck = new Deck();

            SimulateGame(firstPlayerInfo, secondPlayerInfo, deck);

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            var winner = trick.Play();

            Assert.AreEqual(1, firstPlayer.GetTurnCalledCount);
            Assert.AreEqual(1, secondPlayer.GetTurnCalledCount);
            Assert.AreEqual(1, firstPlayer.EndTurnCalledCount);
            Assert.AreEqual(1, secondPlayer.EndTurnCalledCount);

            Assert.IsNotNull(firstPlayer.GetTurnContextObject);
            Assert.IsNotNull(secondPlayer.GetTurnContextObject);
            Assert.IsNotNull(firstPlayer.EndTurnContextObject);
            Assert.IsNotNull(secondPlayer.EndTurnContextObject);

            Assert.IsNotNull(firstPlayer.EndTurnContextObject.FirstPlayedCard);
            Assert.IsNotNull(firstPlayer.EndTurnContextObject.SecondPlayedCard);
            Assert.IsNotNull(secondPlayer.EndTurnContextObject.FirstPlayedCard);
            Assert.IsNotNull(secondPlayer.EndTurnContextObject.SecondPlayedCard);

            Assert.IsTrue(winner == firstPlayerInfo || winner == secondPlayerInfo);
        }

        [Test]
        public void PlayShouldCallGetTurnOnlyForFirstPlayerWhenTheFirstPlayerGoesOutByAnnounce()
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            var deck = new Deck();

            // 53 points in firstPlayerInfo.TrickCards
            firstPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Diamond, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Diamond, CardType.Ten));
            firstPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Spade, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(Card.GetCard(CardSuit.Club, CardType.Ten));

            // Add cards for announcing 20
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Queen));
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));

            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ten));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            var winner = trick.Play();

            Assert.AreEqual(1, firstPlayer.GetTurnCalledCount);
            Assert.AreEqual(0, secondPlayer.GetTurnCalledCount);
            Assert.AreEqual(1, firstPlayer.EndTurnCalledCount);
            Assert.AreEqual(1, secondPlayer.EndTurnCalledCount);
            Assert.AreSame(firstPlayerInfo, winner);

            Assert.IsTrue(firstPlayerInfo.HasAtLeastOneTrick);
            Assert.IsFalse(secondPlayerInfo.HasAtLeastOneTrick);

            Assert.IsTrue(winner.RoundPoints == 73 || winner.RoundPoints == 93);
            Assert.IsTrue(winner.RoundPoints == 73 || winner.RoundPoints == 93);
        }

        [Test]
        public void PlayShouldCorrectlyDetermineTheWinner()
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            var deck = new Deck();

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Nine));
            secondPlayerInfo.AddCard(Card.GetCard(deck.TrumpCard.Suit, CardType.Jack));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            var winner = trick.Play();

            Assert.IsTrue(winner == secondPlayerInfo);
            Assert.AreEqual(2, winner.RoundPoints);
            Assert.AreEqual(2, winner.TrickCards.Count);
            Assert.IsTrue(winner.TrickCards.Contains(Card.GetCard(CardSuit.Heart, CardType.Nine)));
            Assert.IsTrue(winner.TrickCards.Contains(Card.GetCard(deck.TrumpCard.Suit, CardType.Jack)));
            Assert.AreEqual(0, firstPlayerInfo.TrickCards.Count);

            Assert.AreEqual(0, firstPlayer.EndTurnContextObject.FirstPlayerRoundPoints);
            Assert.AreEqual(2, firstPlayer.EndTurnContextObject.SecondPlayerRoundPoints);
            Assert.AreEqual(0, secondPlayer.EndTurnContextObject.FirstPlayerRoundPoints);
            Assert.AreEqual(2, secondPlayer.EndTurnContextObject.SecondPlayerRoundPoints);

            Assert.AreEqual(0, firstPlayer.GetTurnContextObject.FirstPlayerRoundPoints);
            Assert.AreEqual(0, firstPlayer.GetTurnContextObject.SecondPlayerRoundPoints);
            Assert.AreEqual(0, secondPlayer.GetTurnContextObject.FirstPlayerRoundPoints);
            Assert.AreEqual(0, secondPlayer.GetTurnContextObject.SecondPlayerRoundPoints);
        }

        [Test]
        public void PlayShouldProvideCorrectPlayerTurnContextToPlayers()
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            var deck = new Deck();

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Queen));
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));

            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Ten));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            trick.Play();

            Assert.IsTrue(firstPlayer.GetTurnContextObject.IsFirstPlayerTurn);
            Assert.IsFalse(secondPlayer.GetTurnContextObject.IsFirstPlayerTurn);
            Assert.IsTrue(secondPlayer.GetTurnContextObject.FirstPlayerAnnounce != Announce.None);
            Assert.IsNotNull(secondPlayer.GetTurnContextObject.FirstPlayedCard);
            Assert.AreEqual(CardSuit.Heart, secondPlayer.GetTurnContextObject.FirstPlayedCard.Suit);

            Assert.IsTrue(
                secondPlayer.GetTurnContextObject.FirstPlayerRoundPoints == 20
                || secondPlayer.GetTurnContextObject.FirstPlayerRoundPoints == 40);
        }

        [Test]
        public void PlayShouldThrowAnExceptionWhenPlayerPlaysInvalidCard()
        {
            var firstPlayer = new Mock<IPlayer>();
            firstPlayer.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>()))
                .Returns(PlayerAction.PlayCard(Card.GetCard(CardSuit.Club, CardType.Ace)));
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer.Object);

            var secondPlayer = new Mock<IPlayer>();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer.Object);

            var stateManager = new StateManager();
            var deck = new Deck();

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            Assert.Throws<InternalGameException>(() => trick.Play());
        }

        [Test]
        public void PlayShouldThrowAnExceptionWhenPlayerReturnsNullAction()
        {
            var firstPlayer = new Mock<IPlayer>();
            firstPlayer.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>())).Returns((PlayerAction)null);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer.Object);

            var secondPlayer = new Mock<IPlayer>();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer.Object);

            var stateManager = new StateManager();
            var deck = new Deck();

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            Assert.Throws<InternalGameException>(() => trick.Play());
        }

        [Test]
        public void PlayShouldChangeTheDeckTrumpWhenPlayerPlaysChangeTrumpAction()
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.ChangeTrump);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));
            var deck = new Deck();
            var trumpSuit = deck.TrumpCard.Suit;

            var oldTrumpCard = deck.TrumpCard;
            var nineOfTrump = Card.GetCard(trumpSuit, CardType.Nine);

            firstPlayerInfo.AddCard(nineOfTrump);
            secondPlayerInfo.AddCard(
                Card.GetCard(trumpSuit == CardSuit.Heart ? CardSuit.Club : CardSuit.Heart, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            trick.Play();

            Assert.AreEqual(nineOfTrump, deck.TrumpCard);
            Assert.AreEqual(nineOfTrump, secondPlayer.GetTurnContextObject.TrumpCard);
            Assert.IsTrue(firstPlayerInfo.TrickCards.Contains(oldTrumpCard), "Trick cards should contain oldTrumpCard");
            Assert.IsFalse(firstPlayerInfo.Cards.Contains(nineOfTrump));
            Assert.IsFalse(
                firstPlayer.CardsCollection.Contains(nineOfTrump),
                "Player contains nine of trump after changing trump card");
        }

        [Test]
        public void PlayShouldThrowAnExceptionWhenClosingTheGameAndNineOfTrumpsIsMissing()
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.ChangeTrump);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));
            var deck = new Deck();
            var trumpSuit = deck.TrumpCard.Suit;

            firstPlayerInfo.AddCard(Card.GetCard(trumpSuit, CardType.Jack));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            Assert.Throws<InternalGameException>(() => trick.Play());
        }

        [Test]
        public void PlayShouldCloseTheGameWhenPlayerPlaysCloseGameAction()
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.CloseGame);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var stateManager = new StateManager();
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));
            var deck = new Deck();

            SimulateGame(firstPlayerInfo, secondPlayerInfo, deck);

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            trick.Play();

            Assert.IsTrue(firstPlayerInfo.GameCloser);
            Assert.IsFalse(secondPlayerInfo.GameCloser);
            Assert.IsInstanceOf<FinalRoundState>(stateManager.State);
            Assert.IsInstanceOf<FinalRoundState>(secondPlayer.GetTurnContextObject.State);
        }

        private static void SimulateGame(RoundPlayerInfo firstPlayer, RoundPlayerInfo secondPlayer, Deck deck)
        {
            for (var i = 0; i < GameRulesProvider.Santase.CardsAtStartOfTheRound; i++)
            {
                firstPlayer.AddCard(deck.GetNextCard());
            }

            for (var i = 0; i < GameRulesProvider.Santase.CardsAtStartOfTheRound; i++)
            {
                secondPlayer.AddCard(deck.GetNextCard());
            }
        }

        private class ValidPlayer : BasePlayer
        {
            private PlayerActionType actionToPlay;

            public ValidPlayer(PlayerActionType actionToPlay = PlayerActionType.PlayCard)
            {
                this.actionToPlay = actionToPlay;
            }

            public override string Name => "Valid player";

            public int GetTurnCalledCount { get; private set; }

            public int EndTurnCalledCount { get; private set; }

            public PlayerTurnContext GetTurnContextObject { get; private set; }

            public PlayerTurnContext EndTurnContextObject { get; private set; }

            public ICollection<Card> CardsCollection => this.Cards;

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.GetTurnCalledCount++;
                this.GetTurnContextObject = context.DeepClone();

                if (this.actionToPlay == PlayerActionType.ChangeTrump)
                {
                    this.actionToPlay = PlayerActionType.PlayCard;
                    return this.ChangeTrump(context.TrumpCard);
                }

                if (this.actionToPlay == PlayerActionType.CloseGame)
                {
                    this.actionToPlay = PlayerActionType.PlayCard;
                    return PlayerAction.CloseGame();
                }

                var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(possibleCardsToPlay.First());
            }

            public override void EndTurn(PlayerTurnContext context)
            {
                this.EndTurnCalledCount++;
                this.EndTurnContextObject = context.DeepClone();

                base.EndTurn(context);
            }
        }
    }
}
