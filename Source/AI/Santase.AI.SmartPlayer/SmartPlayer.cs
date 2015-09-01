namespace Santase.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class SmartPlayer : BasePlayer
    {
        private readonly IList<Card> playedCards = new List<Card>();

        public override string Name => "Smart Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // Always change trump as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard.Suit);
            }

            // TODO: Close the game?

            return this.ChooseCard(context);
        }

        // TODO: Choose appropriate card
        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.GetPossibleCardsToPlay(context);
            if (context.IsFirstPlayerTurn)
            {
                foreach (var card in possibleCardsToPlay)
                {
                    if (this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) != Announce.None)
                    {
                        return this.PlayCard(card);
                    }
                }
            }

            var cardToPlay = possibleCardsToPlay.First();
            return this.PlayCard(cardToPlay);
        }

        private IList<Card> GetPossibleCardsToPlay(PlayerTurnContext context)
        {
            var possibleCardsToPlay = new List<Card>();
            foreach (var card in this.Cards)
            {
                var action = PlayerAction.PlayCard(card);
                if (this.PlayerActionValidator.IsValid(action, context, this.Cards))
                {
                    possibleCardsToPlay.Add(card);
                }
            }

            return possibleCardsToPlay;
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
            // TODO: Count the points of the other player
        }
    }
}
