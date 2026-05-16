namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    public enum PlayerSlot
    {
        First = 1,
        Second = 2,
    }

    public sealed class TrickResult
    {
        public TrickResult(
            Card? firstCard,
            Card? secondCard,
            Announce firstAnnounce,
            int firstRoundPoints,
            int secondRoundPoints)
        {
            this.FirstCard = firstCard;
            this.SecondCard = secondCard;
            this.FirstAnnounce = firstAnnounce;
            this.FirstRoundPoints = firstRoundPoints;
            this.SecondRoundPoints = secondRoundPoints;
        }

        public Card? FirstCard { get; }

        public Card? SecondCard { get; }

        public Announce FirstAnnounce { get; }

        public int FirstRoundPoints { get; }

        public int SecondRoundPoints { get; }
    }

    public sealed class RoundEndInfo
    {
        public RoundEndInfo(int firstRoundPoints, int secondRoundPoints, int firstGamePoints, int secondGamePoints)
        {
            this.FirstRoundPoints = firstRoundPoints;
            this.SecondRoundPoints = secondRoundPoints;
            this.FirstGamePoints = firstGamePoints;
            this.SecondGamePoints = secondGamePoints;
        }

        public int FirstRoundPoints { get; }

        public int SecondRoundPoints { get; }

        public int FirstGamePoints { get; }

        public int SecondGamePoints { get; }
    }

    public sealed class GameSession
    {
        private const int DefaultTrickSettleMs = 900;

        private const int DefaultAiThinkMs = 400;

        private readonly object stateLock = new();

        private SantaseGame? game;

        private Task? gameTask;

        private int turnEndedCount;

        private int roundEndedCount;

        private int lastFirstRoundPoints;

        private int lastSecondRoundPoints;

        private TaskCompletionSource<object?>? pendingContinue;

        private Card? currentTrump;

        // PlayerTurnContext.FirstPlayer*/SecondPlayer* on the engine actually track the *trick
        // leader* and follower, not PlayerPosition.FirstPlayer/SecondPlayer of the game (Round
        // re-orders Trick's args by the previous trick's winner). We latch the leader's slot on
        // the first TurnAboutToStart of each trick so HandleTurnEndedFromObserver can translate
        // engine values into slot-stable ones before publishing TrickResult / RoundEndInfo.
        private PlayerSlot? currentTrickLeader;

        private bool announceShownThisTrick;

        public GameSession(GameMode mode, string firstPlayerName, string secondPlayerName)
        {
            this.Mode = mode;
            this.FirstPlayerName = firstPlayerName;
            this.SecondPlayerName = secondPlayerName;

            this.FirstHuman = new HumanPlayer(firstPlayerName);
            IPlayer firstInner = this.FirstHuman;

            IPlayer secondInner;
            switch (mode)
            {
                case GameMode.VsEasy:
                    secondInner = new DummyPlayerChangingTrump();
                    break;
                case GameMode.VsHard:
                    secondInner = new SmartPlayer();
                    break;
                case GameMode.HotSeat:
                    this.SecondHuman = new HumanPlayer(secondPlayerName);
                    secondInner = this.SecondHuman;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            this.FirstObserver = new PlayerObserver(firstInner) { ThinkDelayMs = DefaultAiThinkMs };
            this.SecondObserver = new PlayerObserver(secondInner) { ThinkDelayMs = DefaultAiThinkMs };

            this.WireObservers();

            if (this.FirstHuman != null)
            {
                this.FirstHuman.TurnRequested += this.OnHumanTurnRequested;
            }

            if (this.SecondHuman != null)
            {
                this.SecondHuman.TurnRequested += this.OnHumanTurnRequested;
            }
        }

        public GameMode Mode { get; }

        public string FirstPlayerName { get; }

        public string SecondPlayerName { get; }

        public HumanPlayer? FirstHuman { get; }

        public HumanPlayer? SecondHuman { get; }

        public PlayerObserver FirstObserver { get; }

        public PlayerObserver SecondObserver { get; }

        public int TrickSettleMs { get; set; } = DefaultTrickSettleMs;

        public int FirstGamePoints => this.game?.FirstPlayerTotalPoints ?? 0;

        public int SecondGamePoints => this.game?.SecondPlayerTotalPoints ?? 0;

        public IPlayerActionValidator ActionValidator => PlayerActionValidator.Instance;

        public IAnnounceValidator AnnounceValidator => Santase.Logic.PlayerActionValidate.AnnounceValidator.Instance;

        public bool IsRunning { get; private set; }

        public Task? GameTask => this.gameTask;

        // Fires once when Start() is called.
        public event Action? GameStarting;

        // Fires once when the game ends. Argument: which slot won.
        public event Action<PlayerSlot>? GameOver;

        // Fires when the engine thread crashes with an unexpected exception.
        public event Action<Exception>? GameError;

        // Fires once at the start of each round, after both players' StartRound has run.
        public event Action<int, int, Card>? RoundStarting;

        // Fires when a hand is initialized at the start of a round (one event per player).
        public event Action<PlayerSlot, IReadOnlyList<Card>>? PlayerHandInitialized;

        // Fires when a player draws a card from the deck after a trick.
        public event Action<PlayerSlot, Card>? CardDealtToPlayer;

        // Fires when an observer's GetTurn begins. isHuman tells the UI whether to expect input.
        public event Action<PlayerSlot, bool>? TurnStarting;

        // Fires when a HumanPlayer is awaiting input. UI should enable card tap on the human's hand.
        public event Action<PlayerSlot, HumanPlayer, PlayerTurnContext>? HumanInputRequested;

        // Fires after a player completes a PlayCard action.
        public event Action<PlayerSlot, Card>? CardPlayed;

        // Fires after a player swaps the trump 9 for the trump card.
        public event Action<PlayerSlot, Card>? TrumpCardSwapped;

        // Fires after a player closes the game.
        public event Action<PlayerSlot>? GameClosed;

        // Fires the moment a player leads a card with a 20/40 marriage. The engine has already
        // added the announce to that player's round points by this point, so the UI can show it
        // and bump the score immediately instead of waiting for the trick to settle.
        public event Action<PlayerSlot, Announce>? AnnouncementMade;

        // Fires after both players' EndTurn has run, so the trick is settled.
        public event Action<TrickResult>? TrickCompleted;

        // Fires after a round ends (both EndRound calls + game points updated).
        public event Action<RoundEndInfo>? RoundOver;

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            this.IsRunning = true;
            this.game = new SantaseGame(this.FirstObserver, this.SecondObserver);

            this.GameStarting?.Invoke();

            this.gameTask = Task.Run(() =>
            {
                try
                {
                    var winner = this.game.Start(PlayerPosition.FirstPlayer);
                    var winnerSlot = winner == PlayerPosition.FirstPlayer ? PlayerSlot.First : PlayerSlot.Second;
                    this.GameOver?.Invoke(winnerSlot);
                }
                catch (TaskCanceledException)
                {
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    this.GameError?.Invoke(ex);
                }
                finally
                {
                    this.IsRunning = false;
                }
            });
        }

        public void Stop()
        {
            this.FirstHuman?.CancelPendingTurn();
            this.SecondHuman?.CancelPendingTurn();
            this.pendingContinue?.TrySetCanceled();
            this.IsRunning = false;
        }

        public void Continue()
        {
            this.pendingContinue?.TrySetResult(null);
        }

        public void Restart()
        {
            // Wait for any in-flight game task to finish naturally (it will because GameOver
            // already fired). If it's stuck, Stop() unblocks pending TCSes.
            this.Stop();

            // Reset round/turn bookkeeping so the new game starts clean.
            lock (this.stateLock)
            {
                this.turnEndedCount = 0;
                this.roundEndedCount = 0;
                this.lastFirstRoundPoints = 0;
                this.lastSecondRoundPoints = 0;
                this.currentTrump = null;
                this.currentTrickLeader = null;
                this.announceShownThisTrick = false;
            }

            this.pendingContinue = null;
            this.gameTask = null;
            this.game = null;

            this.Start();
        }

        public bool SubmitPlayCard(HumanPlayer player, Card card)
        {
            return player.TrySubmit(PlayerAction.PlayCard(card));
        }

        public bool SubmitChangeTrump(HumanPlayer player)
        {
            return player.TrySubmit(PlayerAction.ChangeTrump());
        }

        public bool SubmitCloseGame(HumanPlayer player)
        {
            return player.TrySubmit(PlayerAction.CloseGame());
        }

        public bool IsHumanSlot(PlayerSlot slot)
        {
            return slot == PlayerSlot.First ? this.FirstHuman != null : this.SecondHuman != null;
        }

        public HumanPlayer? GetHuman(PlayerSlot slot)
        {
            return slot == PlayerSlot.First ? this.FirstHuman : this.SecondHuman;
        }

        public string GetName(PlayerSlot slot)
        {
            return slot == PlayerSlot.First ? this.FirstPlayerName : this.SecondPlayerName;
        }

        private void WireObservers()
        {
            this.FirstObserver.RoundStarted += (cards, trump, my, opp) =>
            {
                lock (this.stateLock)
                {
                    this.turnEndedCount = 0;
                    this.roundEndedCount = 0;
                    this.lastFirstRoundPoints = 0;
                    this.lastSecondRoundPoints = 0;
                    this.currentTrump = trump;
                    this.currentTrickLeader = null;
                    this.announceShownThisTrick = false;
                }

                this.RoundStarting?.Invoke(my, opp, trump);
                this.PlayerHandInitialized?.Invoke(PlayerSlot.First, cards.ToList());
            };

            this.SecondObserver.RoundStarted += (cards, trump, my, opp) =>
            {
                this.PlayerHandInitialized?.Invoke(PlayerSlot.Second, cards.ToList());
            };

            this.FirstObserver.CardAdded += card => this.CardDealtToPlayer?.Invoke(PlayerSlot.First, card);
            this.SecondObserver.CardAdded += card => this.CardDealtToPlayer?.Invoke(PlayerSlot.Second, card);

            this.FirstObserver.TurnAboutToStart += context =>
            {
                lock (this.stateLock)
                {
                    this.currentTrickLeader ??= PlayerSlot.First;
                }

                // When the follower's turn starts, the leader has already led; if that lead was
                // a marriage the engine has set context.FirstPlayerAnnounce. Surface it now.
                this.TryEmitAnnounce(context);
                this.TurnStarting?.Invoke(PlayerSlot.First, this.FirstHuman != null);
            };
            this.SecondObserver.TurnAboutToStart += context =>
            {
                lock (this.stateLock)
                {
                    this.currentTrickLeader ??= PlayerSlot.Second;
                }

                this.TryEmitAnnounce(context);
                this.TurnStarting?.Invoke(PlayerSlot.Second, this.SecondHuman != null);
            };

            this.FirstObserver.TurnCompleted += action => this.OnTurnCompleted(PlayerSlot.First, action);
            this.SecondObserver.TurnCompleted += action => this.OnTurnCompleted(PlayerSlot.Second, action);

            this.FirstObserver.TurnEnded += this.HandleTurnEndedFromObserver;
            this.SecondObserver.TurnEnded += this.HandleTurnEndedFromObserver;

            this.FirstObserver.RoundEnded += () => this.HandleRoundEndedFromObserver();
            this.SecondObserver.RoundEnded += () => this.HandleRoundEndedFromObserver();
        }

        private void OnHumanTurnRequested(HumanPlayer player, PlayerTurnContext context)
        {
            var slot = ReferenceEquals(player, this.FirstHuman) ? PlayerSlot.First : PlayerSlot.Second;
            this.HumanInputRequested?.Invoke(slot, player, context);
        }

        private void OnTurnCompleted(PlayerSlot slot, PlayerAction action)
        {
            switch (action.Type)
            {
                case PlayerActionType.PlayCard:
                    this.CardPlayed?.Invoke(slot, action.Card);
                    break;
                case PlayerActionType.ChangeTrump:
                    if (this.currentTrump != null)
                    {
                        var newTrump = Card.GetCard(this.currentTrump.Suit, CardType.Nine);
                        this.currentTrump = newTrump;
                        this.TrumpCardSwapped?.Invoke(slot, newTrump);
                    }

                    break;
                case PlayerActionType.CloseGame:
                    this.GameClosed?.Invoke(slot);
                    break;
            }
        }

        // Surfaces the trick leader's 20/40 marriage exactly once per trick. The engine sets
        // context.FirstPlayerAnnounce the moment the leader leads the marriage card, so the
        // follower's turn-start context (or, if the leader announced 40 and went straight out,
        // the end-of-turn context) carries it. AnnouncementMade is raised outside the lock.
        private void TryEmitAnnounce(PlayerTurnContext context)
        {
            PlayerSlot leader;
            Announce announce;
            lock (this.stateLock)
            {
                if (this.announceShownThisTrick
                    || context.FirstPlayedCard == null
                    || context.FirstPlayerAnnounce == Announce.None
                    || this.currentTrickLeader == null)
                {
                    return;
                }

                this.announceShownThisTrick = true;
                leader = this.currentTrickLeader.Value;
                announce = context.FirstPlayerAnnounce;
            }

            this.AnnouncementMade?.Invoke(leader, announce);
        }

        private void HandleTurnEndedFromObserver(PlayerTurnContext context)
        {
            // Catches the leader-announces-40-and-goes-out case, where the follower's
            // TurnAboutToStart never fires. No-op if already surfaced this trick.
            this.TryEmitAnnounce(context);

            int n;
            int slotFirstPoints;
            int slotSecondPoints;
            Card? slotFirstCard;
            Card? slotSecondCard;
            lock (this.stateLock)
            {
                this.turnEndedCount++;
                n = this.turnEndedCount;

                // Translate engine "leader / follower" values into stable slot-1 / slot-2 values.
                // If TurnAboutToStart never set the leader (shouldn't happen in practice), default
                // to slot 1 so we don't crash.
                var leaderIsFirst = (this.currentTrickLeader ?? PlayerSlot.First) == PlayerSlot.First;
                if (leaderIsFirst)
                {
                    slotFirstPoints = context.FirstPlayerRoundPoints;
                    slotSecondPoints = context.SecondPlayerRoundPoints;
                    slotFirstCard = context.FirstPlayedCard;
                    slotSecondCard = context.SecondPlayedCard;
                }
                else
                {
                    slotFirstPoints = context.SecondPlayerRoundPoints;
                    slotSecondPoints = context.FirstPlayerRoundPoints;
                    slotFirstCard = context.SecondPlayedCard;
                    slotSecondCard = context.FirstPlayedCard;
                }

                this.lastFirstRoundPoints = slotFirstPoints;
                this.lastSecondRoundPoints = slotSecondPoints;
            }

            if (n < 2)
            {
                return;
            }

            lock (this.stateLock)
            {
                this.turnEndedCount = 0;
                this.currentTrickLeader = null;
                this.announceShownThisTrick = false;
            }

            var trick = new TrickResult(
                slotFirstCard,
                slotSecondCard,
                context.FirstPlayerAnnounce,
                slotFirstPoints,
                slotSecondPoints);

            this.TrickCompleted?.Invoke(trick);

            if (this.TrickSettleMs > 0)
            {
                Thread.Sleep(this.TrickSettleMs);
            }
        }

        private void HandleRoundEndedFromObserver()
        {
            int n;
            lock (this.stateLock)
            {
                this.roundEndedCount++;
                n = this.roundEndedCount;
            }

            if (n < 2)
            {
                return;
            }

            lock (this.stateLock)
            {
                this.roundEndedCount = 0;
            }

            // Game points haven't been updated yet (UpdatePoints runs AFTER both EndRound calls
            // return). The overlay shows round-points only; game-point badges refresh when the
            // next round starts (or via GameOver if the game is finished).
            var info = new RoundEndInfo(
                this.lastFirstRoundPoints,
                this.lastSecondRoundPoints,
                this.FirstGamePoints,
                this.SecondGamePoints);

            this.pendingContinue = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.RoundOver?.Invoke(info);

            try
            {
                this.pendingContinue.Task.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this.pendingContinue = null;
            }
        }
    }
}
