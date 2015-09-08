namespace Santase.Logic.Logger
{
    using System.IO;

    public class FileLogger : ILogger
    {
        private readonly TextWriter writer;

        public FileLogger(string filePath)
        {
            this.writer = new StreamWriter(filePath, true);
        }

        public void Log(string message)
        {
            this.writer.Write(message);
        }

        public void LogLine(string message)
        {
            this.writer.WriteLine(message);
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }
    }
}
