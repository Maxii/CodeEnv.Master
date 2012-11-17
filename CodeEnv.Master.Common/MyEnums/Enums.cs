﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Enums.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using CodeEnv.Master.Resources;

    /// <summary>
    /// Generic Class that simplifies the syntax associated with Enums. Useful when you know the enum type you are
    /// dealing with.  Allows syntax such as:
    /// <code>var getValues = Enums<MyEnumbers>.GetValues();
    ///var parse = Enums<MyEnumbers>.Parse("Seven");
    ///var isDefined = Enums<MyEnumbers>.IsDefined(MyEnumbers.Eight);
    ///var getName = Enums<MyEnumbers>.GetName(MyEnumbers.Eight);
    ///
    ///MyEnumbers tryParse;
    ///Enums<MyEnumbers>.TryParse("Zero", out tryParse);
    /// </code>
    /// </summary>
    /// <remarks>Courtesy of Damien Guard. http://damieng.com/blog/category/development/net </remarks>
    public static class Enums<T> where T : struct {

        // Each dIctionary is completely populated for the Type T the first time any of these methods are called for the type.
        private static readonly IEnumerable<T> All = Enum.GetValues(typeof(T)).Cast<T>();
        private static readonly Dictionary<string, T> InsensitiveNames = All.ToDictionary(k => Enum.GetName(typeof(T), k).ToUpperInvariant());  // Upper chg from FxCop
        private static readonly Dictionary<string, T> SensitiveNames = All.ToDictionary(k => Enum.GetName(typeof(T), k));
        private static readonly Dictionary<int, T> Values = All.ToDictionary(k => Convert.ToInt32(k, CultureInfo.InvariantCulture));    // Culture chg from FxCop
        private static readonly Dictionary<T, string> Names = All.ToDictionary(k => k, v => v.ToString());

        /// <summary>
        /// Determines whether the specified enumConstant is defined by T.
        /// Syntax: <c>var isDefined = Enums<MyEnumbers>.IsDefined(MyEnumbers.Eight);</c>
        /// </summary>
        /// <param name="enumConstant">The enumConstant.</param>
        /// <returns>
        ///   <c>true</c> if the specified enumConstant is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(T enumConstant) {
            return Names.Keys.Contains(enumConstant);
        }

        /// <summary>
        /// Determines whether the specified enumName is defined by T.
        /// Syntax: <c>var isDefined = Enums<MyEnumbers>.IsDefined("Eight");</c>
        /// </summary>
        /// <param name="enumName">The enumName.</param>
        /// <returns>
        ///   <c>true</c> if the specified enumName is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(string enumName) {
            return SensitiveNames.Keys.Contains(enumName);
        }

        /// <summary>
        /// Determines whether the specified int value is defined by T.
        /// Syntax: <c>var isDefined = Enums<MyEnumbers>.IsDefined(8);</c>
        /// </summary>
        /// <param name="value">The int value.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(int value) {
            return Values.Keys.Contains(value);
        }

        /// <summary>
        /// Gets the values. Syntax: <c>var getValues = Enums<MyEnumbers>.GetValues();</c>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> GetValues() {
            return All;
        }

        /// <summary>
        /// Gets the names of all Type T constants.
        /// </summary>
        /// <returns>An array of string names.</returns>
        public static string[] GetNames() {
            return Names.Values.ToArray();
        }

        /// <summary>
        /// Gets the string name associated with the provided Enum constant.
        /// </summary>
        /// <param name="enumConstant">The enumConstant.</param>
        /// <returns>The string name associated with the Enum constant. Can be string.EMPTY if the constant doesn'fieldType exist.</returns>
        public static string GetName(T enumConstant) {
            string name;
            Names.TryGetValue(enumConstant, out name);
            return name;
        }

        /// <summary>
        /// Parses the specified string name into its corresponding Type T constant.
        /// Syntax: <c>var parse = Enums<MyEnumbers>.Parse("Seven");</c>
        /// </summary>
        /// <param name="enumConstant">The string name equivalent of a Type T constant.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static T Parse(string enumName) {
            T parsed = default(T);  // the default enumConstant(null, 0, false) of the generic type T
            if (!SensitiveNames.TryGetValue(enumName, out parsed)) {
                throw new ArgumentException(ErrorMessages.NoEnumForString.Inject(enumName));
            }
            return parsed;
        }

        /// <summary>
        ///  Parses the specified string name into its corresponding Type T constant.
        /// </summary>
        /// <param name="enumConstant">The string name equivalent of a Type T constant.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static T Parse(string enumName, bool ignoreCase) {
            if (!ignoreCase) {
                return Parse(enumName);
            }

            T parsed = default(T);  // the default enumConstant(null, 0, false) of the generic type T
            if (!InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out parsed)) {
                throw new ArgumentException(ErrorMessages.NoEnumForString.Inject(enumName));
            }
            return parsed;
        }

        /// <summary>
        /// Attempts to parse the string name provided into its corresponding Type T  constant.
        /// Syntax: <c>MyEnumbers tryParse;     Enums<MyEnumbers>.TryParse("Zero", out tryParse);</c>
        /// </summary>
        /// <param name="enumConstant">The string name equivalent of a Type T  constant.</param>
        /// <param name="returnValue">The Type T  constant represented by the string name.</param>
        /// <returns><c>true</c> if successful.</returns>
        public static bool TryParse(string enumName, out T returnValue) {
            return SensitiveNames.TryGetValue(enumName, out returnValue);
        }

        /// <summary>
        ///Attempts to parse the string name provided into its corresponding Type T  constant.
        /// </summary>
        /// <param name="enumConstant">The string name equivalent of an Type T  constant.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <param name="returnValue">The Type T  constant represented by the string name.</param>
        /// <returns><c>true</c> if successful.</returns>
        public static bool TryParse(string enumName, bool ignoreCase, out T returnValue) {
            if (!ignoreCase) {
                return TryParse(enumName, out returnValue);
            }
            return InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out returnValue);
        }

        /// <summary>
        /// Parses the specified string name into its corresponding Type T  constant.
        /// Syntax: <c>MyEnumbers myValue = Enums<MyEnumbers>.ParseOrNull("Nine-teen") ?? MyEnumbers.Zero;</c>
        /// </summary>
        /// <param name="enumName">The string enumName.</param>
        /// <returns>The Type T  Constant associated with enumName or null if not found.</returns>
        public static T? ParseOrNull(string enumName) {
            if (!Utility.CheckForContent(enumName)) {
                return null;
            }

            T foundConstant;
            if (InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out foundConstant)) {
                return foundConstant;
            }
            return null;
        }

        /// <summary>
        /// Casts the provided int value into its equivalentType T Constant.
        /// </summary>
        /// <param name="value">The int value.</param>
        /// <returns>The Type T constant or null if there is no equivalent.</returns>
        public static T? CastOrNull(int value) {
            T foundConstant;
            if (Values.TryGetValue(value, out foundConstant)) {
                return foundConstant;
            }
            return null;
        }

        private static Random rng = new Random();

        /// <summary>
        /// Gets a random Enum constant selected from all values of type T.
        /// </summary>
        /// <returns></returns>
        public static T GetRandom() {
            T[] values = GetValues().ToArray<T>();
            return values[rng.Next(values.Length)];
        }

        /// <summary>
        /// Gets a random Enum constant of Type T from the array of Type T enums provided.
        /// </summary>
        /// <param name="values">The enum values to select from.</param>
        /// <returns></returns>
        public static T GetRandom(T[] values) {
            return values[rng.Next(values.Length)];
        }
    }
}
