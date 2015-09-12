// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.UniversalWindows
{
    using Santase.Logic.Cards;
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
            this.PlayerCard1.SetCard(deck.GetNextCard());
            this.PlayerCard2.SetCard(deck.GetNextCard());
            this.PlayerCard3.SetCard(deck.GetNextCard());
            this.PlayerCard4.SetCard(deck.GetNextCard());
            this.PlayerCard5.SetCard(deck.GetNextCard());
            this.PlayerCard6.SetCard(deck.GetNextCard());
            this.TrumpCard.SetCard(deck.TrumpCard);
        }
    }
}
