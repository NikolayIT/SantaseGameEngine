namespace Santase.Logic.GameMechanics
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    /// <summary>
    /// What one seat may see of a <see cref="SantaseMatch"/>: its own hand and options, and the
    /// public state (never the opponent's hand or the talon order). A plain model: its public
    /// properties are all there is, so a copy built from them works the same (a host can map it to
    /// its own model and back). Positions are absolute (first/second player); <see cref="Seat"/>
    /// says whose view this is.
    /// </summary>
    public sealed class SantaseSeatView
    {
        /// <summary>
        /// Gets the seat this view belongs to (<see cref="PlayerPosition.NoOne"/> for the final view).
        /// </summary>
        public PlayerPosition Seat { get; init; }

        /// <summary>
        /// Gets who must act now (<see cref="PlayerPosition.NoOne"/> after the match).
        /// </summary>
        public PlayerPosition ToMove { get; init; }

        /// <summary>
        /// Gets a value indicating whether the match is over.
        /// </summary>
        public bool IsMatchFinished { get; init; }

        /// <summary>
        /// Gets the match winner once it is over.
        /// </summary>
        public PlayerPosition MatchWinner { get; init; }

        /// <summary>
        /// Gets the first player's game points.
        /// </summary>
        public int FirstPlayerTotalPoints { get; init; }

        /// <summary>
        /// Gets the second player's game points.
        /// </summary>
        public int SecondPlayerTotalPoints { get; init; }

        /// <summary>
        /// Gets the 1-based number of the round shown below (after the match: the last round, which
        /// is also the last entry of <see cref="PreviousRounds"/>).
        /// </summary>
        public int RoundNumber { get; init; }

        /// <summary>
        /// Gets the results of the finished rounds, in order.
        /// </summary>
        public IReadOnlyList<SantaseRoundSummary> PreviousRounds { get; init; }

        /// <summary>
        /// Gets the phase of the round.
        /// </summary>
        public RoundPhase Phase { get; init; }

        /// <summary>
        /// Gets who closed the talon, if anyone.
        /// </summary>
        public PlayerPosition ClosedBy { get; init; }

        /// <summary>
        /// Gets the trump card: face-up under the talon while <see cref="IsTrumpCardOnTable"/>,
        /// afterwards the card that was there (its suit is trumps either way).
        /// </summary>
        public Card TrumpCard { get; init; }

        /// <summary>
        /// Gets a value indicating whether the trump card still lies face-up under the talon.
        /// </summary>
        public bool IsTrumpCardOnTable { get; init; }

        /// <summary>
        /// Gets the number of cards in the talon, the face-up trump card included.
        /// </summary>
        public int CardsLeftInDeck { get; init; }

        /// <summary>
        /// Gets how many cards the first player holds.
        /// </summary>
        public int FirstPlayerCardCount { get; init; }

        /// <summary>
        /// Gets how many cards the second player holds.
        /// </summary>
        public int SecondPlayerCardCount { get; init; }

        /// <summary>
        /// Gets the first player's round points (cards won plus announces).
        /// </summary>
        public int FirstPlayerRoundPoints { get; init; }

        /// <summary>
        /// Gets the second player's round points (cards won plus announces).
        /// </summary>
        public int SecondPlayerRoundPoints { get; init; }

        /// <summary>
        /// Gets how many tricks the first player has won this round.
        /// </summary>
        public int FirstPlayerTricksWon { get; init; }

        /// <summary>
        /// Gets how many tricks the second player has won this round.
        /// </summary>
        public int SecondPlayerTricksWon { get; init; }

        /// <summary>
        /// Gets who exchanged the Nine of trumps for the face-up trump card this round, if anyone.
        /// </summary>
        public PlayerPosition TrumpSwappedBy { get; init; }

        /// <summary>
        /// Gets the face-up card taken in that exchange (public: everyone saw it), or null.
        /// </summary>
        public Card SwappedTrumpCard { get; init; }

        /// <summary>
        /// Gets the finished tricks of this round, in order.
        /// </summary>
        public IReadOnlyList<SantaseTrick> Tricks { get; init; }

        /// <summary>
        /// Gets who led the card now on the table, or <see cref="PlayerPosition.NoOne"/>.
        /// </summary>
        public PlayerPosition CurrentTrickLeader { get; init; }

        /// <summary>
        /// Gets the card now on the table (led, not yet answered), or null.
        /// </summary>
        public Card CurrentTrickLeadCard { get; init; }

        /// <summary>
        /// Gets the marriage announced with that lead.
        /// </summary>
        public Announce CurrentTrickAnnounce { get; init; }

        /// <summary>
        /// Gets the most recently finished trick of the match (possibly the last of the previous
        /// round), or null.
        /// </summary>
        public SantaseTrick LastTrick { get; init; }

        /// <summary>
        /// Gets this seat's cards (empty for the final view).
        /// </summary>
        public IReadOnlyList<Card> Hand { get; init; }

        /// <summary>
        /// Gets the cards this seat may play now; empty unless it is this seat's turn.
        /// </summary>
        public IReadOnlyList<Card> PlayableCards { get; init; }

        /// <summary>
        /// Gets a value indicating whether this seat may exchange the Nine of trumps now.
        /// </summary>
        public bool CanChangeTrump { get; init; }

        /// <summary>
        /// Gets a value indicating whether this seat may close the talon now.
        /// </summary>
        public bool CanClose { get; init; }

        /// <summary>
        /// Gets the full record of the match, hidden cards included; only in the final view.
        /// </summary>
        public SantaseMatchRecord Record { get; init; }

        /// <summary>
        /// Creates the context <see cref="IPlayer.GetTurn"/> would receive for this seat: the same
        /// values the engine gives it, so a bot can decide from the view alone.
        /// </summary>
        /// <returns>A new <see cref="PlayerTurnContext"/>.</returns>
        public PlayerTurnContext CreateTurnContext()
        {
            if (this.Seat == PlayerPosition.NoOne || this.Seat != this.ToMove)
            {
                throw new InvalidOperationException("A turn context exists only for the seat to move.");
            }

            var mine = this.Seat == PlayerPosition.FirstPlayer ? this.FirstPlayerRoundPoints : this.SecondPlayerRoundPoints;
            var theirs = this.Seat == PlayerPosition.FirstPlayer ? this.SecondPlayerRoundPoints : this.FirstPlayerRoundPoints;
            var state = RoundPhases.CreateState(this.Phase);
            if (this.CurrentTrickLeadCard == null)
            {
                // Leading: the context's "first player" is this seat.
                return new PlayerTurnContext(state, this.TrumpCard, this.CardsLeftInDeck, mine, theirs);
            }

            // Following: the leader is the "first player"; their points include the announce.
            return new PlayerTurnContext(state, this.TrumpCard, this.CardsLeftInDeck, theirs, mine)
            {
                FirstPlayedCard = this.CurrentTrickLeadCard,
                FirstPlayerAnnounce = this.CurrentTrickAnnounce,
            };
        }

        /// <summary>
        /// Gets the cards played in this round's finished tricks.
        /// </summary>
        /// <returns>A new collection.</returns>
        public CardCollection GetPlayedCards()
        {
            var played = new CardCollection();
            if (this.Tricks == null)
            {
                return played;
            }

            foreach (var trick in this.Tricks)
            {
                played.Add(trick.LeadCard);
                if (trick.FollowCard != null)
                {
                    played.Add(trick.FollowCard);
                }
            }

            return played;
        }
    }
}
