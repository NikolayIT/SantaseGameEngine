// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.WindowsUniversal
{
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private UiPlayer uiPlayer;

        private IPlayer smartPlayer;

        private SantaseGame game;

        public MainPage()
        {
            this.InitializeComponent();

            this.uiPlayer = new UiPlayer();
            this.smartPlayer = new SmartPlayer();
            this.game = new SantaseGame(this.uiPlayer, this.smartPlayer);

            this.PlayerCard.Hide();
            this.OldPlayerCard.Hide();
            this.OtherPlayerCard.Hide();
            this.OldOtherPlayerCard.Hide();

            //// this.game.Start();

            this.TrumpCard.SetCard(new Card(CardSuit.Club, CardType.Ace));
        }
    }
}
