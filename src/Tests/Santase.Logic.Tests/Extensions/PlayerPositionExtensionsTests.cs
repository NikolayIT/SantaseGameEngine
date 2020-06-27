namespace Santase.Logic.Tests.Extensions
{
    using System;

    using Santase.Logic.Extensions;

    using Xunit;

    public class PlayerPositionExtensionsTests
    {
        [Fact]
        public void OtherPlayerShouldReturnSecondPlayerWhenGivenFirstPlayer()
        {
            var position = PlayerPosition.FirstPlayer;
            var result = position.OtherPlayer();
            Assert.Equal(PlayerPosition.SecondPlayer, result);
        }

        [Fact]
        public void OtherPlayerShouldReturnFirstPlayerWhenGivenSecondPlayer()
        {
            var position = PlayerPosition.SecondPlayer;
            var result = position.OtherPlayer();
            Assert.Equal(PlayerPosition.FirstPlayer, result);
        }

        [Fact]
        public void OtherPlayerShouldReturnNoOnePlayerWhenGivenNoOnePlayer()
        {
            var position = PlayerPosition.NoOne;
            var result = position.OtherPlayer();
            Assert.Equal(PlayerPosition.NoOne, result);
        }

        [Fact]
        public void OtherPlayerShouldThrowAnExceptionWhenGivenInvalidValue()
        {
            var position = (PlayerPosition)99999;
            Assert.Throws<ArgumentException>(() => position.OtherPlayer());
        }
    }
}
