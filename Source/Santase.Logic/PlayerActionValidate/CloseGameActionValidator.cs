namespace Santase.Logic.PlayerActionValidate
{
    using Santase.Logic.Players;

    public class CloseGameActionValidator
    {
        public bool CanCloseGame(PlayerTurnContext context)
        {
            if (!context.State.CanClose || !context.AmITheFirstPlayer)
            {
                return false;
            }

            return true;
        }
    }
}
