﻿namespace Santase.Logic.Tests.Extensions
{
    using NUnit.Framework;

    using Santase.Logic.Extensions;

    [TestFixture]
    public class PlayerPositionExtensionsTests
    {
        [Test]
        public void OtherPlayerShouldReturnSecondPlayerWhenGivenFirstPlayer()
        {
            var position = PlayerPosition.FirstPlayer;
            var result = position.OtherPlayer();
            Assert.AreEqual(PlayerPosition.SecondPlayer, result);
        }

        [Test]
        public void OtherPlayerShouldReturnFirstPlayerWhenGivenSecondPlayer()
        {
            var position = PlayerPosition.SecondPlayer;
            var result = position.OtherPlayer();
            Assert.AreEqual(PlayerPosition.FirstPlayer, result);
        }

        [Test]
        public void OtherPlayerShouldReturnNoOnePlayerWhenGivenNoOnePlayer()
        {
            var position = PlayerPosition.NoOne;
            var result = position.OtherPlayer();
            Assert.AreEqual(PlayerPosition.NoOne, result);
        }
    }
}
