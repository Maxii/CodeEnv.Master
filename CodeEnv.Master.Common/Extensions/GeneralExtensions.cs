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
        /// <param name="allowedPercentageVariation">The allowed percentage variation as a whole number. 1.0 = one percent variation.</param>
        /// <returns></returns>
        public static bool CheckRange(this float number, float target, float allowedPercentageVariation) {
            float allowedLow = (100F - allowedPercentageVariation) / 100 * target;
            float allowedHigh = (100F + allowedPercentageVariation) / 100 * target;
            return Utility.IsInRange(number, allowedLow, allowedHigh);
        }

    }
}

