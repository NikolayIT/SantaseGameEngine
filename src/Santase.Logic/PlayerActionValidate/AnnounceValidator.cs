﻿namespace Santase.Logic.PlayerActionValidate
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic.Cards;

    public class AnnounceValidator : IAnnounceValidator
    {
        private static readonly Lazy<AnnounceValidator> Lazy =
            new Lazy<AnnounceValidator>(() => new AnnounceValidator());

        public static AnnounceValidator Instance => Lazy.Value;

        public Announce GetPossibleAnnounce(
            ICollection<Card> playerCards,
            Card cardToBePlayed,
            Card trumpCard,
            bool amITheFirstPlayer = true)
        {
            if (cardToBePlayed == null)
            {
                return Announce.None;
            }

            if (!amITheFirstPlayer)
            {
                return Announce.None;
            }

            CardType cardTypeToSearch;
            switch (cardToBePlayed.Type)
            {
                case CardType.Queen:
                    cardTypeToSearch = CardType.King;
                    break;
                case CardType.King:
                    cardTypeToSearch = CardType.Queen;
                    break;
                default:
                    return Announce.None;
            }

            var cardToSearch = Card.GetCard(cardToBePlayed.Suit, cardTypeToSearch);
            if (!playerCards.Contains(cardToSearch))
            {
                return Announce.None;
            }

            return cardToBePlayed.Suit == trumpCard.Suit ? Announce.Forty : Announce.Twenty;
        }
    }
}
