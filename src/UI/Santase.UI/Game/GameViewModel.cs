namespace Santase.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Dispatching;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public sealed class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GameSession session;

        private readonly IDispatcher dispatcher;

        private TaskCompletionSource<object?>? pendingHandoff;

        private PlayerSlot pendingHandoffSlot;

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

        public GameViewModel(GameSession session, IDispatcher dispatcher)
        {
            this.session = session;
            this.dispatcher = dispatcher;

            this.MyHand = new ObservableCollection<CardSlot>();
            this.OpponentHand = new ObservableCollection<CardSlot>();

            // VS-AI mode: always show the human (slot 1) at the bottom.
            // Hot-seat: starts at slot 1; swaps when slot 2's turn comes.
            this.SetPerspective(PlayerSlot.First, raiseChange: false);

            this.TapCardCommand = new RelayCommand<CardSlot>(this.OnTapCard);
            this.ChangeTrumpCommand = new RelayCommand(this.OnChangeTrump, () => this.CanChangeTrump);
            this.CloseGameCommand = new RelayCommand(this.OnCloseGame, () => this.CanCloseGame);
            this.HandoffContinueCommand = new RelayCommand(this.OnHandoffContinue);
            this.RoundOverlayContinueCommand = new RelayCommand(this.OnRoundOverlayContinue);
            this.PlayAgainCommand = new RelayCommand(this.OnPlayAgain);
            this.LeaveCommand = new RelayCommand(this.OnLeave);

            this.Subscribe();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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
            private set => this.SetField(ref this.myGamePoints, value);
        }

        public int OpponentGamePoints
        {
            get => this.opponentGamePoints;
            private set => this.SetField(ref this.opponentGamePoints, value);
        }

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

        public bool IsMyTurn
        {
            get => this.isMyTurn;
            private set => this.SetField(ref this.isMyTurn, value, nameof(this.IsMyTurn), nameof(this.MyTurnIndicatorOpacity), nameof(this.OpponentTurnIndicatorOpacity));
        }

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
            private set => this.SetField(ref this.gameClosedByMe, value);
        }

        public bool GameClosedByOpponent
        {
            get => this.gameClosedByOpponent;
            private set => this.SetField(ref this.gameClosedByOpponent, value);
        }

        public string ModeLabel => this.session.Mode switch
        {
            GameMode.VsEasy => "Easy",
            GameMode.VsHard => "Hard",
            GameMode.HotSeat => "Hot-Seat",
            _ => string.Empty,
        };

        public ICommand TapCardCommand { get; }

        public ICommand ChangeTrumpCommand { get; }

        public ICommand CloseGameCommand { get; }

        public ICommand HandoffContinueCommand { get; }

        public ICommand RoundOverlayContinueCommand { get; }

        public ICommand PlayAgainCommand { get; }

        public ICommand LeaveCommand { get; }

        public int MyMatchWins
        {
            get => this.mySlot == PlayerSlot.First ? this.matchWinsSlot1 : this.matchWinsSlot2;
            private set
            {
                if (this.mySlot == PlayerSlot.First)
                {
                    this.matchWinsSlot1 = value;
                }
                else
                {
                    this.matchWinsSlot2 = value;
                }

                this.OnPropertyChanged(nameof(this.MyMatchWins));
                this.OnPropertyChanged(nameof(this.MatchScoreText));
            }
        }

        public int OpponentMatchWins
        {
            get => this.mySlot == PlayerSlot.First ? this.matchWinsSlot2 : this.matchWinsSlot1;
            private set
            {
                if (this.mySlot == PlayerSlot.First)
                {
                    this.matchWinsSlot2 = value;
                }
                else
                {
                    this.matchWinsSlot1 = value;
                }

                this.OnPropertyChanged(nameof(this.OpponentMatchWins));
                this.OnPropertyChanged(nameof(this.MatchScoreText));
            }
        }

        public string MatchScoreText => $"{this.MyMatchWins} – {this.OpponentMatchWins}";

        public bool HasMatchHistory => this.matchWinsSlot1 + this.matchWinsSlot2 > 0;

        public void StartGame()
        {
            this.dispatcher.Dispatch(() =>
            {
                this.MyName = this.session.GetName(this.mySlot);
                this.OpponentName = this.session.GetName(this.OtherSlot(this.mySlot));
                this.StatusMessage = "Dealing...";
            });

            this.session.Start();
        }

        public void Dispose()
        {
            this.Unsubscribe();

            // Release the engine if it's blocked in RequestHandoffAndBlock.
            this.pendingHandoff?.TrySetCanceled();
            this.pendingHandoff = null;

            this.session.Stop();
        }

        private PlayerSlot OtherSlot(PlayerSlot slot) => slot == PlayerSlot.First ? PlayerSlot.Second : PlayerSlot.First;

        private void Subscribe()
        {
            this.session.RoundStarting += this.OnRoundStarting;
            this.session.PlayerHandInitialized += this.OnPlayerHandInitialized;
            this.session.CardDealtToPlayer += this.OnCardDealtToPlayer;
            this.session.TurnStarting += this.OnTurnStarting;
            this.session.HumanInputRequested += this.OnHumanInputRequested;
            this.session.CardPlayed += this.OnCardPlayed;
            this.session.TrumpCardSwapped += this.OnTrumpCardSwapped;
            this.session.GameClosed += this.OnGameClosed;
            this.session.AnnouncementMade += this.OnAnnouncementMade;
            this.session.TrickCompleted += this.OnTrickCompleted;
            this.session.RoundOver += this.OnRoundOver;
            this.session.GameOver += this.OnGameOver;
            this.session.GameError += this.OnGameError;
        }

        private void Unsubscribe()
        {
            this.session.RoundStarting -= this.OnRoundStarting;
            this.session.PlayerHandInitialized -= this.OnPlayerHandInitialized;
            this.session.CardDealtToPlayer -= this.OnCardDealtToPlayer;
            this.session.TurnStarting -= this.OnTurnStarting;
            this.session.HumanInputRequested -= this.OnHumanInputRequested;
            this.session.CardPlayed -= this.OnCardPlayed;
            this.session.TrumpCardSwapped -= this.OnTrumpCardSwapped;
            this.session.GameClosed -= this.OnGameClosed;
            this.session.AnnouncementMade -= this.OnAnnouncementMade;
            this.session.TrickCompleted -= this.OnTrickCompleted;
            this.session.RoundOver -= this.OnRoundOver;
            this.session.GameOver -= this.OnGameOver;
            this.session.GameError -= this.OnGameError;
        }

        private void OnRoundStarting(int firstGamePoints, int secondGamePoints, Card trump)
        {
            this.dispatcher.Dispatch(() =>
            {
                // Note: the (my, opp) parameters in the engine are RELATIVE to the first player.
                // So firstGamePoints = first player's total, secondGamePoints = second player's total.
                this.MyHand.Clear();
                this.OpponentHand.Clear();
                this.OpponentCardsCount = 0;
                this.ClearBothPlayedCards();
                this.MyRoundPoints = 0;
                this.OpponentRoundPoints = 0;
                this.GameClosedByMe = false;
                this.GameClosedByOpponent = false;
                this.TrumpCard = trump;
                this.DeckCount = 12;
                this.UpdateGamePointsForPerspective(firstGamePoints, secondGamePoints);
                this.StatusMessage = "New round";
            });
        }

        private void OnPlayerHandInitialized(PlayerSlot slot, IReadOnlyList<Card> cards)
        {
            this.dispatcher.Dispatch(() =>
            {
                if (slot == this.mySlot)
                {
                    this.MyHand.Clear();
                    foreach (var c in this.SortHand(cards))
                    {
                        this.MyHand.Add(new CardSlot(c));
                    }
                }
                else
                {
                    this.OpponentHand.Clear();
                    var dummy = Card.GetCard(CardSuit.Club, CardType.Nine);
                    for (var i = 0; i < cards.Count; i++)
                    {
                        this.OpponentHand.Add(new CardSlot(dummy, isFaceDown: true));
                    }

                    this.OpponentCardsCount = cards.Count;
                }

                // DeckCount is set to 12 in OnRoundStarting and decremented per draw in
                // OnCardDealtToPlayer; the initial 6+6 deal is implicit in that 12.
            });
        }

        private void OnCardDealtToPlayer(PlayerSlot slot, Card card)
        {
            this.dispatcher.Dispatch(() =>
            {
                if (slot == this.mySlot)
                {
                    var slotItem = new CardSlot(card);
                    var inserted = false;

                    // Keep the hand sorted on insert.
                    var sorted = this.SortHand(this.MyHand.Select(s => s.Card).Concat(new[] { card })).ToList();
                    var idx = sorted.IndexOf(card);
                    if (idx >= 0 && idx <= this.MyHand.Count)
                    {
                        this.MyHand.Insert(idx, slotItem);
                        inserted = true;
                    }

                    if (!inserted)
                    {
                        this.MyHand.Add(slotItem);
                    }
                }
                else
                {
                    var dummy = Card.GetCard(CardSuit.Club, CardType.Nine);
                    this.OpponentHand.Add(new CardSlot(dummy, isFaceDown: true));
                    this.OpponentCardsCount++;
                }

                if (this.DeckCount > 0)
                {
                    this.DeckCount--;
                }
            });
        }

        private void OnTurnStarting(PlayerSlot slot, bool isHuman)
        {
            // Hot-seat handoff: swap perspective if needed BEFORE letting the next human play.
            if (this.session.Mode == GameMode.HotSeat && isHuman && slot != this.mySlot)
            {
                this.RequestHandoffAndBlock(slot);
            }

            this.dispatcher.Dispatch(() =>
            {
                this.IsMyTurn = slot == this.mySlot;
                this.StatusMessage = slot == this.mySlot
                    ? "Your turn"
                    : $"{this.OpponentName}'s turn...";
            });
        }

        private void RequestHandoffAndBlock(PlayerSlot nextSlot)
        {
            this.pendingHandoffSlot = nextSlot;
            this.pendingHandoff = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            var nextName = this.session.GetName(nextSlot);
            this.dispatcher.Dispatch(() =>
            {
                this.IsMyTurn = false;
                this.HandoffMessage = $"Pass the device to {nextName}\nTap when ready to start your turn";
                this.IsHandoffOverlayVisible = true;
            });

            try
            {
                this.pendingHandoff.Task.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnHandoffContinue()
        {
            // Runs on the UI thread (button command). Swap perspective synchronously
            // BEFORE unblocking the engine, so the new hand is visible when the next
            // GetTurn fires HumanInputRequested.
            this.SetPerspective(this.pendingHandoffSlot, raiseChange: true);
            this.IsHandoffOverlayVisible = false;
            this.pendingHandoff?.TrySetResult(null);
            this.pendingHandoff = null;
        }

        private void OnHumanInputRequested(PlayerSlot slot, HumanPlayer player, PlayerTurnContext context)
        {
            this.dispatcher.Dispatch(() =>
            {
                if (slot != this.mySlot)
                {
                    return;
                }

                this.IsMyTurn = true;
                this.TrumpCard = context.TrumpCard;

                var validator = player.ActionValidator;
                var possibleCards = validator.GetPossibleCardsToPlay(context, GetPlayerCards(player));
                var possibleSet = new HashSet<Card>(possibleCards);

                foreach (var slotItem in this.MyHand)
                {
                    slotItem.IsPlayable = possibleSet.Contains(slotItem.Card);
                }

                this.CanChangeTrump = validator.IsValid(PlayerAction.ChangeTrump(), context, GetPlayerCards(player));
                this.CanCloseGame = validator.IsValid(PlayerAction.CloseGame(), context, GetPlayerCards(player));

                this.StatusMessage = "Your turn";
            });
        }

        private static ICollection<Card> GetPlayerCards(HumanPlayer player)
        {
            return player.CardsSnapshot.ToList();
        }

        private void OnCardPlayed(PlayerSlot slot, Card card)
        {
            this.dispatcher.Dispatch(() =>
            {
                var played = new CardSlot(card);
                this.SetPlayedCard(slot, played);

                if (slot == this.mySlot)
                {
                    var existing = this.MyHand.FirstOrDefault(s => s.Card.Equals(card));
                    if (existing != null)
                    {
                        this.MyHand.Remove(existing);
                    }

                    this.IsMyTurn = false;
                    this.CanChangeTrump = false;
                    this.CanCloseGame = false;
                    foreach (var s in this.MyHand)
                    {
                        s.IsPlayable = false;
                    }
                }
                else
                {
                    if (this.OpponentHand.Count > 0)
                    {
                        this.OpponentHand.RemoveAt(this.OpponentHand.Count - 1);
                    }

                    this.OpponentCardsCount = Math.Max(0, this.OpponentCardsCount - 1);
                }
            });
        }

        private void OnTrumpCardSwapped(PlayerSlot slot, Card newTrump)
        {
            this.dispatcher.Dispatch(() =>
            {
                this.TrumpCard = newTrump;

                if (slot == this.mySlot)
                {
                    // The 9 left my hand and the old trump card is now in my hand.
                    var human = this.session.GetHuman(slot);
                    if (human != null)
                    {
                        this.MyHand.Clear();
                        foreach (var c in this.SortHand(human.CardsSnapshot))
                        {
                            this.MyHand.Add(new CardSlot(c));
                        }
                    }
                }

                var who = slot == this.mySlot ? "You" : this.OpponentName;
                this.ShowToast($"{who} swapped the trump 9");
            });
        }

        private void OnGameClosed(PlayerSlot slot)
        {
            this.dispatcher.Dispatch(() =>
            {
                if (slot == this.mySlot)
                {
                    this.GameClosedByMe = true;
                    this.ShowToast("You closed the game");
                }
                else
                {
                    this.GameClosedByOpponent = true;
                    this.ShowToast($"{this.OpponentName} closed the game");
                }

                this.DeckCount = 0;
            });
        }

        private void OnAnnouncementMade(PlayerSlot slot, Announce announce)
        {
            this.dispatcher.Dispatch(() =>
            {
                var mine = slot == this.mySlot;
                var who = mine ? "You" : this.OpponentName;

                // The engine already added the announce to this player's round points the moment
                // the marriage card was led; reflect it now instead of waiting for the trick to
                // settle. OnTrickCompleted later SETs the authoritative total, so this can't
                // double-count.
                if (mine)
                {
                    this.MyRoundPoints += (int)announce;
                }
                else
                {
                    this.OpponentRoundPoints += (int)announce;
                }

                var label = announce == Announce.Forty ? "trump marriage 40!" : "marriage 20!";
                this.ShowToast($"{who} announced {label}");
            });
        }

        private void OnTrickCompleted(TrickResult result)
        {
            this.dispatcher.Dispatch(() =>
            {
                // Update round points based on the perspective. These are authoritative and
                // already include any announce surfaced earlier by OnAnnouncementMade.
                if (this.mySlot == PlayerSlot.First)
                {
                    this.MyRoundPoints = result.FirstRoundPoints;
                    this.OpponentRoundPoints = result.SecondRoundPoints;
                }
                else
                {
                    this.MyRoundPoints = result.SecondRoundPoints;
                    this.OpponentRoundPoints = result.FirstRoundPoints;
                }
            });

            // Stay visible during TrickSettleMs (handled in session by Thread.Sleep).
            // Clear the played cards just before the engine's sleep ends.
            Task.Run(async () =>
            {
                await Task.Delay(Math.Max(0, this.session.TrickSettleMs - 150));
                this.dispatcher.Dispatch(this.ClearBothPlayedCards);
            });
        }

        private void OnRoundOver(RoundEndInfo info)
        {
            this.dispatcher.Dispatch(() =>
            {
                int myRound, oppRound;
                if (this.mySlot == PlayerSlot.First)
                {
                    myRound = info.FirstRoundPoints;
                    oppRound = info.SecondRoundPoints;
                }
                else
                {
                    myRound = info.SecondRoundPoints;
                    oppRound = info.FirstRoundPoints;
                }

                this.MyRoundPoints = myRound;
                this.OpponentRoundPoints = oppRound;

                // Note: info.{First,Second}GamePoints are stale here — the engine runs
                // UpdatePoints AFTER both EndRound calls. Game-point badges refresh when
                // the next round starts (or via OnGameOver if the game is over).
                var iWon = myRound > oppRound;
                var tie = myRound == oppRound;
                this.RoundIWon = !tie && iWon;
                this.RoundOpponentWon = !tie && !iWon;

                if (tie)
                {
                    this.RoundOverlayIcon = "\U0001F91D"; // handshake
                    this.RoundOverlayTitle = "Round tied";
                }
                else if (iWon)
                {
                    this.RoundOverlayIcon = "\U0001F3C6"; // trophy
                    this.RoundOverlayTitle = "You won the round!";
                }
                else
                {
                    this.RoundOverlayIcon = "\U0001F0A0"; // playing card
                    this.RoundOverlayTitle = $"{this.OpponentName} won the round";
                }

                this.IsRoundOverlayVisible = true;
            });

            // Engine thread is already blocked inside HandleRoundEndedFromObserver; the user
            // tapping Continue calls session.Continue() which releases that block.
        }

        private void OnRoundOverlayContinue()
        {
            this.IsRoundOverlayVisible = false;
            this.session.Continue();
        }

        private void OnGameError(Exception ex)
        {
            this.dispatcher.Dispatch(() =>
            {
                this.GameOverlayTitle = "Game error";
                this.GameOverlayBody = $"An unexpected error stopped the game.\n\n{ex.GetType().Name}: {ex.Message}";
                this.IsHandoffOverlayVisible = false;
                this.IsRoundOverlayVisible = false;
                this.IsGameOverlayVisible = true;
                this.IsMyTurn = false;
                this.CanChangeTrump = false;
                this.CanCloseGame = false;
            });
        }

        private void OnGameOver(PlayerSlot winnerSlot)
        {
            // Update match counts (slot-based — survives perspective swaps).
            if (winnerSlot == PlayerSlot.First)
            {
                this.matchWinsSlot1++;
            }
            else
            {
                this.matchWinsSlot2++;
            }

            this.dispatcher.Dispatch(() =>
            {
                this.UpdateGamePointsForPerspective(this.session.FirstGamePoints, this.session.SecondGamePoints);

                this.OnPropertyChanged(nameof(this.MyMatchWins));
                this.OnPropertyChanged(nameof(this.OpponentMatchWins));
                this.OnPropertyChanged(nameof(this.MatchScoreText));
                this.OnPropertyChanged(nameof(this.HasMatchHistory));

                var iWon = winnerSlot == this.mySlot;
                var winnerName = iWon ? this.MyName : this.OpponentName;
                this.GameOverlayTitle = iWon ? "Victory!" : "Game Over";
                this.GameOverlayBody = $"{winnerName} won this game\nFinal score  {this.MyGamePoints} – {this.OpponentGamePoints}";
                this.IsRoundOverlayVisible = false;
                this.IsGameOverlayVisible = true;
                this.IsMyTurn = false;
                this.CanChangeTrump = false;
                this.CanCloseGame = false;
            });
        }

        private void OnPlayAgain()
        {
            this.IsGameOverlayVisible = false;
            this.IsRoundOverlayVisible = false;
            this.IsHandoffOverlayVisible = false;
            this.MyHand.Clear();
            this.OpponentHand.Clear();
            this.OpponentCardsCount = 0;
            this.ClearBothPlayedCards();
            this.MyRoundPoints = 0;
            this.OpponentRoundPoints = 0;

            // Reset perspective to slot 1 (the start-page convention) before restarting.
            if (this.mySlot != PlayerSlot.First)
            {
                this.SetPerspective(PlayerSlot.First, raiseChange: true);
            }

            this.session.Restart();
        }

        private void OnTapCard(CardSlot? slot)
        {
            if (slot == null || !slot.IsPlayable)
            {
                return;
            }

            var human = this.session.GetHuman(this.mySlot);
            if (human == null || !human.IsAwaitingInput)
            {
                return;
            }

            this.session.SubmitPlayCard(human, slot.Card);
        }

        private void OnChangeTrump()
        {
            var human = this.session.GetHuman(this.mySlot);
            if (human == null || !human.IsAwaitingInput || !this.CanChangeTrump)
            {
                return;
            }

            this.session.SubmitChangeTrump(human);
        }

        private void OnCloseGame()
        {
            var human = this.session.GetHuman(this.mySlot);
            if (human == null || !human.IsAwaitingInput || !this.CanCloseGame)
            {
                return;
            }

            this.session.SubmitCloseGame(human);
        }

        private void OnLeave()
        {
            this.session.Stop();
            this.IsGameOverlayVisible = false;
            _ = Microsoft.Maui.Controls.Shell.Current?.GoToAsync("..");
        }

        private void SetPerspective(PlayerSlot newMe, bool raiseChange)
        {
            this.mySlot = newMe;
            this.MyName = this.session.GetName(newMe);
            this.OpponentName = this.session.GetName(this.OtherSlot(newMe));

            // Rebuild hand views to match the new perspective.
            var myHuman = this.session.GetHuman(newMe);
            var oppHuman = this.session.GetHuman(this.OtherSlot(newMe));

            this.MyHand.Clear();
            if (myHuman != null)
            {
                foreach (var c in this.SortHand(myHuman.CardsSnapshot))
                {
                    this.MyHand.Add(new CardSlot(c));
                }
            }

            this.OpponentHand.Clear();
            var dummy = Card.GetCard(CardSuit.Club, CardType.Nine);
            int oppCount;
            if (oppHuman != null)
            {
                oppCount = oppHuman.CardsSnapshot.Count;
            }
            else
            {
                oppCount = this.OtherSlot(newMe) == PlayerSlot.First
                    ? this.session.FirstObserver.CardsCount
                    : this.session.SecondObserver.CardsCount;
            }

            for (var i = 0; i < oppCount; i++)
            {
                this.OpponentHand.Add(new CardSlot(dummy, isFaceDown: true));
            }

            this.OpponentCardsCount = oppCount;

            // Played cards are stored per-slot, so swapping perspective just means raising
            // PropertyChanged on the derived MyPlayedCard / OpponentPlayedCard.
            this.OnPropertyChanged(nameof(this.MyPlayedCard));
            this.OnPropertyChanged(nameof(this.OpponentPlayedCard));

            // Update game points to match perspective.
            this.UpdateGamePointsForPerspective(this.session.FirstGamePoints, this.session.SecondGamePoints);

            if (raiseChange)
            {
                this.OnPropertyChanged(nameof(this.MyName));
                this.OnPropertyChanged(nameof(this.OpponentName));
            }
        }

        private void UpdateGamePointsForPerspective(int firstGamePoints, int secondGamePoints)
        {
            if (this.mySlot == PlayerSlot.First)
            {
                this.MyGamePoints = firstGamePoints;
                this.OpponentGamePoints = secondGamePoints;
            }
            else
            {
                this.MyGamePoints = secondGamePoints;
                this.OpponentGamePoints = firstGamePoints;
            }
        }

        private void ShowToast(string message)
        {
            this.ToastMessage = message;
            Task.Run(async () =>
            {
                await Task.Delay(2500);
                this.dispatcher.Dispatch(() =>
                {
                    if (this.ToastMessage == message)
                    {
                        this.ToastMessage = null;
                    }
                });
            });
        }

        private IEnumerable<Card> SortHand(IEnumerable<Card> cards)
        {
            // Sort by suit (visually) then by descending value, with Ace highest.
            return cards
                .OrderBy(c => SuitOrder(c.Suit))
                .ThenByDescending(c => CardOrderValue(c.Type));
        }

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
