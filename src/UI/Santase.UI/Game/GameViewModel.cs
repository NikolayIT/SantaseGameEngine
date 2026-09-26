namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.UI.Localization;

    /// <summary>
    /// The game table. It shows one seat's view of the <see cref="GameSession"/> ("my" seat: the
    /// person in a game against the computer, the seat to move in a hot-seat game) and turns taps
    /// into moves. The session raises its events on the UI thread, so every handler here updates
    /// the screen directly; the host times toasts and hint highlights. No MAUI types here: the UI
    /// tests compile this file and play whole games through its commands.
    /// </summary>
    public sealed class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly LocalizationManager Loc = LocalizationManager.Instance;

        // What a face-down card is drawn with; only the count of the opponent's hand is shown.
        private static readonly Card FaceDown = Card.GetCard(CardSuit.Club, CardType.Nine);

        private static readonly TimeSpan NoticeDuration = TimeSpan.FromMilliseconds(2500);

        private readonly GameSession session;

        private readonly AiOpponent? opponent;

        private readonly IGameTableHost host;

        // Hot-seat: the seat that must take the device before it can move.
        private PlayerSlot? pendingHandoffSlot;

        private PlayerSlot mySlot = PlayerSlot.First;

        private string myName = string.Empty;

        private string opponentName = string.Empty;

        private int myGamePoints;

        private int opponentGamePoints;

        private int myRoundPoints;

        private int opponentRoundPoints;

        private int opponentCardsCount;

        private Card? trumpCard;

        private int deckCount = 12;

        private CardSlot? slot1PlayedCard;

        private CardSlot? slot2PlayedCard;

        private bool isMyTurn;

        private bool isHandoffOverlayVisible;

        private string handoffMessage = string.Empty;

        private bool isRoundOverlayVisible;

        private string roundOverlayTitle = string.Empty;

        private string roundOverlayIcon = string.Empty;

        private bool roundIWon;

        private bool roundOpponentWon;

        private bool isGameOverlayVisible;

        private string gameOverlayTitle = string.Empty;

        private string gameOverlayBody = string.Empty;

        private bool canChangeTrump;

        private bool canCloseGame;

        private string? toastMessage;

        private string statusMessage = string.Empty;

        private bool gameClosedByMe;

        private bool gameClosedByOpponent;

        private int matchWinsSlot1;

        private int matchWinsSlot2;

        private bool isRatingChangeVisible;

        private string ratingChangeText = string.Empty;

        private string roundAwardText = string.Empty;

        private string myAnnouncesText = string.Empty;

        private string opponentAnnouncesText = string.Empty;

        private bool hasAnnounces;

        private string gameOverlayIcon = "\U0001F3C6";

        private CardSlot? lastTrickSlot1Card;

        private CardSlot? lastTrickSlot2Card;

        public GameViewModel(GameSession session, AiOpponent? opponent, IGameTableHost host)
        {
            this.session = session;
            this.opponent = opponent;
            this.host = host;

            this.MyHand = new ObservableCollection<CardSlot>();
            this.OpponentHand = new ObservableCollection<CardSlot>();

            // Against the computer the person (slot 1) is always at the bottom. Hot-seat starts
            // with slot 1 and hands the device over when the other seat is to move.
            this.SetPerspective(PlayerSlot.First);

            this.TapCardCommand = new RelayCommand<CardSlot>(this.OnTapCard);
            this.ChangeTrumpCommand = new RelayCommand(this.OnChangeTrump, () => this.CanChangeTrump);
            this.CloseGameCommand = new RelayCommand(this.OnCloseGame, () => this.CanCloseGame);
            this.HintCommand = new RelayCommand(this.OnHint);
            this.HandoffContinueCommand = new RelayCommand(this.OnHandoffContinue);
            this.RoundOverlayContinueCommand = new RelayCommand(this.OnRoundOverlayContinue);
            this.PlayAgainCommand = new RelayCommand(this.OnPlayAgain);
            this.LeaveCommand = new RelayCommand(this.OnLeave);

            this.Subscribe();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private enum RoundOutcome
        {
            Won,
            Lost,
            Draw,
        }

        public ObservableCollection<CardSlot> MyHand { get; }

        public ObservableCollection<CardSlot> OpponentHand { get; }

        public string MyName
        {
            get => this.myName;
            private set => this.SetField(ref this.myName, value);
        }

        public string OpponentName
        {
            get => this.opponentName;
            private set => this.SetField(ref this.opponentName, value);
        }

        public int MyGamePoints
        {
            get => this.myGamePoints;
            private set => this.SetField(ref this.myGamePoints, value, nameof(this.MyGamePoints), nameof(this.MyGamePointsDisplay));
        }

        public int OpponentGamePoints
        {
            get => this.opponentGamePoints;
            private set => this.SetField(ref this.opponentGamePoints, value, nameof(this.OpponentGamePoints), nameof(this.OpponentGamePointsDisplay));
        }

        // The status-bar chips show the running game score against the 11-point target so new
        // players always see how far the race has to go.
        public string MyGamePointsDisplay => $"{this.MyGamePoints} / {this.session.GamePointsTarget}";

        public string OpponentGamePointsDisplay => $"{this.OpponentGamePoints} / {this.session.GamePointsTarget}";

        public int MyRoundPoints
        {
            get => this.myRoundPoints;
            private set => this.SetField(ref this.myRoundPoints, value);
        }

        public int OpponentRoundPoints
        {
            get => this.opponentRoundPoints;
            private set => this.SetField(ref this.opponentRoundPoints, value);
        }

        public int OpponentCardsCount
        {
            get => this.opponentCardsCount;
            private set => this.SetField(ref this.opponentCardsCount, value);
        }

        public Card? TrumpCard
        {
            get => this.trumpCard;
            private set => this.SetField(ref this.trumpCard, value, nameof(this.TrumpCard), nameof(this.TrumpImage), nameof(this.TrumpDescription), nameof(this.TrumpSuitGlyph), nameof(this.TrumpSuitColor));
        }

        public string TrumpImage => this.TrumpCard != null ? CardImageProvider.For(this.TrumpCard) : CardImageProvider.BackImage;

        public string TrumpDescription => this.TrumpCard != null ? $"Trump: {this.TrumpCard}" : "Trump: -";

        public string TrumpSuitGlyph => this.TrumpCard?.Suit switch
        {
            CardSuit.Club => "♣",
            CardSuit.Diamond => "♦",
            CardSuit.Heart => "♥",
            CardSuit.Spade => "♠",
            _ => "-",
        };

        public string TrumpSuitColor => this.TrumpCard?.Suit is CardSuit.Heart or CardSuit.Diamond ? "#D6453C" : "#1A1006";

        public int DeckCount
        {
            get => this.deckCount;
            private set => this.SetField(ref this.deckCount, value, nameof(this.DeckCount), nameof(this.DeckCountText), nameof(this.IsDeckVisible));
        }

        public string DeckCountText => this.DeckCount > 0 ? this.DeckCount.ToString() : "-";

        public bool IsDeckVisible => this.DeckCount > 0;

        public CardSlot? MyPlayedCard => this.mySlot == PlayerSlot.First ? this.slot1PlayedCard : this.slot2PlayedCard;

        public CardSlot? OpponentPlayedCard => this.mySlot == PlayerSlot.First ? this.slot2PlayedCard : this.slot1PlayedCard;

        public bool IsMyTurn
        {
            get => this.isMyTurn;
            private set => this.SetField(ref this.isMyTurn, value, nameof(this.IsMyTurn), nameof(this.MyTurnIndicatorOpacity), nameof(this.OpponentTurnIndicatorOpacity), nameof(this.IsHintVisible));
        }

        /// <summary>The hint button shows only on the person's turn in vs-AI games with assists on.</summary>
        public bool IsHintVisible => this.IsMyTurn && this.session.SupportsHints && AppSettings.AssistsEnabled;

        public CardSlot? LastTrickMyCard => this.mySlot == PlayerSlot.First ? this.lastTrickSlot1Card : this.lastTrickSlot2Card;

        public CardSlot? LastTrickOpponentCard => this.mySlot == PlayerSlot.First ? this.lastTrickSlot2Card : this.lastTrickSlot1Card;

        public bool HasLastTrick => this.lastTrickSlot1Card != null || this.lastTrickSlot2Card != null;

        public double MyTurnIndicatorOpacity => this.IsMyTurn ? 1.0 : 0.25;

        public double OpponentTurnIndicatorOpacity => this.IsMyTurn ? 0.25 : 1.0;

        public bool IsHandoffOverlayVisible
        {
            get => this.isHandoffOverlayVisible;
            private set => this.SetField(ref this.isHandoffOverlayVisible, value);
        }

        public string HandoffMessage
        {
            get => this.handoffMessage;
            private set => this.SetField(ref this.handoffMessage, value);
        }

        public bool IsRoundOverlayVisible
        {
            get => this.isRoundOverlayVisible;
            private set => this.SetField(ref this.isRoundOverlayVisible, value);
        }

        public string RoundOverlayTitle
        {
            get => this.roundOverlayTitle;
            private set => this.SetField(ref this.roundOverlayTitle, value);
        }

        public string RoundOverlayIcon
        {
            get => this.roundOverlayIcon;
            private set => this.SetField(ref this.roundOverlayIcon, value);
        }

        public bool RoundIWon
        {
            get => this.roundIWon;
            private set => this.SetField(ref this.roundIWon, value);
        }

        public bool RoundOpponentWon
        {
            get => this.roundOpponentWon;
            private set => this.SetField(ref this.roundOpponentWon, value);
        }

        public bool IsGameOverlayVisible
        {
            get => this.isGameOverlayVisible;
            private set => this.SetField(ref this.isGameOverlayVisible, value);
        }

        public string GameOverlayTitle
        {
            get => this.gameOverlayTitle;
            private set => this.SetField(ref this.gameOverlayTitle, value);
        }

        public string GameOverlayBody
        {
            get => this.gameOverlayBody;
            private set => this.SetField(ref this.gameOverlayBody, value);
        }

        public bool IsRatingChangeVisible
        {
            get => this.isRatingChangeVisible;
            private set => this.SetField(ref this.isRatingChangeVisible, value);
        }

        public string RatingChangeText
        {
            get => this.ratingChangeText;
            private set => this.SetField(ref this.ratingChangeText, value);
        }

        public string RoundAwardText
        {
            get => this.roundAwardText;
            private set => this.SetField(ref this.roundAwardText, value);
        }

        public string MyAnnouncesText
        {
            get => this.myAnnouncesText;
            private set => this.SetField(ref this.myAnnouncesText, value);
        }

        public string OpponentAnnouncesText
        {
            get => this.opponentAnnouncesText;
            private set => this.SetField(ref this.opponentAnnouncesText, value);
        }

        public bool HasAnnounces
        {
            get => this.hasAnnounces;
            private set => this.SetField(ref this.hasAnnounces, value);
        }

        public bool CanChangeTrump
        {
            get => this.canChangeTrump;
            private set
            {
                if (this.SetField(ref this.canChangeTrump, value))
                {
                    ((RelayCommand)this.ChangeTrumpCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanCloseGame
        {
            get => this.canCloseGame;
            private set
            {
                if (this.SetField(ref this.canCloseGame, value))
                {
                    ((RelayCommand)this.CloseGameCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string? ToastMessage
        {
            get => this.toastMessage;
            private set => this.SetField(ref this.toastMessage, value, nameof(this.ToastMessage), nameof(this.IsToastVisible));
        }

        public bool IsToastVisible => !string.IsNullOrEmpty(this.ToastMessage);

        public string StatusMessage
        {
            get => this.statusMessage;
            private set => this.SetField(ref this.statusMessage, value);
        }

        public bool GameClosedByMe
        {
            get => this.gameClosedByMe;
            private set => this.SetField(ref this.gameClosedByMe, value, nameof(this.GameClosedByMe), nameof(this.IsGameClosed), nameof(this.ClosedChipText));
        }

        public bool GameClosedByOpponent
        {
            get => this.gameClosedByOpponent;
            private set => this.SetField(ref this.gameClosedByOpponent, value, nameof(this.GameClosedByOpponent), nameof(this.IsGameClosed), nameof(this.ClosedChipText));
        }

        public bool IsGameClosed => this.GameClosedByMe || this.GameClosedByOpponent;

        public string ClosedChipText => this.GameClosedByMe ? Loc["Game_ClosedByYou"] : Loc["Game_ClosedByOpp"];

        public string GameOverlayIcon
        {
            get => this.gameOverlayIcon;
            private set => this.SetField(ref this.gameOverlayIcon, value);
        }

        public string ModeLabel => this.session.Mode switch
        {
            GameMode.VsAi => this.opponent?.DisplayName ?? "Computer",
            GameMode.HotSeat => "Hot-Seat",
            _ => string.Empty,
        };

        public bool IsRanked => this.session.Mode == GameMode.VsAi && this.opponent != null;

        public int OpponentElo => this.opponent?.Elo ?? 0;

        public string OpponentEloText => this.IsRanked ? $"ELO {this.OpponentElo}" : string.Empty;

        public ICommand TapCardCommand { get; }

        public ICommand ChangeTrumpCommand { get; }

        public ICommand CloseGameCommand { get; }

        public ICommand HintCommand { get; }

        public ICommand HandoffContinueCommand { get; }

        public ICommand RoundOverlayContinueCommand { get; }

        public ICommand PlayAgainCommand { get; }

        public ICommand LeaveCommand { get; }

        public int MyMatchWins => this.mySlot == PlayerSlot.First ? this.matchWinsSlot1 : this.matchWinsSlot2;

        public int OpponentMatchWins => this.mySlot == PlayerSlot.First ? this.matchWinsSlot2 : this.matchWinsSlot1;

        public string MatchScoreText => $"{this.MyMatchWins} – {this.OpponentMatchWins}";

        public bool HasMatchHistory => this.matchWinsSlot1 + this.matchWinsSlot2 > 0;

        public void StartGame()
        {
            this.StatusMessage = Loc["Status_Dealing"];
            this.session.Start();
        }

        public void Dispose()
        {
            this.Unsubscribe();
            this.session.Stop();
        }


        private static string FormatAnnounces(IReadOnlyList<Announce> announces)
        {
            if (announces.Count == 0)
            {
                return "—";
            }

            return string.Join("   ", announces.Select(a => ((int)a).ToString()));
        }

        // Sort by suit (visually) then by descending value, with Ace highest.
        private static IEnumerable<Card> SortHand(IEnumerable<Card> cards) => cards
            .OrderBy(c => SuitOrder(c.Suit))
            .ThenByDescending(c => CardOrderValue(c.Type));

        private static int SuitOrder(CardSuit suit) => suit switch
        {
            CardSuit.Spade => 0,
            CardSuit.Heart => 1,
            CardSuit.Club => 2,
            CardSuit.Diamond => 3,
            _ => 4,
        };

        private static int CardOrderValue(CardType type) => type switch
        {
            CardType.Ace => 6,
            CardType.Ten => 5,
            CardType.King => 4,
            CardType.Queen => 3,
            CardType.Jack => 2,
            CardType.Nine => 1,
            _ => 0,
        };

        private void Subscribe()
        {
            this.session.RoundStarted += this.OnRoundStarted;
            this.session.TurnStarted += this.OnTurnStarted;
            this.session.MovePlayed += this.OnMovePlayed;
            this.session.TrickCollected += this.OnTrickCollected;
            this.session.RoundFinished += this.OnRoundFinished;
            this.session.GameOver += this.OnGameOver;
            this.session.GameError += this.OnGameError;
        }

        private void Unsubscribe()
        {
            this.session.RoundStarted -= this.OnRoundStarted;
            this.session.TurnStarted -= this.OnTurnStarted;
            this.session.MovePlayed -= this.OnMovePlayed;
            this.session.TrickCollected -= this.OnTrickCollected;
            this.session.RoundFinished -= this.OnRoundFinished;
            this.session.GameOver -= this.OnGameOver;
            this.session.GameError -= this.OnGameError;
        }

        private void OnRoundStarted()
        {
            this.ClearBothPlayedCards();
            this.ClearLastTrick();
            this.ShowSeat();
            this.StatusMessage = Loc["Status_NewRound"];
        }

        private void OnTurnStarted(PlayerSlot slot, bool isHuman)
        {
            if (!isHuman)
            {
                this.IsMyTurn = false;
                this.StatusMessage = Loc.Format("Status_OpponentTurn", this.OpponentName);
                return;
            }

            if (slot != this.mySlot)
            {
                // Hot-seat: the other person takes the device before their cards are shown.
                this.pendingHandoffSlot = slot;
                this.IsMyTurn = false;
                this.HandoffMessage = Loc.Format("Handoff_Pass", this.session.GetName(slot));
                this.IsHandoffOverlayVisible = true;
                return;
            }

            this.OfferMove();
        }

        private void OnHandoffContinue()
        {
            if (this.pendingHandoffSlot is not { } next)
            {
                return;
            }

            this.pendingHandoffSlot = null;
            this.SetPerspective(next);
            this.IsHandoffOverlayVisible = false;
            this.OfferMove();
        }

        // My turn: mark the playable cards (and, as a beginner assist, the K/Q leads that would
        // announce a marriage) and enable exchanging and closing when the rules allow them.
        private void OfferMove()
        {
            var view = this.session.GetView(this.mySlot);
            if (view == null)
            {
                return;
            }

            var context = view.CreateTurnContext();
            var playable = new HashSet<Card>(view.PlayableCards);
            var hand = view.Hand.ToList();
            var markAnnounces = AppSettings.AssistsEnabled && context.State.CanAnnounce20Or40 && context.IsFirstPlayerTurn;
            foreach (var slot in this.MyHand)
            {
                slot.IsPlayable = playable.Contains(slot.Card);
                slot.IsHinted = false;
                var announce = markAnnounces && slot.IsPlayable
                    ? AnnounceValidator.Instance.GetPossibleAnnounce(hand, slot.Card, view.TrumpCard)
                    : Announce.None;
                slot.AnnounceText = announce switch
                {
                    Announce.Forty => "40",
                    Announce.Twenty => "20",
                    _ => string.Empty,
                };
            }

            this.CanChangeTrump = view.CanChangeTrump;
            this.CanCloseGame = view.CanClose;
            this.IsMyTurn = true;
            this.StatusMessage = Loc["Status_YourTurn"];
            this.Vibrate(isLong: false);
        }

        private void EndMyTurn()
        {
            this.IsMyTurn = false;
            this.CanChangeTrump = false;
            this.CanCloseGame = false;
            foreach (var slot in this.MyHand)
            {
                slot.IsPlayable = false;
                slot.IsHinted = false;
                slot.AnnounceText = string.Empty;
            }
        }

        private void OnMovePlayed(MoveInfo move)
        {
            var mine = move.Slot == this.mySlot;
            var who = mine ? Loc["Word_You"] : this.OpponentName;
            switch (move.Action.Type)
            {
                case PlayerActionType.PlayCard:
                    this.SetPlayedCard(move.Slot, new CardSlot(move.Action.Card));
                    if (mine)
                    {
                        var played = this.MyHand.FirstOrDefault(s => s.Card == move.Action.Card);
                        if (played != null)
                        {
                            this.MyHand.Remove(played);
                        }

                        this.EndMyTurn();
                    }
                    else
                    {
                        this.ShowOpponentCards(this.OpponentHand.Count - 1);
                    }

                    if (move.Announce != Announce.None)
                    {
                        this.ShowToast(move.Announce == Announce.Forty
                            ? Loc.Format("Toast_Announce40", who)
                            : Loc.Format("Toast_Announce20", who));
                    }

                    break;

                case PlayerActionType.ChangeTrump:
                    // The nine goes to the table and the old trump card to the exchanger's hand.
                    this.TrumpCard = this.session.GetView(this.mySlot)?.TrumpCard;
                    if (mine)
                    {
                        this.EndMyTurn();
                        this.ShowHand();
                    }

                    this.ShowToast(Loc.Format("Toast_SwapTrump", who));
                    break;

                case PlayerActionType.CloseGame:
                    if (mine)
                    {
                        this.EndMyTurn();
                        this.GameClosedByMe = true;
                        this.ShowToast(Loc["Toast_YouClosed"]);
                    }
                    else
                    {
                        this.GameClosedByOpponent = true;
                        this.ShowToast(Loc.Format("Toast_OppClosed", this.OpponentName));
                    }

                    this.DeckCount = 0;
                    break;
            }

            this.ShowRoundPoints(move.FirstRoundPoints, move.SecondRoundPoints);
        }

        // The finished trick leaves the table for the "last trick" corner; unless it ended the
        // round, both players have drawn by now.
        private void OnTrickCollected(TrickInfo trick)
        {
            this.lastTrickSlot1Card = trick.CardOf(PlayerSlot.First) is { } first ? new CardSlot(first) : null;
            this.lastTrickSlot2Card = trick.CardOf(PlayerSlot.Second) is { } second ? new CardSlot(second) : null;
            this.RaiseLastTrickChanged();
            this.ClearBothPlayedCards();
            if (!trick.RoundOver)
            {
                this.ShowDraws();
            }
        }

        private void OnRoundFinished(RoundEndInfo info)
        {
            var outcome = this.ShowRoundResult(info);
            this.RoundIWon = outcome == RoundOutcome.Won;
            this.RoundOpponentWon = outcome == RoundOutcome.Lost;
            (this.RoundOverlayIcon, this.RoundOverlayTitle) = outcome switch
            {
                RoundOutcome.Won => ("\U0001F3C6", Loc["Round_YouWon"]),
                RoundOutcome.Lost => ("\U0001F0A0", Loc.Format("Round_OppWon", this.OpponentName)),
                _ => ("\U0001F91D", Loc["Round_Draw"]),
            };
            this.IsRoundOverlayVisible = true;
            this.Vibrate(isLong: true);
        }

        private void OnRoundOverlayContinue()
        {
            this.IsRoundOverlayVisible = false;
            this.session.Continue();
        }

        private void OnGameOver(PlayerSlot winner, RoundEndInfo lastRound)
        {
            if (winner == PlayerSlot.First)
            {
                this.matchWinsSlot1++;
            }
            else
            {
                this.matchWinsSlot2++;
            }

            this.ShowRoundResult(lastRound);
            this.OnPropertyChanged(nameof(this.MyMatchWins));
            this.OnPropertyChanged(nameof(this.OpponentMatchWins));
            this.OnPropertyChanged(nameof(this.MatchScoreText));
            this.OnPropertyChanged(nameof(this.HasMatchHistory));

            var iWon = winner == this.mySlot;

            // Ranked (vs-AI) games move the persisted player rating; the AI is a fixed anchor.
            if (this.IsRanked)
            {
                var change = PlayerRatingStore.RecordResult(this.opponent!.Elo, iWon);
                var sign = change.Delta >= 0 ? "+" : string.Empty;
                this.RatingChangeText = Loc.Format("Rating_Change", change.OldElo, change.NewElo, $"{sign}{change.Delta}");
                this.IsRatingChangeVisible = true;
            }
            else
            {
                this.IsRatingChangeVisible = false;
            }

            this.RecordHistory(iWon);

            this.GameOverlayIcon = iWon ? "\U0001F3C6" : "\U0001F494";
            this.GameOverlayTitle = iWon ? Loc["GameOver_Victory"] : Loc["GameOver_Defeat"];
            this.GameOverlayBody = Loc.Format("GameOver_WonGame", iWon ? this.MyName : this.OpponentName);
            this.IsRoundOverlayVisible = false;
            this.IsGameOverlayVisible = true;
            this.EndMyTurn();
            this.Vibrate(isLong: true);
        }

        private void OnGameError(Exception ex)
        {
            this.GameOverlayTitle = Loc["Error_Title"];
            this.GameOverlayBody = Loc.Format("Error_Body", ex.GetType().Name, ex.Message);
            this.IsHandoffOverlayVisible = false;
            this.IsRoundOverlayVisible = false;
            this.IsGameOverlayVisible = true;
            this.EndMyTurn();
        }

        // Shows a finished round from my side (points, award, marriages, game score) and says how
        // it went for me. The award comes from the engine's scoring, not from comparing points: a
        // player who closes and misses 66 loses the round with more points.
        private RoundOutcome ShowRoundResult(RoundEndInfo info)
        {
            var first = this.mySlot == PlayerSlot.First;
            this.MyRoundPoints = first ? info.FirstRoundPoints : info.SecondRoundPoints;
            this.OpponentRoundPoints = first ? info.SecondRoundPoints : info.FirstRoundPoints;
            this.UpdateGamePointsForPerspective(info.FirstGamePoints, info.SecondGamePoints);

            var myAnnounces = first ? info.FirstAnnounces : info.SecondAnnounces;
            var opponentAnnounces = first ? info.SecondAnnounces : info.FirstAnnounces;
            this.MyAnnouncesText = FormatAnnounces(myAnnounces);
            this.OpponentAnnouncesText = FormatAnnounces(opponentAnnounces);
            this.HasAnnounces = myAnnounces.Count > 0 || opponentAnnounces.Count > 0;

            if (info.WinnerSlot is not { } winner)
            {
                this.RoundAwardText = string.Empty;
                return RoundOutcome.Draw;
            }

            var award = winner == PlayerSlot.First ? info.FirstAwardedGamePoints : info.SecondAwardedGamePoints;
            var iWon = winner == this.mySlot;
            this.RoundAwardText = Loc.Format(
                "Award_Format",
                iWon ? this.MyName : this.OpponentName,
                award,
                award == 1 ? Loc["Word_PointSingular"] : Loc["Word_PointPlural"]);
            return iWon ? RoundOutcome.Won : RoundOutcome.Lost;
        }

        private void RecordHistory(bool iWon)
        {
            // Only vs-AI games go in history (hot-seat is person against person). MyGamePoints /
            // OpponentGamePoints already hold the final game-point totals for this perspective.
            if (this.session.Mode != GameMode.VsAi)
            {
                return;
            }

            var opponentId = this.opponent?.Id ?? string.Empty;
            MatchHistoryStore.Add(new MatchHistoryEntry(
                this.OpponentName,
                this.MyGamePoints,
                this.OpponentGamePoints,
                iWon,
                DateTime.UtcNow,
                opponentId));
            OpponentStatsStore.Record(opponentId, iWon);
        }

        private void OnPlayAgain()
        {
            this.IsGameOverlayVisible = false;
            this.IsRoundOverlayVisible = false;
            this.IsHandoffOverlayVisible = false;
            this.pendingHandoffSlot = null;
            this.ClearBothPlayedCards();
            this.ClearLastTrick();
            this.SetPerspective(PlayerSlot.First);
            this.StatusMessage = Loc["Status_Dealing"];
            this.session.Restart();
        }

        private void OnTapCard(CardSlot? slot)
        {
            if (slot == null || !slot.IsPlayable || !this.IsMyTurn)
            {
                return;
            }

            this.session.TryPlay(this.mySlot, PlayerAction.PlayCard(slot.Card));
        }

        private void OnChangeTrump()
        {
            if (this.CanChangeTrump)
            {
                this.session.TryPlay(this.mySlot, PlayerAction.ChangeTrump());
            }
        }

        private void OnCloseGame()
        {
            if (this.CanCloseGame)
            {
                this.session.TryPlay(this.mySlot, PlayerAction.CloseGame());
            }
        }

        // Shows what the hint player would do from my view: highlights the card for a moment, or
        // explains an exchange / close in a toast.
        private void OnHint()
        {
            if (!this.IsMyTurn)
            {
                return;
            }

            var hint = this.session.GetHint();
            switch (hint?.Type)
            {
                case PlayerActionType.PlayCard:
                    var suggested = this.MyHand.FirstOrDefault(s => s.Card == hint.Card);
                    if (suggested == null || !suggested.IsPlayable)
                    {
                        this.ShowToast(Loc["Hint_None"]);
                        return;
                    }

                    foreach (var slot in this.MyHand)
                    {
                        slot.IsHinted = false;
                    }

                    suggested.IsHinted = true;
                    this.host.After(NoticeDuration, () => suggested.IsHinted = false);
                    break;
                case PlayerActionType.ChangeTrump:
                    this.ShowToast(Loc["Hint_SwapTrump"]);
                    break;
                case PlayerActionType.CloseGame:
                    this.ShowToast(Loc["Hint_CloseGame"]);
                    break;
                default:
                    this.ShowToast(Loc["Hint_None"]);
                    break;
            }
        }

        private void OnLeave()
        {
            this.session.Stop();
            this.IsGameOverlayVisible = false;
            this.host.Leave();
        }

        // Looks at the table from newMe's seat: names, hands, points and the per-seat cards on the
        // table (kept by slot, so only the "my/opponent" mapping changes).
        private void SetPerspective(PlayerSlot newMe)
        {
            this.mySlot = newMe;
            this.MyName = this.session.GetName(newMe);
            this.OpponentName = this.session.GetName(GameSession.Other(newMe));
            this.ShowSeat();
            this.OnPropertyChanged(nameof(this.MyPlayedCard));
            this.OnPropertyChanged(nameof(this.OpponentPlayedCard));
            this.RaiseLastTrickChanged();
            this.OnPropertyChanged(nameof(this.MyMatchWins));
            this.OnPropertyChanged(nameof(this.OpponentMatchWins));
            this.OnPropertyChanged(nameof(this.MatchScoreText));
        }

        // Everything my seat's view shows: hands, round and game points, trump, talon, close.
        private void ShowSeat()
        {
            var view = this.session.GetView(this.mySlot);
            if (view == null)
            {
                return;
            }

            this.ShowHand();
            this.ShowOpponentCards(OpponentCardCount(view));
            this.ShowRoundPoints(view.FirstPlayerRoundPoints, view.SecondPlayerRoundPoints);
            this.UpdateGamePointsForPerspective(view.FirstPlayerTotalPoints, view.SecondPlayerTotalPoints);
            this.ShowTalon(view);
            this.GameClosedByMe = view.ClosedBy == view.Seat;
            this.GameClosedByOpponent = view.ClosedBy != PlayerPosition.NoOne && view.ClosedBy != view.Seat;
        }

        private void ShowHand()
        {
            this.MyHand.Clear();
            foreach (var card in SortHand(this.session.GetView(this.mySlot)?.Hand ?? Array.Empty<Card>()))
            {
                this.MyHand.Add(new CardSlot(card));
            }
        }

        // After a trick: the cards just drawn slide into place in my sorted hand, a face-down
        // card joins the opponent's, and the talon shrinks.
        private void ShowDraws()
        {
            var view = this.session.GetView(this.mySlot);
            if (view == null)
            {
                return;
            }

            foreach (var card in view.Hand.Where(card => this.MyHand.All(s => s.Card != card)))
            {
                var sorted = SortHand(this.MyHand.Select(s => s.Card).Append(card)).ToList();
                this.MyHand.Insert(sorted.IndexOf(card), new CardSlot(card));
            }

            this.ShowOpponentCards(OpponentCardCount(view));
            this.ShowTalon(view);
        }

        private void ShowTalon(SantaseSeatView view)
        {
            this.TrumpCard = view.TrumpCard;
            this.DeckCount = view.ClosedBy == PlayerPosition.NoOne ? view.CardsLeftInDeck : 0;
        }

        private void ShowOpponentCards(int count)
        {
            count = Math.Max(0, count);
            while (this.OpponentHand.Count > count)
            {
                this.OpponentHand.RemoveAt(this.OpponentHand.Count - 1);
            }

            while (this.OpponentHand.Count < count)
            {
                this.OpponentHand.Add(new CardSlot(FaceDown, isFaceDown: true));
            }

            this.OpponentCardsCount = count;
        }

        private static int OpponentCardCount(SantaseSeatView view) =>
            view.Seat == PlayerPosition.FirstPlayer ? view.SecondPlayerCardCount : view.FirstPlayerCardCount;

        private void ShowRoundPoints(int firstRoundPoints, int secondRoundPoints)
        {
            var first = this.mySlot == PlayerSlot.First;
            this.MyRoundPoints = first ? firstRoundPoints : secondRoundPoints;
            this.OpponentRoundPoints = first ? secondRoundPoints : firstRoundPoints;
        }

        private void UpdateGamePointsForPerspective(int firstGamePoints, int secondGamePoints)
        {
            var first = this.mySlot == PlayerSlot.First;
            this.MyGamePoints = first ? firstGamePoints : secondGamePoints;
            this.OpponentGamePoints = first ? secondGamePoints : firstGamePoints;
        }

        private void SetPlayedCard(PlayerSlot slot, CardSlot? value)
        {
            if (slot == PlayerSlot.First)
            {
                this.slot1PlayedCard = value;
            }
            else
            {
                this.slot2PlayedCard = value;
            }

            this.OnPropertyChanged(nameof(this.MyPlayedCard));
            this.OnPropertyChanged(nameof(this.OpponentPlayedCard));
        }

        private void ClearBothPlayedCards()
        {
            this.slot1PlayedCard = null;
            this.slot2PlayedCard = null;
            this.OnPropertyChanged(nameof(this.MyPlayedCard));
            this.OnPropertyChanged(nameof(this.OpponentPlayedCard));
        }

        private void ClearLastTrick()
        {
            this.lastTrickSlot1Card = null;
            this.lastTrickSlot2Card = null;
            this.RaiseLastTrickChanged();
        }

        private void RaiseLastTrickChanged()
        {
            this.OnPropertyChanged(nameof(this.LastTrickMyCard));
            this.OnPropertyChanged(nameof(this.LastTrickOpponentCard));
            this.OnPropertyChanged(nameof(this.HasLastTrick));
        }

        private void ShowToast(string message)
        {
            this.ToastMessage = message;
            this.host.After(NoticeDuration, () =>
            {
                if (this.ToastMessage == message)
                {
                    this.ToastMessage = null;
                }
            });
        }

        private void Vibrate(bool isLong)
        {
            if (AppSettings.HapticsEnabled)
            {
                this.host.Vibrate(isLong);
            }
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        private bool SetField<T>(ref T field, T value, params string[] propertyNames)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            foreach (var name in propertyNames)
            {
                this.OnPropertyChanged(name);
            }

            return true;
        }

        private void OnPropertyChanged(string? name)
        {
            if (name == null)
            {
                return;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
