namespace Santase.Logic.GameMechanics
{
    // TODO: Unit test this class
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
