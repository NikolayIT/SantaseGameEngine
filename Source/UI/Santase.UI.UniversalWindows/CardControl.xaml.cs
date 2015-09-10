using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Santase.UI.UniversalWindows
{
    using Windows.UI.Xaml.Media.Imaging;

    using Santase.Logic.Cards;

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
