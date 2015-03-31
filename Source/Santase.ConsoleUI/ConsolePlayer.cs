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

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            throw new NotImplementedException();
        }
    }
}
