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

    using CodeEnv.Master.Common;

    /// <summary>
    /// Intel with a CurrentCoverage value that doesn't change once instantiated.
    /// </summary>
    public class FixedIntel : Intel {

        public override IntelCoverage DatedCoverage {
            get {
                D.Warn("{0} does not support DatedCoverage.", typeof(FixedIntel).Name);
                return base.DatedCoverage;
            }
        }

        public override IntelCoverage CurrentCoverage {
            get { return base.CurrentCoverage; }
            set { D.Warn("{0} does not support setting CurrentCoverage. Use FixedIntel(fixedCoverage) instead.", typeof(FixedIntel).Name); }
        }

        public override IGameDate DateStamp {
            get {
                D.Warn("{0} does not support DateStamp.", typeof(FixedIntel).Name);
                return base.DateStamp;
            }
        }

        public FixedIntel(IntelCoverage fixedCoverage) : base(fixedCoverage) { }

        protected override void ProcessChange(IntelCoverage newCurrentCoverage) {
            // does nothing as DatedCoverage and DateStamp aren't supported
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

