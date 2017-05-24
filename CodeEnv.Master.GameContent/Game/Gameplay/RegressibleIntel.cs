// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RegressibleIntel.cs
// Metadata describing the degree of intelligence coverage a player has about a particular item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Metadata describing the degree of intelligence coverage a player has about a particular item.
    /// This version's CurrentCoverage value is allowed to regress to a lower value after being instantiated. 
    /// <remarks>It keeps track of the previous coverage high (DatedCoverage) along with the date it was obtained (DateStamp), 
    /// so that the player 'remembers' how much was known at a particular point in time, 
    /// even if CurrentCoverage has regressed since that high.</remarks>
    /// </summary>
    public class RegressibleIntel : AIntel {

        /// <summary>
        /// The DatedCoverage and DateStamp values spend much of their time reset to their default value.
        /// </summary>
        public bool IsDatedCoverageValid { get { return DatedCoverage != default(IntelCoverage) && DateStamp != default(GameDate); } }

        private IntelCoverage _datedCoverage;
        /// <summary>
        /// The highest level of intel coverage previously achieved on this object, now out of date.
        /// </summary>
        public IntelCoverage DatedCoverage {
            get { return _datedCoverage; }
            private set { SetProperty<IntelCoverage>(ref _datedCoverage, value, "DatedCoverage", null, DatedCoveragePropChangingHandler); }
        }

        /// <summary>
        /// The "time stamp" associated with the DatedCoverage, aka when DatedCoverage was last updated.
        /// Used to calculate the age of the dated coverage level of intel.
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public GameDate DateStamp { get; set; }

        public override IntelCoverage LowestAllowedCoverageValue { get { return _lowestRegressedCoverage; } }

        /// <summary>
        /// The lowest IntelCoverage value this Intel is allowed to regress too.
        /// </summary>
        private IntelCoverage _lowestRegressedCoverage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressibleIntel"/> class.
        /// </summary>
        /// <param name="lowestRegressedCoverage">The lowest IntelCoverage value this Intel is allowed to regress too.</param>
        public RegressibleIntel(IntelCoverage lowestRegressedCoverage) : base() {
            _lowestRegressedCoverage = lowestRegressedCoverage;
        }

        /// <summary>
        /// Copy constructor. Initializes a new instance of the <see cref="RegressibleIntel"/> class,
        /// a copy of <c>intelToCopy</c>.
        /// </summary>
        /// <param name="intelToCopy">The intel to copy.</param>
        public RegressibleIntel(RegressibleIntel intelToCopy)
            : base(intelToCopy) {
            DatedCoverage = intelToCopy.DatedCoverage;
            DateStamp = intelToCopy.DateStamp;
            _lowestRegressedCoverage = intelToCopy._lowestRegressedCoverage;
        }

        public override bool IsCoverageChangeAllowed(IntelCoverage newCoverage) {
            if (CurrentCoverage < _lowestRegressedCoverage) {
                // CurrentCoverage has not yet been set at or above _lowestRegressedCoverage so any value is acceptable.
                // Once CurrentCoverage becomes >= _lowestRegressedCoverage, it won't be able to be set back below _lowestRegressedCoverage.
                return true;
            }
            return newCoverage >= _lowestRegressedCoverage;
        }

        /// <summary>
        /// Processes the change to a new level of coverage BEFORE the new level of coverage
        /// is applied.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        protected override void PreProcessChange(IntelCoverage newCoverage) {
            if (newCoverage < CurrentCoverage) {
                // we have less data than before so record the level we had and stamp the date
                DatedCoverage = CurrentCoverage;
                DateStamp = GameTime.Instance.CurrentDate;
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

        #region Event and Property Change Handlers

        private void DatedCoveragePropChangingHandler(IntelCoverage newDatedCoverage) {
            //D.Log("Intel.DatedCoverage changing from {0} to {1}.", DatedCoverage, newDatedCoverage);
        }

        #endregion


    }
}

