// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Intel.cs
// Metadata describing the intelligence data known about a particular object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Metadata describing the intelligence data known about a particular object.
    /// This version keeps track of the previous high of knowledge obtained on the object (DatedCoverage)
    /// along with the date it was obtained (DateStamp), so that we remember how much was known at a
    /// particular point in history, even if our CurrentCoverage is no longer that high.
    /// </summary>
    public class Intel : AIntel {

        /// <summary>
        /// The DatedCoverage and DateStamp values spend much of their time reset to their default value.
        /// </summary>
        public bool IsDatedCoverageValid { get { return DatedCoverage != default(IntelCoverage) && DateStamp != default(GameDate); } }

        private IntelCoverage _datedCoverage;
        /// <summary>
        /// The highest level of data coverage previously achieved on this object, now out of date.
        /// </summary>
        public virtual IntelCoverage DatedCoverage {
            get { return _datedCoverage; }
            private set { SetProperty<IntelCoverage>(ref _datedCoverage, value, "DatedCoverage", null, OnDatedCoverageChanging); }
        }

        /// <summary>
        /// The "time stamp" associated with the DatedCoverage, aka when DatedCoverage was last updated.
        /// Used to calculate the age of the dated coverage level of intel.
        /// </summary>
        public virtual GameDate DateStamp { get; private set; }

        public Intel() : base() { }

        public Intel(IntelCoverage coverage) : base(coverage) { }

        /// <summary>
        /// Processes the change to a new level of coverage BEFORE the new level of coverage
        /// is applied.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        protected override void PreProcessChange(IntelCoverage newCoverage) {
            if (newCoverage < CurrentCoverage) {
                // we have less data than before so record the level we had and stamp the date
                DatedCoverage = CurrentCoverage;
                if (DateStamp != GameTime.Instance.CurrentDate) {    // avoids PropertyChangeTracking equals warning
                    DateStamp = GameTime.Instance.CurrentDate;
                }
            }
            if (newCoverage > CurrentCoverage && newCoverage >= DatedCoverage) {
                // we have more data than before and it is the same or more than our previous record, so erase the previous record
                DatedCoverage = default(IntelCoverage);
                DateStamp = default(GameDate);
            }
            // if newCoverage is same as currentCoverage than they are both None and this is a new instance - nothing to change
            // if we have more data than before, but we still haven't reached our record, then nothing to change

            // CurrentCoverage is set to newCoverage after PreProcessChange(newCoverage) finishes
        }

        private void OnDatedCoverageChanging(IntelCoverage newDatedCoverage) {
            D.Log("Intel.DatedCoverage changing from {0} to {1}.", DatedCoverage, newDatedCoverage);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

