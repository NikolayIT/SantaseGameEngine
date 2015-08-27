namespace Santase.ConsoleUI
{
    using System;

    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    public static class Program
    {
        public static void Main()
        {
            //// Console.BufferHeight = Console.WindowHeight = 17;
            //// Console.BufferWidth = Console.WindowWidth = 50;

            IPlayer firstPlayer = new DummyPlayer("First Dummy Player", new ConsoleLogger("[1] ")); // new ConsolePlayer(5, 10);
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new ConsoleLogger("[2] ")); // new ConsolePlayer(10, 10);

            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer);
            game.Start();

            Console.WriteLine("Game finished!");
            Console.WriteLine("{0} - {1}", game.FirstPlayerTotalPoints, game.SecondPlayerTotalPoints);
            Console.WriteLine("Round: {0}", game.RoundsPlayed);
        }
    }
}
