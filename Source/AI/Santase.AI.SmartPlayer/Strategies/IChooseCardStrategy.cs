namespace Santase.AI.SmartPlayer.Strategies
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public interface IChooseCardStrategy
    {
        PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay);
    }
}
