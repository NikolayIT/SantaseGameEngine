namespace Santase.Logic.Tests.Logger
{
    using System;

    using Santase.Logic.Logger;

    using Xunit;

    public class MemoryLoggerTests
    {
        [Fact]
        public void LogLineShouldAppendLineAtTheEnd()
        {
            const string Message = "test";
            ILogger logger = new MemoryLogger();
            logger.LogLine(Message);
            Assert.Equal(Message + Environment.NewLine, logger.ToString());
        }

        [Fact]
        public void LogShouldAppendTheTextWhenCalledTwoTimesInARow()
        {
            ILogger logger = new MemoryLogger();
            logger.Log("test");
            logger.Log("тест");
            Assert.Equal("testтест", logger.ToString());
        }

        [Fact]
        public void LogLineShouldAppendLineBetweenTwoLogCalls()
        {
            const string FirstMessage = "test";
            const string SecondMessage = "тест";
            ILogger logger = new MemoryLogger();
            logger.LogLine(FirstMessage);
            logger.Log(SecondMessage);
            Assert.Equal(FirstMessage + Environment.NewLine + SecondMessage, logger.ToString());
        }

        [Fact]
        public void MemoryLoggerShouldBeDisposable()
        {
            using (new MemoryLogger())
            {
            }
        }
    }
}
