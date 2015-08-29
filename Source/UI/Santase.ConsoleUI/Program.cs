namespace Santase.ConsoleUI
{
    using System;

    using Santase.AI.DummyPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    public static class Program
    {
        public static void Main()
        {
            var game = CreateGameVersusBot();
            game.Start();

            Console.WriteLine("Game finished!");
            Console.WriteLine("{0} - {1}", game.FirstPlayerTotalPoints, game.SecondPlayerTotalPoints);
            Console.WriteLine("Rounds: {0}", game.RoundsPlayed);
        }

        private static ISantaseGame CreateTwoPlayerGame()
        {
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;

            IPlayer firstPlayer = new ConsolePlayer(5, 10);
            IPlayer secondPlayer = new ConsolePlayer(10, 10);
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer);
            return game;
        }

        private static ISantaseGame CreateGameVersusBot()
        {
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;

            IPlayer firstPlayer = new ConsolePlayer(5, 10);
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new NoLogger()); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer);
            return game;
        }

        private static ISantaseGame CreateGameWithBots()
        {
            IPlayer firstPlayer = new DummyPlayer("First Dummy Player", new NoLogger()); // new ConsoleLogger("[1] "));
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new NoLogger()); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer, new ConsoleLogger("[game] "));
            return game;
        }
    }
}
