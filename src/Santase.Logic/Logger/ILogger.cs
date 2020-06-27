namespace Santase.Logic.Logger
{
    using System;

    public interface ILogger : IDisposable
    {
        void Log(string message);

        void LogLine(string message);
    }
}
