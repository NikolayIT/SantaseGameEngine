using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic
{
    public class GameRound : IGameRound
    {
        public void Start()
        {
            throw new NotImplementedException();
        }

        public int FirstPlayerPoints
        {
            get { throw new NotImplementedException(); }
        }

        public int SecondPlayerPoints
        {
            get { throw new NotImplementedException(); }
        }

        public bool FirstPlayerHasHand
        {
            get { throw new NotImplementedException(); }
        }

        public bool SecondPlayerHasHand
        {
            get { throw new NotImplementedException(); }
        }

        public PlayerPosition ClosedByPlayer
        {
            get { throw new NotImplementedException(); }
        }
    }
}
