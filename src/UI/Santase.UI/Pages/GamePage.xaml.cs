namespace Santase.UI.Pages
{
    using System.Collections.Generic;

    using Santase.UI.Game;

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
            // Confirm with the user before abandoning the game.
            this.viewModel?.LeaveCommand.Execute(null);
            return true;
        }
    }
}
