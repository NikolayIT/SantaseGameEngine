namespace Santase.UI
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public class UiPlayer : BasePlayer
    {
        private PlayerTurnContext currentContext;

        private PlayerAction userAction;

        private bool firstThisTurn;

        public event EventHandler<ICollection<Card>> RedrawCards;

        public event EventHandler<Tuple<Card, Announce>> RedrawOtherPlayerAction;

        public event EventHandler<Tuple<Card, Card>> RedrawPlayedCards;

        public event EventHandler GameClosed;

        public event EventHandler<bool> GameEnded;

        public override string Name => "UI Player";

        public int MyGamePoints { get; set; }

        public int OpponentGamePoints { get; set; }

        public int MyTotalPoints { get; set; }

        public int OpponentTotalPoints { get; set; }

        public int MyRoundPoints { get; set; }

        public int OpponentRoundPoints { get; set; }

        public Card TrumpCard { get; set; }

        public int CardsLeftInDeck { get; set; }

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.MyTotalPoints = myTotalPoints;
            this.OpponentTotalPoints = opponentTotalPoints;
            this.MyRoundPoints = 0;
            this.OpponentRoundPoints = 0;
            this.TrumpCard = trumpCard;
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
            this.RedrawCards?.Invoke(this, cards);
        }

        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.firstThisTurn = context.IsFirstPlayerTurn;
            this.UpdateContextInfo(context);
            this.currentContext = context;
            while (this.userAction == null)
            {
                Thread.Sleep(50);
            }

            PlayerAction action;
            switch (this.userAction.Type)
            {
                case PlayerActionType.PlayCard:
                    action = this.PlayCard(this.userAction.Card);
                    this.RedrawCards?.Invoke(this, this.Cards);
                    break;
                case PlayerActionType.ChangeTrump:
                    action = this.ChangeTrump(context.TrumpCard);
                    this.RedrawCards?.Invoke(this, this.Cards);
                    break;
                case PlayerActionType.CloseGame:
                    action = this.CloseGame();
                    this.GameClosed?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(this.userAction.Type));
            }

            this.userAction = null;
            return action;
        }

        public override void AddCard(Card card)
        {
            base.AddCard(card);
            this.RedrawCards?.Invoke(this, this.Cards);
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.UpdateContextInfo(context);
            var playedCards = this.firstThisTurn
                                  ? new Tuple<Card, Card>(context.FirstPlayedCard, context.SecondPlayedCard)
                                  : new Tuple<Card, Card>(context.SecondPlayedCard, context.FirstPlayedCard);
            this.RedrawPlayedCards?.Invoke(this, playedCards);
            base.EndTurn(context);
        }

        public override void EndGame(bool amIWinner)
        {
            base.EndGame(amIWinner);
            if (amIWinner)
            {
                this.MyGamePoints++;
            }
            else
            {
                this.OpponentGamePoints++;
            }

            this.GameEnded?.Invoke(this, amIWinner);
        }

        public void Action(PlayerAction playerAction)
        {
            if (!this.PlayerActionValidator.IsValid(playerAction, this.currentContext, this.Cards))
            {
                // Invalid player action
                return;
            }

            this.userAction = playerAction;
        }

        private void UpdateContextInfo(PlayerTurnContext context)
        {
            this.MyRoundPoints = context.IsFirstPlayerTurn
                                     ? context.FirstPlayerRoundPoints
                                     : context.SecondPlayerRoundPoints;
            this.OpponentRoundPoints = context.IsFirstPlayerTurn
                                           ? context.SecondPlayerRoundPoints
                                           : context.FirstPlayerRoundPoints;
            if (context.State.ShouldObserveRules && context.CardsLeftInDeck > 0)
            {
                // Game closed
                this.TrumpCard = null;
                this.GameClosed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.TrumpCard = context.TrumpCard;
            }

            this.CardsLeftInDeck = context.CardsLeftInDeck;
            this.RedrawOtherPlayerAction?.Invoke(
                this,
                new Tuple<Card, Announce>(context.FirstPlayedCard, context.FirstPlayerAnnounce));
        }
    }
}
