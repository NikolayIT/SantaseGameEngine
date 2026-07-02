namespace Santase.UI.Pages
{
    using System;

    using Microsoft.Maui;
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Controls.Shapes;
    using Microsoft.Maui.Graphics;

    using Santase.UI.Localization;

    public partial class RulesPage : ContentPage
    {
        // (emoji, titleKey, bodyKey) per section, in reading order.
        private static readonly (string Icon, string TitleKey, string BodyKey)[] Sections =
        {
            ("🂡", "Rules_Cards_Title", "Rules_Cards_Body"),
            ("🎴", "Rules_Play_Title", "Rules_Play_Body"),
            ("💍", "Rules_Marriages_Title", "Rules_Marriages_Body"),
            ("9️⃣", "Rules_Nine_Title", "Rules_Nine_Body"),
            ("🔒", "Rules_Closing_Title", "Rules_Closing_Body"),
            ("🃏", "Rules_Endgame_Title", "Rules_Endgame_Body"),
            ("🎯", "Rules_Scoring_Title", "Rules_Scoring_Body"),
            ("🏆", "Rules_Match_Title", "Rules_Match_Body"),
        };

        public RulesPage()
        {
            this.InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.BuildSections();
        }

        private async void OnBack(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        // Rebuilt on every appearance so a language switch (done on the start page) is reflected.
        private void BuildSections()
        {
            var mgr = LocalizationManager.Instance;
            this.SectionsHost.Clear();

            foreach (var (icon, titleKey, bodyKey) in Sections)
            {
                var title = new Label
                {
                    TextColor = Color.FromArgb("#F4D586"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                };
                title.FormattedText = new FormattedString
                {
                    Spans =
                    {
                        new Span { Text = icon + "  " },
                        new Span { Text = mgr[titleKey] },
                    },
                };

                var body = new Label
                {
                    Text = mgr[bodyKey],
                    TextColor = Color.FromArgb("#E8EFE2"),
                    FontSize = 13.5,
                    LineHeight = 1.3,
                };

                this.SectionsHost.Add(new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                    BackgroundColor = Color.FromArgb("#26000000"),
                    Padding = new Thickness(16, 12),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children = { title, body },
                    },
                });
            }
        }
    }
}
