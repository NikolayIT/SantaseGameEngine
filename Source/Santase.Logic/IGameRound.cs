using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic
{
    public interface IGameRound
    {
        void Start();

        int TotalPointsWonByFirstPlayer { get; }
        
        int TotalPointsWonBySecondPlayer { get; }
    }
}
