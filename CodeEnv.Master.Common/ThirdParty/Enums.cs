// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Enums.cs
// Generic Class that simplifies the syntax associated with Enums. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Generic Class that simplifies the syntax associated with Enums. Useful when you know the enum type you are
    /// dealing with.  Allows syntax such as:
    /// <code>var getValues = Enums&lt;MyEnumbers&gt;.GetValues();
    ///var parse = Enums&lt;MyEnumbers&gt;.Parse("Seven");
    ///var isDefined = Enums&lt;MyEnumbers&gt;.IsDefined(MyEnumbers.Eight);
    ///var getName = Enums&lt;MyEnumbers&gt;;.GetName(MyEnumbers.Eight);
    ///
    ///MyEnumbers tryParse;
    ///Enums&lt;MyEnumbers&gt;.TryParse("Zero", out tryParse);
    /// </code>
    /// </summary>
    /// <remarks>Courtesy of Damien Guard. http://damieng.com/blog/category/development/net </remarks>
    public static class Enums<E> where E : struct {

        private static Random _rng = new Random();

        // Each dIctionary is completely populated for the Type E the first time any of these methods are called for the type.
        private static readonly IEnumerable<E> All = Enum.GetValues(typeof(E)).Cast<E>();
        private static readonly Dictionary<string, E> InsensitiveNames = All.ToDictionary(k => Enum.GetName(typeof(E), k).ToUpperInvariant());  // Upper chg from FxCop
        private static readonly Dictionary<string, E> SensitiveNames = All.ToDictionary(k => Enum.GetName(typeof(E), k));
        private static readonly Dictionary<int, E> Values = All.ToDictionary(k => Convert.ToInt32(k, CultureInfo.InvariantCulture));    // Culture chg from FxCop
        private static readonly Dictionary<E, string> Names = All.ToDictionary(k => k, v => v.ToString());

        /// <summary>
        /// Determines whether the specified enumConstant is defined by E.
        /// Syntax: <c>var isDefined = Enums&lt;MyEnumbers&gt;.IsDefined(MyEnumbers.Eight);</c>
        /// </summary>
        /// <param name="enumConstant">The enumConstant.</param>
        /// <returns>
        ///   <c>true</c> if the specified enumConstant is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(E enumConstant) {
            return Names.Keys.Contains(enumConstant);
        }

        /// <summary>
        /// Determines whether the specified enumName is defined by E.
        /// Syntax: <c>var isDefined = Enums&lt;MyEnumbers&gt;.IsDefined("Eight");</c>
        /// </summary>
        /// <param name="enumName">The enumName.</param>
        /// <returns>
        ///   <c>true</c> if the specified enumName is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(string enumName) {
            return SensitiveNames.Keys.Contains(enumName);
        }

        /// <summary>
        /// Determines whether the specified int value is defined by E.
        /// Syntax: <c>var isDefined = Enums&lt;MyEnumbers&gt;.IsDefined(8);</c>
        /// </summary>
        /// <param name="value">The int value.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(int value) {
            return Values.Keys.Contains(value);
        }

        /// <summary>
        /// Gets all the values of the Enum Type E.
        /// </summary>
        /// <param name="excludeDefault">if set to <c>true</c> [exclude default].</param>
        /// <returns></returns>
        public static IEnumerable<E> GetValues(bool excludeDefault = false) {
            if (excludeDefault) {
                return All.Except<E>(default(E));
            }
            return All;
        }

        /// <summary>
        /// Gets the names of all Type E constants.
        /// </summary>
        /// <param name="excludeDefault">if set to <c>true</c> [exclude default].</param>
        /// <returns>
        /// An array of string names.
        /// </returns>
        public static string[] GetNames(bool excludeDefault = false) {
            if (excludeDefault) {
                return Names.Values.ToList().Except(GetName(default(E))).ToArray();
            }
            return Names.Values.ToArray();
        }

        /// <summary>
        /// Gets the names of all Type E constants except those E constants provided.
        /// </summary>
        /// <param name="exceptions">The exceptions.</param>
        /// <returns></returns>
        public static string[] GetNamesExcept(params E[] exceptions) {
            var exceptionNames = new List<string>(exceptions.Length);
            exceptions.ForAll(e => exceptionNames.Add(GetName(e)));
            return Names.Values.ToArray().Except(exceptionNames).ToArray();
        }

        /// <summary>
        /// Gets the string name associated with the provided Enum constant.
        /// </summary>
        /// <param name="enumConstant">The enumConstant.</param>
        /// <returns>The string name associated with the Enum constant. Can be string.EMPTY if the constant doesn'fieldType exist.</returns>
        public static string GetName(E enumConstant) {
            string name;
            Names.TryGetValue(enumConstant, out name);
            return name;
        }

        /// <summary>
        /// Parses the specified string name into its corresponding Type E constant.
        /// Syntax: <c>var parse = Enums&lt;MyEnumbers&gt;.Parse("Seven");</c>
        /// </summary>
        /// <param name="enumName">The string name equivalent of a Type E constant.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static E Parse(string enumName) {
            E parsed = default(E);  // the default enumConstant(null, 0, false) of the generic type E
            if (!SensitiveNames.TryGetValue(enumName, out parsed)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.NoEnumForString.Inject(enumName, callingMethodName));
            }
            return parsed;
        }

        /// <summary>
        ///  Parses the specified string name into its corresponding Type E constant.
        /// </summary>
        /// <param name="enumName">The string name equivalent of a Type E constant.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static E Parse(string enumName, bool ignoreCase) {
            Arguments.ValidateForContent(enumName);
            if (!ignoreCase) {
                return Parse(enumName);
            }

            E parsed = default(E);  // the default enumConstant(null, 0, false) of the generic type T
            if (!InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out parsed)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(ErrorMessages.NoEnumForString.Inject(enumName, callingMethodName));
            }
            return parsed;
        }

        /// <summary>
        /// Attempts to parse the string name provided into its corresponding Type E constant.
        /// Syntax: <c>MyEnumbers tryParse;     Enums&lt;MyEnumbers&gt;.TryParse("Zero", out tryParse);</c>
        /// </summary>
        /// <param name="enumName">The string name equivalent of a Type E  constant.</param>
        /// <param name="returnValue">The Type E  constant represented by the string name. If the parsing
        /// fails, the value returned is the default value of E.</param>
        /// <returns><c>true</c> if successful.</returns>
        public static bool TryParse(string enumName, out E returnValue) {
            return SensitiveNames.TryGetValue(enumName, out returnValue);
        }

        /// <summary>
        ///Attempts to parse the string name provided into its corresponding Type E  constant.
        /// </summary>
        /// <param name="enumName">The string name equivalent of an Type E  constant.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <param name="returnValue">The Type E constant represented by the string name. If the parsing
        /// fails, the value returned is the default value of E.</param>
        /// <returns><c>true</c> if successful.</returns>
        public static bool TryParse(string enumName, bool ignoreCase, out E returnValue) {
            Arguments.ValidateForContent(enumName);
            if (!ignoreCase) {
                return TryParse(enumName, out returnValue);
            }
            return InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out returnValue);
        }

        /// <summary>
        /// Parses the specified string name into its corresponding Type E  constant.
        /// Syntax: <c>MyEnumbers myValue = Enums&lt;MyEnumbers&gt;.ParseOrNull("Nine-teen") ?? MyEnumbers.Zero;</c>
        /// </summary>
        /// <param name="enumName">The string enumName.</param>
        /// <returns>The Type E Constant associated with enumName or null if not found.</returns>
        public static E? ParseOrNull(string enumName) {
            if (!Utility.CheckForContent(enumName)) {
                return null;
            }

            E foundConstant;
            if (InsensitiveNames.TryGetValue(enumName.ToUpperInvariant(), out foundConstant)) {
                return foundConstant;
            }
            return null;
        }

        /// <summary>
        /// Casts the provided int value into its equivalentType E Constant.
        /// </summary>
        /// <param name="value">The int value.</param>
        /// <returns>The Type Econstant or null if there is no equivalent.</returns>
        public static E? CastOrNull(int value) {
            E foundConstant;
            if (Values.TryGetValue(value, out foundConstant)) {
                return foundConstant;
            }
            return null;
        }


        /// <summary>
        /// Gets a random Enum constant selected from all values of type E
        /// </summary>
        /// <param name="excludeDefault">if set to <c>true</c> [exclude default].</param>
        /// <returns></returns>
        public static E GetRandom(bool excludeDefault = false) {
            E[] values = GetValues().ToArray();
            values = excludeDefault ? values.Except(default(E)).ToArray() : values;
            return GetRandomFrom(values);
        }

        /// <summary>
        /// Gets a random Enum constant selected from all values of type E except 
        /// those provided.
        /// </summary>
        /// <param name="exclusions">The exclusions.</param>
        /// <returns></returns>
        public static E GetRandomExcept(params E[] exclusions) {
            E[] values = GetValues().Except(exclusions).ToArray();
            return GetRandomFrom(values);
        }

        /// <summary>
        /// Gets a random Enum constant of Type Efrom the array of Type E enums provided.
        /// </summary>
        /// <param name="values">The enum values to select from.</param>
        /// <returns></returns>
        public static E GetRandomFrom(E[] values) {
            return values[_rng.Next(values.Length)];
        }
    }
}

