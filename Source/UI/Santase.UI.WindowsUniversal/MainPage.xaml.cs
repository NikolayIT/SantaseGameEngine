// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.WindowsUniversal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Santase.AI.SmartPlayer;
    using Santase.Logic.Cards;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Players;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private UiPlayer uiPlayer;

        private IPlayer smartPlayer;

        private SantaseGame game;

        private CardControl[] playerCardControls;

        public MainPage()
        {
            this.InitializeComponent();
            this.playerCardControls = new[]
                                          {
                                              this.PlayerCard1, this.PlayerCard2, this.PlayerCard3,
                                              this.PlayerCard4, this.PlayerCard5, this.PlayerCard6
                                          };

            this.uiPlayer = new UiPlayer();
            this.uiPlayer.RedrawCards += this.UiPlayerRedrawCards;

            this.smartPlayer = new SmartPlayer();
            this.game = new SantaseGame(this.uiPlayer, this.smartPlayer);

            this.PlayerCard.Hide();
            this.OldPlayerCard.Hide();
            this.OtherPlayerCard.Hide();
            this.OldOtherPlayerCard.Hide();

            Task.Run(() => this.game.Start());

            this.TrumpCard.SetCard(new Card(CardSuit.Club, CardType.Ace));
        }

        private void UiPlayerRedrawCards(object sender, ICollection<Card> cardsCollection)
        {
            this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        var cards = cardsCollection.ToList();
                        for (var i = 0; i < this.playerCardControls.Length; i++)
                        {
                            var playerCardControl = this.playerCardControls[i];
                            if (cards.Count > i)
                            {
                                playerCardControl.SetCard(cards[i]);
                            }
                            else
                            {
                                playerCardControl.Hide();
                            }
                        }
                    }).GetResults();
        }
    }
}
