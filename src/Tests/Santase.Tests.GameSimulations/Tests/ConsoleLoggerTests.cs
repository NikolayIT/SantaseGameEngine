namespace Santase.Tests.GameSimulations.Tests
{
    using System;
    using System.IO;

    using Santase.Logic.Logger;
    using Santase.Tests.GameSimulations;

    using Xunit;

    public class ConsoleLoggerTests
    {
        [Fact]
        public void LogShouldWriteToTheConsole()
        {
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            ILogger logger = new ConsoleLogger();
            logger.Log(Message);

            Assert.Equal(Message, textWriter.ToString());
        }

        [Fact]
        public void LogLineShouldWriteTextEndingWithNewLineToTheConsole()
        {
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            ILogger logger = new ConsoleLogger();
            logger.LogLine(Message);

            Assert.Equal(Message + Environment.NewLine, textWriter.ToString());
        }

        [Fact]
        public void LogWithPrefixShouldWritePrefixedTextToTheConsole()
        {
            const string Prefix = "[prefix] ";
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            ILogger logger = new ConsoleLogger(Prefix);
            logger.Log(Message);

            Assert.Equal(Prefix + Message, textWriter.ToString());
        }

        [Fact]
        public void LogLineWithPrefixShouldWritePrefixedTextEndingWithNewLineToTheConsole()
        {
            const string Prefix = "[prefix] ";
            const string Message = "тест";

            var textWriter = new StringWriter();
            Console.SetOut(textWriter);

            ILogger logger = new ConsoleLogger(Prefix);
            logger.LogLine(Message);

            Assert.Equal(Prefix + Message + Environment.NewLine, textWriter.ToString());
        }

        [Fact]
        public void ConsoleLoggerShouldBeDisposable()
        {
            using (new ConsoleLogger())
            {
            }
        }
    }
}
