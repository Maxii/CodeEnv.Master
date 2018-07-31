﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdData.cs
// Abstract class for Data associated with an AUnitBaseCmdItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitBaseCmdItem.
    /// </summary>
    public abstract class AUnitBaseCmdData : AUnitCmdData {

        public new FacilityData HQElementData {
            protected get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private int _population;
        public int Population {
            get { return _population; }
            set { SetProperty<int>(ref _population, value, "Population"); }
        }

        private float _approval = Constants.OneHundredPercent;
        public float Approval { // IMPROVE not currently used
            get { return _approval; }
            set { SetProperty<float>(ref _approval, value, "Approval", ApprovalPropChangedHandler); }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            private set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        private ResourcesYield _resources;
        public ResourcesYield Resources {
            get { return _resources; }
            protected set { SetProperty<ResourcesYield>(ref _resources, value, "Resources"); }
        }

        public sealed override IEnumerable<Formation> AcceptableFormations { get { return TempGameValues.AcceptableBaseFormations; } }

        public new IEnumerable<FacilityData> ElementsData { get { return base.ElementsData.Cast<FacilityData>(); } }

        private ConstructionTask _currentConstruction = TempGameValues.NoConstruction;
        public ConstructionTask CurrentConstruction {
            get { return _currentConstruction; }
            set {
                D.AssertNotNull(value, DebugName); // CurrentConstruction should never be changed to null
                if (_currentConstruction != value) {
                    D.Log("{0}.CurrentConstruction is changing from {1} to {2}.", DebugName, _currentConstruction.DebugName, value.DebugName);
                    _currentConstruction = value;
                }
            }
        }

        private IntVector3 _sectorID;
        public /*override*/ IntVector3 SectorID {
            get {
                if (_sectorID == default(IntVector3)) {
                    _sectorID = InitializeSectorID();
                }
                return _sectorID;
            }
        }

        #region Initialization 

        public AUnitBaseCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDamper ftlDamper, AUnitCmdModuleDesign cmdModDesign)
            : base(cmd, owner, passiveCMs, sensors, ftlDamper, cmdModDesign) {
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.Basic);
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            D.AssertNotEqual(Constants.Zero, Population);
            // Deployment has already occurred
        }

        private IntVector3 InitializeSectorID() {
            IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDContaining(Position);
            MarkAsChanged();
            return sectorID;
        }

        #endregion

        #region Event and Property Change Handlers

        private void ApprovalPropChangedHandler() {
            Utility.ValidateForRange(Approval, Constants.ZeroPercent, Constants.OneHundredPercent);
        }

        #endregion

        protected sealed override OutputsYield RecalcUnitOutputs() {
            var unitOutputs = ElementsData.Select(fData => fData.Outputs).Sum();
            __ValidateProductionPresenceIn(ref unitOutputs);
            return unitOutputs;
        }

        protected override void HandleUnitWeaponsRangeChanged() {
            if (UnitWeaponsRange.Max > TempGameValues.__MaxBaseWeaponsRangeDistance) {
                D.Warn("{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.", DebugName, UnitWeaponsRange.Max, TempGameValues.__MaxBaseWeaponsRangeDistance);
            }
        }

        protected override void RefreshComposition() {
            var elementCategories = ElementsData.Select(fd => fd.HullCategory);
            UnitComposition = new BaseComposition(elementCategories);
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateProductionPresenceIn(ref OutputsYield unitOutputs) {
            // 11.20.17 Zero unit production in a Base will result in Base ConstructionMgr attempting to divide by zero when 
            // recalculating expected completion dates.
            if (unitOutputs == default(OutputsYield)) {
                // 11.20.17 Occurs when Base loses last facility and is about to die (IsDead not yet set). 
                D.Log("{0} cannot have UnitOutput with zero production. Fixing.", DebugName);
                unitOutputs += OutputsYield.OneProduction;
            }
            else if (!unitOutputs.IsPresent(OutputID.Production) || unitOutputs.GetYield(OutputID.Production) == Constants.ZeroF) {
                // 11.20.17 Occurs if no facility has any production. TODO I need to make sure that doesn't happen during game setup.
                D.Warn("{0} cannot have UnitOutput with zero production. Fixing.", DebugName);
                unitOutputs += OutputsYield.OneProduction;
            }
        }

        protected sealed override void __ValidateUnitMaxFormationRadius() {
            // 7.16.18 Currently only Fleets need to validate their max formation radius
        }

        #endregion

    }
}

