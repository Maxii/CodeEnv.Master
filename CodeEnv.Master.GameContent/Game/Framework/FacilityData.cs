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

        public FacilityHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        public override bool IsHQ {  // HACK temp override to add Assertion protection
            get { return base.IsHQ; }
            set {
                D.Assert(value && HullCategory == FacilityHullCategory.CentralHub);
                base.IsHQ = value;
            }
        }

        public override Index3D SectorIndex {
            get { return References.SectorGrid.GetSectorIndex(Position); } // Settlement Facilities get relocated
        }

        protected new FacilityHullEquipment HullEquipment { get { return base.HullEquipment as FacilityHullEquipment; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facilityTransform">The facility transform.</param>
        /// <param name="facilityRigidbody">The facility rigidbody.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="topography">The topography.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        public FacilityData(Transform facilityTransform, Rigidbody facilityRigidbody, FacilityHullEquipment hullEquipment, Topography topography, Player owner,
    IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<ShieldGenerator> shieldGenerators)
            : base(facilityTransform, hullEquipment, owner, activeCMs, sensors, passiveCMs, shieldGenerators) {
            facilityRigidbody.mass = Mass;
            Topography = topography;
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

