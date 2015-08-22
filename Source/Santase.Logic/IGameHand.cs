namespace Santase.Logic
{
    using Santase.Logic.Cards;

    public interface IGameHand
    {
        void Start();

        PlayerPosition Winner { get; }

        Card FirstPlayerCard { get; }

        Announce FirstPlayerAnnounce { get; }

        Card SecondPlayerCard { get; }

        Announce SecondPlayerAnnounce { get; }

        PlayerPosition GameClosedBy { get; }
    }
}
