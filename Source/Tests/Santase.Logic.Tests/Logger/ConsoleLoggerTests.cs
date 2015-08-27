namespace Santase.Logic.Tests.Logger
{
    using System;
    using System.IO;

    using NUnit.Framework;

    using Santase.Logic.Logger;

    [TestFixture]
    public class ConsoleLoggerTests
    {
        [Test]
        public void LogShouldWriteToTheConsole()
        {
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            var logger = new ConsoleLogger();
            logger.Log(Message);

            Assert.AreEqual(Message, textWriter.ToString());
        }

        [Test]
        public void LogLineShouldWriteTextEndingWithNewLineToTheConsole()
        {
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            var logger = new ConsoleLogger();
            logger.LogLine(Message);

            Assert.AreEqual(Message + Environment.NewLine, textWriter.ToString());
        }

        [Test]
        public void LogWithPrefixShouldWritePrefixedTextToTheConsole()
        {
            const string Prefix = "[prefix] ";
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            var logger = new ConsoleLogger(Prefix);
            logger.Log(Message);

            Assert.AreEqual(Prefix + Message, textWriter.ToString());
        }

        [Test]
        public void LogLineWithPrefixShouldWritePrefixedTextEndingWithNewLineToTheConsole()
        {
            const string Prefix = "[prefix] ";
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            var logger = new ConsoleLogger(Prefix);
            logger.LogLine(Message);

            Assert.AreEqual(Prefix + Message + Environment.NewLine, textWriter.ToString());
        }
    }
}
