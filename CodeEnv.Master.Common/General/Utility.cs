// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: General.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Resources;

    using CodeEnv.Master.Resources;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class Utility {

        /// <summary>
        /// Determines whether a enumConstant is in the range low..high, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The acceptable low.</param>
        /// <param name="high">The acceptable high.</param>
        /// <returns>
        ///   <c>true</c> if in range; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentException">Low is greater than High.</exception>
        public static bool IsInRange(int number, int low, int high) {
            if (low > high) {
                string errorMsg = ErrorMessages.LowGreaterThanHigh.Inject(low, high);
                throw new ArgumentException(errorMsg);
            }
            return ((low <= number) && (number <= high));
        }

        /// <summary>
        /// Parses the boolean.
        /// </summary>
        /// <param name="boolText">The boolean string.</param>
        /// <returns> Returns <c>true</c> if bool parameter equals "true" (ignoring
        /// case), or <c>false</c>  if bool equals "false" (ignoring case).</returns>
        /// <exception cref="System.ArgumentException">Cannot parse into Boolean: 
        ///                                                    + boolText</exception>
        public static bool ParseBoolean(string boolText) {
            if (boolText.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            else if (boolText.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            else {
                throw new ArgumentException(ErrorMessages.NotBoolean.Inject(boolText));
            }
        }

        /// <summary>
        /// Checks for legal content - ie. not null, not empty and not all whitespace.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static bool CheckForContent(string text) {
            bool result = true;
            if (String.IsNullOrEmpty(text) || text.Trim().Length == 0) {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Validates access to the method or object throwing an Exception if access is not valid.
        /// </summary>
        /// <param name="accessIsValid">if set to <c>true</c> [is valid].</param>
        /// <exception cref="System.MethodAccessException"></exception>
        public static void ValidateAccess(bool accessIsValid) {
            if (!accessIsValid) {
                throw new MethodAccessException(ErrorMessages.InvalidAccess);
            }
        }

        /// <summary>
        /// Generates and returns a list of substrings from the provided text, using the specified delimiter.
        /// </summary>
        /// <param name="text">The text to separate.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static IList<String> GetListFromString(String text, char delimiter) {
            Arguments.ValidateForContent(text);
            IList<string> result = text.Split(delimiter);
            return result;
        }
    }
}

