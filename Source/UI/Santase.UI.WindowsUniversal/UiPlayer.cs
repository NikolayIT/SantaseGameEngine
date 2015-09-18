namespace Santase.UI.WindowsUniversal
{
    using Santase.Logic.Players;

    public class UiPlayer : BasePlayer
    {
        public override string Name => "UI Player";

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
