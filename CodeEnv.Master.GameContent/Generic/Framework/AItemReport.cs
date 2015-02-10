// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemReport.cs
// Abstract class for Reports that support Items with no PlayerIntel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract class for Reports that support Items with no PlayerIntel.
    /// </summary>
    public abstract class AItemReport : AReport {

        public AItemReport(Player player)
            : base(player) {
        }

        /// <summary>
        /// Assigns the values to this report. Some values may be acquired from
        /// data, others from other Constructor parameters provided by the derived
        /// class. The derived class is responsible for calling this method once all
        /// Constructor parameters needed by it have been recorded.
        /// </summary>
        /// <param name="data">The data.</param>
        protected abstract void AssignValues(AItemData data);
        // can't call from this Constructor as derived class parameters not yet recorded
    }
}

