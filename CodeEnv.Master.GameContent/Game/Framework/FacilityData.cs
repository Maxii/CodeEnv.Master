// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityData.cs
// Class for Data associated with a FacilityItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a FacilityItem.
    /// </summary>
    public class FacilityData : AUnitElementItemData {

        public FacilityCategory Category { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facilityTransform">The facility transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="topography">The topography.</param>
        public FacilityData(Transform facilityTransform, FacilityStat stat, Topography topography)
            : base(facilityTransform, stat.Name, stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            base.Topography = topography;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

