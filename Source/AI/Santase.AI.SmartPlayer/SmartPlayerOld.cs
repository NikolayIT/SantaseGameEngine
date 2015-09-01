namespace Santase.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class SmartPlayerOld : BasePlayer
    {
        public override string Name => "Smart Player Old";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // Always change trump as this is always a good move
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                this.Cards.Remove(new Card(context.TrumpCard.Suit, CardType.Nine));
                return PlayerAction.ChangeTrump();
            }

            // TODO: Close the game?
            // TODO: Choose appropriate card
            var possibleCardsToPlay = new List<Card>();
            foreach (var card in this.Cards)
            {
                var action = PlayerAction.PlayCard(card);

                if (this.PlayerActionValidator.IsValid(action, context, this.Cards))
                {
                    // If 20 or 40 => return the card (Just for the test)
                    if (action.Announce != Announce.None)
                    {
                        this.Cards.Remove(card);
                        return action;
                    }

                    possibleCardsToPlay.Add(card);
                }
            }

            var cardToPlay = possibleCardsToPlay.OrderBy(x => x.Type).First();
            this.Cards.Remove(cardToPlay);
            return PlayerAction.PlayCard(cardToPlay);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            // TODO: Count the points of the other player
        }
    }
}
