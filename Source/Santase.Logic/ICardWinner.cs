using Santase.Logic.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic
{
    public interface ICardWinner
    {
        PlayerPosition Winner(
            Card firstPlayerCard,
            Card secondPlayerCard,
            CardSuit trumpSuit);
    }
}
