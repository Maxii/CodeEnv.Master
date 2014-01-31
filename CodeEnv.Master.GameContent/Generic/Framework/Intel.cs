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
    /// </summary>
    public class Intel : APropertyChangeTracking, IIntel {

        private IntelCoverage _datedCoverage;
        /// <summary>
        /// The highest level of data coverage previously achieved on this object, now out of date.
        /// </summary>
        public virtual IntelCoverage DatedCoverage {
            get { return _datedCoverage; }
            private set { SetProperty<IntelCoverage>(ref _datedCoverage, value, "DatedCoverage", null, OnDatedCoverageChanging); }
        }

        private IntelCoverage _currentCoverage;
        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public virtual IntelCoverage CurrentCoverage {
            get { return _currentCoverage; }
            set { SetProperty<IntelCoverage>(ref _currentCoverage, value, "CurrentCoverage", null, OnCurrentCoverageChanging); }
        }

        public virtual IGameDate DateStamp { get; private set; }

        public Intel() : this(IntelCoverage.None) { }

        public Intel(IntelCoverage currentCoverage) {
            ProcessChange(currentCoverage);
            _currentCoverage = currentCoverage;
        }

        protected virtual void ProcessChange(IntelCoverage newCurrentCoverage) {
            if (newCurrentCoverage < CurrentCoverage) {
                // we have less data than before so record the level we had and stamp the date
                DatedCoverage = CurrentCoverage;
                GameDate currentDate = new GameDate(GameDate.PresetDateSelector.Current);
                if (!currentDate.Equals(DateStamp)) {
                    DateStamp = currentDate;    // avoids PropertyChangeTracking equals warning
                }
            }
            if (newCurrentCoverage > CurrentCoverage && newCurrentCoverage >= DatedCoverage) {
                // we have more data than before and it is the same or more than our previous record, so erase the previous record
                DatedCoverage = IntelCoverage.None;
                DateStamp = null;
            }
            // if newCoverage is same as currentCoverage than they are both None and this is a new instance - nothing to change
            // if we have more data than before, but we still haven't reached our record, then nothing to change
        }

        private void OnCurrentCoverageChanging(IntelCoverage newCurrentCoverage) {
            ProcessChange(newCurrentCoverage);
        }

        private void OnDatedCoverageChanging(IntelCoverage newDatedCoverage) {
            D.Log("Intel.DatedCoverage changing from {0} to {1}.", DatedCoverage, newDatedCoverage);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

