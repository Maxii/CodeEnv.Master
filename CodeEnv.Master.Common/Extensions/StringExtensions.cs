﻿// --------------------------------------------------------------------------------------------------------------------
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
            return string.Format(CultureInfo.CurrentCulture, sourceText, itemsToInject);
        }

    }
}

