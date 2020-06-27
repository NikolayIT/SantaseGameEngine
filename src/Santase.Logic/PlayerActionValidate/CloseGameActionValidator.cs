namespace Santase.Logic.PlayerActionValidate
{
    using Santase.Logic.RoundStates;

    internal static class CloseGameActionValidator
    {
        public static bool CanCloseGame(bool isThePlayerFirst, BaseRoundState state)
        {
            return isThePlayerFirst && state.CanClose;
        }
    }
}
