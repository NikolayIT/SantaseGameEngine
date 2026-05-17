namespace Santase.AI.ClaudePlayer.Tests.Neural
{
    using System.Collections.Generic;

    using Santase.AI.ClaudePlayer.Neural;
    using Santase.AI.ClaudePlayer.Tests.TestHelpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Santase.Logic.RoundStates;

    using Xunit;

    public class NeuralFeatureEncoderTests
    {
        [Fact]
        public void FeatureCountShouldMatchNetworkInputSize()
        {
            Assert.Equal(NeuralNetwork.InputSize, NeuralFeatureEncoder.FeatureCount);
        }

        [Fact]
        public void CardIndexShouldBeWithinOutputRange()
        {
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
            {
                foreach (var type in new[] { CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace })
                {
                    var idx = NeuralFeatureEncoder.CardIndex(suit, type);
                    Assert.InRange(idx, 0, NeuralFeatureEncoder.CardCount - 1);
                }
            }
        }

        [Fact]
        public void CardIndexShouldBeUniquePerCard()
        {
            var seen = new HashSet<int>();
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
            {
                foreach (var type in new[] { CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace })
                {
                    var idx = NeuralFeatureEncoder.CardIndex(suit, type);
                    Assert.True(seen.Add(idx), $"Duplicate index {idx} for {suit} {type}");
                }
            }

            Assert.Equal(NeuralFeatureEncoder.CardCount, seen.Count);
        }

        [Fact]
        public void EncodeShouldMarkOnlyHeldCardsInHandSlot()
        {
            var context = TestContexts.LeadingContext();
            var hand = new List<Card> { Card.GetCard(CardSuit.Club, CardType.Ace) };
            var unknown = new List<Card>();

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, hand, playedCards: null, unknown);

            var handBits = 0;
            for (var i = 0; i < 24; i++)
            {
                if (features[NeuralFeatureEncoder.MyHandOffset + i] > 0f)
                {
                    handBits++;
                }
            }

            Assert.Equal(1, handBits);
            var expectedIdx = NeuralFeatureEncoder.CardIndex(CardSuit.Club, CardType.Ace);
            Assert.Equal(1f, features[NeuralFeatureEncoder.MyHandOffset + expectedIdx]);
        }

        [Fact]
        public void EncodeShouldSetTrumpSuitAndTypeOneHot()
        {
            var context = TestContexts.LeadingContext();
            context.TrumpCard = Card.GetCard(CardSuit.Heart, CardType.Queen);

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.Equal(1f, features[NeuralFeatureEncoder.TrumpSuitOffset + (int)CardSuit.Heart]);
            // Queen is rank 2 (Nine=0, Jack=1, Queen=2, King=3, Ten=4, Ace=5).
            Assert.Equal(1f, features[NeuralFeatureEncoder.TrumpTypeOffset + 2]);
        }

        [Fact]
        public void EncodeShouldSetIsFirstPlayerTurnWhenLeading()
        {
            var context = TestContexts.LeadingContext();

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.Equal(1f, features[NeuralFeatureEncoder.IsFirstPlayerTurnOffset]);
            // No opponent lead card encoded.
            for (var i = 0; i < 24; i++)
            {
                Assert.Equal(0f, features[NeuralFeatureEncoder.OpponentLeadOffset + i]);
            }
        }

        [Fact]
        public void EncodeShouldRecordOpponentLeadWhenFollowing()
        {
            var context = TestContexts.LeadingContext();
            context.FirstPlayedCard = Card.GetCard(CardSuit.Spade, CardType.Ten);

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.Equal(0f, features[NeuralFeatureEncoder.IsFirstPlayerTurnOffset]);
            var idx = NeuralFeatureEncoder.CardIndex(CardSuit.Spade, CardType.Ten);
            Assert.Equal(1f, features[NeuralFeatureEncoder.OpponentLeadOffset + idx]);
        }

        [Fact]
        public void EncodeShouldNormalizePointsAndCardsLeft()
        {
            var context = TestContexts.LeadingContext();
            context.FirstPlayerRoundPoints = 33;
            context.SecondPlayerRoundPoints = 22;

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.InRange(features[NeuralFeatureEncoder.MyPointsOffset], 0.499f, 0.501f);
            Assert.InRange(features[NeuralFeatureEncoder.OppPointsOffset], 0.332f, 0.334f);
            Assert.InRange(features[NeuralFeatureEncoder.PointDiffOffset], 0.165f, 0.168f);
            Assert.InRange(features[NeuralFeatureEncoder.CardsLeftOffset], 0.999f, 1.001f);
        }

        [Fact]
        public void EncodeShouldDetectNineOfTrumpInHand()
        {
            var context = TestContexts.LeadingContext();
            context.TrumpCard = Card.GetCard(CardSuit.Club, CardType.Ace);
            var hand = new List<Card> { Card.GetCard(CardSuit.Club, CardType.Nine) };

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, hand, null, new List<Card>());

            Assert.Equal(1f, features[NeuralFeatureEncoder.IHoldNineOfTrumpOffset]);
        }

        [Fact]
        public void EncodeShouldClearBufferBetweenCalls()
        {
            var features = new float[NeuralFeatureEncoder.FeatureCount];
            for (var i = 0; i < features.Length; i++)
            {
                features[i] = 99f;
            }

            var context = TestContexts.LeadingContext();
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            // Most slots have no positive signal in an empty-context encode; verify the buffer
            // was cleared rather than carrying over stale values from before.
            for (var i = 0; i < 24; i++)
            {
                Assert.Equal(0f, features[NeuralFeatureEncoder.MyHandOffset + i]);
                Assert.Equal(0f, features[NeuralFeatureEncoder.PlayedCardsOffset + i]);
                Assert.Equal(0f, features[NeuralFeatureEncoder.UnknownCardsOffset + i]);
            }
        }
    }

    public class FeatureEncoderAnnounceTests
    {
        [Fact]
        public void EncodeShouldMarkOppAnnounce20()
        {
            var context = TestContexts.LeadingContext();
            context.FirstPlayedCard = Card.GetCard(CardSuit.Club, CardType.King);
            context.FirstPlayerAnnounce = Announce.Twenty;

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.Equal(1f, features[NeuralFeatureEncoder.OppAnnounced20Offset]);
            Assert.Equal(0f, features[NeuralFeatureEncoder.OppAnnounced40Offset]);
        }

        [Fact]
        public void EncodeShouldMarkOppAnnounce40()
        {
            var context = TestContexts.LeadingContext();
            context.FirstPlayedCard = Card.GetCard(CardSuit.Club, CardType.King);
            context.FirstPlayerAnnounce = Announce.Forty;

            var features = new float[NeuralFeatureEncoder.FeatureCount];
            NeuralFeatureEncoder.Encode(features, context, new List<Card>(), null, new List<Card>());

            Assert.Equal(0f, features[NeuralFeatureEncoder.OppAnnounced20Offset]);
            Assert.Equal(1f, features[NeuralFeatureEncoder.OppAnnounced40Offset]);
        }
    }
}
