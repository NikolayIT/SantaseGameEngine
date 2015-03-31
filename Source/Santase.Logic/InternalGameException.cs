using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
