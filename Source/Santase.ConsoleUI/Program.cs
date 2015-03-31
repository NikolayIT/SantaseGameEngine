using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.ConsoleUI
{
    public static class Program
    {
        public static void Main()
        {
            var deck = new Deck();
            for (int i = 0; i < 24; i++)
            {
                Console.WriteLine(deck.GetNextCard());
            }
        }
    }
}
