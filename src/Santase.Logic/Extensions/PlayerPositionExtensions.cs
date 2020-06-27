namespace Santase.Logic.Extensions
{
    using System;

    public static class PlayerPositionExtensions
    {
        public static PlayerPosition OtherPlayer(this PlayerPosition playerPosition)
        {
            switch (playerPosition)
            {
                case PlayerPosition.FirstPlayer:
                    return PlayerPosition.SecondPlayer;
                case PlayerPosition.SecondPlayer:
                    return PlayerPosition.FirstPlayer;
                case PlayerPosition.NoOne:
                    return PlayerPosition.NoOne;
                default:
                    throw new ArgumentException("Invalid PlayerPosition value", nameof(playerPosition));
            }
        }
    }
}
