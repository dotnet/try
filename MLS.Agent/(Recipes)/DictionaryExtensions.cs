using System;
using System.Collections.Generic;

namespace Recipes
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        ///     Adds a key/value pair to the dictionary if the key does not already exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key.</param>
        /// <returns>
        ///     The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value for the key as returned by valueFactory if the key was not in the dictionary.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);
            dictionary.Add(key, value);
            return value;
        }
    }
}
