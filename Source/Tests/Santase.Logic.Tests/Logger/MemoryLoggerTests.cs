namespace Santase.Logic.Tests.Logger
{
    using System;

    using NUnit.Framework;

    using Santase.Logic.Logger;

    [TestFixture]
    public class MemoryLoggerTests
    {
        [Test]
        public void LogLineShouldAppendLineAtTheEnd()
        {
            const string Message = "test";
            ILogger logger = new MemoryLogger();
            logger.LogLine(Message);
            Assert.AreEqual(Message + Environment.NewLine, logger.ToString());
        }

        [Test]
        public void LogShouldAppendTheTextWhenCalledTwoTimesInARow()
        {
            ILogger logger = new MemoryLogger();
            logger.Log("test");
            logger.Log("тест");
            Assert.AreEqual("testтест", logger.ToString());
        }

        [Test]
        public void LogLineShouldAppendLineBetweenTwoLogCalls()
        {
            const string FirstMessage = "test";
            const string SecondMessage = "тест";
            ILogger logger = new MemoryLogger();
            logger.LogLine(FirstMessage);
            logger.Log(SecondMessage);
            Assert.AreEqual(FirstMessage + Environment.NewLine + SecondMessage, logger.ToString());
        }

        [Test]
        public void MemoryLoggerShouldBeDisposable()
        {
            using (new MemoryLogger())
            {
            }
        }
    }
}
