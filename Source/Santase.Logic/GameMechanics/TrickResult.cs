namespace Santase.Logic.GameMechanics
{
    public class TrickResult
    {
        public TrickResult(RoundPlayerInfo winner)
        {
            this.Winner = winner;
        }

        public RoundPlayerInfo Winner { get; }
    }
}
