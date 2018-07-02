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

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data associated with a FacilityItem.
    /// </summary>
    public class FacilityData : AUnitElementData {

        public FacilityHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        public new FacilityInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as FacilityInfoAccessController; } }

        private IntVector3 _sectorID;
        public override IntVector3 SectorID {
            get {
                if (_sectorID == default(IntVector3)) {
                    _sectorID = InitializeSectorID();
                }
                return _sectorID;
            }
        }

        private FacilityPublisher _publisher;
        public FacilityPublisher Publisher {
            get { return _publisher = _publisher ?? new FacilityPublisher(this); }
        }

        public new FacilityDesign Design { get { return base.Design as FacilityDesign; } }

        private new FacilityHullEquipment HullEquipment { get { return base.HullEquipment as FacilityHullEquipment; } }

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
        /// <param name="topography">The topography.</param>
        /// <param name="design">The design.</param>
        public FacilityData(IFacility facility, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, FacilityHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<ElementSensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Topography topography, FacilityDesign design)
            : base(facility, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, design) {
            Topography = topography;
            Mass = CalculateMass();
            Outputs = MakeOutputs();
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
            ////IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDThatContains(Position);
            IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDContaining(Position);
            D.AssertNotDefault(sectorID);
            MarkAsChanged();
            return sectorID;
        }

        #endregion

        public FacilityReport GetReport(Player player) { return Publisher.GetReport(player); }

        public override void RestoreInitialConstructionValues() {
            base.RestoreInitialConstructionValues();
            Outputs = MakeOutputs();
        }

        public override void RestorePreReworkValues(PreReworkValuesStorage valuesPriorToRework) {
            base.RestorePreReworkValues(valuesPriorToRework);
            Outputs = MakeOutputs();
        }

        private OutputsYield MakeOutputs() {
            IList<OutputsYield.OutputValuePair> outputPairs = new List<OutputsYield.OutputValuePair>(7);

            float nonHullExpense = HullEquipment.Weapons.Sum(w => w.Expense) + PassiveCountermeasures.Sum(pcm => pcm.Expense)
                + ActiveCountermeasures.Sum(acm => acm.Expense) + Sensors.Sum(s => s.Expense) + ShieldGenerators.Sum(gen => gen.Expense);

            var allOutputIDs = Enums<OutputID>.GetValues(excludeDefault: true);
            foreach (var id in allOutputIDs) {
                float yield;
                if (HullEquipment.TryGetYield(id, out yield)) {
                    if (id == OutputID.Expense) {
                        yield += nonHullExpense;
                    }
                    else if (id == OutputID.NetIncome) {
                        yield -= nonHullExpense;
                    }
                    outputPairs.Add(new OutputsYield.OutputValuePair(id, yield));
                }
            }
            return new OutputsYield(outputPairs.ToArray());
        }

    }
}

