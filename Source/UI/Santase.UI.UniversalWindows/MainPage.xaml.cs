// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.UniversalWindows
{
    using System;
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
        private readonly UiPlayer player;

        private readonly SantaseGame game;

        public MainPage()
        {
            this.InitializeComponent();

            this.player = new UiPlayer();
            this.player.RedrawCards += this.PlayerOnRedrawCards;
            this.player.CardsLeftChanged += this.PlayerOnCardsLeftChanged;
            this.player.OtherPlayerPlayedCardChanged += this.PlayerOnOtherPlayerPlayedCardChanged;
            this.player.PlayerPlayedCardChanged += this.PlayerOnPlayerPlayedCardChanged;

            this.game = new SantaseGame(this.player, new SmartPlayer());
            Task.Run(() => this.game.Start());
        }

        private void PlayerOnPlayerPlayedCardChanged(object sender, Card card)
        {
            this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.PlayerPlayedCard.SetCard(card);
                });
        }

        private void PlayerOnOtherPlayerPlayedCardChanged(object sender, Card card)
        {
            this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        this.OtherPlayerPlayedCard.SetCard(card);
                    });
        }

        private void PlayerOnCardsLeftChanged(object sender, int i)
        {
            this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        this.CardsLeftInfo.Text = $"Cards left: {i}";
                    });
        }

        private void PlayerOnRedrawCards(object sender, IEnumerable<Card> enumerable)
        {
            this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        var cards = enumerable.ToList();
                        if (cards.Count > 0)
                        {
                            this.PlayerCard1.SetCard(cards[0]);
                        }
                        else
                        {
                            this.PlayerCard1.SetCard(null);
                        }

                        if (cards.Count > 1)
                        {
                            this.PlayerCard2.SetCard(cards[1]);
                        }
                        else
                        {
                            this.PlayerCard2.SetCard(null);
                        }

                        if (cards.Count > 2)
                        {
                            this.PlayerCard3.SetCard(cards[2]);
                        }
                        else
                        {
                            this.PlayerCard3.SetCard(null);
                        }

                        if (cards.Count > 3)
                        {
                            this.PlayerCard4.SetCard(cards[3]);
                        }
                        else
                        {
                            this.PlayerCard4.SetCard(null);
                        }

                        if (cards.Count > 4)
                        {
                            this.PlayerCard5.SetCard(cards[4]);
                        }
                        else
                        {
                            this.PlayerCard5.SetCard(null);
                        }

                        if (cards.Count > 5)
                        {
                            this.PlayerCard6.SetCard(cards[5]);
                        }
                        else
                        {
                            this.PlayerCard6.SetCard(null);
                        }
                    });
        }

        private void PlayerCardTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            this.player.Action((sender as CardControl).Card);
        }
    }
}
