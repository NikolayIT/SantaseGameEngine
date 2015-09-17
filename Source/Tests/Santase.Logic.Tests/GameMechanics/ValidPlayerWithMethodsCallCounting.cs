namespace Santase.Logic.Tests.GameMechanics
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class ValidPlayerWithMethodsCallCounting : BasePlayer
    {
        public override string Name => "Valid player";

        public int StartGameCalledCount { get; private set; }

        public int StartRoundCalledCount { get; private set; }

        public int AddCardCalledCount { get; private set; }

        public int GetTurnCalledCount { get; private set; }

        public int GetTurnWhenFirst { get; private set; }

        public int GetTurnWhenSecond { get; private set; }

        public int EndTurnCalledCount { get; private set; }

        public int EndRoundCalledCount { get; private set; }

        public int EndGameCalledCount { get; private set; }

        public override void StartGame(string otherPlayerIdentifier)
        {
            this.StartGameCalledCount++;
            base.StartGame(otherPlayerIdentifier);
        }

        public override void StartRound(IEnumerable<Card> cards, Card trumpCard)
        {
            this.StartRoundCalledCount++;
            base.StartRound(cards, trumpCard);
        }

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