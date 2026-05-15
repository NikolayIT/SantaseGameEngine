namespace Santase.Logic.GameMechanics
{
    internal class RoundResult
    {
        public RoundResult(
            RoundPlayerInfo firstPlayer,
            RoundPlayerInfo secondPlayer,
            PlayerPosition lastTrickWinner = PlayerPosition.NoOne)
        {
            this.FirstPlayer = firstPlayer;
            this.SecondPlayer = secondPlayer;
            this.LastTrickWinner = lastTrickWinner;
        }

        public RoundPlayerInfo FirstPlayer { get; }

        public RoundPlayerInfo SecondPlayer { get; }

        // Set only when the round ended via natural talon exhaustion (both hands empty).
        // PlayerPosition.NoOne if the round ended early (someone reached 66 mid-round),
        // signalling that the +10 last-trick bonus is not in play.
        public PlayerPosition LastTrickWinner { get; }

        public PlayerPosition GameClosedBy
        {
            get
            {
                if (this.FirstPlayer.GameCloser)
                {
                    return PlayerPosition.FirstPlayer;
                }

                if (this.SecondPlayer.GameCloser)
                {
                    return PlayerPosition.SecondPlayer;
                }

                return PlayerPosition.NoOne;
            }
        }

        public PlayerPosition NoTricksPlayer
        {
            get
            {
                if (!this.FirstPlayer.HasAtLeastOneTrick)
                {
                    return PlayerPosition.FirstPlayer;
                }

                if (!this.SecondPlayer.HasAtLeastOneTrick)
                {
                    return PlayerPosition.SecondPlayer;
                }

                return PlayerPosition.NoOne;
            }
        }
    }
}
