// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityData.cs
// All the data associated with a particular Facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Facility.
    /// </summary>
    public class FacilityData : AElementData {

        public FacilityCategory Category { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public FacilityData(FacilityStat stat)
            : base(stat.Name, stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

