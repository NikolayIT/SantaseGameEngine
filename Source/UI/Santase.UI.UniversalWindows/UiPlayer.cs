namespace Santase.UI.UniversalWindows
{
    using Santase.Logic.Players;

    public class UiPlayer : BasePlayer
    {
        public UiPlayer(string name)
        {
            this.Name = name;
        }

        public override string Name { get; }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            return null;
        }
    }
}
