namespace Santase.Logic.GameMechanics
{
    public class RoundResult
    {
        public RoundResult(RoundPlayerInfo firstPlayer, RoundPlayerInfo secondPlayer)
        {
            this.FirstPlayer = firstPlayer;
            this.SecondPlayer = secondPlayer;
        }

        public RoundPlayerInfo FirstPlayer { get; }

        public RoundPlayerInfo SecondPlayer { get; }

        public PlayerPosition GameClosedBy
        {
            get
            {
                if (this.FirstPlayer.GameCloser)
                {
                    return PlayerPosition.FirstPlayer;
                }
                else if (this.SecondPlayer.GameCloser)
                {
                    return PlayerPosition.SecondPlayer;
                }
                else
                {
                    return PlayerPosition.NoOne;
                }
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
                else if (!this.SecondPlayer.HasAtLeastOneTrick)
                {
                    return PlayerPosition.SecondPlayer;
                }
                else
                {
                    return PlayerPosition.NoOne;
                }
            }
        }
    }
}
