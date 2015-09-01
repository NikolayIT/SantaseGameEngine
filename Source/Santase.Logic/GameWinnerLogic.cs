namespace Santase.Logic
{
    using Santase.Logic.GameMechanics;

    // TODO: Unit test this class
    public class GameWinnerLogic : IGameWinnerLogic
    {
        public PlayerPosition UpdatePointsAndGetFirstToPlay(RoundResult round, ref int firstPlayerPoints, ref int secondPlayerPoints)
        {
            if (round.FirstPlayer.GameCloser)
            {
                if (round.FirstPlayer.RoundPoints < 66)
                {
                    secondPlayerPoints += 3;
                    return PlayerPosition.FirstPlayer;
                }
            }

            if (round.SecondPlayer.GameCloser)
            {
                if (round.SecondPlayer.RoundPoints < 66)
                {
                    firstPlayerPoints += 3;
                    return PlayerPosition.SecondPlayer;
                }
            }

            if (round.FirstPlayer.RoundPoints < 66 && round.SecondPlayer.RoundPoints < 66)
            {
                if (round.FirstPlayer.RoundPoints > round.SecondPlayer.RoundPoints)
                {
                    firstPlayerPoints += 1;
                    return PlayerPosition.SecondPlayer;
                }
                else
                {
                    secondPlayerPoints += 1;
                    return PlayerPosition.FirstPlayer;
                }

                // TODO: What if equal points?
            }

            if (round.FirstPlayer.RoundPoints > round.SecondPlayer.RoundPoints)
            {
                if (round.SecondPlayer.RoundPoints >= 33)
                {
                    firstPlayerPoints += 1;
                    return PlayerPosition.SecondPlayer;
                }
                else if (round.SecondPlayer.HasAtLeastOneTrick)
                {
                    firstPlayerPoints += 2;
                    return PlayerPosition.SecondPlayer;
                }
                else
                {
                    firstPlayerPoints += 3;
                    return PlayerPosition.SecondPlayer;
                }
            }
            else if (round.SecondPlayer.RoundPoints > round.FirstPlayer.RoundPoints)
            {
                if (round.FirstPlayer.RoundPoints >= 33)
                {
                    secondPlayerPoints += 1;
                    return PlayerPosition.FirstPlayer;
                }
                else if (round.FirstPlayer.HasAtLeastOneTrick)
                {
                    secondPlayerPoints += 2;
                    return PlayerPosition.FirstPlayer;
                }
                else
                {
                    secondPlayerPoints += 3;
                    return PlayerPosition.FirstPlayer;
                }
            }
            else
            {
                // Equal points => 0 points to each
                return PlayerPosition.NoOne;
            }
        }
    }
}
