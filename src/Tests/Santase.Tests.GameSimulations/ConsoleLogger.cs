namespace Santase.Tests.GameSimulations
{
    using System;

    using Santase.Logic.Logger;

    // ReSharper disable once UnusedMember.Global
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

        public void Dispose()
        {
            this.Dispose(true);
        }

        // ReSharper disable once UnusedParameter.Global
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
