namespace Santase.Logic.Logger
{
    using System.IO;

    // TODO: Unit test this class
    public class FileLogger : ILogger
    {
        private readonly TextWriter writer;

        public FileLogger(string filePath)
        {
            this.writer = new StreamWriter(filePath);
        }

        public void Log(string message)
        {
            this.writer.Write(message);
        }

        public void LogLine(string message)
        {
            this.writer.WriteLine(message);
        }
    }
}
