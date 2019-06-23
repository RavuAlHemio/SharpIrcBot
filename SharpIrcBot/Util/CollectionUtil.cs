using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace SharpIrcBot.Util
{
    public static class CollectionUtil
    {
        public static void Shuffle<T>(this IList<T> list, [CanBeNull] Random rng = null)
        {
            if (rng == null)
            {
                rng = new Random();
            }

            // Fisher-Yates shuffle (Knuth shuffle)
            for (int i = 0; i < list.Count - 1; ++i)
            {
                // i <= j < count
                int j = rng.Next(i, list.Count);

                // swap
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Returns a list containing the elements of the enumerable in shuffled order.
        /// </summary>
        /// <typeparam name="T">The type of item in the enumerable.</typeparam>
        /// <param name="itemsToShuffle">The enumerable whose items to return in a shuffled list.</param>
        /// <param name="rng">The random number generator to use, or <c>null</c> to create one.</param>
        /// <returns>List containing the enumerable's items in shuffled order.</returns>
        [CanBeNull]
        public static List<T> ToShuffledList<T>(this IEnumerable<T> itemsToShuffle, [CanBeNull] Random rng = null)
        {
            if (rng == null)
            {
                rng = new Random();
            }

            var list = itemsToShuffle.ToList();
            list.Shuffle(rng);
            return list;
        }

        public static T? TryTake<T>(this IEnumerator<T> enumer)
            where T : struct
        {
            if (!enumer.MoveNext())
            {
                return null;
            }
            return enumer.Current;
        }

        public static string StringJoin<T>(this IEnumerable<T> pieces, string glue)
        {
            return string.Join<T>(glue, pieces);
        }

        public static bool IsSorted<T>(this IEnumerable<T> enumerable)
            where T : IComparable<T>
            => enumerable.IsSorted((a, b) => a.CompareTo(b));

        public static bool IsSorted<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparator)
        {
            using (IEnumerator<T> numer = enumerable.GetEnumerator())
            {
                if (!numer.MoveNext())
                {
                    // zero elements => vacuous truth
                    return true;
                }

                T elem = numer.Current;

                while (numer.MoveNext())
                {
                    if (comparator(elem, numer.Current) > 0)
                    {
                        return false;
                    }
                    elem = numer.Current;
                }
            }

            return true;
        }

        public static ImmutableDictionary<TKey, TValue>.Builder Adding<TKey, TValue>(
                this ImmutableDictionary<TKey, TValue>.Builder dict, TKey key, TValue value)
        {
            dict.Add(key, value);
            return dict;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> tuple, out TKey key, out TValue value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
