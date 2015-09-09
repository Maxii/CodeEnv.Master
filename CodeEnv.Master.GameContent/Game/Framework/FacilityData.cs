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

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a FacilityItem.
    /// </summary>
    public class FacilityData : AUnitElementItemData {

        public FacilityCategory Category { get { return HullStat.Category; } }

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

        protected new FacilityHullStat HullStat { get { return base.HullStat as FacilityHullStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facilityTransform">The facility transform.</param>
        /// <param name="hullStat">The hull stat.</param>
        /// <param name="topography">The topography.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="weapons">The weapons.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        public FacilityData(Transform facilityTransform, FacilityHullStat hullStat, Topography topography, Player owner, IEnumerable<AWeapon> weapons,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<ShieldGenerator> shieldGenerators)
            : base(facilityTransform, hullStat, owner, weapons, activeCMs, sensors, passiveCMs, shieldGenerators) {
            Topography = topography;
            Science = hullStat.Science;
            Culture = hullStat.Culture;
            Income = hullStat.Income;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

