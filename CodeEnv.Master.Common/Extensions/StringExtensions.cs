// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StringExtensions.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

using System.Globalization;
namespace CodeEnv.Master.Common {

    /// <summary>
    /// TODO 
    /// </summary>
    public static class StringExtensions {

        /// <summary>
        /// Inserts the ToString() verion of the arguments provided into the calling string using string.Format().
        /// Usage syntax: "The {0} jumped over the {1}.".Inject("Cat", "Moon");
        /// </summary>
        /// <param name="text">The calling string.</param>
        /// <param name="args">The arguments to be inserted into the formated string.</param>
        /// <returns></returns>
        public static string Inject(this string text, params object[] args) {
            Arguments.ValidateForContent(text);
            return string.Format(CultureInfo.CurrentCulture, text, args);
        }

    }
}

