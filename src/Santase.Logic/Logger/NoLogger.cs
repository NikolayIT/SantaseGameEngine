namespace Santase.Logic.Logger
{
    public class NoLogger : ILogger
    {
        public void Log(string message)
        {
        }

        public void LogLine(string message)
        {
        }

        public override string ToString()
        {
            return string.Empty;
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
