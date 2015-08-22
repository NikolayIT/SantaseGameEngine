namespace Santase.Logic.Extensions
{
    using System;

    /// <summary>
    /// Static class representing a single instance of the Random class
    /// </summary>
    public static class RandomProvider
    {
        private static Random instance;

        /// <summary>
        /// The instance of the random class
        /// </summary>
        /// <value>
        /// The instance of the random class
        /// </value>
        public static Random Instance => instance ?? (instance = new Random());
    }
}
