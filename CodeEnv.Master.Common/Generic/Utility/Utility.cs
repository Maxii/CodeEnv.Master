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
    using System.Linq;
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

        #region Argument Validation

        /// <summary>
        /// Validates that the provided argument is not null. Commonly used to check method parameters. If the parameter is used to
        /// call a method, then an exception is automatically thrown and this check
        /// is not needed. If the parameter is simply assigned to another field, or
        /// if it is simply passed onto another method as a parameter, then this
        /// explicit test can be useful.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ValidateNotNull(object arg) {
            if (arg == null) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentNullException(ErrorMessages.Null.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided text is not null and that it contains non-whitespace content.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <exception cref="ArgumentException">SbText is null or has no content.</exception>
        public static void ValidateForContent(string text) {
            if (!CheckForContent(text)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentNullException(ErrorMessages.EmptyOrNullString.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The acceptable low.</param>
        /// <param name="high">The acceptable high.</param>
        /// <exception cref="IllegalArgumentException"></exception>
        public static void ValidateForRange(int number, int low, int high) {
            if (!IsInRange(number, low, high)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, low, high, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateForRange(float number, float low, float high) {
            if (!IsInRange(number, low, high)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, low, high, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="range">The range.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static void ValidateForRange(int number, ValueRange<int> range) {
            if (!range.ContainsValue(number)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, range.Minimum, range.Maximum, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number is within the designated range, inclusive.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="range">The range.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static void ValidateForRange(float number, ValueRange<float> range) {
            if (!range.ContainsValue(number)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(number, range.Minimum, range.Maximum, callingMethodName));
            }
        }

        /// <summary>
        /// Validates the provided number(s) are not negative.
        /// </summary>
        /// <param name="numbers">The numbers to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateNotNegative(params int[] numbers) {
            numbers.ForAll(n => {
                if (n < Constants.Zero) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentOutOfRangeException(ErrorMessages.NegativeValue.Inject(n, callingMethodName));
                }
            });
        }

        /// <summary>
        /// Validates the provided number(s) are not negative.
        /// </summary>
        /// <param name="numbers">The numbers to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateNotNegative(params float[] numbers) {
            numbers.ForAll(n => {
                if (n < Constants.ZeroF) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentOutOfRangeException(ErrorMessages.NegativeValue.Inject(n, callingMethodName));
                }
            });
        }

        /// <summary>
        /// Validates the provided IEnumerable is not empty or null;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The collection.</param>
        /// <exception cref="System.ArgumentException">enumerable is null or empty</exception>
        public static void ValidateNotNullOrEmpty<T>(IEnumerable<T> enumerable) {
            if (enumerable.IsNullOrEmpty()) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.CollectionEmpty.Inject(callingMethodName));
            }
        }

        /// <summary>
        /// Validates the objects provided are all of Type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void ValidateType<T>(params object[] args) {
            foreach (object arg in args) {
                if (arg.GetType() != typeof(T)) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentException(ErrorMessages.IncorrectType.Inject(arg.GetType(), typeof(T), callingMethodName));
                }
            }
        }

        public static void ValidateTypeAndLength<T>(int length, params object[] args) {
            if (args.Length != length) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.IncorrectLength.Inject(args.Length, length, callingMethodName));
            }
            foreach (object arg in args) {
                if (arg.GetType() != typeof(T)) {
                    string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                    throw new ArgumentException(ErrorMessages.IncorrectType.Inject(arg.GetType(), typeof(T), callingMethodName));
                }
            }
        }

        /// <summary>
        /// General purpose validation method that throws an exception if isValid is false.
        /// </summary>
        /// <param name="isValid">if set to <c>true</c> [is valid].</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void Validate(bool isValid) {
            if (!isValid) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.InvalidArguments.Inject(callingMethodName));
            }
        }

        #endregion

        /// <summary>
        /// The current DateTime as measured by .Net.
        /// </summary>
        public static DateTime SystemTime { get { return DateTime.UtcNow; } }

        /// <summary>
        /// A text time stamp as measured by .Net. Format is seconds.hundredths.
        /// <remarks>Hundredths as accuracy of System.DateTime is no better than 15 milliseconds.</remarks>
        /// </summary>
        public static string TimeStamp { get { return DateTime.UtcNow.ToString("ss.ff"); } }

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
        /// Checks for legal content - i.e. not null, not empty and not all whitespace.
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
            ValidateForContent(text);
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
        /// Returns a number rounded to the nearest multiple of another number (rounds up)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundCeil(float number, float multiple) {
            return Mathf.Ceil(number / multiple) * multiple;
        }

        /// <summary>
        /// Returns a number rounded to the nearest multiple of another number (rounds up or down)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundMultiple(float number, float multiple) {
            return Mathf.Round(number / multiple) * multiple;
        }

        /// <summary>
        /// Returns a number rounded to the nearest multiple of another number (rounds down)
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        /// <returns></returns>
        public static float RoundFloor(float number, float multiple) {
            return Mathf.Floor(number / multiple) * multiple;
        }

        /// <summary>
        /// Combines the specified enumerables into 1 enumerable. Duplicates are allowed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerables">The enumerables.</param>
        /// <returns></returns>
        public static IEnumerable<T> Combine<T>(IEnumerable<IEnumerable<T>> enumerables) {
            IEnumerable<T> result = Enumerable.Empty<T>();
            foreach (var e in enumerables) {
                result = result.Concat<T>(e);
            }
            return result;
        }

        /// <summary>
        /// Combines the specified enumerables into 1 enumerable of unique elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerables">The enumerables.</param>
        /// <returns></returns>
        public static IEnumerable<T> CombineUnique<T>(IEnumerable<IEnumerable<T>> enumerables) {
            IEnumerable<T> result = Enumerable.Empty<T>();
            foreach (var e in enumerables) {
                result = result.Union<T>(e);
            }
            return result;
        }

        /// <summary>
        /// Returns a 32 character Binary representation of n.
        /// </summary>
        /// <param name="n">The int to show in binary.</param>
        /// <returns></returns>
        public static string GetBinaryString(int n) {
            char[] b = new char[32];
            int pos = 31;
            int i = 0;
            while (i < 32) {
                if ((n & (1 << i)) != 0) {
                    b[pos] = '1';
                }
                else {
                    b[pos] = '0';
                }
                pos--;
                i++;
            }
            return new string(b);
        }

        /// <summary>
        /// Returns an 8 character Hex representation of n.
        /// </summary>
        /// <param name="n">The int to show in hex.</param>
        /// <returns></returns>
        public static string GetHexString(int n) {
            return n.ToString("X8");
        }

    }
}

