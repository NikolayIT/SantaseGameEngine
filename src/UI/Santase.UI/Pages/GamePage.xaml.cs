namespace Santase.UI.Pages
{
    using System.Collections.Generic;

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
            this.BindingContext = this.viewModel;

            this.viewModel.StartGame();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (this.viewModel != null)
            {
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
    }
}
