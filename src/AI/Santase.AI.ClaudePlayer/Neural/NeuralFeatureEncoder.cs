namespace Santase.AI.ClaudePlayer.Neural
{
    using System;
    using System.Collections.Generic;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// Encodes a (PlayerTurnContext, my hand, played cards, unknown cards) tuple
    /// into the fixed-size <see cref="float"/> array expected by <see cref="NeuralNetwork"/>.
    /// Layout (offsets in floats):
    ///   [0..24)   my hand, one-hot per card (suit*6 + typeRank)
    ///   [24..48)  cards already played this round, one-hot
    ///   [48..72)  cards still unknown to me (opp could hold or in deck), one-hot
    ///   [72..76)  trump suit, one-hot (Club, Diamond, Heart, Spade)
    ///   [76..82)  trump card type, one-hot (Nine, Jack, Queen, King, Ten, Ace)
    ///   [82..106) opponent's lead card this trick, one-hot (zeros if I lead)
    ///   [106]     CardsLeftInDeck / 12
    ///   [107]     my round points / 66
    ///   [108]     opp round points / 66
    ///   [109]     (my - opp) round points / 66
    ///   [110]     ShouldObserveRules
    ///   [111]     CanClose
    ///   [112]     CanChangeTrump
    ///   [113]     CanAnnounce20Or40
    ///   [114]     IsFirstPlayerTurn (I am leading)
    ///   [115]     trump card still visible (CardsLeftInDeck >= 2)
    ///   [116]     I hold the 9 of trump
    ///   [117]     opp announced 20 on their lead
    ///   [118]     opp announced 40 on their lead
    ///   [119..128) reserved (zero)
    /// </summary>
    public static class NeuralFeatureEncoder
    {
        public const int FeatureCount = NeuralNetwork.InputSize;
        public const int CardCount = NeuralNetwork.OutputSize;

        public const int MyHandOffset = 0;
        public const int PlayedCardsOffset = 24;
        public const int UnknownCardsOffset = 48;
        public const int TrumpSuitOffset = 72;
        public const int TrumpTypeOffset = 76;
        public const int OpponentLeadOffset = 82;

        public const int CardsLeftOffset = 106;
        public const int MyPointsOffset = 107;
        public const int OppPointsOffset = 108;
        public const int PointDiffOffset = 109;

        public const int ShouldObserveRulesOffset = 110;
        public const int CanCloseOffset = 111;
        public const int CanChangeTrumpOffset = 112;
        public const int CanAnnounceOffset = 113;
        public const int IsFirstPlayerTurnOffset = 114;
        public const int TrumpVisibleOffset = 115;
        public const int IHoldNineOfTrumpOffset = 116;
        public const int OppAnnounced20Offset = 117;
        public const int OppAnnounced40Offset = 118;

        // CardType enum values are non-contiguous (Ace=1, Nine=9, Ten=10, Jack=11, Queen=12, King=13).
        // Map to a contiguous 0..5 in value order so the one-hot output index lines up with
        // ClaudePlayer.AllTypes (used throughout the engine).
        private static readonly int[] TypeRank = BuildTypeRank();

        public static int CardIndex(Card card)
        {
            return ((int)card.Suit * 6) + TypeRank[(int)card.Type];
        }

        public static int CardIndex(CardSuit suit, CardType type)
        {
            return ((int)suit * 6) + TypeRank[(int)type];
        }

        public static void Encode(
            float[] features,
            PlayerTurnContext context,
            ICollection<Card> myCards,
            ICollection<Card> playedCards,
            ICollection<Card> unknownCards)
        {
            if (features == null || features.Length != FeatureCount)
            {
                throw new ArgumentException($"Expected features array of length {FeatureCount}.", nameof(features));
            }

            Array.Clear(features, 0, features.Length);

            foreach (var c in myCards)
            {
                features[MyHandOffset + CardIndex(c)] = 1f;
            }

            if (playedCards != null)
            {
                foreach (var c in playedCards)
                {
                    features[PlayedCardsOffset + CardIndex(c)] = 1f;
                }
            }

            foreach (var c in unknownCards)
            {
                features[UnknownCardsOffset + CardIndex(c)] = 1f;
            }

            var trumpCard = context.TrumpCard;
            if (trumpCard != null)
            {
                features[TrumpSuitOffset + (int)trumpCard.Suit] = 1f;
                features[TrumpTypeOffset + TypeRank[(int)trumpCard.Type]] = 1f;
            }

            var lead = context.FirstPlayedCard;
            var amLeading = lead == null;
            if (!amLeading)
            {
                features[OpponentLeadOffset + CardIndex(lead)] = 1f;
            }

            features[CardsLeftOffset] = context.CardsLeftInDeck / 12f;

            // Match ClaudePlayer's convention: "first player" = leader of current trick,
            // so when I'm leading, my round points are FirstPlayerRoundPoints.
            var myPts = amLeading ? context.FirstPlayerRoundPoints : context.SecondPlayerRoundPoints;
            var oppPts = amLeading ? context.SecondPlayerRoundPoints : context.FirstPlayerRoundPoints;
            features[MyPointsOffset] = myPts / 66f;
            features[OppPointsOffset] = oppPts / 66f;
            features[PointDiffOffset] = (myPts - oppPts) / 66f;

            var state = context.State;
            features[ShouldObserveRulesOffset] = state.ShouldObserveRules ? 1f : 0f;
            features[CanCloseOffset] = state.CanClose ? 1f : 0f;
            features[CanChangeTrumpOffset] = state.CanChangeTrump ? 1f : 0f;
            features[CanAnnounceOffset] = state.CanAnnounce20Or40 ? 1f : 0f;
            features[IsFirstPlayerTurnOffset] = amLeading ? 1f : 0f;
            features[TrumpVisibleOffset] = context.CardsLeftInDeck >= 2 ? 1f : 0f;

            if (trumpCard != null)
            {
                var nineOfTrumpIdx = CardIndex(trumpCard.Suit, CardType.Nine);
                if (features[MyHandOffset + nineOfTrumpIdx] > 0f)
                {
                    features[IHoldNineOfTrumpOffset] = 1f;
                }
            }

            if (!amLeading)
            {
                if (context.FirstPlayerAnnounce == Announce.Twenty)
                {
                    features[OppAnnounced20Offset] = 1f;
                }
                else if (context.FirstPlayerAnnounce == Announce.Forty)
                {
                    features[OppAnnounced40Offset] = 1f;
                }
            }
        }

        private static int[] BuildTypeRank()
        {
            var rank = new int[14];
            rank[(int)CardType.Nine] = 0;
            rank[(int)CardType.Jack] = 1;
            rank[(int)CardType.Queen] = 2;
            rank[(int)CardType.King] = 3;
            rank[(int)CardType.Ten] = 4;
            rank[(int)CardType.Ace] = 5;
            return rank;
        }
    }
}
