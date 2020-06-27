namespace Santase.Logic.Tests.RoundStates
{
    using Santase.Logic.RoundStates;

    using Xunit;

    public class StateManagerTests
    {
        [Fact]
        public void DefaultStateShouldBeStartRoundState()
        {
            var stateManager = new StateManager();
            Assert.Equal(typeof(StartRoundState), stateManager.State.GetType());
        }

        [Fact]
        public void SetStateShouldChangeTheState()
        {
            var stateManager = new StateManager();
            var state = new FinalRoundState(stateManager);
            stateManager.SetState(state);
            Assert.Equal(state, stateManager.State);
        }
    }
}
