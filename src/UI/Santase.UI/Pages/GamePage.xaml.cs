namespace Santase.UI.Pages
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using Microsoft.Maui.Devices;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    [QueryProperty(nameof(ModeRaw), "mode")]
    [QueryProperty(nameof(FirstName), "first")]
    [QueryProperty(nameof(SecondName), "second")]
    [QueryProperty(nameof(OpponentId), "opponent")]
    public partial class GamePage : ContentPage, IGameTableHost
    {
        // One leave prompt at a time (the back button and Menu both ask).
        private readonly OneAtATime leaving = new();

        private GameSession? session;

        private GameViewModel? viewModel;

        // As dark as the table under the round, game and hand-off screens' dimming.
        private static readonly SolidColorBrush OverlayBackdrop = new(Color.FromArgb("#020805"));

        private readonly Brush felt;

        private bool started;

        public GamePage()
        {
            this.InitializeComponent();
            PageBackground.CoverTheWholeScreen(this);
            this.felt = this.Background;
            KeepTextSize(this.TableRoot);
            KeepTextSize(this.MenuButton);
        }

        public string ModeRaw { get; set; } = nameof(GameMode.VsAi);

        public string FirstName { get; set; } = "Player 1";

        public string SecondName { get; set; } = "Player 2";

        public string OpponentId { get; set; } = "smart";

        // The page itself is going back, not a gesture of the system (see AppShell.OnNavigating).
        internal bool IsLeaving { get; private set; }

#if STORE_SHOTS
        internal GameSession? Session => this.session;

#endif
        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.IsLeaving = false;
            SetKeepScreenOn(true);

            if (this.started)
            {
                return;
            }

            this.started = true;

            var mode = Enum.TryParse<GameMode>(this.ModeRaw, ignoreCase: true, out var parsed)
                ? parsed
                : GameMode.VsAi;

            var opponent = mode == GameMode.VsAi ? AiOpponents.ById(this.OpponentId) : null;
            var secondName = opponent?.DisplayName ?? this.SecondName;

            var pace = new GamePace(AppSettings.AiThinkDelayMs, AppSettings.TrickSettleMs);
            this.session = new GameSession(mode, this.FirstName, secondName, opponent?.CreatePlayer(), pace);
            this.viewModel = new GameViewModel(this.session, opponent, this);
            this.viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
            this.BindingContext = this.viewModel;

            this.viewModel.StartGame();
        }

        void IGameTableHost.After(TimeSpan delay, Action action) => this.Dispatcher.DispatchDelayed(delay, action);

        void IGameTableHost.Vibrate(bool isLong)
        {
            try
            {
                HapticFeedback.Default.Perform(isLong ? HapticFeedbackType.LongPress : HapticFeedbackType.Click);
            }
            catch
            {
                // Not supported on this platform (e.g. desktop) — silently skip.
            }
        }

        void IGameTableHost.Leave()
        {
            this.IsLeaving = true;
            _ = Shell.Current?.GoToAsync("..");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            SetKeepScreenOn(false);

            if (this.viewModel != null)
            {
                this.viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
                this.viewModel.Dispose();
                this.viewModel = null;
            }

            this.session = null;
            this.started = false;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = this.leaving.RunAsync(this.ConfirmAndLeaveAsync);
            return true; // Handled — leaving goes through the confirmation prompt below.
        }

        private async void OnMenuClicked(object? sender, EventArgs e)
        {
            await this.leaving.RunAsync(this.ConfirmAndLeaveAsync);
        }

        // Closing is the one irreversible in-game action a player can regret (fail to reach 66
        // and the opponent scores 3), so it gets a short confirmation that also teaches the rule.
        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            if (this.viewModel == null || !this.viewModel.CanCloseGame)
            {
                return;
            }

            var mgr = LocalizationManager.Instance;
            var confirmed = await this.DisplayAlertAsync(
                mgr["Close_Title"],
                mgr.Format("Close_Message", this.viewModel.OpponentName),
                mgr["Close_Confirm"],
                mgr["Common_Cancel"]);
            // The page may have gone away while the question was up.
            if (confirmed && this.viewModel is { } table)
            {
                table.CloseGameCommand.Execute(null);
            }
        }

        // Confirms before abandoning a game that is still in progress; if the game is already over
        // (or never started) it just leaves. The game-over overlay's own "Back to menu" button
        // calls LeaveCommand directly and is intentionally not gated.
        private async Task ConfirmAndLeaveAsync()
        {
            if (this.viewModel == null)
            {
                this.IsLeaving = true;
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (this.session is { IsRunning: true })
            {
                var mgr = LocalizationManager.Instance;
                var leave = await this.DisplayAlertAsync(
                    mgr["Leave_Title"],
                    mgr["Leave_Message"],
                    mgr["Leave_Confirm"],
                    mgr["Leave_Cancel"]);
                if (!leave)
                {
                    return;
                }
            }

            // The page may have gone away while the question was up.
            this.viewModel?.LeaveCommand.Execute(null);
        }

        // View-only animations reacting to view-model state changes. The game raises everything on
        // the UI thread, so animating here is safe.
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var vm = this.viewModel;
            if (vm == null)
            {
                return;
            }

            // The overlays dim the table inside the safe area, and on an iPhone the page around
            // them, behind the status bar and the home indicator, has to go as dark. It takes
            // Background: changing BackgroundColor did nothing there (measured on the simulator).
            if (e.PropertyName is nameof(GameViewModel.IsHandoffOverlayVisible) or nameof(GameViewModel.IsRoundOverlayVisible) or nameof(GameViewModel.IsGameOverlayVisible))
            {
                this.Background = vm.IsHandoffOverlayVisible || vm.IsRoundOverlayVisible || vm.IsGameOverlayVisible ? OverlayBackdrop : this.felt;
            }

            switch (e.PropertyName)
            {
                case nameof(GameViewModel.MyPlayedCard) when vm.MyPlayedCard != null:
                    AnimateCardPop(this.MyPlayedBorder);
                    break;
                case nameof(GameViewModel.OpponentPlayedCard) when vm.OpponentPlayedCard != null:
                    AnimateCardPop(this.OppPlayedBorder);
                    break;
                case nameof(GameViewModel.IsToastVisible) when vm.IsToastVisible:
                    AnimateToastPop(this.ToastBorder);
                    break;
                case nameof(GameViewModel.IsRoundOverlayVisible) when vm.IsRoundOverlayVisible:
                    AnimateOverlayFadeIn(this.RoundOverlayRoot);
                    break;
                case nameof(GameViewModel.IsGameOverlayVisible) when vm.IsGameOverlayVisible:
                    AnimateOverlayFadeIn(this.GameOverlayRoot);
                    break;
            }
        }

        private static void AnimateCardPop(VisualElement element)
        {
            element.CancelAnimations();
            element.Scale = 0.6;
            element.Opacity = 0;
            _ = element.ScaleToAsync(1.0, 180, Easing.CubicOut);
            _ = element.FadeToAsync(1.0, 140, Easing.CubicOut);
        }

        private static void AnimateToastPop(VisualElement element)
        {
            element.CancelAnimations();
            element.Scale = 0.85;
            element.Opacity = 0;
            _ = element.ScaleToAsync(1.0, 200, Easing.SpringOut);
            _ = element.FadeToAsync(1.0, 150, Easing.CubicOut);
        }

        private static void AnimateOverlayFadeIn(VisualElement element)
        {
            element.CancelAnimations();
            element.Opacity = 0;
            _ = element.FadeToAsync(1.0, 220, Easing.CubicOut);
        }

        // The table is laid out for a phone: cards of a fixed size with one line of status above
        // and below them. A bigger system text size wrapped the names letter by letter and pushed
        // the hand over the status bar, so here the text keeps its size. The round and game
        // screens, which scroll, and the other pages still follow the system.
        private static void KeepTextSize(IVisualTreeElement element)
        {
            switch (element)
            {
                case Label label:
                    label.FontAutoScalingEnabled = false;
                    foreach (var span in label.FormattedText?.Spans ?? Enumerable.Empty<Span>())
                    {
                        span.FontAutoScalingEnabled = false;
                    }

                    break;
                case Button button:
                    button.FontAutoScalingEnabled = false;
                    break;
            }

            foreach (var child in element.GetVisualChildren())
            {
                KeepTextSize(child);
            }
        }

        // Card games get abandoned mid-hand when the screen sleeps; keep it awake during play.
        private static void SetKeepScreenOn(bool value)
        {
            try
            {
                DeviceDisplay.Current.KeepScreenOn = value;
            }
            catch
            {
                // Not supported on this platform — fine to ignore.
            }
        }
    }
}
