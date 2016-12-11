namespace Santase.AI.SmartPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.AI.SmartPlayer.Strategies;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    // Overall strategy can be based on the game score. When opponent is close to the winning the player should be riskier.
    public class SmartPlayer : BasePlayer
    {
        private readonly CardTracker cardTracker = new CardTracker();

        private readonly IChooseCardStrategy chooseBestCardStrategy;

        public SmartPlayer()
        {
            this.chooseBestCardStrategy = new ChooseBestCardToPlayStrategy(this.cardTracker, this.AnnounceValidator, this.Cards);
        }

        public override string Name => "Smart Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            // When possible change the trump card as this is almost always a good move
            // Changing trump can be non-optimal when:
            // 1. Current player is planning to close the game and don't want to give additional points to his opponent
            // 2. The other player will close the game and you will give him additional points by giving him bigger trump card instead of 9
            // 3. Want to confuse the opponent
            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                this.cardTracker.ChangeTrumpCard(context.TrumpCard);
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.CloseGame(context))
            {
                GlobalStats.GamesClosedByPlayer++;
                return this.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);

            this.cardTracker.Clear();
            foreach (var card in cards)
            {
                this.cardTracker.UnknownCards.Remove(card);
            }
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.cardTracker.UnknownCards.Remove(card);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            if (context.CardsLeftInDeck == 2)
            {
                this.cardTracker.UnknownCards.Add(context.TrumpCard);
            }

            this.cardTracker.CardPlayed(context.FirstPlayedCard);
            this.cardTracker.CardPlayed(context.SecondPlayedCard);
        }

        // TODO: Improve close game decision
        private bool CloseGame(PlayerTurnContext context)
        {
            if (!this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards))
            {
                return false;
            }

            if (this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5)
            {
                return true;
            }

            return false;
        }

        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            this.cardTracker.TrumpCardSaw(context.TrumpCard);
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            var action = this.chooseBestCardStrategy.ChooseCard(context, possibleCardsToPlay);

            if (action.Type == PlayerActionType.ChangeTrump)
            {
                return this.ChangeTrump(action.Card);
            }

            if (action.Type == PlayerActionType.CloseGame)
            {
                return this.CloseGame();
            }

            return this.PlayCard(action.Card);
        }
    }
}
