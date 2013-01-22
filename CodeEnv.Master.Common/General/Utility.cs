// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: General.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Resources;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// COMMENT 
    /// </summary>
    public static class Utility {

        /// <summary>
        /// Determines whether an integer is in the range low..high, inclusive.
        /// </summary>
        /// <param name="number">The int number.</param>
        /// <param name="low">The int acceptable low.</param>
        /// <param name="high">The int acceptable high.</param>
        /// <returns>
        ///   <c>true</c> if in range; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Low is greater than High.</exception>
        public static bool IsInRange(int number, int low, int high) {
            if (low > high) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.LowGreaterThanHigh.Inject(low, high, callingMethodName));
            }
            return ((low <= number) && (number <= high));
        }

        /// <summary>
        /// Determines whether a float is in the range low..high, inclusive.
        /// </summary>
        /// <param name="number">The float number.</param>
        /// <param name="low">The float acceptable low.</param>
        /// <param name="high">The float acceptable high.</param>
        /// <returns>
        ///   <c>true</c> if in range; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Low is greater than High.</exception>
        public static bool IsInRange(float number, float low, float high) {
            if (low > high) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.LowGreaterThanHigh.Inject(low, high, callingMethodName));
            }
            return ((low <= number) && (number <= high));
        }


        /// <summary>
        /// Parses the boolean.
        /// </summary>
        /// <param name="textToParse">The boolean string.</param>
        /// <returns> Returns <c>true</c> if bool parameter equals "true" (ignoring
        /// case), or <c>false</c>  if bool equals "false" (ignoring case).</returns>
        /// <exception cref="System.ArgumentException">Cannot parse into Boolean: 
        ///                                                    + textToParse</exception>
        public static bool ParseBoolean(string textToParse) {
            if (textToParse.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            else if (textToParse.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            else {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.NotBoolean.Inject(textToParse, callingMethodName));
            }
        }

        /// <summary>
        /// Checks for legal content - ie. not null, not empty and not all whitespace.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static bool CheckForContent(string text) {
            if (String.IsNullOrEmpty(text) || text.Trim().Length == 0) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates access to the method or object throwing an Exception if access is not valid.
        /// </summary>
        /// <param name="accessIsValid">if set to <c>true</c> [is valid].</param>
        /// <exception cref="System.MethodAccessException"></exception>
        public static void ValidateAccess(bool accessIsValid) {
            if (!accessIsValid) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new MethodAccessException(ErrorMessages.InvalidAccess.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Checks whether the provided ICollection has content.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>false if null or empty, true otherwise</returns>
        public static bool CheckForContent<T>(ICollection<T> collection) {
            if (collection == null || collection.Count == 0) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generates and returns a list of substrings from the provided text, using the specified delimiter.
        /// </summary>
        /// <param name="text">The text to separate.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static IList<String> ConstructListFromString(String text, char delimiter) {
            Arguments.ValidateForContent(text);
            IList<string> result = text.Split(delimiter);
            return result;
        }
    }
}

