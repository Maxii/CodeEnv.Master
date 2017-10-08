// --------------------------------------------------------------------------------------------------------------------
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

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            private set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public new IEnumerable<FacilityData> ElementsData { get { return base.ElementsData.Cast<FacilityData>(); } }

        private float _unitProduction;
        public float UnitProduction {
            get { return _unitProduction; }
            private set { SetProperty<float>(ref _unitProduction, value, "UnitProduction"); }
        }

        private ConstructionInfo _currentConstruction = TempGameValues.NoConstruction;
        public ConstructionInfo CurrentConstruction {
            get { return _currentConstruction; }
            set {
                D.AssertNotNull(value, DebugName); // CurrentConstruction should never be changed to null
                _currentConstruction = value;
                D.Log("{0}.CurrentConstruction changed to {1}.", DebugName, _currentConstruction.DebugName);
            }
        }

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

        public AUnitBaseCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDampener ftlDampener, ACmdModuleStat cmdStat, string designName)
            : base(cmd, owner, passiveCMs, sensors, ftlDampener, cmdStat, designName) {
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.Basic);
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

        protected override void Subscribe(AUnitElementData elementData) {
            base.Subscribe(elementData);
            var anElementsSubscriptions = _elementSubscriptionsLookup[elementData];
            FacilityData facilityData = elementData as FacilityData;
            anElementsSubscriptions.Add(facilityData.SubscribeToPropertyChanged<FacilityData, float>(ed => ed.Production, ElementProductionPropChangedHandler));
        }

        protected override void RecalcPropertiesDerivedFromCombinedElements() {
            base.RecalcPropertiesDerivedFromCombinedElements();
            RecalcUnitProduction();
        }

        #region Event and Property Change Handlers

        private void ElementProductionPropChangedHandler() {
            RecalcUnitProduction();
        }

        #endregion

        protected override void HandleUnitWeaponsRangeChanged() {
            if (UnitWeaponsRange.Max > TempGameValues.__MaxBaseWeaponsRangeDistance) {
                D.Warn("{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.", DebugName, UnitWeaponsRange.Max, TempGameValues.__MaxBaseWeaponsRangeDistance);
            }
        }

        private void RecalcUnitProduction() {
            // FIXME 10.1.17 Keeps production above zero as its used as a denominator in division. Require CentralHub facility?
            UnitProduction = Mathf.Clamp(_elementsData.Cast<FacilityData>().Sum(ed => ed.Production), Constants.OneF, Mathf.Infinity);
        }

        protected override void RefreshComposition() {
            var elementCategories = _elementsData.Cast<FacilityData>().Select(fd => fd.HullCategory);
            UnitComposition = new BaseComposition(elementCategories);
        }

        #region Debug


        #endregion

    }
}

