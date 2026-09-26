namespace Santase.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.UI.Game;
    using Santase.UI.Localization;

    using Xunit;

    // Plays games at the game table (GameViewModel) the way people do: only through its commands,
    // looking only at what it shows. At every decision it checks the screen against the seat's
    // view: the hand in display order, the playable cards and options, the opponent's card count,
    // trump, talon, points, the card on the table and the last trick. It also checks every round
    // result and the game-over screen against the session's own results.
    internal sealed class TableTester
    {
        private static readonly LocalizationManager Loc = LocalizationManager.Instance;

        private readonly GameSession session;

        private readonly GameViewModel table;

        private readonly Random random;

        private TaskCompletionSource changed = NewSignal();

        public TableTester(GameSession session, GameViewModel table, int seed)
        {
            this.session = session;
            this.table = table;
            this.random = new Random(seed);

            // Subscribed after the table, so the table has already updated when these run.
            table.PropertyChanged += (_, _) => this.Pulse();
            session.RoundStarted += this.Pulse;
            session.TurnStarted += (_, _) => this.Pulse();
            session.MovePlayed += _ =>
            {
                this.Moves++;
                this.Pulse();
            };
            session.TrickCollected += _ => this.Pulse();
            session.RoundFinished += round =>
            {
                this.Rounds.Add(round);
                this.Pulse();
            };
            session.GameOver += (winner, round) =>
            {
                this.Rounds.Add(round);
                this.Winner = winner;
                this.Pulse();
            };
            session.GameError += error =>
            {
                this.Errors.Add(error);
                this.Pulse();
            };
        }

        public int Moves { get; private set; }

        public int Decisions { get; private set; }

        public int Handoffs { get; private set; }

        public List<RoundEndInfo> Rounds { get; } = new();

        public PlayerSlot? Winner { get; private set; }

        public List<Exception> Errors { get; } = new();

        // Called while the hot-seat "pass the device" screen is up, with the seat taking over.
        public Action<PlayerSlot>? OnHandoff { get; set; }

        // Called at every decision, before the move, with the seat to move.
        public Action<PlayerSlot>? OnDecision { get; set; }

        // The seat the table shows at the bottom (names are distinct in these tests).
        public PlayerSlot ShownSlot => this.table.MyName == this.session.FirstPlayerName ? PlayerSlot.First : PlayerSlot.Second;

        // The order the table shows a hand in: spades, hearts, clubs, diamonds; A 10 K Q J 9.
        public static IEnumerable<Card> DisplayOrder(IEnumerable<Card> cards) => cards
            .OrderBy(c => c.Suit switch { CardSuit.Spade => 0, CardSuit.Heart => 1, CardSuit.Club => 2, _ => 3 })
            .ThenBy(c => c.Type switch { CardType.Ace => 0, CardType.Ten => 1, CardType.King => 2, CardType.Queen => 3, CardType.Jack => 4, _ => 5 });

        // Waits until the condition holds, re-checking whenever the table or the session changes.
        public async Task Until(Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (!condition())
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"The table never got to: {what}.");
                }

                var signal = this.changed;
                try
                {
                    await signal.Task.WaitAsync(remaining);
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException($"The table never got to: {what}.");
                }
            }
        }

        // Waits for the next thing to do at the table and does it; returns false once the game is over.
        public async Task<bool> StepAsync()
        {
            await this.Until(
                () => this.table.IsGameOverlayVisible || this.table.IsRoundOverlayVisible || this.table.IsHandoffOverlayVisible || this.table.IsMyTurn,
                "a decision, a result or the handoff screen");
            Assert.Empty(this.Errors);

            if (this.table.IsGameOverlayVisible)
            {
                this.CheckGameOver();
                return false;
            }

            if (this.table.IsRoundOverlayVisible)
            {
                this.CheckRoundResult(this.Rounds[^1]);
                this.table.RoundOverlayContinueCommand.Execute(null);
                Assert.False(this.table.IsRoundOverlayVisible);
                return true;
            }

            if (this.table.IsHandoffOverlayVisible)
            {
                this.Handoffs++;
                Assert.False(this.table.IsMyTurn);
                this.OnHandoff?.Invoke(GameSession.Other(this.ShownSlot));
                this.table.HandoffContinueCommand.Execute(null);
                Assert.False(this.table.IsHandoffOverlayVisible);
                return true;
            }

            this.Decisions++;
            var slot = this.ShownSlot;
            Assert.True(this.session.IsAwaitingMove(slot));
            this.CheckSeat(slot);
            this.OnDecision?.Invoke(slot);

            var before = this.Moves;
            this.MakeAMove();
            await this.Until(() => this.Moves > before, "the move being played");
            return true;
        }

        public async Task PlayToTheEndAsync()
        {
            while (await this.StepAsync())
            {
            }
        }

        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static int Mine(PlayerSlot slot, int first, int second) => slot == PlayerSlot.First ? first : second;

        private void Pulse()
        {
            var signal = this.changed;
            this.changed = NewSignal();
            signal.TrySetResult();
        }

        private void MakeAMove()
        {
            if (this.table.CanChangeTrump && this.random.Next(2) == 0)
            {
                this.table.ChangeTrumpCommand.Execute(null);
                return;
            }

            if (this.table.CanCloseGame && this.random.Next(8) == 0)
            {
                this.table.CloseGameCommand.Execute(null);
                return;
            }

            var playable = this.table.MyHand.Where(s => s.IsPlayable).ToList();
            this.table.TapCardCommand.Execute(playable[this.random.Next(playable.Count)]);
        }

        // Everything the table shows at a decision, against the seat's view.
        private void CheckSeat(PlayerSlot slot)
        {
            var view = this.session.GetView(slot)!;
            var seat = view.Seat;
            var other = GameSession.Other(slot);

            Assert.Equal(this.session.GetName(slot), this.table.MyName);
            Assert.Equal(this.session.GetName(other), this.table.OpponentName);

            Assert.Equal(DisplayOrder(view.Hand), this.table.MyHand.Select(s => s.Card));
            Assert.All(this.table.MyHand, s => Assert.False(s.IsFaceDown));
            Assert.Equal(view.PlayableCards.OrderBy(c => c.GetHashCode()), this.table.MyHand.Where(s => s.IsPlayable).Select(s => s.Card).OrderBy(c => c.GetHashCode()));
            Assert.Equal(view.CanChangeTrump, this.table.CanChangeTrump);
            Assert.Equal(view.CanClose, this.table.CanCloseGame);

            var opponentCards = slot == PlayerSlot.First ? view.SecondPlayerCardCount : view.FirstPlayerCardCount;
            Assert.Equal(opponentCards, this.table.OpponentHand.Count);
            Assert.Equal(opponentCards, this.table.OpponentCardsCount);
            Assert.All(this.table.OpponentHand, s => Assert.True(s.IsFaceDown));

            Assert.Equal(view.TrumpCard, this.table.TrumpCard);
            Assert.Equal(view.ClosedBy == PlayerPosition.NoOne ? view.CardsLeftInDeck : 0, this.table.DeckCount);
            Assert.Equal(view.ClosedBy == seat, this.table.GameClosedByMe);
            Assert.Equal(view.ClosedBy != PlayerPosition.NoOne && view.ClosedBy != seat, this.table.GameClosedByOpponent);

            Assert.Equal(Mine(slot, view.FirstPlayerRoundPoints, view.SecondPlayerRoundPoints), this.table.MyRoundPoints);
            Assert.Equal(Mine(other, view.FirstPlayerRoundPoints, view.SecondPlayerRoundPoints), this.table.OpponentRoundPoints);
            Assert.Equal(Mine(slot, view.FirstPlayerTotalPoints, view.SecondPlayerTotalPoints), this.table.MyGamePoints);
            Assert.Equal(Mine(other, view.FirstPlayerTotalPoints, view.SecondPlayerTotalPoints), this.table.OpponentGamePoints);

            // The card on the table is the opponent's lead, if they led.
            Assert.Null(this.table.MyPlayedCard);
            Assert.Equal(view.CurrentTrickLeadCard, this.table.OpponentPlayedCard?.Card);

            // The last-trick corner shows this round's last trick.
            if (view.Tricks.Count == 0)
            {
                Assert.False(this.table.HasLastTrick);
            }
            else
            {
                var last = view.LastTrick;
                var mine = last.Leader == seat ? last.LeadCard : last.FollowCard;
                var theirs = last.Leader == seat ? last.FollowCard : last.LeadCard;
                Assert.Equal(mine, this.table.LastTrickMyCard?.Card);
                Assert.Equal(theirs, this.table.LastTrickOpponentCard?.Card);
            }
        }

        private void CheckRoundResult(RoundEndInfo round)
        {
            var me = this.ShownSlot;
            var other = GameSession.Other(me);
            Assert.Equal(round.WinnerSlot == me, this.table.RoundIWon);
            Assert.Equal(round.WinnerSlot == other, this.table.RoundOpponentWon);
            Assert.Equal(Mine(me, round.FirstRoundPoints, round.SecondRoundPoints), this.table.MyRoundPoints);
            Assert.Equal(Mine(other, round.FirstRoundPoints, round.SecondRoundPoints), this.table.OpponentRoundPoints);
            Assert.Equal(Mine(me, round.FirstGamePoints, round.SecondGamePoints), this.table.MyGamePoints);
            Assert.Equal(Mine(other, round.FirstGamePoints, round.SecondGamePoints), this.table.OpponentGamePoints);
            var title = round.WinnerSlot == null
                ? Loc["Round_Draw"]
                : (round.WinnerSlot == me ? Loc["Round_YouWon"] : Loc.Format("Round_OppWon", this.table.OpponentName));
            Assert.Equal(title, this.table.RoundOverlayTitle);
        }

        private void CheckGameOver()
        {
            var winner = this.Winner!.Value;
            Assert.True(this.Rounds[^1].IsGameOver);
            Assert.False(this.table.IsMyTurn);
            Assert.False(this.table.IsRoundOverlayVisible);
            Assert.Equal(Loc.Format("GameOver_WonGame", this.session.GetName(winner)), this.table.GameOverlayBody);
            Assert.True(this.Rounds[^1].FirstGamePoints >= 11 || this.Rounds[^1].SecondGamePoints >= 11);
        }
    }
}
