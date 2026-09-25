namespace Santase.Logic.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Santase.Logic.WinnerLogic;

    using Xunit;

    // Deterministic check of the talon draw order through a real Round: after every trick
    // the trick winner draws first, and on the final draw the trick loser receives the
    // face-up trump card. Verified by recording every AddCard call with a shared sequence
    // counter and independently re-deriving each trick's winner via CardWinnerLogic.
    public class RoundDrawOrderTests
    {
        [Fact]
        public void TrickWinnerShouldDrawFirstAndTrickLoserShouldDrawTheTrumpCardLast()
        {
            const int NumberOfRounds = 200;

            var fullTalonRounds = 0;
            var pairsChecked = 0;

            for (var i = 0; i < NumberOfRounds; i++)
            {
                var sharedSequence = new int[1];
                var firstPlayer = new DrawRecordingPlayer(sharedSequence);
                var secondPlayer = new DrawRecordingPlayer(sharedSequence);
                var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase);

                RoundTestDriver.Play(round, 0, 0);

                Assert.Equal(firstPlayer.TrumpCardAtRoundStart, secondPlayer.TrumpCardAtRoundStart);
                var trumpSuit = firstPlayer.TrumpCardAtRoundStart.Suit;

                var draws = firstPlayer.Draws.Select(d => (d.Sequence, d.Card, Player: firstPlayer))
                    .Concat(secondPlayer.Draws.Select(d => (d.Sequence, d.Card, Player: secondPlayer)))
                    .OrderBy(d => d.Sequence)
                    .ToList();

                // Draws always happen in pairs (winner then loser) with no gaps.
                Assert.Equal(0, draws.Count % 2);
                for (var s = 0; s < draws.Count; s++)
                {
                    Assert.Equal(s + 1, draws[s].Sequence);
                }

                for (var pair = 0; pair < draws.Count / 2; pair++)
                {
                    // Both players see EndTurn for every trick, so context index == trick index,
                    // and the draw pair for trick t happens right after that trick.
                    var context = firstPlayer.EndTurnContexts[pair];
                    if (context.SecondPlayedCard == null)
                    {
                        // The leader went out by announce: no card contest to derive a winner from.
                        continue;
                    }

                    var winnerPosition = CardWinnerLogic.GetWinner(
                        context.FirstPlayedCard,
                        context.SecondPlayedCard,
                        trumpSuit);
                    var firstPlayerLed = firstPlayer.LedByTrick.TryGetValue(pair, out var led) && led;
                    var leaderWonTrick = winnerPosition == PlayerPosition.FirstPlayer;
                    var trickWinner = leaderWonTrick == firstPlayerLed ? firstPlayer : secondPlayer;

                    Assert.Same(trickWinner, draws[2 * pair].Player);
                    pairsChecked++;
                }

                if (draws.Count == 12)
                {
                    // Talon fully exhausted: the last card drawn (by the loser of the sixth
                    // trick) must be the face-up trump card.
                    fullTalonRounds++;
                    Assert.Equal(firstPlayer.TrumpCardAtRoundStart, draws[11].Card);
                }
            }

            Assert.True(pairsChecked > 0, "expected at least one draw pair with a card-decided trick");
            Assert.True(fullTalonRounds > 0, "expected at least one round with a fully exhausted talon");
        }

        [Fact]
        public void DrawnCardsShouldComeOffTheTalonInOrderWinnerFirstAndTheTrumpLast()
        {
            // A stacked deal with a known talon: every draw must take the next talon card, the
            // trick winner first, and the twelfth draw must be the face-up trump. Every King and
            // Queen pair is split between the hands and the talon holds none, so no announce can
            // end the round early; with these hands nobody reaches 66 in the first six tricks
            // (61 - 6), so all twelve draws happen.
            const string Talon = "9C JC 9D JD 9H JH 10C 10D 10H 10S JS";
            var shuffle = StackedDeal.Create("AS", "AC AD KC KD KH QS", "AH 9S KS QC QD QH", Talon);
            var sharedSequence = new int[1];
            var firstPlayer = new DrawRecordingPlayer(sharedSequence);
            var secondPlayer = new DrawRecordingPlayer(sharedSequence);
            var round = new Round(firstPlayer, secondPlayer, GameRulesProvider.Santase, PlayerPosition.FirstPlayer, shuffle);

            round.Start(0, 0);
            for (var trick = 0; trick < 6 && !round.IsFinished; trick++)
            {
                RoundTestDriver.PlayTrick(round);
            }

            Assert.False(round.IsFinished);
            Assert.Equal(61, round.FirstPlayer.RoundPoints);
            Assert.Equal(6, round.SecondPlayer.RoundPoints);
            Assert.Equal(0, round.Deck.CardsLeft);

            var draws = firstPlayer.Draws.Select(d => (d.Sequence, d.Card))
                .Concat(secondPlayer.Draws.Select(d => (d.Sequence, d.Card)))
                .OrderBy(d => d.Sequence)
                .Select(d => d.Card)
                .ToList();
            Assert.Equal(TestCards.List(Talon + " AS"), draws);
        }

        private sealed class DrawRecordingPlayer : BasePlayer
        {
            private readonly int[] sharedSequence;

            public DrawRecordingPlayer(int[] sharedSequence)
            {
                this.sharedSequence = sharedSequence;
            }

            public override string Name => "Draw recording player";

            public List<(int Sequence, Card Card)> Draws { get; } = new List<(int Sequence, Card Card)>();

            public Dictionary<int, bool> LedByTrick { get; } = new Dictionary<int, bool>();

            public List<PlayerTurnContext> EndTurnContexts { get; } = new List<PlayerTurnContext>();

            public Card TrumpCardAtRoundStart { get; private set; }

            public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
            {
                this.TrumpCardAtRoundStart = trumpCard;
                base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            }

            public override void AddCard(Card card)
            {
                this.sharedSequence[0]++;
                this.Draws.Add((this.sharedSequence[0], card));
                base.AddCard(card);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.LedByTrick[this.EndTurnContexts.Count] = context.IsFirstPlayerTurn;
                var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(possibleCardsToPlay.First());
            }

            public override void EndTurn(PlayerTurnContext context)
            {
                this.EndTurnContexts.Add(context.DeepClone());
                base.EndTurn(context);
            }
        }
    }
}
