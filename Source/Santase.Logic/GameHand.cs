using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Santase.Logic
{
    public class GameHand : IGameHand
    {
        public GameHand()
        {

        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public PlayerPosition Winner
        {
            get { throw new NotImplementedException(); }
        }


        public Cards.Card FirstPlayerCard
        {
            get { throw new NotImplementedException(); }
        }

        public Announce FirstPlayerAnnounce
        {
            get { throw new NotImplementedException(); }
        }

        public Card SecondPlayerCard
        {
            get { throw new NotImplementedException(); }
        }

        public Announce SecondPlayerAnnounce
        {
            get { throw new NotImplementedException(); }
        }
    }
}
