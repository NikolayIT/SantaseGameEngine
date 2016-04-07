namespace Santase.UI.WindowsUniversal
{
    using Windows.Storage;

    public class TotalResultPersister
    {
        private const string PlayerValueName = "PlayerScore";
        private const string OtherPlayerValueName = "OtherPlayerScore";

        private int playerScore;
        private int otherPlayerScore;

        public TotalResultPersister()
        {
            try
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(PlayerValueName))
                {
                    ApplicationData.Current.LocalSettings.Values.Add(PlayerValueName, 0);
                }

                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(OtherPlayerValueName))
                {
                    ApplicationData.Current.LocalSettings.Values.Add(OtherPlayerValueName, 0);
                }

                int.TryParse(ApplicationData.Current.LocalSettings.Values[PlayerValueName].ToString(), out this.playerScore);
                int.TryParse(ApplicationData.Current.LocalSettings.Values[OtherPlayerValueName].ToString(), out this.otherPlayerScore);
            }
            catch
            {
                this.playerScore = 0;
                this.otherPlayerScore = 0;
            }
        }

        public int PlayerScore => this.playerScore;

        public int OtherPlayerScore => this.otherPlayerScore;

        public void Update(bool playerWins)
        {
            if (playerWins)
            {
                this.playerScore++;
            }
            else
            {
                this.otherPlayerScore++;
            }

            try
            {
                ApplicationData.Current.LocalSettings.Values[PlayerValueName] = this.playerScore.ToString();
                ApplicationData.Current.LocalSettings.Values[OtherPlayerValueName] = this.otherPlayerScore.ToString();
            }
            catch
            {
            }
        }
    }
}
