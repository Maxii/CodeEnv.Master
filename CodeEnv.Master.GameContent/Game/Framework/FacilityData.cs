// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityData.cs
// Data associated with a FacilityItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Data associated with a FacilityItem.
    /// </summary>
    public class FacilityData : AUnitElementData {

        public FacilityHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        public new FacilityInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as FacilityInfoAccessController; } }

        public override Index3D SectorIndex { get { return _sectorIndex; } }

        protected new FacilityHullEquipment HullEquipment { get { return base.HullEquipment as FacilityHullEquipment; } }

        private Index3D _sectorIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facility">The facility.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        /// <param name="topography">The topography.</param>
        public FacilityData(IFacility facility, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, FacilityHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Topography topography)
            : base(facility, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators) {
            Topography = topography;
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new FacilityInfoAccessController(this);
        }

        protected override void FinalInitialize() {
            base.FinalInitialize();
            // Deployment has already occurred
            _sectorIndex = InitializeSectorIndex();
        }

        private Index3D InitializeSectorIndex() {
            Index3D sectorIndex = References.SectorGrid.GetSectorIndex(Position);
            D.Assert(sectorIndex != default(Index3D));
            MarkAsChanged();
            return sectorIndex;
        }

        #region Event and Property Change Handlers

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

