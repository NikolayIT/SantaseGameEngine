namespace Santase.AI.SmartPlayer.Strategies
{
    using System;
    using System.Collections.Generic;

    using Santase.AI.SmartPlayer.Helpers;
    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;

    // TODO: Improve choosing best card to play
    public class ChooseBestCardToPlayStrategy : BaseChooseCardStrategy
    {
        private readonly IChooseCardStrategy playingFirstAndRulesApplyStrategy;

        private readonly IChooseCardStrategy playingFirstAndRulesDoNotApplyStrategy;

        private readonly IChooseCardStrategy playingSecondAndRulesApplyStrategy;

        private readonly IChooseCardStrategy playingSecondAndRulesDoNotApplyStrategy;

        public ChooseBestCardToPlayStrategy(CardTracker cardTracker, IAnnounceValidator announceValidator, ICollection<Card> cards)
            : base(cardTracker, announceValidator, cards)
        {
            this.playingFirstAndRulesApplyStrategy = new PlayingFirstAndRulesApplyStrategy(cardTracker, announceValidator, cards);
            this.playingFirstAndRulesDoNotApplyStrategy = new PlayingFirstAndRulesDoNotApplyStrategy(cardTracker, announceValidator, cards);
            this.playingSecondAndRulesApplyStrategy = new PlayingSecondAndRulesApplyStrategy(cardTracker, announceValidator, cards);
            this.playingSecondAndRulesDoNotApplyStrategy = new PlayingSecondAndRulesDoNotApplyStrategy(cardTracker, announceValidator, cards);
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            var action = context.State.ShouldObserveRules
                       ? (context.IsFirstPlayerTurn
                              ? this.playingFirstAndRulesApplyStrategy.ChooseCard(context, possibleCardsToPlay)
                              : this.playingSecondAndRulesApplyStrategy.ChooseCard(context, possibleCardsToPlay))
                       : (context.IsFirstPlayerTurn
                              ? this.playingFirstAndRulesDoNotApplyStrategy.ChooseCard(context, possibleCardsToPlay)
                              : this.playingSecondAndRulesDoNotApplyStrategy.ChooseCard(context, possibleCardsToPlay));

            return action;
        }
    }
}
