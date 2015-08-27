namespace Santase.Logic.Trick
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.PlayerActionValidate;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    public class Trick
    {
        private readonly PlayerInfo firstToPlay;

        private readonly PlayerInfo secondToPlay;

        private readonly BaseRoundState roundState;

        private readonly IDeck deck;

        private readonly IPlayerActionValidator playerActionValidator;

        public Trick(PlayerInfo firstToPlay, PlayerInfo secondToPlay, BaseRoundState roundState, IDeck deck)
        {
            this.firstToPlay = firstToPlay;
            this.secondToPlay = secondToPlay;
            this.roundState = roundState;
            this.deck = deck;
            this.playerActionValidator = new PlayerActionValidator();
        }

        public TrickResult Play()
        {
            var context = new PlayerTurnContext(this.roundState, this.deck.TrumpCard, this.deck.CardsLeft);

            var firstPlayerAction = this.GetFirstPlayerAction(this.firstToPlay, context);
            var secondPlayerAction = this.GetPlayerAction(this.secondToPlay, context);

            return null;
        }

        private PlayerAction GetFirstPlayerAction(PlayerInfo playerInfo, PlayerTurnContext context)
        {
            while (true)
            {
                var action = this.GetPlayerAction(this.firstToPlay, context);

                switch (action.Type)
                {
                    case PlayerActionType.ChangeTrump:
                        playerInfo.Player.AddCard(this.deck.TrumpCard);
                        var newTrumpCard = new Card(this.deck.TrumpCard.Suit, CardType.Nine);
                        this.deck.ChangeTrumpCard(newTrumpCard);
                        context.TrumpCard = newTrumpCard;
                        break;
                    case PlayerActionType.CloseGame:
                        context.State.Close();
                        break;
                    case PlayerActionType.PlayCard:
                        context.FirstPlayedCard = action.Card;
                        return action;
                }
            }
        }

        private PlayerAction GetPlayerAction(PlayerInfo player, PlayerTurnContext context)
        {
            var action = player.Player.GetTurn(context, this.playerActionValidator);
            var isActionValid = this.playerActionValidator.IsValid(action, context, player.Cards);
            if (!isActionValid)
            {
                throw new InternalGameException($"Invalid turn from {this.firstToPlay.Player.Name}");
            }

            return action;
        }
    }
}
