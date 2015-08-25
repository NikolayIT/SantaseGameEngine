namespace Santase.Logic.PlayerActionValidate
{
    using Santase.Logic.RoundStates;

    public class CloseGameActionValidator
    {
        public bool CanCloseGame(bool isThePlayerFirst, BaseRoundState state)
        {
            return isThePlayerFirst && state.CanClose;
        }
    }
}
