namespace Santase.Logic.Logger
{
    using System;

    public class ConsoleLogger : ILogger
    {
        private readonly string prefix;

        public ConsoleLogger()
            : this(string.Empty)
        {
        }

        public ConsoleLogger(string prefix)
        {
            this.prefix = prefix;
        }

        public void Log(string message)
        {
            Console.Write(this.prefix + message);
        }

        public void LogLine(string message)
        {
            Console.WriteLine(this.prefix + message);
        }
    }
}
