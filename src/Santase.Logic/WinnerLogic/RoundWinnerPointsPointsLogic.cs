namespace Santase.Logic.WinnerLogic
{
    public class RoundWinnerPointsPointsLogic : IRoundWinnerPointsLogic
    {
        private const int LastTrickBonus = 10;

        public RoundWinnerPoints GetWinnerPoints(
            int firstPlayerPoints,
            int secondPlayerPoints,
            PlayerPosition gameClosedBy,
            PlayerPosition noTricksPlayer,
            PlayerPosition lastTrickWinner,
            IGameRules gameRules)
        {
            // +10 bonus to the winner of the last trick, but only when the talon was
            // exhausted naturally (no one closed). When the game was closed the bonus
            // is suspended per Santase rules.
            if (gameClosedBy == PlayerPosition.NoOne)
            {
                if (lastTrickWinner == PlayerPosition.FirstPlayer)
                {
                    firstPlayerPoints += LastTrickBonus;
                }
                else if (lastTrickWinner == PlayerPosition.SecondPlayer)
                {
                    secondPlayerPoints += LastTrickBonus;
                }
            }

            // Failed close: the opponent always wins 3 game points. This is the intended
            // (Bulgarian Santase) rule — a flat 3, NOT German 66's "2, or 3 only if the
            // opponent was trickless at the moment of closing". The same branch also covers
            // the rare case where the non-closer reaches 66 first: the closer still forfeits 3.
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

            // Unreachable through real engine play: a non-closed round that runs to exhaustion
            // distributes 120 card points + the 10-point bonus, so "both below 66" forces the
            // exact 65-65 draw handled above, and a closed round returned earlier. Kept because
            // this method is public and callable with arbitrary inputs.
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
