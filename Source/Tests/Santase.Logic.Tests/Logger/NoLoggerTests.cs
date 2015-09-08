namespace Santase.Logic.Tests.Logger
{
    using NUnit.Framework;

    using Santase.Logic.Logger;

    [TestFixture]
    public class NoLoggerTests
    {
        [Test]
        public void LogShouldNotLogAnything()
        {
            ILogger logger = new NoLogger();
            logger.Log("test");
            Assert.AreEqual(string.Empty, logger.ToString());
        }

        [Test]
        public void LogLineShouldNotLogAnything()
        {
            ILogger logger = new NoLogger();
            logger.LogLine("test");
            Assert.AreEqual(string.Empty, logger.ToString());
        }

        [Test]
        public void NoLoggerShouldBeDisposable()
        {
            using (new NoLogger())
            {
            }
        }
    }
}
