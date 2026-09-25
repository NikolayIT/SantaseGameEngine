namespace Santase.Logic.Tests.Players
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;

    using Xunit;

    public class RestorablePlayerTests
    {
        [Fact]
        public void ChooseMoveShouldRestoreFromTheViewThenAskForTheTurn()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = new Random(3).Next });
            match.Start();
            var view = match.GetView(match.ToMove);
            var player = new FirstPlayableCardPlayer();

            var move = player.ChooseMove(view);

            Assert.Equal(new[] { "Restore", "GetTurn" }, player.Calls);
            Assert.Same(view, player.RestoredFrom);
            Assert.Equal(view.CurrentTrickLeadCard == null, player.Context.IsFirstPlayerTurn);
            Assert.Equal(view.TrumpCard, player.Context.TrumpCard);
            Assert.Equal(PlayerActionType.PlayCard, move.Type);
            Assert.Equal(view.PlayableCards[0], move.Card);

            // The played card left the restored hand, as in a live game.
            Assert.Equal(view.Hand.Count - 1, player.Hand.Count);
            Assert.DoesNotContain(move.Card, player.Hand);
        }

        [Fact]
        public void ChooseMoveShouldRefuseAViewWhoseSeatIsNotToMove()
        {
            var match = new SantaseMatch();
            match.Start();
            var waiting = match.ToMove == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
            var player = new FirstPlayableCardPlayer();

            Assert.Throws<InvalidOperationException>(() => player.ChooseMove(match.GetView(waiting)));
            Assert.Empty(player.Calls);
        }

        [Fact]
        public void ChooseMoveShouldRejectNulls()
        {
            var match = new SantaseMatch();
            match.Start();

            Assert.Throws<ArgumentNullException>(() => ((IRestorablePlayer)null).ChooseMove(match.GetView(match.ToMove)));
            Assert.Throws<ArgumentNullException>(() => new FirstPlayableCardPlayer().ChooseMove(null));
        }

        [Fact]
        public void RestoreHandShouldReplaceTheHandAndOnlyAcceptASeatView()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = new Random(5).Next });
            match.Start();
            var player = new FirstPlayableCardPlayer();
            player.StartRound(new List<Card> { Card.GetCard(CardSuit.Club, CardType.Ace) }, Card.GetCard(CardSuit.Heart, CardType.Nine), 0, 0);

            var view = match.GetView(PlayerPosition.SecondPlayer);
            player.Restore(view);
            Assert.Equal(view.Hand.OrderBy(c => c.GetHashCode()), player.Hand.OrderBy(c => c.GetHashCode()));

            while (!match.IsFinished)
            {
                match.Act(match.ToMove, PlayerAction.PlayCard(match.GetView(match.ToMove).PlayableCards[0]));
            }

            Assert.Throws<ArgumentException>(() => player.Restore(match.GetFinalView()));
            Assert.Throws<ArgumentNullException>(() => player.Restore(null));
        }

        [Fact]
        public void AMatchCanBePlayedWithANewPlayerForEveryMove()
        {
            var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = new Random(8).Next });
            match.Start();
            var moves = 0;
            while (!match.IsFinished)
            {
                Assert.Equal(SantaseActResult.Ok, match.Act(match.ToMove, new FirstPlayableCardPlayer().ChooseMove(match.GetView(match.ToMove))));
                moves++;
            }

            Assert.True(moves > 12);
            Assert.NotEqual(PlayerPosition.NoOne, match.Winner);
        }

        private sealed class FirstPlayableCardPlayer : BasePlayer, IRestorablePlayer
        {
            public override string Name => "First playable card";

            public List<string> Calls { get; } = new List<string>();

            public SantaseSeatView RestoredFrom { get; private set; }

            public PlayerTurnContext Context { get; private set; }

            public ICollection<Card> Hand => this.Cards;

            public void Restore(SantaseSeatView view)
            {
                this.RestoreHand(view);
                this.Calls.Add("Restore");
                this.RestoredFrom = view;
            }

            public override PlayerAction GetTurn(PlayerTurnContext context)
            {
                this.Calls.Add("GetTurn");
                this.Context = context;
                return this.PlayCard(this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards).First());
            }
        }
    }
}
