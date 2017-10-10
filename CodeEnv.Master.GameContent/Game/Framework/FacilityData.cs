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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        private float _food;
        public float Food {
            get { return _food; }
            set { SetProperty<float>(ref _food, value, "Food"); }
        }

        private float _production;
        public float Production {
            get { return _production; }
            set { SetProperty<float>(ref _production, value, "Production"); }
        }

        protected new FacilityHullEquipment HullEquipment { get { return base.HullEquipment as FacilityHullEquipment; } }

        private IntVector3 _sectorID;
        public override IntVector3 SectorID {
            get {
                if (_sectorID == default(IntVector3)) {
                    _sectorID = InitializeSectorID();
                }
                return _sectorID;
            }
        }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="facility">The facility.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">SR sensors.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        /// <param name="hqPriority">The HQ priority.</param>
        /// <param name="topography">The topography.</param>
        /// <param name="constructionCost">The construction cost.</param>
        /// <param name="designName">Name of the design.</param>
        public FacilityData(IFacility facility, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, FacilityHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<ElementSensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Priority hqPriority, Topography topography, float constructionCost, string designName)
            : base(facility, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority, constructionCost, designName) {
            Topography = topography;
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;
            Production = hullEquipment.Production;
            Food = hullEquipment.Food;
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.Basic);
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new FacilityInfoAccessController(this);
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            // Deployment has already occurred
        }

        private IntVector3 InitializeSectorID() {
            IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDThatContains(Position);
            D.AssertNotDefault(sectorID);
            MarkAsChanged();
            return sectorID;
        }

        #endregion


    }
}

