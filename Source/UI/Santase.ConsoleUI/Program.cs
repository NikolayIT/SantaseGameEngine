namespace Santase.ConsoleUI
{
    using System;

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
            const int GamesToPlay = 10000;
            var firstPlayerWins = 0;
            var secondPlayerWins = 0;
            for (var i = 0; i < GamesToPlay; i++)
            {
                var game = CreateGameSmartVsDummyBots();
                var winner = game.Start();

                if (winner == PlayerPosition.FirstPlayer)
                {
                    firstPlayerWins++;
                }
                else if (winner == PlayerPosition.SecondPlayer)
                {
                    secondPlayerWins++;
                }

                Console.WriteLine($"Game finished! Game score: {game.FirstPlayerTotalPoints} - {game.SecondPlayerTotalPoints} ({game.RoundsPlayed} rounds)");
                Console.WriteLine($"Total: {firstPlayerWins} - {secondPlayerWins}");
            }
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
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new NoLogger()); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer);
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameWithBots()
        {
            IPlayer firstPlayer = new DummyPlayer("First Dummy Player", new NoLogger()); // new ConsoleLogger("[1] "));
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new NoLogger()); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer, new ConsoleLogger("[game] "));
            return game;
        }

        // ReSharper disable once UnusedMember.Local
        private static ISantaseGame CreateGameSmartVsDummyBots()
        {
            IPlayer firstPlayer = new SmartPlayer();
            IPlayer secondPlayer = new DummyPlayer("Second Dummy Player", new NoLogger()); // new ConsoleLogger("[2] "));
            ISantaseGame game = new SantaseGame(firstPlayer, secondPlayer, PlayerPosition.FirstPlayer, new NoLogger()); // new ConsoleLogger("[game] "));
            return game;
        }
    }
}
