namespace Santase.Logic.Trick
{
    public abstract class TrickResult
    {
        public TrickResult(PlayerInfo Winner)
        {
            this.Winner = Winner;
        }

        public PlayerInfo Winner { get; }
    }
}
