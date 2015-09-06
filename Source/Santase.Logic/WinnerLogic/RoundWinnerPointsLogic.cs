namespace Santase.Logic.WinnerLogic
{
    // TODO: Unit test this class
    public class RoundWinnerPointsPointsLogic : IRoundWinnerPointsLogic
    {
        public RoundWinnerPoints GetWinnerPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer)
        {
            if (gameClosedBy == PlayerPosition.FirstPlayer)
            {
                if (firstPlayerPoints < 66)
                {
                    return RoundWinnerPoints.Second(3);
                }
            }

            if (gameClosedBy == PlayerPosition.SecondPlayer)
            {
                if (secondPlayerPoints < 66)
                {
                    return RoundWinnerPoints.First(3);
                }
            }

            if (firstPlayerPoints == secondPlayerPoints)
            {
                return RoundWinnerPoints.Draw();
            }

            if (firstPlayerPoints < 66 && secondPlayerPoints < 66)
            {
                if (firstPlayerPoints > secondPlayerPoints)
                {
                    return RoundWinnerPoints.First(1);
                }
                else if (secondPlayerPoints > firstPlayerPoints)
                {
                    return RoundWinnerPoints.Second(1);
                }
                else
                {
                    return RoundWinnerPoints.Draw();
                }
            }

            if (firstPlayerPoints > secondPlayerPoints)
            {
                if (secondPlayerPoints >= 33)
                {
                    return RoundWinnerPoints.First(1);
                }
                else if (noTricksPlayer == PlayerPosition.SecondPlayer)
                {
                    return RoundWinnerPoints.First(3);
                }
                else
                {
                    // at lest one trick and less than 33 points
                    return RoundWinnerPoints.First(2);
                }
            }
            else if (secondPlayerPoints > firstPlayerPoints)
            {
                if (firstPlayerPoints >= 33)
                {
                    return RoundWinnerPoints.Second(1);
                }
                else if (noTricksPlayer == PlayerPosition.FirstPlayer)
                {
                    return RoundWinnerPoints.Second(3);
                }
                else
                {
                    // at lest one trick and less than 33 points
                    return RoundWinnerPoints.Second(2);
                }
            }
            else
            {
                // Equal points => 0 points to each
                return RoundWinnerPoints.Draw();
            }
        }
    }
}
