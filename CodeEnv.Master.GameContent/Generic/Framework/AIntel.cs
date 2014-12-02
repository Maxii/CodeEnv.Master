﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntel.cs
// Abstract base class for the intelligence data known about a particular object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for the intelligence data known about a particular object.
    /// </summary>
    public abstract class AIntel : APropertyChangeTracking {

        protected IntelCoverage _currentCoverage;
        /// <summary>
        /// The current level of data coverage achieved on this object.
        /// </summary>
        public virtual IntelCoverage CurrentCoverage {
            get { return _currentCoverage; }
            set { SetProperty<IntelCoverage>(ref _currentCoverage, value, "CurrentCoverage", null, OnCurrentCoverageChanging); }
        }

        public AIntel() : this(IntelCoverage.None) { }

        public AIntel(IntelCoverage currentCoverage) {
            PreProcessChange(currentCoverage);
            _currentCoverage = currentCoverage;
        }

        /// <summary>
        /// Processes the change to a new level of coverage BEFORE the new level of coverage
        /// is applied.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        protected virtual void PreProcessChange(IntelCoverage newCoverage) { }

        private void OnCurrentCoverageChanging(IntelCoverage newCurrentCoverage) {
            PreProcessChange(newCurrentCoverage);
        }

    }
}

