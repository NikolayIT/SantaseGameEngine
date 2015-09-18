// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Santase.UI.WindowsUniversal
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
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly UiPlayer uiPlayer;

        private readonly IPlayer smartPlayer;

        private readonly SantaseGame game;

        private readonly CardControl[] playerCardControls;

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
            this.uiPlayer.RedrawTrumpCard += this.UiPlayerRedrawTrumpCard;
            this.uiPlayer.RedrawNumberOfCardsLeftInDeck += this.UiPlayerOnRedrawNumberOfCardsLeftInDeck;
            this.uiPlayer.RedrawPlayerPlayedCard += this.UiPlayerOnRedrawPlayerPlayedCard;
            this.uiPlayer.RedrawOtherPlayerPlayedCard += this.UiPlayerOnRedrawOtherPlayerPlayedCard;
            this.uiPlayer.RedrawCurrentAndOtherPlayerRoundPoints += this.UiPlayerOnRedrawCurrentAndOtherPlayerRoundPoints;

            this.smartPlayer = new SmartPlayer();
            this.game = new SantaseGame(this.uiPlayer, this.smartPlayer);

            this.PlayerCard.Transparent();
            this.OldPlayerCard.Transparent();
            this.OtherPlayerCard.Transparent();
            this.OldOtherPlayerCard.Transparent();

            Task.Run(() => this.game.Start());
        }

        private void PlayerCardTapped(object sender, TappedRoutedEventArgs eventArgs)
        {
            this.uiPlayer.Action(PlayerAction.PlayCard((sender as CardControl).Card));
        }

        private void UiPlayerRedrawTrumpCard(object sender, Card card)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        if (card != null)
                        {
                            this.TrumpCard.SetCard(card);
                        }
                        else
                        {
                            this.TrumpCard.Transparent();
                        }
                    });
        }

        private void UiPlayerOnRedrawPlayerPlayedCard(object sender, Card card)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        //// if (card == null)
                        //// {
                        ////     this.PlayerCard.Transparent();
                        //// }
                        //// else
                        //// {
                        ////     this.PlayerCard.SetCard(card);
                        //// }
                    });
            Task.Delay(2000);
        }

        private void UiPlayerOnRedrawOtherPlayerPlayedCard(object sender, Card card)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        if (card == null)
                        {
                            this.OtherPlayerCard.Transparent();
                        }
                        else
                        {
                            this.OtherPlayerCard.SetCard(card);
                        }
                    });
        }

        private void UiPlayerOnRedrawCurrentAndOtherPlayerRoundPoints(object sender, Tuple<int, int> pointsInfo)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.PlayerTotalPoints.Text = pointsInfo.Item1.ToString();
                    this.OtherPlayerTotalPoints.Text = pointsInfo.Item2.ToString();
                });
        }

        private void UiPlayerRedrawCards(object sender, ICollection<Card> cardsCollection)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        var cards =
                            cardsCollection.OrderBy(x => x.Suit.MapAsSortableByColor()).ThenByDescending(x => x.GetValue()).ToList();
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
                    });
        }

        private void UiPlayerOnRedrawNumberOfCardsLeftInDeck(object sender, int cardsLeft)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        this.CardsLeftInDeck.Text = cardsLeft.ToString();
                        if (cardsLeft == 0)
                        {
                            this.CardsLeftInDeck.Visibility = Visibility.Collapsed;
                            this.TrumpCard.Visibility = Visibility.Collapsed;
                            this.DeckCards.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            this.CardsLeftInDeck.Visibility = Visibility.Visible;
                            this.TrumpCard.Visibility = Visibility.Visible;
                            this.DeckCards.Visibility = Visibility.Visible;
                        }
                    });
        }

        private void TrumpCardOnTapped(object sender, TappedRoutedEventArgs e)
        {
            this.uiPlayer.Action(PlayerAction.ChangeTrump());
        }
    }
}
