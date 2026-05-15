namespace Santase.Logic.GameMechanics
{
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    internal class Round
    {
        private readonly IGameRules gameRules;

        private readonly IDeck deck;

        private readonly IStateManager stateManager;

        private readonly RoundPlayerInfo firstPlayer;

        private readonly RoundPlayerInfo secondPlayer;

        private PlayerPosition firstToPlay;

        private PlayerPosition lastTrickWinner;

        public Round(
            IPlayer firstPlayer,
            IPlayer secondPlayer,
            IGameRules gameRules,
            PlayerPosition firstToPlay = PlayerPosition.FirstPlayer)
        {
            this.gameRules = gameRules;
            this.deck = new Deck();
            this.stateManager = new StateManager();

            this.firstPlayer = new RoundPlayerInfo(firstPlayer);
            this.secondPlayer = new RoundPlayerInfo(secondPlayer);

            this.firstToPlay = firstToPlay;
            this.lastTrickWinner = PlayerPosition.NoOne;
        }

        public RoundResult Play(int firstPlayerTotalPoints, int secondPlayerTotalPoints)
        {
            this.CallStartRoundAndDealCards(this.firstPlayer, firstPlayerTotalPoints, secondPlayerTotalPoints);
            this.CallStartRoundAndDealCards(this.secondPlayer, secondPlayerTotalPoints, firstPlayerTotalPoints);

            while (!this.IsFinished())
            {
                this.PlayTrick();
            }

            this.firstPlayer.Player.EndRound();
            this.secondPlayer.Player.EndRound();

            // The +10 last-trick bonus is only earned when both players exhausted their
            // cards — i.e. the round played all the way to the end. If either hand still
            // has cards (someone reached 66 from card values mid-round, or via an
            // announce-to-66 mid-trick), surface NoOne so scoring skips the bonus.
            // The reverse case — 66 reached *on* the last trick from card values — has
            // both hands empty, so the bonus correctly applies.
            var bothHandsEmpty = this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0;
            var lastTrickWinnerForBonus = bothHandsEmpty ? this.lastTrickWinner : PlayerPosition.NoOne;

            return new RoundResult(this.firstPlayer, this.secondPlayer, lastTrickWinnerForBonus);
        }

        private void PlayTrick()
        {
            var trick = this.firstToPlay == PlayerPosition.FirstPlayer
                ? new Trick(this.firstPlayer, this.secondPlayer, this.stateManager, this.deck, this.gameRules)
                : new Trick(this.secondPlayer, this.firstPlayer, this.stateManager, this.deck, this.gameRules);

            var trickWinner = trick.Play();

            // The one who wins the trick should play first
            this.firstToPlay = trickWinner == this.firstPlayer
                                   ? PlayerPosition.FirstPlayer
                                   : PlayerPosition.SecondPlayer;
            this.lastTrickWinner = this.firstToPlay;

            if (this.stateManager.State.ShouldDrawCard)
            {
                // The player who wins last trick takes card first
                if (this.firstToPlay == PlayerPosition.FirstPlayer)
                {
                    this.GiveCardToPlayer(this.firstPlayer);
                    this.GiveCardToPlayer(this.secondPlayer);
                }
                else
                {
                    this.GiveCardToPlayer(this.secondPlayer);
                    this.GiveCardToPlayer(this.firstPlayer);
                }
            }

            this.stateManager.State.PlayHand(this.deck.CardsLeft);
        }

        private bool IsFinished()
        {
            if (this.firstPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                return true;
            }

            if (this.secondPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut)
            {
                return true;
            }

            // No cards left => round over
            return this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0;
        }

        private void CallStartRoundAndDealCards(RoundPlayerInfo player, int playerTotalPoints, int opponentTotalPoints)
        {
            var cards = new List<Card>();

            for (var i = 0; i < GameRulesProvider.Santase.CardsAtStartOfTheRound; i++)
            {
                var card = this.deck.GetNextCard();
                cards.Add(card);
                player.Cards.Add(card);
            }

            player.Player.StartRound(cards, this.deck.TrumpCard, playerTotalPoints, opponentTotalPoints);
        }

        private void GiveCardToPlayer(RoundPlayerInfo player)
        {
            var card = this.deck.GetNextCard();
            player.AddCard(card);
        }
    }
}
