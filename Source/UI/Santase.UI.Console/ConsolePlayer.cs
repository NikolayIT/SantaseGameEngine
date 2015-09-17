namespace Santase.UI.Console
{
    using System;
    using System.Linq;
    using System.Threading;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class ConsolePlayer : BasePlayer
    {
        private readonly int row;

        private readonly int col;

        public ConsolePlayer(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public override string Name => "Console Player";

        public override void AddCard(Card card)
        {
            base.AddCard(card);

            Console.SetCursorPosition(this.col, this.row);
            foreach (var item in this.Cards)
            {
                Console.Write("{0} ", item);
            }

            Thread.Sleep(150);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.PrintGameInfo(context);
            while (true)
            {
                PlayerAction playerAction;

                Console.SetCursorPosition(0, this.row + 1);
                Console.Write(new string(' ', 79));
                Console.SetCursorPosition(0, this.row + 1);
                Console.Write("Turn? [1-{0}]=Card", this.Cards.Count);
                if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
                {
                    Console.Write("; [T]=Change trump");
                }

                if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
                {
                    Console.Write("; [C]=Close");
                }

                Console.Write(": ");
                var userActionAsString = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userActionAsString))
                {
                    Console.WriteLine("Empty turn!                 ");
                    continue;
                }

                if (userActionAsString[0] >= '1' && userActionAsString[0] <= '6')
                {
                    var cardIndex = int.Parse(userActionAsString[0].ToString()) - 1;
                    if (cardIndex >= this.Cards.Count)
                    {
                        Console.WriteLine("Invalid card!              ");
                        continue;
                    }

                    var card = this.Cards.ToList()[cardIndex];
                    var possibleAnnounce = this.AnnounceValidator.GetPossibleAnnounce(
                        this.Cards,
                        card,
                        context.TrumpCard,
                        context.IsFirstPlayerTurn);
                    Console.WriteLine(possibleAnnounce);

                    playerAction = PlayerAction.PlayCard(card);
                }
                else if (userActionAsString[0] == 'T')
                {
                    playerAction = PlayerAction.ChangeTrump();
                }
                else if (userActionAsString[0] == 'C')
                {
                    playerAction = PlayerAction.CloseGame();
                }
                else
                {
                    Console.WriteLine("Invalid turn!                ");
                    continue;
                }

                if (this.PlayerActionValidator.IsValid(playerAction, context, this.Cards))
                {
                    if (playerAction.Type == PlayerActionType.PlayCard)
                    {
                        this.Cards.Remove(playerAction.Card);
                    }

                    if (playerAction.Type == PlayerActionType.ChangeTrump)
                    {
                        this.Cards.Remove(new Card(context.TrumpCard.Suit, CardType.Nine));
                    }

                    this.PrintGameInfo(context);

                    return playerAction;
                }
                else
                {
                    Console.WriteLine("Invalid action!                  ");
                }
            }
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            Console.SetCursorPosition(20, 9);
            Console.WriteLine($"{context.FirstPlayedCard} - {context.SecondPlayedCard}             ");
            Thread.Sleep(3000);
        }

        private void PrintGameInfo(PlayerTurnContext context)
        {
            Console.SetCursorPosition(20, 9);
            Console.WriteLine($"{context.FirstPlayedCard} - {context.SecondPlayedCard}");

            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Trump card: {0}            ", context.TrumpCard);
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Cards left in deck: {0}    ", context.CardsLeftInDeck);
            Console.SetCursorPosition(0, 2);
            Console.WriteLine("Board: {0}{1}              ", context.FirstPlayedCard, context.SecondPlayedCard);
            Console.SetCursorPosition(0, 3);
            Console.WriteLine("Game state: {0}            ", context.State.GetType().Name);
        }
    }
}
