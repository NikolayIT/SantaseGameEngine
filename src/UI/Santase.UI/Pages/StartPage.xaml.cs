namespace Santase.UI.Pages
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Microsoft.Maui;
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Controls.Shapes;
    using Microsoft.Maui.Graphics;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    public partial class StartPage : ContentPage
    {
        public StartPage()
        {
            this.InitializeComponent();

            this.SelectOpponentCommand = new RelayCommand<AiOpponent>(opponent => _ = this.OnSelectOpponent(opponent));

            // Set once; AiOpponent raises PropertyChanged on a language switch so the bound name /
            // tagline labels refresh in place (no list rebuild needed).
            BindableLayout.SetItemsSource(this.OpponentList, AiOpponents.All);
        }

        public ICommand SelectOpponentCommand { get; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.ApplyTexts();
        }

        private void OnToggleLanguage(object? sender, EventArgs e)
        {
            LocalizationManager.Instance.Toggle();
            this.ApplyTexts();
        }

        // Refreshes everything that is set from code (so it follows a language switch): the rating
        // block, the opponent list (rebuilt so localized taglines re-render), the recent-games list,
        // entry placeholders and the language button. Static XAML text uses {loc:Tr} and updates
        // itself via the LocalizationManager indexer.
        private void ApplyTexts()
        {
            var mgr = LocalizationManager.Instance;

            this.LangButton.Text = mgr.IsBulgarian ? "English" : "Български";

            // The start page is the only screen with a live language toggle, so its text is set
            // here in code (rather than via {loc:Tr} bindings) to guarantee it re-renders on switch.
            this.SubtitleLabel.Text = mgr["Start_Subtitle"];
            this.YouLabel.Text = mgr["Start_You"];
            this.ChooseOpponentLabel.Text = mgr["Start_ChooseOpponent"];
            this.RecentGamesLabel.Text = mgr["Start_RecentGames"];
            this.P2Label.Text = mgr["Start_P2"];
            this.HotSeatButton.Text = mgr["Start_HotSeat"];

            this.RatingCaptionLabel.Text = mgr["Start_YourRating"].ToUpperInvariant();
            this.RatingLabel.Text = PlayerRatingStore.CurrentElo.ToString(CultureInfo.InvariantCulture);

            var games = PlayerRatingStore.GamesPlayed;
            this.RecordLabel.Text = games > 0
                ? mgr.Format("Start_RecordFormat", games, PlayerRatingStore.Wins, PlayerRatingStore.Losses)
                : mgr["Start_NoGames"];

            this.FirstPlayerEntry.Placeholder = mgr["Start_Player1"];
            this.SecondPlayerEntry.Placeholder = mgr["Start_Player2"];

            this.RebuildHistory(mgr);
        }

        private void RebuildHistory(LocalizationManager mgr)
        {
            this.HistoryList.Clear();

            var entries = MatchHistoryStore.All().Take(6).ToList();
            this.NoHistoryLabel.Text = mgr["Start_NoHistory"];
            this.NoHistoryLabel.IsVisible = entries.Count == 0;

            foreach (var entry in entries)
            {
                this.HistoryList.Add(BuildHistoryRow(mgr, entry));
            }
        }

        private static View BuildHistoryRow(LocalizationManager mgr, MatchHistoryEntry entry)
        {
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6) },
                BackgroundColor = entry.Won ? Color.FromArgb("#1FA15A") : Color.FromArgb("#C0504D"),
                Padding = new Thickness(9, 2),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = entry.Won ? mgr["History_Win"] : mgr["History_Loss"],
                    TextColor = Colors.White,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                },
            };

            var score = new Label
            {
                Text = entry.ScoreText,
                TextColor = Colors.White,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
            };

            var versus = new Label
            {
                Text = $"{mgr["History_Vs"]} {entry.OpponentName}",
                TextColor = Color.FromArgb("#C7D2BD"),
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
            };

            var grid = new Grid
            {
                ColumnSpacing = 12,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
            };
            grid.Add(chip, 0, 0);
            grid.Add(score, 1, 0);
            grid.Add(versus, 2, 0);

            return new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                BackgroundColor = Color.FromArgb("#1E000000"),
                Padding = new Thickness(12, 8),
                Content = grid,
            };
        }

        private async void OnPlayHotSeat(object? sender, EventArgs e)
        {
            var mgr = LocalizationManager.Instance;
            var first = string.IsNullOrWhiteSpace(this.FirstPlayerEntry.Text) ? mgr["Start_Player1"] : this.FirstPlayerEntry.Text.Trim();
            var second = string.IsNullOrWhiteSpace(this.SecondPlayerEntry.Text) ? mgr["Start_Player2"] : this.SecondPlayerEntry.Text.Trim();

            var query = $"?mode={GameMode.HotSeat}&first={Uri.EscapeDataString(first)}&second={Uri.EscapeDataString(second)}";
            await Shell.Current.GoToAsync($"GamePage{query}");
        }

        private Task OnSelectOpponent(AiOpponent? opponent)
        {
            if (opponent == null)
            {
                return Task.CompletedTask;
            }

            var mgr = LocalizationManager.Instance;
            var first = string.IsNullOrWhiteSpace(this.FirstPlayerEntry.Text) ? mgr["Start_Player1"] : this.FirstPlayerEntry.Text.Trim();
            var query = $"?mode={GameMode.VsAi}&first={Uri.EscapeDataString(first)}&opponent={Uri.EscapeDataString(opponent.Id)}";
            return Shell.Current.GoToAsync($"GamePage{query}");
        }
    }
}
