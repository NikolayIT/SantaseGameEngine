namespace Santase.Tests.GameSimulations.Tests
{
    using System;
    using System.IO;

    using NUnit.Framework;

    using Santase.Logic.Logger;

    [TestFixture]
    public class FileLoggerTests
    {
        [Test]
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

            Assert.AreEqual(Message, File.ReadAllText(FileName));
        }

        [Test]
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

            Assert.AreEqual(Message + Message, File.ReadAllText(FileName));
        }

        [Test]
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

            Assert.AreEqual(Message + Environment.NewLine, File.ReadAllText(FileName));
        }

        [Test]
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

            Assert.AreEqual(Message + Environment.NewLine + Message + Environment.NewLine, File.ReadAllText(FileName));
        }
    }
}
