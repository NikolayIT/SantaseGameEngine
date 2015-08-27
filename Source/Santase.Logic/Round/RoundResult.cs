namespace Santase.Logic.Round
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
    }
}
