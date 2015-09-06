﻿namespace Santase.AI.DummyPlayer
{
    using System.Linq;
    
    using Santase.Logic.Extensions;
    using Santase.Logic.Players;

    /// <summary>
    /// This dummy player follows the rules and always plays random card.
    /// Dummy never changes the trump or closes the game.
    /// </summary>
    public class DummyPlayer : BasePlayer
    {
        public DummyPlayer(string name = "Dummy Player")
        {
            this.Name = name;
        }

        public override string Name { get; }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            var shuffledCards = possibleCardsToPlay.Shuffle();
            var cardToPlay = shuffledCards.First();
            var action = PlayerAction.PlayCard(cardToPlay);
            return action;
        }
    }
}
