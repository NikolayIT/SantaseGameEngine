namespace Santase.Logic.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;
    using Santase.Logic.WinnerLogic;

    /// <summary>
    /// One round (one deal) of Santase as a step-by-step state machine. The round never asks a
    /// player for a move: whoever drives it reads <see cref="ToMove"/> and passes that player's
    /// action to <see cref="TryAct"/>, which advances the round up to the next decision. Every
    /// other callback (StartRound, AddCard, EndTurn, EndRound) goes to the seats' observers, in
    /// the same order the engine has always used.
    /// </summary>
    internal sealed class Round
    {
        private readonly IGameRules gameRules;

        private readonly IDeck deck;

        private readonly IStateManager stateManager;

        private readonly RoundPlayerInfo firstPlayer;

        private readonly RoundPlayerInfo secondPlayer;

        // The player who leads the current trick.
        private PlayerPosition leader;

        private PlayerPosition lastTrickWinner;

        // The current trick as the players see it; null before the deal and after the round.
        private PlayerTurnContext context;

        private bool waitingForFollower;

        public Round(
            IPlayer firstPlayer,
            IPlayer secondPlayer,
            IGameRules gameRules,
            PlayerPosition firstToPlay = PlayerPosition.FirstPlayer,
            Func<int, int> shuffle = null)
            : this(
                new RoundPlayerInfo(firstPlayer),
                new RoundPlayerInfo(secondPlayer),
                new Deck(shuffle),
                new StateManager(),
                gameRules,
                firstToPlay)
        {
        }

        // Builds a round around prepared parts: hands, won tricks, the talon and the phase. Call
        // Start to deal from the talon, or Continue to go on from the prepared position (tests
        // use that to start from a mid-round situation).
        internal Round(
            RoundPlayerInfo firstPlayer,
            RoundPlayerInfo secondPlayer,
            IDeck deck,
            IStateManager stateManager,
            IGameRules gameRules,
            PlayerPosition firstToPlay)
        {
            if (firstToPlay != PlayerPosition.FirstPlayer && firstToPlay != PlayerPosition.SecondPlayer)
            {
                throw new ArgumentOutOfRangeException(nameof(firstToPlay), firstToPlay, "A round is led by the first or the second player.");
            }

            this.firstPlayer = firstPlayer;
            this.secondPlayer = secondPlayer;
            this.deck = deck;
            this.stateManager = stateManager;
            this.gameRules = gameRules;
            this.leader = firstToPlay;
            this.lastTrickWinner = PlayerPosition.NoOne;
        }

        public RoundPlayerInfo FirstPlayer => this.firstPlayer;

        public RoundPlayerInfo SecondPlayer => this.secondPlayer;

        public IDeck Deck => this.deck;

        public IStateManager StateManager => this.stateManager;

        public bool IsFinished { get; private set; }

        // Tricks finished so far this round (a lead that took the leader out counts as one).
        public int TricksPlayed { get; private set; }

        // Who leads the current trick; after the last trick, its winner.
        public PlayerPosition Leader => this.leader;

        // Set once the round is finished.
        public RoundResult Result { get; private set; }

        // Who must act now: the leader until they play a card, then the follower. NoOne before
        // the deal and after the round.
        public PlayerPosition ToMove =>
            this.context == null
                ? PlayerPosition.NoOne
                : (this.waitingForFollower ? Other(this.leader) : this.leader);

        // Deals the starting hands (first player, then second) and waits for the first lead.
        public void Start(int firstPlayerTotalPoints, int secondPlayerTotalPoints)
        {
            this.DealAndStartRound(this.firstPlayer, firstPlayerTotalPoints, secondPlayerTotalPoints);
            this.DealAndStartRound(this.secondPlayer, secondPlayerTotalPoints, firstPlayerTotalPoints);
            this.Continue();
        }

        // Starts the next trick, or finishes the round when a player has enough points or both
        // hands are empty.
        public void Continue()
        {
            if (this.firstPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut
                || this.secondPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut
                || (this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0))
            {
                this.Finish();
                return;
            }

            this.context = new PlayerTurnContext(
                this.stateManager.State,
                this.deck.TrumpCard,
                this.deck.CardsLeft,
                this.Info(this.leader).RoundPoints,
                this.Info(Other(this.leader)).RoundPoints);
        }

        // A copy of the current trick context for the player to move (what GetTurn receives).
        public PlayerTurnContext CreateTurnContext()
        {
            if (this.context == null)
            {
                throw new InvalidOperationException("Nobody is to move in this round.");
            }

            return this.context.DeepClone();
        }

        // Applies the action of the player to move (see ToMove). Returns false, changing nothing,
        // when the action is not legal for them now.
        public bool TryAct(PlayerAction action)
        {
            if (this.context == null)
            {
                throw new InvalidOperationException("Nobody is to move in this round.");
            }

            return this.waitingForFollower ? this.FollowerActs(action) : this.LeaderActs(action);
        }

        private static PlayerPosition Other(PlayerPosition position)
        {
            return position == PlayerPosition.FirstPlayer ? PlayerPosition.SecondPlayer : PlayerPosition.FirstPlayer;
        }

        private RoundPlayerInfo Info(PlayerPosition position)
        {
            return position == PlayerPosition.FirstPlayer ? this.firstPlayer : this.secondPlayer;
        }

        private bool LeaderActs(PlayerAction action)
        {
            var leaderInfo = this.Info(this.leader);
            if (!PlayerActionValidator.Instance.IsValid(action, this.context, leaderInfo.Cards))
            {
                return false;
            }

            switch (action.Type)
            {
                case PlayerActionType.ChangeTrump:
                    {
                        // The leader swaps the Nine of trumps for the face-up trump card and leads again.
                        var oldTrumpCard = this.deck.TrumpCard;
                        var nineOfTrump = Card.GetCard(oldTrumpCard.Suit, CardType.Nine);
                        this.deck.ChangeTrumpCard(nineOfTrump);
                        this.context.TrumpCard = nineOfTrump;
                        leaderInfo.Cards.Remove(nineOfTrump);
                        leaderInfo.Cards.Add(oldTrumpCard);
                        return true;
                    }

                case PlayerActionType.CloseGame:
                    {
                        // The leader closes the talon and leads again.
                        this.stateManager.State.Close();
                        this.context.State = this.stateManager.State;
                        leaderInfo.GameCloser = true;
                        return true;
                    }
            }

            // PlayCard. The validator has already decided the announce for this lead.
            if (action.Announce != Announce.None)
            {
                leaderInfo.AddAnnounce(action.Announce);
            }

            this.context.FirstPlayedCard = action.Card;
            this.context.FirstPlayerAnnounce = action.Announce;
            var leaderRoundPoints = leaderInfo.RoundPoints;
            this.context.FirstPlayerRoundPoints = leaderRoundPoints;
            leaderInfo.Cards.Remove(action.Card);

            if (leaderRoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                // The announce took the leader to the target: the round ends before the follower
                // plays. Both players are told about the lead, and the leader counts as the trick
                // winner (for who draws first and who leads next).
                leaderInfo.Player?.EndTurn(this.context);
                this.Info(Other(this.leader)).Player?.EndTurn(this.context);
                this.CompleteTrick(this.leader);
                return true;
            }

            this.waitingForFollower = true;
            return true;
        }

        private bool FollowerActs(PlayerAction action)
        {
            var leaderInfo = this.Info(this.leader);
            var followerInfo = this.Info(Other(this.leader));
            if (!PlayerActionValidator.Instance.IsValid(action, this.context, followerInfo.Cards))
            {
                return false;
            }

            var ledCard = this.context.FirstPlayedCard;
            this.context.SecondPlayedCard = action.Card;
            followerInfo.Cards.Remove(action.Card);

            var winner = CardWinnerLogic.GetWinner(ledCard, action.Card, this.deck.TrumpCard.Suit) == PlayerPosition.FirstPlayer
                ? this.leader
                : Other(this.leader);
            var winnerInfo = this.Info(winner);
            winnerInfo.WinCard(ledCard);
            winnerInfo.WinCard(action.Card);

            this.context.FirstPlayerRoundPoints = leaderInfo.RoundPoints;
            this.context.SecondPlayerRoundPoints = followerInfo.RoundPoints;
            leaderInfo.Player?.EndTurn(this.context);
            followerInfo.Player?.EndTurn(this.context);

            this.waitingForFollower = false;
            this.CompleteTrick(winner);
            return true;
        }

        private void CompleteTrick(PlayerPosition trickWinner)
        {
            // The trick winner leads next and, while the talon is open, draws first.
            this.leader = trickWinner;
            this.lastTrickWinner = trickWinner;
            this.waitingForFollower = false;
            this.TricksPlayed++;

            if (this.stateManager.State.ShouldDrawCard)
            {
                this.GiveCardToPlayer(this.Info(trickWinner));
                this.GiveCardToPlayer(this.Info(Other(trickWinner)));
            }

            this.stateManager.State.PlayHand(this.deck.CardsLeft);
            this.Continue();
        }

        private void Finish()
        {
            this.context = null;
            this.IsFinished = true;

            this.firstPlayer.Player?.EndRound();
            this.secondPlayer.Player?.EndRound();

            // The +10 last-trick bonus is only earned when both players exhausted their
            // cards, i.e. the round played all the way to the end. If either hand still
            // has cards (someone reached 66 from card values mid-round, or via an
            // announce-to-66 mid-trick), surface NoOne so scoring skips the bonus.
            // The reverse case (66 reached *on* the last trick from card values) has
            // both hands empty, so the bonus correctly applies.
            var bothHandsEmpty = this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0;
            var lastTrickWinnerForBonus = bothHandsEmpty ? this.lastTrickWinner : PlayerPosition.NoOne;

            this.Result = new RoundResult(this.firstPlayer, this.secondPlayer, lastTrickWinnerForBonus);
        }

        private void DealAndStartRound(RoundPlayerInfo player, int playerTotalPoints, int opponentTotalPoints)
        {
            var count = this.gameRules.CardsAtStartOfTheRound;
            var cards = new List<Card>(count);
            for (var i = 0; i < count; i++)
            {
                var card = this.deck.GetNextCard();
                cards.Add(card);
                player.Cards.Add(card);
            }

            player.Player?.StartRound(cards, this.deck.TrumpCard, playerTotalPoints, opponentTotalPoints);
        }

        private void GiveCardToPlayer(RoundPlayerInfo player)
        {
            player.AddCard(this.deck.GetNextCard());
        }
    }
}
