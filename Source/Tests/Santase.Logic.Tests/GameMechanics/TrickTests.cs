namespace Santase.Logic.Tests.GameMechanics
{
    using System.Linq;

    using NUnit.Framework;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    [TestFixture]
    public class TrickTests
    {
        [Test]
        public void PlayShouldCallGetTurnForBothPlayers()
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
            firstPlayerInfo.TrickCards.Add(new Card(CardSuit.Diamond, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(new Card(CardSuit.Diamond, CardType.Ten));
            firstPlayerInfo.TrickCards.Add(new Card(CardSuit.Spade, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ace));
            firstPlayerInfo.TrickCards.Add(new Card(CardSuit.Club, CardType.Ten));

            // Add cards for announcing 20
            firstPlayerInfo.AddCard(new Card(CardSuit.Heart, CardType.King));
            firstPlayerInfo.AddCard(new Card(CardSuit.Heart, CardType.Queen));
            stateManager.SetState(new MoreThanTwoCardsLeftRoundState(stateManager));

            secondPlayerInfo.AddCard(new Card(CardSuit.Heart, CardType.Ten));
            secondPlayerInfo.AddCard(new Card(CardSuit.Heart, CardType.Ace));

            var trick = new Trick(firstPlayerInfo, secondPlayerInfo, stateManager, deck, GameRulesProvider.Santase);
            var winner = trick.Play();

            Assert.AreEqual(1, firstPlayer.GetTurnCalledCount);
            Assert.AreEqual(0, secondPlayer.GetTurnCalledCount);
            Assert.AreSame(firstPlayerInfo, winner);
        }

        private static void SimulateGame(RoundPlayerInfo firstPlayer, RoundPlayerInfo secondPlayer, Deck deck)
        {
            for (var i = 0; i < 6; i++)
            {
                firstPlayer.AddCard(deck.GetNextCard());
            }

            for (var i = 0; i < 6; i++)
            {
                secondPlayer.AddCard(deck.GetNextCard());
            }
        }

        private class ValidPlayer : BasePlayer
        {
            public override string Name => "Valid player";

            public int GetTurnCalledCount { get; private set; }

            public PlayerTurnContext GetTurnContextObject { get; private set; }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.GetTurnCalledCount++;
                this.GetTurnContextObject = context.Clone() as PlayerTurnContext;
                var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(possibleCardsToPlay.First());
            }
        }
    }
}
