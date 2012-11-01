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

namespace CodeEnv.Master.Common.Extensions {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class StringExtensions {

        /// <summary>
        /// Inserts the ToString() verion of the arguments provided into the calling string using string.Format().
        /// Usage syntax: "The {0} jumped over the {1}.".Inject("Cat", "Moon");
        /// </summary>
        /// <param name="s">The calling string.</param>
        /// <param name="args">The arguments to be inserted into the formated string.</param>
        /// <returns></returns>
        public static string Inject(this string s, params object[] args) {
            return string.Format(s, args);
        }

    }
}

