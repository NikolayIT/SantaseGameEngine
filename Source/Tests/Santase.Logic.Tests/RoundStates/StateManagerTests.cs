namespace Santase.Logic.Tests.RoundStates
{
    using NUnit.Framework;

    using Santase.Logic.RoundStates;

    [TestFixture]
    public class StateManagerTests
    {
        [Test]
        public void DefaultStateShouldBeStartRoundState()
        {
            var stateManager = new StateManager();
            Assert.AreEqual(typeof(StartRoundState), stateManager.State.GetType());
        }

        [Test]
        public void SetStateShouldChangeTheState()
        {
            var stateManager = new StateManager();
            var state = new FinalRoundState(stateManager);
            stateManager.SetState(state);
            Assert.AreEqual(state, stateManager.State);
        }
    }
}
