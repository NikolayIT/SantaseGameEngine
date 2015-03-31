using Santase.Logic;
using Santase.Logic.Cards;
using Santase.Logic.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Santase.ConsoleUI
{
    public class ConsolePlayer : BasePlayer
    {
        int row;
        int col;

        public ConsolePlayer(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);

            Console.SetCursorPosition(this.col, this.row);
            foreach (var item in this.cards)
            {
                Console.Write("{0} ", item.ToString());
            }

            Thread.Sleep(150);
        }

        public override PlayerAction GetTurn(
            PlayerTurnContext context,
            IPlayerActionValidater actionValidater)
        {
            PrintGameInfo(context);
            while (true)
            {
                PlayerAction playerAction = null;

                Console.SetCursorPosition(0, this.row + 1);
                Console.Write(new string(' ', 79));
                Console.SetCursorPosition(0, this.row + 1);
                Console.Write("Turn? [1-{0}]=Card{1}",
                    this.cards.Count,
                    context.AmITheFirstPlayer ? "; [T]=Change trump; [C]=Close: " : ": ");
                var userActionAsString = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userActionAsString))
                {
                    Console.WriteLine("Empty turn!                 ");
                    continue;
                }

                if (userActionAsString[0] >= '1'
                    && userActionAsString[0] <= '6')
                {
                    var cardIndex =
                        int.Parse(userActionAsString[0].ToString()) - 1;
                    if (cardIndex >= this.cards.Count)
                    {
                        Console.WriteLine("Invalid card!              ");
                        continue;
                    }

                    var card = this.cards[cardIndex];
                    var possibleAnnounce = Announce.None;

                    if (context.AmITheFirstPlayer)
                    {
                        possibleAnnounce = this.PossibleAnnounce(card, context.TrumpCard);
                        if (possibleAnnounce != Announce.None)
                        {
                            while (true)
                            {
                                Console.SetCursorPosition(0, this.row + 2);
                                Console.Write("Announce {0} [Y]/[N]?         ", possibleAnnounce.ToString());
                                var userInput = Console.ReadLine();
                                if (string.IsNullOrWhiteSpace(userInput))
                                {
                                    Console.WriteLine("Please enter [Y] or [N]         ");
                                    continue;
                                }

                                if (userInput[0] == 'N')
                                {
                                    possibleAnnounce = Announce.None;
                                }
                                else if (userInput[0] == 'Y')
                                {
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Please enter [Y] or [N]         ");
                                    continue;
                                }
                            }
                        }
                    }

                    playerAction =
                        new PlayerAction(
                            PlayerActionType.PlayCard,
                            card,
                            possibleAnnounce);
                }
                else if (userActionAsString[0] == 'T')
                {
                    playerAction =
                        new PlayerAction(
                            PlayerActionType.ChangeTrump,
                            null,
                            Announce.None);
                }
                else if (userActionAsString[0] == 'C')
                {
                    playerAction =
                        new PlayerAction(
                            PlayerActionType.CloseGame,
                            null,
                            Announce.None);
                }
                else
                {
                    Console.WriteLine("Invalid turn!                ");
                    continue;
                }

                if (actionValidater.IsValid(playerAction, context, this.cards))
                {
                    if (playerAction.Type == PlayerActionType.PlayCard)
                    {
                        this.cards.Remove(playerAction.Card);
                    }

                    if (playerAction.Type == PlayerActionType.ChangeTrump)
                    {
                        this.cards.Remove(new Card(context.TrumpCard.Suit, CardType.Nine));
                    }

                    this.PrintGameInfo(context);

                    return playerAction;
                }
                else
                {
                    Console.WriteLine("Invalid action!                  ");
                    continue;
                }
            }
        }

        private void PrintGameInfo(PlayerTurnContext context)
        {
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
