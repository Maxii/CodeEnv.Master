// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NonRegressibleIntel.cs
// Metadata describing the degree of intelligence coverage a player has about a particular item. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// Metadata describing the degree of intelligence coverage a player has about a particular item. 
    /// This version has a CurrentCoverage value that is not allowed to regress to a lower value than it
    /// has already attained. It can only improve once instantiated. 
    /// </summary>
    public class NonRegressibleIntel : AIntel {

        /// <summary>
        /// Initializes a new instance of the <see cref="NonRegressibleIntel"/> class.
        /// </summary>
        public NonRegressibleIntel() : base() { }

        /// <summary>
        /// Copy constructor. Initializes a new instance of the <see cref="NonRegressibleIntel"/> class,
        /// a copy of <c>intelToCopy</c>.
        /// </summary>
        /// <param name="intelToCopy">The intel to copy.</param>
        public NonRegressibleIntel(NonRegressibleIntel intelToCopy)
            : base(intelToCopy) { }

        /// <summary>
        /// Returns <c>true</c> if an assignment to newCoverage is allowed (including the case where newCoverage == CurrentCoverage),
        /// <c>false</c> if the assignment is not allowed due to the inability of IntelCoverage to regress to newCoverage.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns>
        ///   <c>true</c> if [is coverage change allowed] [the specified new coverage]; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsCoverageChangeAllowed(IntelCoverage newCoverage) {
            return newCoverage >= CurrentCoverage;
        }

    }
}

