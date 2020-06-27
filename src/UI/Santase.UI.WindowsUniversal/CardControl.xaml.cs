// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Santase.UI.WindowsUniversal
{
    using System;
    using Santase.Logic.Cards;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class CardControl
    {
        public CardControl()
        {
            this.InitializeComponent();
        }

        public Card Card { get; private set; }

        public void SetCard(Card card)
        {
            this.Card = card;
            this.Image.Source = ImageFromRelativePath(
                this,
                card != null ? $"Assets/Cards/{card.Type}{card.Suit}.png" : "Assets/Cards/Back.png");
            this.Image.Visibility = Visibility.Visible;
            this.Image.Opacity = 1;
        }

        public void Hide()
        {
            this.Image.Visibility = Visibility.Collapsed;
        }

        public void Transparent()
        {
            this.Image.Opacity = 0;
        }

        // http://stackoverflow.com/questions/11814917/how-to-reference-image-source-files-that-are-packaged-with-my-metro-style-app
        private static BitmapImage ImageFromRelativePath(FrameworkElement parent, string path)
        {
            var uri = new Uri(parent.BaseUri, path);
            var bmp = new BitmapImage { UriSource = uri };
            return bmp;
        }
    }
}
