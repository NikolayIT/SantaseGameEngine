namespace Santase.UI.Pages
{
    using System;
    using System.Linq;

    using Microsoft.Maui.ApplicationModel;
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Graphics;

    using Santase.UI.Game;
    using Santase.UI.Localization;

    public partial class SettingsPage : ContentPage
    {
        // One action at a time, while the page is shown: a double tap on Back goes back once, and
        // the policy link's failure notice belongs to the page visit that opened the browser.
        private readonly PageActions actions = new();

        private const string RepoUrl = "https://github.com/NikolayIT/SantaseGameEngine";

        public SettingsPage()
        {
            this.InitializeComponent();
            PageBackground.CoverTheWholeScreen(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.actions.Activate();

            this.HapticsSwitch.IsToggled = AppSettings.HapticsEnabled;
            this.AssistsSwitch.IsToggled = AppSettings.AssistsEnabled;
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);

            this.RefreshLanguageButtons();
            this.RefreshSpeedButtons();
        }

        protected override void OnDisappearing()
        {
            this.actions.Deactivate();
            base.OnDisappearing();
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await this.actions.RunAsync(() => Shell.Current.GoToAsync(".."));
        }

        private void OnLanguageEn(object? sender, EventArgs e)
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.English);
            this.RefreshLanguageButtons();
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);
        }

        private void OnLanguageBg(object? sender, EventArgs e)
        {
            LocalizationManager.Instance.SetLanguage(LocalizationManager.Bulgarian);
            this.RefreshLanguageButtons();
            this.VersionLabel.Text = LocalizationManager.Instance.Format("Settings_Version", AppInfo.Current.VersionString);
        }

        private void OnSpeedRelaxed(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Relaxed);

        private void OnSpeedNormal(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Normal);

        private void OnSpeedFast(object? sender, EventArgs e) => this.SetSpeed(GameSpeed.Fast);

        private void OnHapticsToggled(object? sender, ToggledEventArgs e)
        {
            AppSettings.HapticsEnabled = e.Value;
        }

        private void OnAssistsToggled(object? sender, ToggledEventArgs e)
        {
            AppSettings.AssistsEnabled = e.Value;
        }

        private async void OnResetStats(object? sender, EventArgs e)
        {
            var mgr = LocalizationManager.Instance;
            var confirmed = await this.DisplayAlertAsync(
                mgr["Reset_Title"],
                mgr["Reset_Message"],
                mgr["Reset_Confirm"],
                mgr["Common_Cancel"]);
            if (!confirmed)
            {
                return;
            }

            PlayerRatingStore.Reset();
            MatchHistoryStore.Clear();
            OpponentStatsStore.Clear(AiOpponents.All.Select(o => o.Id));

            await this.DisplayAlertAsync(mgr["Settings_ResetStats"], mgr["Reset_Done"], "OK");
        }

        private async void OnOpenPrivacyPolicy(object? sender, EventArgs e)
        {
            await PrivacyPolicy.OpenAsync(
                this.actions,
                ExternalBrowser.OpenAsync,
                () =>
                {
                    var text = LocalizationManager.Instance;
                    return this.DisplayAlertAsync(text["Settings_PrivacyPolicy"], text["Settings_PrivacyPolicyUnavailable"], "OK");
                });
        }

        private async void OnOpenGitHub(object? sender, EventArgs e)
        {
            try
            {
                await ExternalBrowser.OpenAsync(RepoUrl);
            }
            catch
            {
                // No browser available — nothing sensible to do.
            }
        }

        private void SetSpeed(GameSpeed speed)
        {
            AppSettings.Speed = speed;
            this.RefreshSpeedButtons();
        }

        private void RefreshLanguageButtons()
        {
            var isBg = LocalizationManager.Instance.IsBulgarian;
            StyleSegment(this.LangEnButton, !isBg);
            StyleSegment(this.LangBgButton, isBg);
        }

        private void RefreshSpeedButtons()
        {
            var speed = AppSettings.Speed;
            StyleSegment(this.SpeedRelaxedButton, speed == GameSpeed.Relaxed);
            StyleSegment(this.SpeedNormalButton, speed == GameSpeed.Normal);
            StyleSegment(this.SpeedFastButton, speed == GameSpeed.Fast);
        }

        private static void StyleSegment(Button button, bool selected)
        {
            button.BackgroundColor = selected
                ? Color.FromArgb("#E2B864")
                : Color.FromArgb("#26FFFFFF");
            button.TextColor = selected
                ? Color.FromArgb("#1A1006")
                : Colors.White;
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
