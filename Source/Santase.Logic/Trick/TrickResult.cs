namespace Santase.Logic.Trick
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
