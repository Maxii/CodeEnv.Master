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

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using System.Globalization;

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

    }
}

