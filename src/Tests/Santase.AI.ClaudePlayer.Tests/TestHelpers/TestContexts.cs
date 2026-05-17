namespace Santase.AI.ClaudePlayer.Tests.TestHelpers
{
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    internal static class TestContexts
    {
        public static PlayerTurnContext LeadingContext()
        {
            var stateManager = new NoOpStateManager();
            var state = new FinalRoundState(stateManager);
            stateManager.SetState(state);
            var trump = Card.GetCard(CardSuit.Heart, CardType.Queen);
            return new PlayerTurnContext(state, trump, cardsLeftInDeck: 12, firstPlayerRoundPoints: 0, secondPlayerRoundPoints: 0);
        }
    }

    internal class NoOpStateManager : IStateManager
    {
        public BaseRoundState State { get; private set; }

        public void SetState(BaseRoundState newState)
        {
            this.State = newState;
        }
    }
}
