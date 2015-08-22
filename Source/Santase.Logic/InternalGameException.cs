using System;

namespace Santase.Logic
{
    public class InternalGameException : Exception
    {
        public InternalGameException(string message)
            : base(message)
        {
        }
    }
}
