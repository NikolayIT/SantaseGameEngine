namespace Santase.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class SmartPlayer : BasePlayer
    {
        private readonly ICollection<Card> playedCards = new List<Card>();

        public override string Name => "Smart Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard.Suit);
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

            // Biggest non-trump card
            var cardToPlay =
                possibleCardsToPlay.Where(x => x.Suit == context.TrumpCard.Suit)
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

        private PlayerAction ChooseCardWhenPlayingFirstAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            return this.ChooseCardWhenPlayingFirstAndRulesDoNotApply(context, possibleCardsToPlay);
        }

        private PlayerAction ChooseCardWhenPlayingSecondAndRulesApply(
            PlayerTurnContext context,
            ICollection<Card> possibleCardsToPlay)
        {
            return this.ChooseCardWhenPlayingSecondAndRulesDoNotApply(context, possibleCardsToPlay);
        }
    }
}
