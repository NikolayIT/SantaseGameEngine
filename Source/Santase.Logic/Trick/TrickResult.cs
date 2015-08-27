namespace Santase.Logic.Trick
{
    using Santase.Logic.Round;

    public class TrickResult
    {
        public TrickResult(RoundPlayerInfo winner)
        {
            this.Winner = winner;
        }

        public RoundPlayerInfo Winner { get; }
    }
}
