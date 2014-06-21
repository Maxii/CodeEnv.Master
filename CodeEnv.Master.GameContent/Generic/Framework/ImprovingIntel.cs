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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System;

    /// <summary>
    /// Intel with a CurrentCoverage value that can only improve once instantiated. It never regresses to a lower value.
    /// </summary>
    public class ImprovingIntel : Intel {

        public override bool HasDatedCoverage { get { return false; } }

        public override IntelCoverage DatedCoverage {
            get { throw new InvalidOperationException("{0} does not support DateStamp.".Inject(GetType().Name)); }
        }

        public override GameDate DateStamp {
            get { throw new InvalidOperationException("{0} does not support DateStamp.".Inject(GetType().Name)); }
        }

        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public override IntelCoverage CurrentCoverage {
            get { return base.CurrentCoverage; }
            set {
                if (value < CurrentCoverage) {
                    throw new InvalidOperationException("{0} does not support regressing coverage from {1} to {2}.".Inject(GetType().Name, value.GetName(), CurrentCoverage.GetName()));
                }
                base.CurrentCoverage = value;
            }
        }

        public ImprovingIntel() : this(IntelCoverage.None) { }

        public ImprovingIntel(IntelCoverage currentCoverage) : base(currentCoverage) { }

        protected override void PreProcessChange(IntelCoverage newCurrentCoverage) {
            // does nothing as DatedCoverage and DateStamp aren't supported
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

