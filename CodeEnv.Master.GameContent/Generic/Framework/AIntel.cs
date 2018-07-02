// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntel.cs
// Abstract base class for Metadata describing the degree of intelligence coverage a player has about a particular item. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Metadata describing the degree of intelligence coverage a player has about a particular item. 
    /// </summary>
    public abstract class AIntel : APropertyChangeTracking {

        public string DebugName { get { return GetType().Name; } }

        private IntelCoverage _currentCoverage;
        /// <summary>
        /// The current level of IntelCoverage achieved on this object.
        /// </summary>
        public virtual IntelCoverage CurrentCoverage {
            get { return _currentCoverage; }
            set {
                D.Assert(IsCoverageChangeAllowed(value));
                SetProperty<IntelCoverage>(ref _currentCoverage, value, "CurrentCoverage", null, CurrentCoveragePropChangingHandler);
            }
        }

        /// <summary>
        /// Returns the lowest allowed IntelCoverage value. For NonRegressible Intel, this value
        /// will always be CurrentCoverage. For Regressible Intel, this value will be the value
        /// specified when the Intel was created.
        /// <remarks>5.20.17 Currently only UnitElements and UnitCmds use RegressibleIntel. Ships and FleetCmds
        /// have a LowestAllowedValue of None, whereas Facilities and BaseCmds have Basic.</remarks>
        /// </summary>
        public abstract IntelCoverage LowestAllowedCoverageValue { get; } 

        public AIntel() { }

        /// <summary>
        /// Copy Constructor. Initializes a new instance of the <see cref="AIntel"/> class,
        /// a copy of <c>intelToCopy</c>.
        /// </summary>
        /// <param name="intelToCopy">The intel to copy.</param>
        public AIntel(AIntel intelToCopy) {
            _currentCoverage = intelToCopy.CurrentCoverage;
        }

        public void InitializeCoverage(IntelCoverage coverage) {
            PreProcessChange(coverage);
            _currentCoverage = coverage;
        }

        /// <summary>
        /// Returns <c>true</c> if an assignment to newCoverage is allowed (including the case where newCoverage == CurrentCoverage), 
        /// <c>false</c> if the assignment is not allowed due to the inability of IntelCoverage to regress to newCoverage.
        /// <remarks>Does not make any change to CurrentCoverage.</remarks>
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns>
        ///   <c>true</c> if [is coverage change allowed] [the specified new coverage]; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsCoverageChangeAllowed(IntelCoverage newCoverage);

        #region Event and Property Change Handlers

        private void CurrentCoveragePropChangingHandler(IntelCoverage newCoverage) {
            PreProcessChange(newCoverage);
        }

        #endregion

        /// <summary>
        /// Processes the change to a new level of coverage BEFORE the new level of coverage
        /// is applied.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        protected virtual void PreProcessChange(IntelCoverage newCoverage) { }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

