using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic
{
    public interface ISantaseGame
    {
        void Start();

        int FirstPlayerTotalPoints { get; }

        int SecondPlayerTotalPoints { get; }

        int RoundsPlayed { get; }
    }
}
