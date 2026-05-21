namespace Santase.UI.Pages
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Microsoft.Maui.Controls;

    using Santase.UI.Game;

    public partial class StartPage : ContentPage
    {
        public StartPage()
        {
            this.InitializeComponent();

            this.SelectOpponentCommand = new RelayCommand<AiOpponent>(opponent => _ = this.OnSelectOpponent(opponent));
            BindableLayout.SetItemsSource(this.OpponentList, AiOpponents.All);
        }

        public ICommand SelectOpponentCommand { get; }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.RatingLabel.Text = $"Your rating: {PlayerRatingStore.CurrentElo}";

            var games = PlayerRatingStore.GamesPlayed;
            this.RecordLabel.Text = games > 0
                ? $"{games} game{(games == 1 ? string.Empty : "s")} · {PlayerRatingStore.Wins}W – {PlayerRatingStore.Losses}L"
                : "No games yet — beat the Dummy to climb";
        }

        private async void OnPlayHotSeat(object? sender, EventArgs e)
        {
            var first = string.IsNullOrWhiteSpace(this.FirstPlayerEntry.Text) ? "Player 1" : this.FirstPlayerEntry.Text.Trim();
            var second = string.IsNullOrWhiteSpace(this.SecondPlayerEntry.Text) ? "Player 2" : this.SecondPlayerEntry.Text.Trim();

            var query = $"?mode={GameMode.HotSeat}&first={Uri.EscapeDataString(first)}&second={Uri.EscapeDataString(second)}";
            await Shell.Current.GoToAsync($"GamePage{query}");
        }

        private Task OnSelectOpponent(AiOpponent? opponent)
        {
            if (opponent == null)
            {
                return Task.CompletedTask;
            }

            var first = string.IsNullOrWhiteSpace(this.FirstPlayerEntry.Text) ? "Player 1" : this.FirstPlayerEntry.Text.Trim();
            var query = $"?mode={GameMode.VsAi}&first={Uri.EscapeDataString(first)}&opponent={Uri.EscapeDataString(opponent.Id)}";
            return Shell.Current.GoToAsync($"GamePage{query}");
        }
    }
}
