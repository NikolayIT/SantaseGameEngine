namespace Santase.Logic
{
    using System;

    [Serializable]
    public class InternalGameException : Exception
    {
        public InternalGameException(string message)
            : base(message)
        {
        }
    }
}
