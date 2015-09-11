// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

using Santase.Logic.Cards;

namespace Santase.UI.UniversalWindows
{
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            var deck = new Deck();
            this.Control01.SetCard(deck.GetNextCard());
            this.Control02.SetCard(deck.GetNextCard());
            this.Control03.SetCard(deck.GetNextCard());
            this.Control04.SetCard(deck.GetNextCard());
            this.Control05.SetCard(deck.GetNextCard());
            this.Control06.SetCard(deck.GetNextCard());
        }
    }
}
