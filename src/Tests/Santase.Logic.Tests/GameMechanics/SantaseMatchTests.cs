namespace Santase.Logic.Tests.GameMechanics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    using Xunit;

    // The step-by-step API a game server uses: nothing waits for a player; the caller reads
    // ToMove and passes that player's move to Act.
    public class SantaseMatchTests
    {
        private const string FirstHand = "9S AC KC QC JC 10C";

        private const string SecondHand = "9D JD QD KD 10D AD";

        [Fact]
        public void NobodyShouldBeToMoveBeforeStart()
        {
            var match = new SantaseMatch();

            Assert.Equal(PlayerPosition.NoOne, match.ToMove);
            Assert.False(match.IsFinished);
            Assert.Equal(PlayerPosition.NoOne, match.Winner);
            Assert.Throws<InvalidOperationException>(() => match.CreateTurnContext());
            Assert.Throws<InvalidOperationException>(() => match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame()));
        }

        [Fact]
        public void StartShouldTellBothObserversAndDealTheFirstRound()
        {
            var log = new List<string>();
            var first = new RecordingPlayer("A", log);
            var second = new RecordingPlayer("B", log);
            var match = new SantaseMatch(first, second, new SantaseMatchOptions { FirstToPlay = PlayerPosition.SecondPlayer, Shuffle = StackedDeal.Create("AS", FirstHand, SecondHand) });

            match.Start();

            Assert.Equal(
                new[]
                {
                    "A StartGame B",
                    "B StartGame A",
                    "A StartRound 9S AC KC QC JC 10C / AS 0-0",
                    "B StartRound 9D JD QD KD 10D AD / AS 0-0",
                },
                log);
            Assert.Equal(PlayerPosition.SecondPlayer, match.ToMove);
            Assert.True(match.CreateTurnContext().IsFirstPlayerTurn);
        }

        [Fact]
        public void StartingTwiceShouldThrow()
        {
            var match = new SantaseMatch();
            match.Start();

            Assert.Throws<InvalidOperationException>(() => match.Start());
        }

        [Fact]
        public void OptionsShouldRejectNoOneAsTheFirstToPlay()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SantaseMatch(new SantaseMatchOptions { FirstToPlay = PlayerPosition.NoOne }));
        }

        [Fact]
        public void ActingOutOfTurnShouldBeRefusedWithoutChangingAnything()
        {
            var match = StartStacked();

            Assert.Equal(SantaseActResult.NotYourTurn, match.Act(PlayerPosition.SecondPlayer, Play("9D")));
            Assert.Equal(SantaseActResult.NotYourTurn, match.Act(PlayerPosition.NoOne, Play("AC")));

            Assert.Equal(PlayerPosition.FirstPlayer, match.ToMove);
            Assert.Equal(6, match.CurrentRound.SecondPlayer.Cards.Count);
            Assert.Null(match.CreateTurnContext().FirstPlayedCard);
        }

        [Fact]
        public void AnIllegalActionShouldBeRefusedWithoutChangingAnything()
        {
            var match = StartStacked();

            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.FirstPlayer, Play("9D"))); // not in hand
            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.FirstPlayer, null));
            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame())); // first trick
            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.FirstPlayer, PlayerAction.ChangeTrump())); // first trick

            Assert.Equal(PlayerPosition.FirstPlayer, match.ToMove);
            Assert.Equal(TestCards.List(FirstHand).OrderBy(c => c.GetHashCode()), match.CurrentRound.FirstPlayer.Cards);
            Assert.Equal(TestCards.Parse("AS"), match.CreateTurnContext().TrumpCard);
        }

        [Fact]
        public void TheFollowerCanOnlyPlayACard()
        {
            var match = StartStacked();
            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.FirstPlayer, Play("AC")));
            Assert.Equal(PlayerPosition.SecondPlayer, match.ToMove);

            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.SecondPlayer, PlayerAction.ChangeTrump()));
            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.SecondPlayer, PlayerAction.CloseGame()));
            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.SecondPlayer, Play("9D")));
        }

        [Fact]
        public void ChangingTheTrumpAndClosingShouldKeepTheLeaderToMove()
        {
            var match = StartStacked();

            // Trick 1 (no swaps or closes yet): AC beats the off-suit 9D, so the first player leads again.
            match.Act(PlayerPosition.FirstPlayer, Play("AC"));
            match.Act(PlayerPosition.SecondPlayer, Play("9D"));
            Assert.Equal(PlayerPosition.FirstPlayer, match.ToMove);

            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.FirstPlayer, PlayerAction.ChangeTrump()));
            Assert.Equal(PlayerPosition.FirstPlayer, match.ToMove);
            Assert.Equal(TestCards.Parse("9S"), match.CreateTurnContext().TrumpCard);
            Assert.Contains(TestCards.Parse("AS"), match.CurrentRound.FirstPlayer.Cards);
            Assert.DoesNotContain(TestCards.Parse("9S"), match.CurrentRound.FirstPlayer.Cards);

            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame()));
            Assert.Equal(PlayerPosition.FirstPlayer, match.ToMove);
            Assert.True(match.CreateTurnContext().State.ShouldObserveRules);
            Assert.Equal(SantaseActResult.InvalidAction, match.Act(PlayerPosition.FirstPlayer, PlayerAction.CloseGame()));

            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.FirstPlayer, Play("KC")));
            Assert.Equal(PlayerPosition.SecondPlayer, match.ToMove);
            Assert.Equal(Announce.Twenty, match.CreateTurnContext().FirstPlayerAnnounce);
        }

        [Fact]
        public void TheReturnedTurnContextShouldBeACopy()
        {
            var match = StartStacked();

            var context = match.CreateTurnContext();
            context.FirstPlayedCard = TestCards.Parse("QD");
            context.TrumpCard = TestCards.Parse("9H");

            var fresh = match.CreateTurnContext();
            Assert.Null(fresh.FirstPlayedCard);
            Assert.Equal(TestCards.Parse("AS"), fresh.TrumpCard);
            Assert.Equal(SantaseActResult.Ok, match.Act(PlayerPosition.FirstPlayer, Play("AC")));
        }

        [Fact]
        public void AMatchPlayedThroughActWithoutObserversShouldFinishWithAWinner()
        {
            var random = new Random(5);
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
            match.Start();

            while (!match.IsFinished)
            {
                Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, RandomLegalAction(match, random)));
            }

            var winnerTotal = match.Winner == PlayerPosition.FirstPlayer ? match.FirstPlayerTotalPoints : match.SecondPlayerTotalPoints;
            var loserTotal = match.Winner == PlayerPosition.FirstPlayer ? match.SecondPlayerTotalPoints : match.FirstPlayerTotalPoints;
            Assert.InRange(winnerTotal, 11, 13);
            Assert.InRange(loserTotal, 0, 10);
            Assert.True(match.RoundsPlayed >= 4);

            Assert.Equal(PlayerPosition.NoOne, match.ToMove);
            Assert.Equal(SantaseActResult.MatchFinished, match.Act(match.Winner, PlayerAction.CloseGame()));
            Assert.Throws<InvalidOperationException>(() => match.CreateTurnContext());
        }

        [Fact]
        public void DrivingTheMatchByHandShouldReproduceSantaseGameExactly()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var gameLog = new List<string>();
                var game = new SantaseGame(
                    new RecordingPlayer("A", gameLog),
                    new RecordingPlayer("B", gameLog),
                    GameRulesProvider.Santase,
                    new NoLogger(),
                    new Random(seed).Next);
                var gameWinner = game.Start(seed % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);

                var matchLog = new List<string>();
                var first = new RecordingPlayer("A", matchLog);
                var second = new RecordingPlayer("B", matchLog);
                var options = new SantaseMatchOptions
                {
                    FirstToPlay = seed % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer,
                    Shuffle = new Random(seed).Next,
                };
                var match = new SantaseMatch(first, second, options);
                match.Start();
                while (!match.IsFinished)
                {
                    var player = match.ToMove == PlayerPosition.FirstPlayer ? first : second;
                    Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, player.GetTurn(match.CreateTurnContext())));
                }

                Assert.Equal(gameLog, matchLog);
                Assert.Equal(gameWinner, match.Winner);
                Assert.Equal(game.FirstPlayerTotalPoints, match.FirstPlayerTotalPoints);
                Assert.Equal(game.SecondPlayerTotalPoints, match.SecondPlayerTotalPoints);
                Assert.Equal(game.RoundsPlayed, match.RoundsPlayed);
            }
        }

        [Fact]
        public void TheLoserOfARoundShouldLeadTheNextOne()
        {
            var checkedRounds = 0;
            for (var seed = 0; seed < 50; seed++)
            {
                var random = new Random(seed);
                var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = random.Next });
                match.Start();
                var opener = match.ToMove;
                var roundsBefore = 0;
                var firstBefore = 0;
                var secondBefore = 0;
                while (!match.IsFinished)
                {
                    match.Act(match.ToMove, RandomLegalAction(match, random));
                    if (match.RoundsPlayed == roundsBefore || match.IsFinished)
                    {
                        continue;
                    }

                    var firstWon = match.FirstPlayerTotalPoints > firstBefore;
                    var secondWon = match.SecondPlayerTotalPoints > secondBefore;
                    var expectedOpener = firstWon ? PlayerPosition.SecondPlayer : (secondWon ? PlayerPosition.FirstPlayer : opener);
                    Assert.Equal(expectedOpener, match.ToMove);
                    checkedRounds++;

                    opener = match.ToMove;
                    roundsBefore = match.RoundsPlayed;
                    firstBefore = match.FirstPlayerTotalPoints;
                    secondBefore = match.SecondPlayerTotalPoints;
                }
            }

            Assert.True(checkedRounds > 100);
        }

        [Fact]
        public void TheShuffleSourceShouldDriveEveryDeal()
        {
            var random = new Random(9);
            var draws = 0;
            var match = new SantaseMatch(new SantaseMatchOptions
            {
                Shuffle = max =>
                {
                    draws++;
                    return random.Next(max);
                },
            });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, RandomLegalAction(match, random));
            }

            // 23 draws per deal; the last finished round did not deal a new one.
            Assert.Equal(23 * match.RoundsPlayed, draws);
        }

        [Fact]
        public void TheLoggerShouldGetOneLinePerRound()
        {
            var logger = new MemoryLogger();
            var random = new Random(3);
            var match = new SantaseMatch(new SantaseMatchOptions { Logger = logger, Shuffle = random.Next });
            match.Start();
            while (!match.IsFinished)
            {
                match.Act(match.ToMove, RandomLegalAction(match, random));
            }

            Assert.Equal(match.RoundsPlayed, logger.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact]
        public void CustomRulesShouldDecideWhenTheMatchEnds()
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var random = new Random(seed);
                var match = new SantaseMatch(new SantaseMatchOptions { Rules = new OneGamePointRules(), Shuffle = random.Next });
                match.Start();
                while (!match.IsFinished)
                {
                    match.Act(match.ToMove, RandomLegalAction(match, random));

                    // With a 1-point target any round that awards points ends the match, so
                    // while it runs every finished round must have been a draw.
                    if (!match.IsFinished)
                    {
                        Assert.Equal(0, match.FirstPlayerTotalPoints + match.SecondPlayerTotalPoints);
                    }
                }

                var winnerTotal = match.Winner == PlayerPosition.FirstPlayer ? match.FirstPlayerTotalPoints : match.SecondPlayerTotalPoints;
                var loserTotal = match.Winner == PlayerPosition.FirstPlayer ? match.SecondPlayerTotalPoints : match.FirstPlayerTotalPoints;
                Assert.InRange(winnerTotal, 1, 3);
                Assert.Equal(0, loserTotal);
            }
        }

        private static SantaseMatch StartStacked()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = StackedDeal.Create("AS", FirstHand, SecondHand) });
            match.Start();
            return match;
        }

        private static PlayerAction Play(string card)
        {
            return PlayerAction.PlayCard(TestCards.Parse(card));
        }

        // A legal move for whoever is to move: sometimes a trump swap or a close when allowed.
        private static PlayerAction RandomLegalAction(SantaseMatch match, Random random)
        {
            var hand = match.ToMove == PlayerPosition.FirstPlayer ? match.CurrentRound.FirstPlayer.Cards : match.CurrentRound.SecondPlayer.Cards;
            var context = match.CreateTurnContext();
            if (random.Next(2) == 0 && PlayerActionValidator.Instance.IsValid(PlayerAction.ChangeTrump(), context, hand))
            {
                return PlayerAction.ChangeTrump();
            }

            if (random.Next(10) == 0 && PlayerActionValidator.Instance.IsValid(PlayerAction.CloseGame(), context, hand))
            {
                return PlayerAction.CloseGame();
            }

            var cards = PlayerActionValidator.Instance.GetPossibleCardsToPlay(context, hand);
            return PlayerAction.PlayCard(cards.ElementAt(random.Next(cards.Count)));
        }

        private sealed class OneGamePointRules : IGameRules
        {
            public int RoundPointsForGoingOut => 66;

            public int HalfRoundPoints => 33;

            public int GamePointsNeededForWin => 1;

            public int CardsAtStartOfTheRound => 6;
        }

        // Deterministic player (swaps when it can, otherwise the lowest legal card) that logs
        // every callback it receives.
        private sealed class RecordingPlayer : BasePlayer
        {
            private readonly string tag;

            private readonly List<string> log;

            public RecordingPlayer(string tag, List<string> log)
            {
                this.tag = tag;
                this.log = log;
            }

            public override string Name => this.tag;

            public override void StartGame(string otherPlayerIdentifier)
            {
                this.log.Add($"{this.tag} StartGame {otherPlayerIdentifier}");
            }

            public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
            {
                this.log.Add($"{this.tag} StartRound {string.Join(" ", cards.Select(Code))} / {Code(trumpCard)} {myTotalPoints}-{opponentTotalPoints}");
                base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            }

            public override void AddCard(Card card)
            {
                this.log.Add($"{this.tag} AddCard {Code(card)}");
                base.AddCard(card);
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.log.Add($"{this.tag} GetTurn {Describe(context)}");
                if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
                {
                    return this.ChangeTrump(context.TrumpCard);
                }

                var cards = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
                return this.PlayCard(cards.First());
            }

            public override void EndTurn(PlayerTurnContext context)
            {
                this.log.Add($"{this.tag} EndTurn {Describe(context)}");
            }

            public override void EndRound()
            {
                this.log.Add($"{this.tag} EndRound");
            }

            public override void EndGame(bool amIWinner)
            {
                this.log.Add($"{this.tag} EndGame {amIWinner}");
            }

            private static string Code(Card card)
            {
                if (card == null)
                {
                    return "-";
                }

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

            private static string Describe(PlayerTurnContext c)
            {
                return $"{c.State.GetType().Name} {Code(c.TrumpCard)} {c.CardsLeftInDeck} {Code(c.FirstPlayedCard)} {c.FirstPlayerAnnounce} {Code(c.SecondPlayedCard)} {c.FirstPlayerRoundPoints}/{c.SecondPlayerRoundPoints}";
            }
        }
    }
}
