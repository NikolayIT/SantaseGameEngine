namespace Santase.Logic.Tests.Extensions
{
    using NUnit.Framework;

    using Santase.Logic.Extensions;

    [TestFixture]
    public class RandomProviderTests
    {
        [Test]
        public void InstanceShouldReturnNonNullableValue()
        {
            var randomInstance = RandomProvider.Instance;
            Assert.IsNotNull(randomInstance);
        }

        [Test]
        public void InstanceShouldReturnTheSameInstanceEveryTime()
        {
            var firstRandomInstance = RandomProvider.Instance;
            var secondRandomInstance = RandomProvider.Instance;
            Assert.AreSame(firstRandomInstance, secondRandomInstance);
        }
    }
}
