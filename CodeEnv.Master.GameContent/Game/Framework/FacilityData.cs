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

        public override bool IsHQ {  // HACK temp override to add Assertion protection
            get { return base.IsHQ; }
            set {
                D.Assert(value && Category == FacilityCategory.CentralHub);
                base.IsHQ = value;
            }
        }

        public override Index3D SectorIndex {
            get { return References.SectorGrid.GetSectorIndex(Position); } // Settlement Facilities get relocated
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facilityTransform">The facility transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="topography">The topography.</param>
        /// <param name="owner">The owner.</param>
        public FacilityData(Transform facilityTransform, FacilityStat stat, Topography topography, Player owner)
            : base(facilityTransform, stat.Name, stat.Mass, stat.MaxHitPoints, owner) {
            Category = stat.Category;
            Topography = topography;
            Science = stat.Science;
            Culture = stat.Culture;
            Income = stat.Income;
            Expense = stat.Expense;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

