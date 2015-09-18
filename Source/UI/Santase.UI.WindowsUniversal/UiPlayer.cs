namespace Santase.UI.WindowsUniversal
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class UiPlayer : BasePlayer
    {
        public event EventHandler<ICollection<Card>> RedrawCards;

        public override string Name => "UI Player";

        public override void StartRound(ICollection<Card> cards, Card trumpCard)
        {
            this.RedrawCards?.Invoke(this, cards);
            base.StartRound(cards, trumpCard);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            while (true)
            {
            }
        }
    }
}
