namespace Santase.Logic.Trick
{
    public class TrickResult
    {
        public TrickResult(PlayerInfo winner, Announce firstPlayerAnnounce, bool gameClosed)
        {
            this.GameClosed = gameClosed;
            this.FirstPlayerAnnounce = firstPlayerAnnounce;
            this.Winner = winner;
        }

        public PlayerInfo Winner { get; }

        public Announce FirstPlayerAnnounce { get; }

        public bool GameClosed { get; }
    }
}
