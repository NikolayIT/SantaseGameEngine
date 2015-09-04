namespace Santase.Logic
{
    using Santase.Logic.GameMechanics;

    // TODO: Unit test this class
    public class RoundWinnerPointsPointsLogic : IRoundWinnerPointsLogic
    {
        public RoundWinnerPoints GetWinnerPoints(RoundResult round)
        {
            if (round.FirstPlayer.GameCloser)
            {
                if (round.FirstPlayer.RoundPoints < 66)
                {
                    return RoundWinnerPoints.Second(3);
                }
            }

            if (round.SecondPlayer.GameCloser)
            {
                if (round.SecondPlayer.RoundPoints < 66)
                {
                    return RoundWinnerPoints.First(3);
                }
            }

            if (round.FirstPlayer.RoundPoints == round.SecondPlayer.RoundPoints)
            {
                return RoundWinnerPoints.Draw();
            }

            if (round.FirstPlayer.RoundPoints < 66 && round.SecondPlayer.RoundPoints < 66)
            {
                if (round.FirstPlayer.RoundPoints > round.SecondPlayer.RoundPoints)
                {
                    return RoundWinnerPoints.First(1);
                }
                else
                {
                    return RoundWinnerPoints.Second(1);
                }
            }

            if (round.FirstPlayer.RoundPoints > round.SecondPlayer.RoundPoints)
            {
                if (round.SecondPlayer.RoundPoints >= 33)
                {
                    return RoundWinnerPoints.First(1);
                }
                else if (round.SecondPlayer.HasAtLeastOneTrick)
                {
                    return RoundWinnerPoints.First(2);
                }
                else
                {
                    return RoundWinnerPoints.First(3);
                }
            }
            else if (round.SecondPlayer.RoundPoints > round.FirstPlayer.RoundPoints)
            {
                if (round.FirstPlayer.RoundPoints >= 33)
                {
                    return RoundWinnerPoints.Second(1);
                }
                else if (round.FirstPlayer.HasAtLeastOneTrick)
                {
                    return RoundWinnerPoints.Second(2);
                }
                else
                {
                    return RoundWinnerPoints.Second(3);
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
