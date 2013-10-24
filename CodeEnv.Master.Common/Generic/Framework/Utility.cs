// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: General.cs
// General Utilities Class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// General Utilities Class.
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
        /// <param name="delimiter">The delimiter. Default is a space.</param>
        /// <returns></returns>
        public static IList<string> ConstructListFromString(string text, char delimiter = Constants.SpaceDelimiter) {
            Arguments.ValidateForContent(text);
            IList<string> result = text.Split(delimiter);
            return result;
        }

        /// <summary>
        /// Gets the string name of the property. Usage syntax:
        /// <c>var propertyname = GetPropertyName( () => myObject.AProperty);</c>
        /// returns "AProperty".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <returns></returns>
        public static string GetPropertyName<T>(Expression<Func<T>> propertyExpression) {
            return (propertyExpression.Body as MemberExpression).Member.Name;
        }

        /// <summary>
        /// Converts a list of type T to an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <returns>An array of type T.</returns>
        public static T[] ConvertToArray<T>(IList list) {
            T[] result = new T[list.Count];
            list.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Splits the provided camel case string into words.
        /// </summary>
        /// <see cref="http://stackoverflow.com/questions/773303/splitting-camelcase"/>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string SplitCamelCase(string input) {
            return System.Text.RegularExpressions.Regex.Replace(input, "(?<=[a-z])([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        /// <summary>
        /// Returns a number rounded to the nearest multiple of anothr number (rounds up)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundCeil(float number, float multiple) {
            return Mathf.Ceil(number / multiple) * multiple;
        }

        /// <summary>
        /// Returns a number rounded to the nearest multiple of anothr number (rounds up or down)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundMultiple(float number, float multiple) {
            return Mathf.Round(number / multiple) * multiple;
        }

        /// <summary>
        /// Returns a number rounded to the nearest multiple of anothr number (rounds down)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundFloor(float number, float multiple) {
            return Mathf.Floor(number / multiple) * multiple;
        }

    }
}

