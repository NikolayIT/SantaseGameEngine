namespace Santase.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class SmartPlayerOld : BasePlayer
    {
        private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly OpponentSuitCardsProvider opponentSuitCardsProvider = new OpponentSuitCardsProvider();

        public override string Name => "Smart Player Old";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.CloseGame(context))
            {
                return PlayerAction.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void EndRound()
        {
            this.playedCards.Clear();
            base.EndRound();
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
        }

        // TODO: Close the game?
        private bool CloseGame(PlayerTurnContext context)
        {
            // 5 trump cards => close the game
            return this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards)
                   && this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5;
        }

        // TODO: Choose appropriate card
        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            return context.State.ShouldObserveRules
                       ? (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesApply(context, possibleCardsToPlay))
                       : (context.IsFirstPlayerTurn
                              ? this.ChooseCardWhenPlayingFirstAndRulesDoNotApply(context, possibleCardsToPlay)
                              : this.ChooseCardWhenPlayingSecondAndRulesDoNotApply(context, possibleCardsToPlay));
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesDoNotApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            // Smallest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            cardToPlay = possibleCardsToPlay.OrderByDescending(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
            if (action != null)
            {
                return action;
            }

            var myBiggestTrumpCard =
                this.Cards.Where(x => x.Suit == context.TrumpCard.Suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            var playerTrumpCards = this.opponentSuitCardsProvider.GetOpponentCards(
                this.Cards,
                this.playedCards,
                context.TrumpCard.Suit);
            if (myBiggestTrumpCard != null && playerTrumpCards.Count == 1
                && playerTrumpCards.First().GetValue() < myBiggestTrumpCard.GetValue())
            {
                return this.PlayCard(myBiggestTrumpCard);
            }

            // Biggest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit != context.TrumpCard.Suit)
                    .OrderByDescending(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            cardToPlay = possibleCardsToPlay.OrderByDescending(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesDoNotApply(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Heuristic
            if ((context.FirstPlayedCard.Type == CardType.Ace || context.FirstPlayedCard.Type == CardType.Ten)
                && possibleCardsToPlay.Contains(new Card(context.TrumpCard.Suit, CardType.Jack)))
            {
                return this.PlayCard(new Card(context.TrumpCard.Suit, CardType.Jack));
            }

            // Opponent played non-trump card => Play Ace or Ten if possible
            if (context.FirstPlayedCard.Suit != context.TrumpCard.Suit)
            {
                if (possibleCardsToPlay.Contains(new Card(context.FirstPlayedCard.Suit, CardType.Ace)))
                {
                    return this.PlayCard(new Card(context.FirstPlayedCard.Suit, CardType.Ace));
                }

                if (context.FirstPlayedCard.Type != CardType.Ace &&
                    possibleCardsToPlay.Contains(new Card(context.FirstPlayedCard.Suit, CardType.Ten)))
                {
                    return this.PlayCard(new Card(context.FirstPlayedCard.Suit, CardType.Ten));
                }
            }

            // Smallest trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
                    .OrderBy(x => x.GetValue())
                    .FirstOrDefault();
            if (cardToPlay != null)
            {
                return this.PlayCard(cardToPlay);
            }

            cardToPlay = possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault();
            return this.PlayCard(cardToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            return this.ChooseCardWhenPlayingSecondAndRulesDoNotApply(context, possibleCardsToPlay);
        }

        private PlayerAction TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Choose card with announce 40 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Forty)
                {
                    return this.PlayCard(card);
                }
            }

            // Choose card with announce 20 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Twenty)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }
    }
}
