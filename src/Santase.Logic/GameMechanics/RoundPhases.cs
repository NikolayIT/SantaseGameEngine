namespace Santase.Logic.GameMechanics
{
    using System;

    using Santase.Logic.RoundStates;

    // Maps the engine's round-state objects to RoundPhase and back. The states created here are
    // detached (their own StateManager): enough for a PlayerTurnContext, which only reads the flags.
    internal static class RoundPhases
    {
        public static RoundPhase Of(BaseRoundState state)
        {
            return state switch
            {
                StartRoundState => RoundPhase.Start,
                MoreThanTwoCardsLeftRoundState => RoundPhase.MoreThanTwoCardsLeft,
                TwoCardsLeftRoundState => RoundPhase.TwoCardsLeft,
                FinalRoundState => RoundPhase.Final,
                _ => throw new ArgumentException($"Unknown round state {state?.GetType().Name}.", nameof(state)),
            };
        }

        public static BaseRoundState CreateState(RoundPhase phase)
        {
            var stateManager = new StateManager();
            BaseRoundState state = phase switch
            {
                RoundPhase.Start => new StartRoundState(stateManager),
                RoundPhase.MoreThanTwoCardsLeft => new MoreThanTwoCardsLeftRoundState(stateManager),
                RoundPhase.TwoCardsLeft => new TwoCardsLeftRoundState(stateManager),
                RoundPhase.Final => new FinalRoundState(stateManager),
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
            };
            stateManager.SetState(state);
            return state;
        }
    }
}
