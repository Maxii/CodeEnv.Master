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

        private IntelCoverage _currentCoverage;
        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public virtual IntelCoverage CurrentCoverage {
            get { return _currentCoverage; }
            set { SetProperty<IntelCoverage>(ref _currentCoverage, value, "CurrentCoverage", null, CurrentCoveragePropChangingHandler); }
        }

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

    }
}

