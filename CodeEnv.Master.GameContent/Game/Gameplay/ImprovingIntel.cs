// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ImprovingIntel.cs
// Intel with a CurrentCoverage value that can only improve once instantiated.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// Intel with a CurrentCoverage value that can only improve once instantiated. It never regresses to a lower value.
    /// </summary>
    public class ImprovingIntel : AIntel {

        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public override IntelCoverage CurrentCoverage {
            get { return base.CurrentCoverage; }
            set {
                D.Assert(IsCoverageChangeAllowed(value));
                base.CurrentCoverage = value;
            }
        }

        public ImprovingIntel(IntelCoverage currentCoverage) : base(currentCoverage) { }

        /// <summary>
        /// Copy constructor. Initializes a new instance of the <see cref="ImprovingIntel"/> class,
        /// a copy of <c>intelToCopy</c>.
        /// </summary>
        /// <param name="intelToCopy">The intel to copy.</param>
        public ImprovingIntel(ImprovingIntel intelToCopy)
            : this(intelToCopy.CurrentCoverage) { }

        public override bool IsCoverageChangeAllowed(IntelCoverage newCoverage) {
            return newCoverage > CurrentCoverage;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

