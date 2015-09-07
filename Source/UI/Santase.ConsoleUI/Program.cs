namespace Santase.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Santase.AI.DummyPlayer;
    using Santase.AI.SmartPlayer;
    using Santase.Logic;
    using Santase.Logic.GameMechanics;
    using Santase.Logic.Logger;
    using Santase.Logic.Players;

    public static class Program
    {
        public static void Main()
        {
            var stopwatch = Stopwatch.StartNew();
            const int GamesToPlay = 100000;
            var firstPlayerWins = 0;
            var firstPlayerWinsLock = new object();
            var secondPlayerWins = 0;
            var secondPlayerWinsLock = new object();

            // for (var i = 0; i < GamesToPlay; i++)
            Parallel.For(1, GamesToPlay + 1, i =>
            {
                if (i % 1000 == 0)
                {
                    Console.Write(".");
                }

                var game =
                    CreateGameSmartVsPreviousVersionOfSmartBots(
                        i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);

                var winner = game.Start();

                if (winner == PlayerPosition.FirstPlayer)
                {
                    lock (firstPlayerWinsLock)
                    {
                        firstPlayerWins++;
                    }
                }
                else
                {
                    lock (secondPlayerWinsLock)
                    {
                        secondPlayerWins++;
                    }
                }

                // Console.WriteLine($"{i:00000} Games: {firstPlayerWins} - {secondPlayerWins} == Rounds: {game.FirstPlayerTotalPoints} - {game.SecondPlayerTotalPoints} ({game.RoundsPlayed} rounds)");
            });

            Console.WriteLine(stopwatch.Elapsed);
            Console.WriteLine($"Total: {firstPlayerWins:0,0} - {secondPlayerWins:0,0}");
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateTwoPlayerGame()
        {
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;

            IPlayer firstPlayer = new ConsolePlayer(5, 10);
            IPlayer secondPlayer = new ConsolePlayer(10, 10);
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer);
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameVersusBot()
        {
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;

            IPlayer firstPlayer = new ConsolePlayer(5, 10);
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player"); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer);
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameWithBots()
        {
            IPlayer firstPlayer = new DummyPlayer("First Dummy Player"); // new ConsoleLogger("[1] "));
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player"); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer, new ConsoleLogger("[game] "));
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameSmartVsDummyBots()
        {
            IPlayer firstPlayer = new SmartPlayer();
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player"); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer, new NoLogger()); // new ConsoleLogger("[game] "));
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameSmartVsPreviousVersionOfSmartBots(PlayerPosition playerPosition)
        {
            IPlayer firstPlayer = new SmartPlayer(); // new PlayerWithLoggerDecorator(new SmartPlayer(), new ConsoleLogger("[-]"))
            IPlayer secondPlayer = new SmartPlayerOld(); // new DummyPlayer();
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, playerPosition, new NoLogger()); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
