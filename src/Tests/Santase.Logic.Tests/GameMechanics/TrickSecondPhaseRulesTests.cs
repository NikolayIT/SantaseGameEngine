namespace Santase.Logic.Tests.GameMechanics
{
    using System.Linq;

    using Moq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    // Second-phase (talon exhausted or closed) rule enforcement exercised through a real
    // Trick + StateManager + Deck, not just the validator in isolation: an illegal response
    // must abort the trick with InternalGameException, a forced response must win the trick.
    public class TrickSecondPhaseRulesTests
    {
        private static readonly CardSuit[] AllSuits =
        {
            CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade,
        };

        [Fact]
        public void PlayShouldRejectAResponseThatDoesNotFollowSuit()
        {
            var trick = CreateFinalStateTrick(out var suits, out _);

            // Leader plays A(s1); follower holds 9(s1) but answers K(s2).
            var firstInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[0], CardType.Ace));
            var secondInfo = CreatePlayer(
                Card.GetCard(suits[1], CardType.King),
                Card.GetCard(suits[0], CardType.Nine),
                Card.GetCard(suits[1], CardType.King));

            Assert.Throws<InternalGameException>(() => trick.Play(firstInfo, secondInfo));
        }

        [Fact]
        public void PlayShouldRejectAResponseThatDoesNotBeatTheLedCardWhenAHigherCardIsHeld()
        {
            var trick = CreateFinalStateTrick(out var suits, out _);

            // Leader plays 10(s1); follower holds A(s1) but answers 9(s1).
            var firstInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ten),
                Card.GetCard(suits[0], CardType.Ten));
            var secondInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Nine),
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[0], CardType.Nine));

            Assert.Throws<InternalGameException>(() => trick.Play(firstInfo, secondInfo));
        }

        [Fact]
        public void PlayShouldRejectADiscardWhenVoidInTheLedSuitButHoldingATrump()
        {
            var trick = CreateFinalStateTrick(out var suits, out var trumpSuit);

            // Leader plays A(s1); follower is void in s1, holds a trump, but discards 9(s2).
            var firstInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[0], CardType.Ace));
            var secondInfo = CreatePlayer(
                Card.GetCard(suits[1], CardType.Nine),
                Card.GetCard(trumpSuit, CardType.Nine),
                Card.GetCard(suits[1], CardType.Nine));

            Assert.Throws<InternalGameException>(() => trick.Play(firstInfo, secondInfo));
        }

        [Fact]
        public void PlayShouldAcceptTheForcedResponseAndAwardTheTrick()
        {
            var trick = CreateFinalStateTrick(out var suits, out _);

            // Leader plays 10(s1); follower is forced to beat it with A(s1) and wins 21.
            var firstInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ten),
                Card.GetCard(suits[0], CardType.Ten));
            var secondInfo = CreatePlayer(
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[0], CardType.Ace),
                Card.GetCard(suits[1], CardType.Nine));

            var winner = trick.Play(firstInfo, secondInfo);

            Assert.Same(secondInfo, winner);
            Assert.Equal(21, winner.RoundPoints);
            Assert.Contains(Card.GetCard(suits[0], CardType.Ten), winner.TrickCards);
            Assert.Contains(Card.GetCard(suits[0], CardType.Ace), winner.TrickCards);
        }

        private static Trick CreateFinalStateTrick(out CardSuit[] nonTrumpSuits, out CardSuit trumpSuit)
        {
            var stateManager = new StateManager();
            stateManager.SetState(new FinalRoundState(stateManager));
            var deck = new Deck();
            trumpSuit = deck.TrumpCard.Suit;
            var localTrumpSuit = trumpSuit;
            nonTrumpSuits = AllSuits.Where(s => s != localTrumpSuit).Take(2).ToArray();
            return new Trick(stateManager, deck, GameRulesProvider.Santase);
        }

        private static RoundPlayerInfo CreatePlayer(Card cardToPlay, params Card[] hand)
        {
            var player = new Mock<IPlayer>();
            player.Setup(x => x.GetTurn(It.IsAny<PlayerTurnContext>()))
                .Returns(PlayerAction.PlayCard(cardToPlay));
            var playerInfo = new RoundPlayerInfo(player.Object);
            foreach (var card in hand)
            {
                playerInfo.AddCard(card);
            }

            return playerInfo;
        }
    }
}
