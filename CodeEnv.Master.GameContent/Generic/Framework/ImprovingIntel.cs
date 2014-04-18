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

    /// <summary>
    /// Intel with a CurrentCoverage value that can only improve once instantiated. It never regresses to a lower value.
    /// </summary>
    public class ImprovingIntel : Intel {

        public override IntelCoverage DatedCoverage {
            get {
                D.Warn("{0} does not support DatedCoverage.", typeof(ImprovingIntel).Name);
                return base.DatedCoverage;
            }
        }

        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public override IntelCoverage CurrentCoverage {
            get { return base.CurrentCoverage; }
            set {
                if (value < CurrentCoverage) {
                    D.Warn("{0} does not support regressing coverage from {1} to {2}.", typeof(ImprovingIntel).Name, value.GetName(), CurrentCoverage.GetName());
                    return;
                }
                base.CurrentCoverage = value;
            }
        }

        public override IGameDate DateStamp {
            get {
                D.Warn("{0} does not support DateStamp.", typeof(ImprovingIntel).Name);
                return base.DateStamp;
            }
        }

        public ImprovingIntel() : this(IntelCoverage.None) { }

        public ImprovingIntel(IntelCoverage currentCoverage) : base(currentCoverage) { }

        protected override void ProcessChange(IntelCoverage newCurrentCoverage) {
            // does nothing as DatedCoverage and DateStamp aren't supported
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

