// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GenericExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// COMMENT 
    /// </summary>
    public static class GeneralExtensions {

        /// <summary>
        /// Allows syntax like: <c>if(reallyLongStringVariableName.EqualsAnyOf(string1, string2, string3)</c>, or
        /// <c>if(reallyLongIntVariableName.EqualsAnyOf(1, 2, 4, 8)</c>, or
        /// <c>if(reallyLongMethodParameterName.EqualsAnyOf(SomeEnum.value1, SomeEnum.value2, SomeEnum.value3)</c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source calling the Extension method.</param>
        /// <param name="itemsToCompare">The array of items to compare to the source.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static bool EqualsAnyOf<T>(this T source, params T[] itemsToCompare) {
            //  'this' source can never be null without the CLR throwing a Null reference exception
            Arguments.ValidateNotNullOrEmpty(itemsToCompare);
            return itemsToCompare.Contains<T>(source);
        }

        /// <summary>
        /// Used from an instance of Random to get a 50/50 chance.
        /// </summary>
        /// <param name="sourceRNG">A Random Number Generator instance.</param>
        /// <returns></returns>
        public static bool CoinToss(this Random sourceRNG) {
            //  'this' sourceRNG can never be null without the CLR throwing a Null reference exception
            return sourceRNG.Next(2) == 0;
        }

        /// <summary>
        /// Randomly picks an arg from itemsToPickFrom. Used from an instance of Random.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceRNG">The Random Number Generator instance.</param>
        /// <param name="itemsToPickFrom">The array of items of Type T to pick from.</param>
        /// <returns></returns>
        public static T OneOf<T>(this Random sourceRNG, params T[] itemsToPickFrom) {
            Arguments.ValidateNotNullOrEmpty(itemsToPickFrom);
            return itemsToPickFrom[sourceRNG.Next(itemsToPickFrom.Length)];
        }

        /// <summary>
        /// Provides for the application of a work action to all the elements in an IEnumerable sourceSequence.
        /// Syntax: <code>sequenceOfTypeT.ForAll((T n) => Console.WriteLine(n.ToString()));</code> read as
        /// "For each element in the T sourceSequence, write the string version to the console."
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceSequence">The Sequence of Type T calling the extension.</param>
        /// <param name="actionToExecute">The work to perform on the sequence, usually expressed in lambda form.</param>
        public static void ForAll<T>(this IEnumerable<T> sourceSequence, Action<T> actionToExecute) {
            foreach (T item in sourceSequence) {
                actionToExecute(item);
            }
        }

        /// <summary>
        /// Checks the range of the number against an allowed variation percentage around the number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="target">The target value.</param>
        /// <param name="allowedPercentageVariation">Optional allowed percentage variation as a whole number. 1.0 = one percent variation, the default.</param>
        /// <returns></returns>
        public static bool CheckRange(this float number, float target, float allowedPercentageVariation = 1.0F) {
            float allowedLow = (100F - allowedPercentageVariation) / 100 * target;
            float allowedHigh = (100F + allowedPercentageVariation) / 100 * target;
            return Utility.IsInRange(number, allowedLow, allowedHigh);
        }

        ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="predicate">The expression to test the items against.</param>
        ///<returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate) {
            if (items == null)
                throw new ArgumentNullException("items");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items) {
                if (predicate(item))
                    return retVal;
                retVal++;
            }
            return -1;
        }

        ///<summary>Finds the index of the first occurence of an item in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="item">The item to find.</param>
        ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item) {
            return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i));
        }

        /// <summary>
        /// Removes items from the source sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence">The IEnumerable sequence of Type T.</param>
        /// <param name="itemsToRemove">The items to remove.</param>
        /// <returns>
        /// An IEnumerable sequence of Type T with items removed.
        /// </returns>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> sequence, params T[] itemsToRemove) {
            return sequence.Except<T>(itemsToRemove as IEnumerable<T>);

        }

        /// <summary>
        /// Determines whether the collection is null or contains no elements.
        /// </summary>
        /// <typeparam name="T">The IEnumerable type.</typeparam>
        /// <param name="enumerable">The enumerable, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return true;
            }
            // If this is a list, use the Count property for efficiency. The Count property is O(1) while IEnumerable.Count() is O(N).
            var collection = enumerable as ICollection<T>;
            if (collection != null) {
                return collection.Count < 1;
            }
            return !enumerable.Any();
        }

        /// <summary>
        /// Constructs a string separated by the provided delimiter from the elements of the IEnumerable source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="delimiter">The delimiter string. Default is ",".</param>
        /// <returns></returns>
        public static string Concatenate<T>(this IEnumerable<T> source, string delimiter = Constants.Comma) {
            var sb = new StringBuilder();
            bool first = true;
            foreach (T t in source) {
                if (first) {
                    first = false;
                }
                else {
                    sb.Append(delimiter);
                }
                sb.Append(t);
            }
            return sb.ToString();
        }

    }
}

