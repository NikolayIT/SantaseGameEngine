using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Santase.Logic.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Shuffle algorithm as seen on page 32 in the book "Algorithms" (4th edition) by Robert Sedgewick
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var array = source.ToArray();
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                // Exchange a[i] with random element in a[i..n-1]
                int r = i + RandomProvider.Instance.Next(0, n - i);
                var temp = array[i];
                array[i] = array[r];
                array[r] = temp;
            }

            return array;
        }
    }
}
