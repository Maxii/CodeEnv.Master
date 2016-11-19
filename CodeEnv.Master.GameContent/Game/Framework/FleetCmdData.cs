// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdData.cs
// Class for Data associated with an FleetCmdItem.
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
    /// Class for Data associated with an FleetCmdItem.
    /// </summary>
    public class FleetCmdData : AUnitCmdData {

        private INavigable _target;
        public INavigable Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigable>(ref _target, value, "Target");
            }
        }

        private FleetCategory _category;
        public FleetCategory Category {
            get { return _category; }
            private set { SetProperty<FleetCategory>(ref _category, value, "Category"); }
        }

        /// <summary>
        /// Read-only. The actual speed of the Flagship in Units per hour, normalized for game speed.
        /// </summary>
        public float ActualSpeedValue { get { return HQElementData.ActualSpeedValue; } }

        /// <summary>
        /// The current speed setting of the Fleet.
        /// </summary>
        private Speed _currentSpeedSetting;
        public Speed CurrentSpeedSetting {
            get { return _currentSpeedSetting; }
            set { SetProperty<Speed>(ref _currentSpeedSetting, value, "CurrentSpeedSetting"); }
        }

        private Vector3 _currentHeading;
        /// <summary>
        /// The current heading the entire fleet is pursuing. 
        /// <remarks>Can be different from CurrentFlagshipFacing when
        /// the flagship needs to go around a detour. Will be default(Vector3) when the fleet navigator is disengaged.
        /// </remarks>
        /// </summary>
        public Vector3 CurrentHeading {
            get { return _currentHeading; }
            set { SetProperty<Vector3>(ref _currentHeading, value, "CurrentHeading"); }
        }

        /// <summary>
        /// Read-only. The real-time facing of the Flagship in worldspace coordinates. Equivalent to transform.forward.
        /// <remarks>This is not always the same as CurrentHeading which is only valid when FleetNavigator is engaged.</remarks>
        /// </summary>
        public Vector3 CurrentFlagshipFacing { get { return HQElementData.CurrentHeading; } }

        private float _unitFullSpeedValue;
        /// <summary>
        /// The maximum sustainable speed of the fleet in units per hour.
        /// </summary>
        public float UnitFullSpeedValue {
            get { return _unitFullSpeedValue; }
            private set { SetProperty<float>(ref _unitFullSpeedValue, value, "UnitFullSpeedValue"); }
        }

        private float _unitMaxTurnRate;
        /// <summary>
        /// Gets the maximum turn rate of the fleet in radians per day.
        /// </summary>
        public float UnitMaxTurnRate {
            get { return _unitMaxTurnRate; }
            private set { SetProperty<float>(ref _unitMaxTurnRate, value, "UnitMaxTurnRate"); }
        }

        private FleetComposition _unitComposition;
        public FleetComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<FleetComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public override IntVector3 SectorID { get { return HQElementData.SectorID; } }

        public new ShipData HQElementData {
            protected get { return base.HQElementData as ShipData; }
            set { base.HQElementData = value; }
        }

        public new FleetInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as FleetInfoAccessController; } }

        public new IEnumerable<ShipData> ElementsData { get { return base.ElementsData.Cast<ShipData>(); } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cmdStat">The stat.</param>
        public FleetCmdData(IFleetCmd fleetCmd, Player owner, UnitCmdStat cmdStat)
            : this(fleetCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), cmdStat) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="cmdStat">The stat.</param>
        public FleetCmdData(IFleetCmd fleetCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, UnitCmdStat cmdStat)
            : base(fleetCmd, owner, passiveCMs, cmdStat) { }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new FleetInfoAccessController(this);
        }

        #endregion

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        protected override void RefreshComposition() {
            var elementCategories = _elementsData.Cast<ShipData>().Select(sd => sd.HullCategory);
            UnitComposition = new FleetComposition(elementCategories);
        }

        protected override void RecalcPropertiesDerivedFromCombinedElements() {
            base.RecalcPropertiesDerivedFromCombinedElements();
            RefreshFullSpeed();
            RefreshMaxTurnRate();
        }

        private void RefreshFullSpeed() {
            if (_elementsData.Any()) {
                //D.Log(ShowDebugLog, "{0}.{1}.RefreshFullSpeed() called.", FullName, GetType().Name);
                UnitFullSpeedValue = _elementsData.Min(eData => (eData as ShipData).FullSpeedValue);
            }
        }

        private void RefreshMaxTurnRate() {
            if (_elementsData.Any()) {
                UnitMaxTurnRate = _elementsData.Min(data => (data as ShipData).MaxTurnRate);
            }
        }

        protected override void Subscribe(AUnitElementData elementData) {
            base.Subscribe(elementData);
            IList<IDisposable> anElementsSubscriptions = _subscriptions[elementData];
            ShipData shipData = elementData as ShipData;
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.FullSpeedValue, ShipFullSpeedPropChangedHandler));
        }

        public FleetCategory GenerateCmdCategory(FleetComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            if (ShowDebugLog) {
                D.Log("{0}'s known elements count = {1}.", FullName, elementCount);
            }
            if (elementCount >= 22) {
                return FleetCategory.Armada;
            }
            if (elementCount >= 15) {
                return FleetCategory.BattleGroup;
            }
            if (elementCount >= 9) {
                return FleetCategory.TaskForce;
            }
            if (elementCount >= 4) {
                return FleetCategory.Squadron;
            }
            if (elementCount >= 1) {
                return FleetCategory.Flotilla;
            }
            return FleetCategory.None;
        }

        #region Event and Property Change Handlers

        protected override void HandleUnitWeaponsRangeChanged() {
            if (UnitWeaponsRange.Max > TempGameValues.__MaxFleetWeaponsRangeDistance) {
                D.Warn("{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.", FullName, UnitWeaponsRange.Max, TempGameValues.__MaxFleetWeaponsRangeDistance);
            }
        }

        private void ShipFullSpeedPropChangedHandler() {
            RefreshFullSpeed();
        }

        protected override void HandleHQElementDataChanging(AUnitElementData newHQElementData) {
            base.HandleHQElementDataChanging(newHQElementData);
            (newHQElementData as ShipData).CombatStance = ShipCombatStance.Defensive;
        }

        protected override void HandleHQElementDataChanged() {
            base.HandleHQElementDataChanged();
            D.Assert(HQElementData.CombatStance == ShipCombatStance.Defensive, HQElementData.CombatStance.GetValueName());
        }

        #endregion

        protected override Topography GetTopography() {
            if (!IsOperational) {
                // if not yet operational, the Flagship does not yet know its topography
                return _gameMgr.GameKnowledge.GetSpaceTopography(Position);
            }
            return base.GetTopography();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

