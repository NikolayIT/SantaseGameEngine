namespace Santase.Tests.GameSimulations.Tests
{
    using System;
    using System.IO;

    using Santase.Logic.Logger;

    using Xunit;

    public class FileLoggerTests
    {
        [Fact]
        public void LogShouldWriteToFile()
        {
            const string FileName = "LogShouldWriteToFile.txt";
            const string Message = "тест";

            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.Log(Message);
            }

            Assert.Equal(Message, File.ReadAllText(FileName));
        }

        [Fact]
        public void LogShouldAppendToTheFile()
        {
            const string FileName = "LogShouldAppendToTheFile.txt";
            const string Message = "тест";

            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.Log(Message);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.Log(Message);
            }

            Assert.Equal(Message + Message, File.ReadAllText(FileName));
        }

        [Fact]
        public void LogLineShouldWriteTextEndingWithNewLineToTheFile()
        {
            const string FileName = "LogLineShouldWriteTextEndingWithNewLineToTheFile.txt";
            const string Message = "тест";

            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.LogLine(Message);
            }

            Assert.Equal(Message + Environment.NewLine, File.ReadAllText(FileName));
        }

        [Fact]
        public void LogLineShouldAppendToTheFile()
        {
            const string FileName = "LogLineShouldAppendToTheFile.txt";
            const string Message = "тест";

            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.LogLine(Message);
            }

            using (ILogger logger = new FileLogger(FileName))
            {
                logger.LogLine(Message);
            }

            Assert.Equal(Message + Environment.NewLine + Message + Environment.NewLine, File.ReadAllText(FileName));
        }
    }
}
