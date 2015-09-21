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
    using Windows.UI.Xaml.Input;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private readonly UiPlayer uiPlayer;

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
            this.uiPlayer.RedrawCurrentAndOtherPlayerTotalPoints += this.UiPlayerOnRedrawCurrentAndOtherPlayerTotalPoints;
            this.uiPlayer.RedrawPlayedCards += this.UiPlayerOnRedrawPlayedCards;

            IPlayer smartPlayer = new SmartPlayer();
            this.game = new SantaseGame(this.uiPlayer, smartPlayer);

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

        private void UiPlayerOnRedrawPlayedCards(object sender, Tuple<Card, Card> playedCards)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.Dispatcher.RunAsync(
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.OldPlayerCard.SetCard(playedCards.Item1);
                    this.OldOtherPlayerCard.SetCard(playedCards.Item2);
                });
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
                    this.PlayerRoundPoints.Text = pointsInfo.Item1.ToString();
                    this.OtherPlayerRoundPoints.Text = pointsInfo.Item2.ToString();
                });
        }

        private void UiPlayerOnRedrawCurrentAndOtherPlayerTotalPoints(object sender, Tuple<int, int> pointsInfo)
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
                        if (cardsLeft == 0)
                        {
                            this.CardsLeftInDeck.Text = this.TrumpCard.Card.Suit.ToFriendlyString();
                            this.TrumpCard.Visibility = Visibility.Collapsed;
                            this.DeckCards.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            this.CardsLeftInDeck.Text = cardsLeft.ToString();
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
