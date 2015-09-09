namespace Santase.Logic.Tests.GameMechanics
{
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class ValidPlayerWithMethodsCallCounting : BasePlayer
    {
        public override string Name => "Valid player";

        public int GetTurnCalledCount { get; private set; }

        public int GetTurnWhenFirst { get; private set; }

        public int GetTurnWhenSecond { get; private set; }

        public int AddCardCalledCount { get; private set; }

        public int EndTurnCalledCount { get; private set; }

        public int EndRoundCalledCount { get; private set; }

        public int EndGameCalledCount { get; private set; }

        public override void AddCard(Card card)
        {
            this.AddCardCalledCount++;
            base.AddCard(card);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.GetTurnCalledCount++;
            if (context.IsFirstPlayerTurn)
            {
                this.GetTurnWhenFirst++;
            }
            else
            {
                this.GetTurnWhenSecond++;
            }

            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            return this.PlayCard(possibleCardsToPlay.First());
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.EndTurnCalledCount++;
            base.EndTurn(context);
        }

        public override void EndRound()
        {
            this.EndRoundCalledCount++;
            base.EndRound();
        }

        public override void EndGame(bool amIWinner)
        {
            this.EndGameCalledCount++;
        }
    }
}