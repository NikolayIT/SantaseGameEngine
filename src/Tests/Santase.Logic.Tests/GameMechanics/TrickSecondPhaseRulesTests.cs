namespace Santase.Logic.Tests.GameMechanics
{
    using System.Linq;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    // Second-phase (talon exhausted or closed) rule enforcement exercised through a real round
    // in its final phase, not just the validator in isolation: an illegal response is refused
    // (and makes the IPlayer driver throw), a forced response must win the trick. Every rule is
    // checked with each suit as trumps.
    public class TrickSecondPhaseRulesTests
    {
        private static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldRejectAResponseThatDoesNotFollowSuit(CardSuit trumpSuit)
        {
            var suits = NonTrumpSuits(trumpSuit);

            // Leader plays A(s1); follower holds 9(s1) but answers K(s2).
            var round = CreateFinalStateRound(
                trumpSuit,
                CreatePlayer(Card.GetCard(suits[0], CardType.Ace), Card.GetCard(suits[0], CardType.Ace)),
                CreatePlayer(
                    Card.GetCard(suits[1], CardType.King),
                    Card.GetCard(suits[0], CardType.Nine),
                    Card.GetCard(suits[1], CardType.King)));

            AssertTheResponseIsRefused(round);
        }

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldRejectAResponseThatDoesNotBeatTheLedCardWhenAHigherCardIsHeld(CardSuit trumpSuit)
        {
            var suits = NonTrumpSuits(trumpSuit);

            // Leader plays 10(s1); follower holds A(s1) but answers 9(s1).
            var round = CreateFinalStateRound(
                trumpSuit,
                CreatePlayer(Card.GetCard(suits[0], CardType.Ten), Card.GetCard(suits[0], CardType.Ten)),
                CreatePlayer(
                    Card.GetCard(suits[0], CardType.Nine),
                    Card.GetCard(suits[0], CardType.Ace),
                    Card.GetCard(suits[0], CardType.Nine)));

            AssertTheResponseIsRefused(round);
        }

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldRejectADiscardWhenVoidInTheLedSuitButHoldingATrump(CardSuit trumpSuit)
        {
            var suits = NonTrumpSuits(trumpSuit);

            // Leader plays A(s1); follower is void in s1, holds a trump, but discards 9(s2).
            var round = CreateFinalStateRound(
                trumpSuit,
                CreatePlayer(Card.GetCard(suits[0], CardType.Ace), Card.GetCard(suits[0], CardType.Ace)),
                CreatePlayer(
                    Card.GetCard(suits[1], CardType.Nine),
                    Card.GetCard(trumpSuit, CardType.Nine),
                    Card.GetCard(suits[1], CardType.Nine)));

            AssertTheResponseIsRefused(round);
        }

        [Theory]
        [InlineData(CardSuit.Club)]
        [InlineData(CardSuit.Diamond)]
        [InlineData(CardSuit.Heart)]
        [InlineData(CardSuit.Spade)]
        public void PlayShouldAcceptTheForcedResponseAndAwardTheTrick(CardSuit trumpSuit)
        {
            var suits = NonTrumpSuits(trumpSuit);

            // Leader plays 10(s1); follower is forced to beat it with A(s1) and wins 21.
            var firstInfo = CreatePlayer(Card.GetCard(suits[0], CardType.Ten), Card.GetCard(suits[0], CardType.Ten));
            var secondInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[1], CardType.Nine));
            var round = CreateFinalStateRound(trumpSuit, firstInfo, secondInfo);

            var winner = RoundTestDriver.PlayTrick(round);

            Assert.Same(secondInfo, winner);
            Assert.Equal(21, winner.RoundPoints);
            Assert.Contains(Card.GetCard(suits[0], CardType.Ten), winner.TrickCards);
            Assert.Contains(Card.GetCard(suits[0], CardType.Ace), winner.TrickCards);
        }

        private static void AssertTheResponseIsRefused(Round round)
        {
            RoundTestDriver.Step(round); // the lead
            Assert.Equal(PlayerPosition.SecondPlayer, round.ToMove);

            var follower = round.SecondPlayer;
            var handBefore = follower.Cards.ToList();
            var response = follower.Player.GetTurn(round.CreateTurnContext());

            Assert.False(round.TryAct(response));
            Assert.Equal(PlayerPosition.SecondPlayer, round.ToMove);
            Assert.Equal(handBefore, follower.Cards);
            Assert.Equal(0, round.TricksPlayed);
            Assert.Throws<InternalGameException>(() => RoundTestDriver.Step(round));
        }

        private static CardSuit[] NonTrumpSuits(CardSuit trumpSuit)
        {
            return AllSuits.Where(s => s != trumpSuit).Take(2).ToArray();
        }

        private static Round CreateFinalStateRound(CardSuit trumpSuit, RoundPlayerInfo firstInfo, RoundPlayerInfo secondInfo)
        {
            var stateManager = new StateManager();
            stateManager.SetState(new FinalRoundState(stateManager));

            // The talon only provides the trump card here: nothing is drawn in the final phase.
            var trumpCard = Card.GetCard(trumpSuit, CardType.Jack);
            var talonCard = Card.GetCard(trumpSuit, CardType.Queen);
            var deck = new TestDeck($"{Code(talonCard)} {Code(trumpCard)}");

            var round = new Round(firstInfo, secondInfo, deck, stateManager, GameRulesProvider.Santase, PlayerPosition.FirstPlayer);
            round.Continue();
            return round;
        }

        private static string Code(Card card)
        {
            var rank = card.Type switch
            {
                CardType.Nine => "9",
                CardType.Ten => "10",
                CardType.Jack => "J",
                CardType.Queen => "Q",
                CardType.King => "K",
                _ => "A",
            };
            return rank + "CDHS"[(int)card.Suit];
        }

        private static RoundPlayerInfo CreatePlayer(Card cardToPlay, params Card[] hand)
        {
            var player = new Mock<IPlayer>();
            player.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>()))
                .Returns(() => PlayerAction.PlayCard(cardToPlay));
            var playerInfo = new RoundPlayerInfo(player.Object);
            foreach (var card in hand)
            {
                playerInfo.AddCard(card);
            }

            return playerInfo;
        }
    }
}
