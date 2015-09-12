// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Santase.UI.UniversalWindows
{
    using System;
    using Santase.Logic.Cards;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class CardControl : UserControl
    {
        public CardControl()
        {
            this.InitializeComponent();
        }

        public void SetCard(Card card)
        {
            this.image.Source = ImageFromRelativePath(this, $"Assets/Cards/{card.Type}{card.Suit}.png");
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
