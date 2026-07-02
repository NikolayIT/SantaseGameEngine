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
    public partial class GamePage : ContentPage
    {
        private GameSession? session;

        private GameViewModel? viewModel;

        private bool started;

        public GamePage()
        {
            this.InitializeComponent();
        }

        public string ModeRaw { get; set; } = nameof(GameMode.VsAi);

        public string FirstName { get; set; } = "Player 1";

        public string SecondName { get; set; } = "Player 2";

        public string OpponentId { get; set; } = "smart";

        protected override void OnAppearing()
        {
            base.OnAppearing();

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

            this.session = new GameSession(mode, this.FirstName, secondName, opponent);
            this.viewModel = new GameViewModel(this.session, this.Dispatcher);
            this.viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
            this.BindingContext = this.viewModel;

            this.viewModel.StartGame();
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
            _ = this.ConfirmAndLeaveAsync();
            return true; // Handled — leaving goes through the confirmation prompt below.
        }

        private async void OnMenuClicked(object? sender, EventArgs e)
        {
            await this.ConfirmAndLeaveAsync();
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
            if (confirmed)
            {
                this.viewModel.CloseGameCommand.Execute(null);
            }
        }

        // Confirms before abandoning a game that is still in progress; if the game is already over
        // (or never started) it just leaves. The game-over overlay's own "Back to menu" button
        // calls LeaveCommand directly and is intentionally not gated.
        private async Task ConfirmAndLeaveAsync()
        {
            if (this.viewModel == null)
            {
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

            this.viewModel.LeaveCommand.Execute(null);
        }

        // View-only animations reacting to view-model state changes. PropertyChanged is always
        // raised on the UI thread (the view model dispatches), so animating here is safe.
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var vm = this.viewModel;
            if (vm == null)
            {
                return;
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
