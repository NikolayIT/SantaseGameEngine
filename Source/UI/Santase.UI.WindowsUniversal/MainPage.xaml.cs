// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.WindowsUniversal
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

            this.PlayerCard.Hide();
            this.OldPlayerCard.Hide();
            this.OtherPlayerCard.Hide();
            this.OldOtherPlayerCard.Hide();
            this.TrumpCard.SetCard(new Card(CardSuit.Club, CardType.Ace));
        }
    }
}
