namespace Santase.Tests.GameSimulations
{
    using System.IO;

    using Santase.Logic.Logger;

    public class FileLogger : ILogger
    {
        private readonly TextWriter writer;

        public FileLogger(string filePath)
        {
            var stream = File.Open(filePath, FileMode.Append, FileAccess.Write);
            this.writer = new StreamWriter(stream);
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
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.writer.Dispose();
            }
        }
    }
}
