// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Santase.UI.UniversalWindows
{
    using System;
    using Santase.Logic.Cards;
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
            this.image.Source = new BitmapImage(new Uri($"Assets/Cards/{card.Type}{card.Suit}.png"));
        }
    }
}
