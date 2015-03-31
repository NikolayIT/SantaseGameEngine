using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic
{
    public interface IGameRound
    {
        void Start();

        int FirstPlayerPoints { get; }

        int SecondPlayerPoints { get; }

        bool FirstPlayerHasHand { get; }

        bool SecondPlayerHasHand { get; }

        PlayerPosition ClosedByPlayer { get; }
    }
}
