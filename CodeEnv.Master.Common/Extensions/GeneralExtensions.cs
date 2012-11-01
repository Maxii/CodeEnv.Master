// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GenericExtensions.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Extensions {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class GeneralExtensions {

        /// <summary>
        /// Allows syntax like: <c>if(reallyLongStringVariableName.EqualsAnyOf(string1, string2, string3)</c>, or
        /// <c>if(reallyLongIntVariableName.EqualsAnyOf(1, 2, 4, 8)</c>, or
        /// <c>if(reallyLongMethodParameterName.EqualsAnyOf(SomeEnum.value1, SomeEnum.value2, SomeEnum.value3)</c>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source calling the Extension method.</param>
        /// <param name="args">The list of values of Type T to check against.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static bool EqualsAnyOf<T>(this T source, params T[] args) {
            if (null == source) {   // UNDERSTAND how can the object calling this extension be null?
                throw new ArgumentNullException("source");
            }
            return args.Contains<T>(source);
        }

        /// <summary>
        /// Used from an instance of Random to get a 50/50 chance.
        /// </summary>
        /// <param name="rngInstance">A Random Number Generator instance.</param>
        /// <returns></returns>
        public static bool CoinToss(this Random rngInstance) {
            return rngInstance.Next(2) == 0;
        }

        /// <summary>
        /// Randomly picks an arg from args. Used from an instance of Random.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rngInstance">The Random Number Generator instance.</param>
        /// <param name="args">The list of values of Type T to pick from.</param>
        /// <returns></returns>
        public static T OneOf<T>(this Random rngInstance, params T[] args) {
            return args[rngInstance.Next(args.Length)];
        }

    }
}

