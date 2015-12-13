// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FixedIntel.cs
// Intel with a CurrentCoverage value that doesn't change once instantiated.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Intel with a CurrentCoverage value that doesn't change once instantiated.
    /// </summary>
    public class FixedIntel : AIntel {

        public override IntelCoverage CurrentCoverage {
            get { return base.CurrentCoverage; }
            set { throw new NotSupportedException("{0} does not support setting CurrentCoverage. Use FixedIntel(fixedCoverage) instead.".Inject(GetType().Name)); }
        }

        public FixedIntel(IntelCoverage fixedCoverage) : base(fixedCoverage) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

