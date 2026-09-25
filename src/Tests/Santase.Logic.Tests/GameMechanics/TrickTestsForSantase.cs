namespace Santase.Logic.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    // One trick at a time through a Round prepared at a given position: hands, won cards,
    // talon and phase set up directly, then played through TryAct like the engine does.
    public class TrickTestsForSantase
    {
        [Fact]
        public void PlayShouldCallGetTurnAndEndTurnForBothPlayers()
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var deck = new Deck(new System.Random(7).Next);

            SimulateGame(firstPlayerInfo, secondPlayerInfo, deck);

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, deck, sm => new StartRoundState(sm));
            var winner = RoundTestDriver.PlayTrick(round);

            Assert.Equal(1, firstPlayer.GetTurnCalledCount);
            Assert.Equal(1, secondPlayer.GetTurnCalledCount);
            Assert.Equal(1, firstPlayer.EndTurnCalledCount);
            Assert.Equal(1, secondPlayer.EndTurnCalledCount);

            Assert.NotNull(firstPlayer.GetTurnContextObject);
            Assert.NotNull(secondPlayer.GetTurnContextObject);
            Assert.NotNull(firstPlayer.EndTurnContextObject);
            Assert.NotNull(secondPlayer.EndTurnContextObject);

            Assert.NotNull(firstPlayer.EndTurnContextObject.FirstPlayedCard);
            Assert.NotNull(firstPlayer.EndTurnContextObject.SecondPlayedCard);
            Assert.NotNull(secondPlayer.EndTurnContextObject.FirstPlayedCard);
            Assert.NotNull(secondPlayer.EndTurnContextObject.SecondPlayedCard);

            Assert.True(winner == firstPlayerInfo || winner == secondPlayerInfo);
        }

        [Theory]
        [InlineData("9D JD 9S JS", 73)] // Spades trump: the heart marriage is 20
        [InlineData("9D JD 9S JH", 93)] // Hearts trump: the heart marriage is 40
        public void PlayShouldCallGetTurnOnlyForFirstPlayerWhenTheFirstPlayerGoesOutByAnnounce(string talon, int expectedPoints)
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);

            // 53 points in firstPlayerInfo.TrickCards
            firstPlayerInfo.WinCard(Card.GetCard(CardSuit.Diamond, CardType.Ace));
            firstPlayerInfo.WinCard(Card.GetCard(CardSuit.Diamond, CardType.Ten));
            firstPlayerInfo.WinCard(Card.GetCard(CardSuit.Spade, CardType.Ace));
            firstPlayerInfo.WinCard(Card.GetCard(CardSuit.Club, CardType.Ace));
            firstPlayerInfo.WinCard(Card.GetCard(CardSuit.Club, CardType.Ten));

            // Add cards for announcing 20 (or 40)
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Queen));

            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ten));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, new TestDeck(talon), sm => new MoreThanTwoCardsLeftRoundState(sm));
            var winner = RoundTestDriver.PlayTrick(round);

            Assert.Equal(1, firstPlayer.GetTurnCalledCount);
            Assert.Equal(0, secondPlayer.GetTurnCalledCount);
            Assert.Equal(1, firstPlayer.EndTurnCalledCount);
            Assert.Equal(1, secondPlayer.EndTurnCalledCount);
            Assert.Same(firstPlayerInfo, winner);

            Assert.True(firstPlayerInfo.HasAtLeastOneTrick);
            Assert.False(secondPlayerInfo.HasAtLeastOneTrick);

            Assert.Equal(expectedPoints, winner.RoundPoints);

            // Going out ends the round at once: nobody is asked again, both hands still hold
            // cards, so the +10 last-trick bonus is not in play.
            Assert.True(round.IsFinished);
            Assert.Equal(PlayerPosition.NoOne, round.Result.LastTrickWinner);
            Assert.Null(firstPlayer.EndTurnContextObject.SecondPlayedCard);
        }

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldCorrectlyDetermineTheWinner(CardSuit trumpSuit)
        {
            // The follower's Jack of trumps beats the led Nine of hearts, whether it wins as the
            // higher heart (hearts trump) or as a trump.
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var jackOfTrumps = Card.GetCard(trumpSuit, CardType.Jack);
            var deck = new TestDeck($"KD KS KC KH A{"CDHS"[(int)trumpSuit]}");

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Nine));
            secondPlayerInfo.AddCard(jackOfTrumps);

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, deck, sm => new StartRoundState(sm));
            var winner = RoundTestDriver.PlayTrick(round);

            Assert.True(winner == secondPlayerInfo);
            Assert.Equal(2, winner.RoundPoints);
            Assert.Equal(2, winner.TrickCards.Count);
            Assert.Contains(Card.GetCard(CardSuit.Heart, CardType.Nine), winner.TrickCards);
            Assert.Contains(jackOfTrumps, winner.TrickCards);
            Assert.Empty(firstPlayerInfo.TrickCards);

            Assert.Equal(0, firstPlayer.EndTurnContextObject.FirstPlayerRoundPoints);
            Assert.Equal(2, firstPlayer.EndTurnContextObject.SecondPlayerRoundPoints);
            Assert.Equal(0, secondPlayer.EndTurnContextObject.FirstPlayerRoundPoints);
            Assert.Equal(2, secondPlayer.EndTurnContextObject.SecondPlayerRoundPoints);

            Assert.Equal(0, firstPlayer.GetTurnContextObject.FirstPlayerRoundPoints);
            Assert.Equal(0, firstPlayer.GetTurnContextObject.SecondPlayerRoundPoints);
            Assert.Equal(0, secondPlayer.GetTurnContextObject.FirstPlayerRoundPoints);
            Assert.Equal(0, secondPlayer.GetTurnContextObject.SecondPlayerRoundPoints);

            // The winner leads the next trick and, with the talon open, drew first.
            Assert.Equal(PlayerPosition.SecondPlayer, round.ToMove);
            Assert.Equal(TestCards.Parse("KD"), secondPlayer.CardsCollection.Single());
            Assert.Equal(TestCards.Parse("KS"), firstPlayer.CardsCollection.Single());
        }

        [Theory]
        [InlineData("9C JC 9S AS", 20)]
        [InlineData("9C JC 9S AH", 40)]
        public void PlayShouldProvideCorrectPlayerTurnContextToPlayers(string talon, int expectedAnnouncePoints)
        {
            var firstPlayer = new ValidPlayer();
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Queen));

            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Ten));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Diamond, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, new TestDeck(talon), sm => new MoreThanTwoCardsLeftRoundState(sm));
            RoundTestDriver.PlayTrick(round);

            Assert.True(firstPlayer.GetTurnContextObject.IsFirstPlayerTurn);
            Assert.False(secondPlayer.GetTurnContextObject.IsFirstPlayerTurn);
            Assert.Equal((Announce)expectedAnnouncePoints, secondPlayer.GetTurnContextObject.FirstPlayerAnnounce);
            Assert.NotNull(secondPlayer.GetTurnContextObject.FirstPlayedCard);
            Assert.Equal(CardSuit.Heart, secondPlayer.GetTurnContextObject.FirstPlayedCard.Suit);
            Assert.Equal(expectedAnnouncePoints, secondPlayer.GetTurnContextObject.FirstPlayerRoundPoints);
        }

        [Fact]
        public void PlayShouldRejectAnInvalidCardAndTheDriverShouldThrow()
        {
            var firstPlayer = new Mock<IPlayer>();
            firstPlayer.Setup(x => x.Name).Returns("Cheater");
            firstPlayer.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>()))
                .Returns(() => PlayerAction.PlayCard(Card.GetCard(CardSuit.Club, CardType.Ace)));
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer.Object);

            var secondPlayer = new Mock<IPlayer>();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer.Object);

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, new TestDeck("9C JC 9S AS"), sm => new StartRoundState(sm));

            // Step by step: the card is refused and nothing moves.
            Assert.False(round.TryAct(PlayerAction.PlayCard(Card.GetCard(CardSuit.Club, CardType.Ace))));
            Assert.Equal(PlayerPosition.FirstPlayer, round.ToMove);
            Assert.Contains(Card.GetCard(CardSuit.Heart, CardType.King), firstPlayerInfo.Cards);
            Assert.Equal(0, round.TricksPlayed);
            secondPlayer.Verify(x => x.EndTurn(It.IsAny<PlayerTurnContext>()), Times.Never);

            // Driven like the engine drives bots: an illegal move is a bug and throws.
            var exception = Assert.Throws<InternalGameException>(() => RoundTestDriver.PlayTrick(round));
            Assert.Contains("Cheater", exception.Message);
        }

        [Fact]
        public void PlayShouldRejectANullActionAndTheDriverShouldThrow()
        {
            var firstPlayer = new Mock<IPlayer>();
            firstPlayer.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>())).Returns((PlayerAction)null);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer.Object);

            var secondPlayer = new Mock<IPlayer>();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer.Object);
            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.King));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, new TestDeck("9C JC 9S AS"), sm => new StartRoundState(sm));

            Assert.False(round.TryAct(null));
            Assert.Equal(PlayerPosition.FirstPlayer, round.ToMove);
            Assert.Throws<InternalGameException>(() => RoundTestDriver.PlayTrick(round));
        }

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldChangeTheDeckTrumpWhenPlayerPlaysChangeTrumpAction(CardSuit trumpSuit)
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.ChangeTrump);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);

            var otherSuit = trumpSuit == CardSuit.Heart ? CardSuit.Club : CardSuit.Heart;
            var oldTrumpCard = Card.GetCard(trumpSuit, CardType.Ace);
            var nineOfTrump = Card.GetCard(trumpSuit, CardType.Nine);

            // Enough cards that the Nine (now at the bottom of the talon) is not drawn back.
            var talonSuit = trumpSuit == CardSuit.Diamond ? CardSuit.Spade : CardSuit.Diamond;
            var talon = string.Join(' ', new[] { "J", "Q", "K", "10" }.Select(r => r + "CDHS"[(int)talonSuit]))
                        + " A" + "CDHS"[(int)trumpSuit];
            var deck = new TestDeck(talon);

            firstPlayerInfo.AddCard(nineOfTrump);
            secondPlayerInfo.AddCard(Card.GetCard(otherSuit, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, deck, sm => new MoreThanTwoCardsLeftRoundState(sm));

            // The swap does not end the leader's turn.
            RoundTestDriver.Step(round);
            Assert.Equal(PlayerPosition.FirstPlayer, round.ToMove);
            Assert.Equal(nineOfTrump, deck.TrumpCard);
            Assert.Equal(nineOfTrump, round.CreateTurnContext().TrumpCard);

            RoundTestDriver.PlayTrick(round);

            Assert.Equal(nineOfTrump, deck.TrumpCard);
            Assert.Equal(nineOfTrump, secondPlayer.GetTurnContextObject.TrumpCard);
            Assert.Equal(2, firstPlayer.GetTurnCalledCount);
            Assert.True(firstPlayerInfo.TrickCards.Contains(oldTrumpCard), "Trick cards should contain oldTrumpCard");
            Assert.DoesNotContain(nineOfTrump, firstPlayerInfo.Cards);
            Assert.False(
                firstPlayer.CardsCollection.Contains(nineOfTrump),
                "Player contains nine of trump after changing trump card");
        }

        [Fact]
        public void PlayShouldRejectChangingTheTrumpWithoutTheNineOfTrumps()
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.ChangeTrump);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var deck = new TestDeck("9D JD QD KD AS");

            firstPlayerInfo.AddCard(Card.GetCard(CardSuit.Spade, CardType.Jack));
            secondPlayerInfo.AddCard(Card.GetCard(CardSuit.Heart, CardType.Ace));

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, deck, sm => new MoreThanTwoCardsLeftRoundState(sm));

            Assert.False(round.TryAct(PlayerAction.ChangeTrump()));
            Assert.Equal(TestCards.Parse("AS"), deck.TrumpCard);
            Assert.Contains(Card.GetCard(CardSuit.Spade, CardType.Jack), firstPlayerInfo.Cards);
            Assert.Throws<InternalGameException>(() => RoundTestDriver.PlayTrick(round));
        }

        [Fact]
        public void PlayShouldCloseTheGameWhenPlayerPlaysCloseGameAction()
        {
            var firstPlayer = new ValidPlayer(PlayerActionType.CloseGame);
            var firstPlayerInfo = new RoundPlayerInfo(firstPlayer);
            var secondPlayer = new ValidPlayer();
            var secondPlayerInfo = new RoundPlayerInfo(secondPlayer);
            var deck = new Deck(new System.Random(11).Next);

            SimulateGame(firstPlayerInfo, secondPlayerInfo, deck);

            var round = Prepare(firstPlayerInfo, secondPlayerInfo, deck, sm => new MoreThanTwoCardsLeftRoundState(sm));
            RoundTestDriver.PlayTrick(round);

            Assert.True(firstPlayerInfo.GameCloser);
            Assert.False(secondPlayerInfo.GameCloser);
            Assert.IsType<FinalRoundState>(round.StateManager.State);
            Assert.IsType<FinalRoundState>(secondPlayer.GetTurnContextObject.State);

            // A closed talon is dead: nobody draws after the trick.
            Assert.Equal(12, deck.CardsLeft);
            Assert.Equal(5, firstPlayerInfo.Cards.Count);
            Assert.Equal(5, secondPlayerInfo.Cards.Count);
        }

        private static void SimulateGame(RoundPlayerInfo firstPlayer, RoundPlayerInfo secondPlayer, IDeck deck)
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

        // A round at the given position with the first player to lead.
        private static Round Prepare(
            RoundPlayerInfo firstPlayerInfo,
            RoundPlayerInfo secondPlayerInfo,
            IDeck deck,
            System.Func<IStateManager, BaseRoundState> state)
        {
            var stateManager = new StateManager();
            stateManager.SetState(state(stateManager));
            var round = new Round(firstPlayerInfo, secondPlayerInfo, deck, stateManager, GameRulesProvider.Santase, PlayerPosition.FirstPlayer);
            round.Continue();
            return round;
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
