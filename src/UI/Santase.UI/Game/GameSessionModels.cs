namespace Santase.UI.Game
{
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    public enum PlayerSlot
    {
        First = 1,
        Second = 2,
    }

    /// <summary>
    /// How fast a game plays: the computer's thinking pause before each of its moves, and how long
    /// a finished trick stays on the table.
    /// </summary>
    public readonly record struct GamePace(int ThinkDelayMs, int TrickSettleMs)
    {
        public static GamePace Instant => new(0, 0);
    }

    /// <summary>
    /// A move just made. Round points are the mover's round's totals after the move (a 20/40 is
    /// counted the moment the marriage card is led).
    /// </summary>
    public sealed record MoveInfo(
        PlayerSlot Slot,
        PlayerAction Action,
        Announce Announce,
        int FirstRoundPoints,
        int SecondRoundPoints);

    /// <summary>
    /// A finished trick, collected after the table pause. <see cref="FollowCard"/> is null when a
    /// 20/40 took the leader to 66 before the follower played. <see cref="RoundOver"/> says the
    /// trick ended the round (no cards are drawn then).
    /// </summary>
    public sealed record TrickInfo(
        PlayerSlot Leader,
        Card LeadCard,
        Card? FollowCard,
        PlayerSlot Winner,
        bool RoundOver)
    {
        public Card? CardOf(PlayerSlot slot) => slot == this.Leader ? this.LeadCard : this.FollowCard;
    }

    /// <summary>
    /// A finished round as the engine scored it: round points, the game points awarded (the rules
    /// decide, e.g. a failed close gives the opponent 3 however many points the closer has), the
    /// totals after it, and the marriages each player announced.
    /// </summary>
    public sealed record RoundEndInfo(
        int FirstRoundPoints,
        int SecondRoundPoints,
        int FirstGamePoints,
        int SecondGamePoints,
        int FirstAwardedGamePoints,
        int SecondAwardedGamePoints,
        PlayerSlot? WinnerSlot,
        IReadOnlyList<Announce> FirstAnnounces,
        IReadOnlyList<Announce> SecondAnnounces,
        bool IsGameOver)
    {
        /// <summary>Gets a value indicating whether nobody won the round (equal points: no game points).</summary>
        public bool IsDraw => this.WinnerSlot == null;
    }
}
