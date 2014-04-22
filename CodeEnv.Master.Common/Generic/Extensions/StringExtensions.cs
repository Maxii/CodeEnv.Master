// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StringExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// COMMENT 
    /// </summary>
    public static class StringExtensions {

        /// <summary>
        /// Inserts the ToString() version of the arguments provided into the calling string using string.Format().
        /// Usage syntax: "The {0} jumped over the {1}.".Inject("Cat", "Moon");
        /// </summary>
        /// <param name="sourceText">The calling string.</param>
        /// <param name="itemsToInject">The items to inject into the calling string. If the item is null, it is replaced by string.Empty</param>
        /// <returns></returns>
        /// <exception cref="System.FormatException"></exception>
        public static string Inject(this string sourceText, params object[] itemsToInject) {
            //  'this' sourceText can never be null without the CLR throwing a Null reference exception
            Arguments.ValidateForContent(sourceText);
            // IMPROVE see Effective C#, Item 45 Minimize Boxing and Unboxing
            return string.Format(CultureInfo.CurrentCulture, sourceText, itemsToInject);
        }

        /// <summary>
        /// Adds the specified delimiter to each string item in the sequence except the last one.
        /// </summary>
        /// <param name="sequence">The string sequence.</param>
        /// <param name="delimiter">The delimiter. Default is ",".</param>
        /// <returns></returns>
        public static IEnumerable<string> AddDelimiter(this IEnumerable<string> sequence, string delimiter = Constants.Comma) {
            Arguments.ValidateNotNull(sequence);
            IList<string> delimitedList = new List<string>();
            foreach (string item in sequence) {
                delimitedList.Add(item + Constants.NewLine);
            }
            int lastItemIndex = delimitedList.Count - 1;
            string lastItem = delimitedList[lastItemIndex];
            string lastItemWithoutDelineationEnding = lastItem.Replace(Constants.NewLine, string.Empty);
            delimitedList[lastItemIndex] = lastItemWithoutDelineationEnding;
            return delimitedList;
        }

        /// <summary>
        ///     Clears the contents of the string builder.
        /// </summary>
        /// <param name="sb">
        ///     The <see cref="StringBuilder"/> to clear.
        /// </param>
        public static void Clear(this StringBuilder sb) {
            sb.Length = 0;
            sb.Capacity = 16;
        }

        /// <summary>
        /// Removes the specified string (if present) from the source string and returns the result. 
        /// Only the first instance of <c>stringToRemove</c> is removed. Case sensitive.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="stringToRemove">The string to remove.</param>
        /// <returns></returns>
        public static string Remove(this string source, string stringToRemove) {
            int index = source.IndexOf(stringToRemove);
            string result = index < 0 ? source : source.Remove(index, stringToRemove.Length);
            if (source.Equals(result)) {
                D.Warn("Attempted to remove {0} from {1} but did not find it.", stringToRemove, source);
            }
            else {
                D.Log("Removed {0} from {1} resulting in {2}.", stringToRemove, source, result);
            }
            return result;
        }

    }
}

