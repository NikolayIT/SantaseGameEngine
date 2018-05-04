namespace Santase.Logic.Tests.Logger
{
    using Santase.Logic.Logger;

    using Xunit;

    public class NoLoggerTests
    {
        [Fact]
        public void LogShouldNotLogAnything()
        {
            ILogger logger = new NoLogger();
            logger.Log("test");
            Assert.Equal(string.Empty, logger.ToString());
        }

        [Fact]
        public void LogLineShouldNotLogAnything()
        {
            ILogger logger = new NoLogger();
            logger.LogLine("test");
            Assert.Equal(string.Empty, logger.ToString());
        }

        [Fact]
        public void NoLoggerShouldBeDisposable()
        {
            using (new NoLogger())
            {
            }
        }
    }
}
