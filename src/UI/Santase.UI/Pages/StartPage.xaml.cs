namespace Santase.UI.Pages
{
    using Santase.UI.Game;

    public partial class StartPage : ContentPage
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        private async void OnPlayEasy(object? sender, EventArgs e)
        {
            await this.Launch(GameMode.VsEasy, "Computer");
        }

        private async void OnPlayHard(object? sender, EventArgs e)
        {
            await this.Launch(GameMode.VsHard, "Smart Player");
        }

        private async void OnPlayHotSeat(object? sender, EventArgs e)
        {
            var p2 = string.IsNullOrWhiteSpace(this.SecondPlayerEntry.Text) ? "Player 2" : this.SecondPlayerEntry.Text.Trim();
            await this.Launch(GameMode.HotSeat, p2);
        }

        private Task Launch(GameMode mode, string defaultSecondName)
        {
            var first = string.IsNullOrWhiteSpace(this.FirstPlayerEntry.Text) ? "Player 1" : this.FirstPlayerEntry.Text.Trim();
            var second = mode == GameMode.HotSeat
                ? (string.IsNullOrWhiteSpace(this.SecondPlayerEntry.Text) ? "Player 2" : this.SecondPlayerEntry.Text.Trim())
                : defaultSecondName;

            var query = $"?mode={mode}&first={Uri.EscapeDataString(first)}&second={Uri.EscapeDataString(second)}";
            return Shell.Current.GoToAsync($"GamePage{query}");
        }
    }
}
