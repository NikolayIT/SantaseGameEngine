namespace Santase.Logic.GameMechanics
{
    // TODO: Unit test this class
    public class TrickResult
    {
        public TrickResult(RoundPlayerInfo winner)
        {
            this.Winner = winner;
        }

        public RoundPlayerInfo Winner { get; }
    }
}
