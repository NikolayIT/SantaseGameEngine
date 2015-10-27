namespace Santase.Logic.WinnerLogic
{
    public class RoundWinnerPointsPointsLogic : IRoundWinnerPointsLogic
    {
        // TODO: Should last trick winner wins 10 additional points?
        // TODO: What if the total score is 65:65 after giving 10 additional points? Should the player with the last trick win the round?
        public RoundWinnerPoints GetWinnerPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            IGameRules gameRules)
        {
            if (gameClosedBy == PlayerPosition.FirstPlayer)
            {
                if (firstPlayerPoints < gameRules.RoundPointsForGoingOut)
                {
                    return RoundWinnerPoints.Second(3);
                }
            }

            if (gameClosedBy == PlayerPosition.SecondPlayer)
            {
                if (secondPlayerPoints < gameRules.RoundPointsForGoingOut)
                {
                    return RoundWinnerPoints.First(3);
                }
            }

            if (firstPlayerPoints == secondPlayerPoints)
            {
                // Equal points => 0 points to each
                return RoundWinnerPoints.Draw();
            }

            if (firstPlayerPoints < gameRules.RoundPointsForGoingOut && secondPlayerPoints < gameRules.RoundPointsForGoingOut)
            {
                if (firstPlayerPoints > secondPlayerPoints)
                {
                    return RoundWinnerPoints.First(1);
                }

                if (secondPlayerPoints > firstPlayerPoints)
                {
                    return RoundWinnerPoints.Second(1);
                }
            }

            if (firstPlayerPoints > secondPlayerPoints)
            {
                if (secondPlayerPoints >= gameRules.HalfRoundPoints)
                {
                    return RoundWinnerPoints.First(1);
                }

                if (noTricksPlayer == PlayerPosition.SecondPlayer)
                {
                    return RoundWinnerPoints.First(3);
                }

                // at lest one trick and less than half of the points
                return RoundWinnerPoints.First(2);
            }
            else
            {
                // secondPlayerPoints > firstPlayerPoints
                if (firstPlayerPoints >= gameRules.HalfRoundPoints)
                {
                    return RoundWinnerPoints.Second(1);
                }

                if (noTricksPlayer == PlayerPosition.FirstPlayer)
                {
                    return RoundWinnerPoints.Second(3);
                }

                // at lest one trick and less than half of the points
                return RoundWinnerPoints.Second(2);
            }
        }
    }
}
