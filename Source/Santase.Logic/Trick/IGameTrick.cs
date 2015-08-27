namespace Santase.Logic.Trick
{
    using Santase.Logic.Cards;

    public interface IGameTrick
    {
        PlayerPosition Winner { get; }

        Card FirstPlayerCard { get; }

        Announce FirstPlayerAnnounce { get; }

        Card SecondPlayerCard { get; }

        Announce SecondPlayerAnnounce { get; }

        PlayerPosition GameClosedBy { get; }

        void Start();
    }
}
