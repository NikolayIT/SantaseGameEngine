namespace Santase.Logic.Tests.GameMechanics
{
    using Santase.Logic;
    using Santase.Logic.GameMechanics;

    // Drives a step-by-step Round with the IPlayers attached to its seats, the way the engine
    // used to run a round by itself (Round.Play / Trick.Play): ask whoever is to move and apply
    // the answer; an illegal answer throws InternalGameException, as SantaseGame does.
    internal static class RoundTestDriver
    {
        public static RoundResult Play(Round round, int firstPlayerTotalPoints, int secondPlayerTotalPoints)
        {
            round.Start(firstPlayerTotalPoints, secondPlayerTotalPoints);
            while (!round.IsFinished)
            {
                Step(round);
            }

            return round.Result;
        }

        // Plays the current trick to its end and returns its winner.
        public static RoundPlayerInfo PlayTrick(Round round)
        {
            var tricksBefore = round.TricksPlayed;
            while (round.TricksPlayed == tricksBefore)
            {
                Step(round);
            }

            return round.Leader == PlayerPosition.FirstPlayer ? round.FirstPlayer : round.SecondPlayer;
        }

        public static void Step(Round round)
        {
            var seat = round.ToMove == PlayerPosition.FirstPlayer ? round.FirstPlayer : round.SecondPlayer;
            var action = seat.Player.GetTurn(round.CreateTurnContext());
            if (!round.TryAct(action))
            {
                throw new InternalGameException($"Invalid action played from {seat.Player.Name}");
            }
        }
    }
}
