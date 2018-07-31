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
    using Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with an FleetCmdItem.
    /// </summary>
    public class FleetCmdData : AUnitCmdData {

        #region FTL

        private bool _isFtlCapable;
        /// <summary>
        /// Indicates whether ALL the fleet's ships have FTL engines. If <c>false</c> the fleet is not capable of traveling at FTL speeds.
        /// <remarks>Returning <c>true</c> says nothing about the operational state of the engines.</remarks>
        /// <remarks>Subscribable.</remarks>
        /// </summary>
        public bool IsFtlCapable {
            get { return _isFtlCapable; }
            private set { SetProperty<bool>(ref _isFtlCapable, value, "IsFtlCapable"); }
        }

        private bool _isFtlOperational;
        /// <summary>
        /// Indicates whether ALL the fleet's ships have FTL engines that are operational, aka activated, undamaged and not damped 
        /// by an FTL damping field. If <c>false</c> the fleet is not currently capable of traveling at FTL speeds.
        /// <remarks>Subscribable.</remarks>
        /// </summary>
        public bool IsFtlOperational {
            get { return _isFtlOperational; }
            private set { SetProperty<bool>(ref _isFtlOperational, value, "IsFtlOperational"); }
        }

        private bool _isFtlDamaged;
        /// <summary>
        /// Indicates whether ANY of the fleet's ship's FTL engines, if any, are damaged. If <c>true</c> the fleet is not 
        /// currently capable of traveling at FTL speeds. 
        /// <remarks>Subscribable.</remarks>
        /// </summary>
        public bool IsFtlDamaged {
            get { return _isFtlDamaged; }
            private set { SetProperty<bool>(ref _isFtlDamaged, value, "IsFtlDamaged"); }
        }

        private bool _isFtlDampedByField;
        /// <summary>
        /// Indicates whether ANY of the fleet's ship's FTL engines, if any, are damped by an FTL Damping Field. 
        /// If <c>true</c> the fleet is not currently capable of traveling at FTL speeds.
        /// <remarks>Subscribable.</remarks>
        /// </summary>
        public bool IsFtlDampedByField {
            get { return _isFtlDampedByField; }
            private set { SetProperty<bool>(ref _isFtlDampedByField, value, "IsFtlDampedByField"); }
        }

        #endregion

        private INavigableDestination _target;
        public INavigableDestination Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigableDestination>(ref _target, value, "Target");
            }
        }

        private FleetCategory _category;
        public FleetCategory Category {
            get { return _category; }
            private set { SetProperty<FleetCategory>(ref _category, value, "Category"); }
        }

        public override IEnumerable<Formation> AcceptableFormations { get { return TempGameValues.AcceptableFleetFormations; } }

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
        /// <remarks>When FleetNavigator is engaged, this is set to point at the ApTarget (final destination) 
        /// which means it can be different than CurrentFlagshipFacing when the flagship needs to go around a detour.</remarks>
        /// <remarks>5.16.17 Set to default(Vector3) when the fleet navigator is disengaged. 
        /// In this case, CurrentFlagshipFacing will be used instead.</remarks>
        /// <remarks>UNCLEAR 4.30.18 I'm not sure this makes sense, manually manipulating this value with the set property.</remarks>
        /// </summary>
        public Vector3 CurrentHeading {
            get {
                if (_currentHeading == default(Vector3)) {
                    _currentHeading = CurrentFlagshipFacing;
                }
                return _currentHeading;
            }
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
        /// <remarks>This is the lowest FullSpeedValue of any ship in the fleet.</remarks>
        /// </summary>
        public float UnitFullSpeedValue {
            get { return _unitFullSpeedValue; }
            private set { SetProperty<float>(ref _unitFullSpeedValue, value, "UnitFullSpeedValue"); }
        }

        private float _unitMaxTurnRate;
        /// <summary>
        /// Gets the maximum turn rate of the fleet in degrees per hour.
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

        public new ShipData HQElementData {
            protected get { return base.HQElementData as ShipData; }
            set { base.HQElementData = value; }
        }

        public new FleetInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as FleetInfoAccessController; } }

        public new IEnumerable<ShipData> ElementsData { get { return base.ElementsData.Cast<ShipData>(); } }

        private FleetPublisher _publisher;
        public FleetPublisher Publisher {
            get { return _publisher = _publisher ?? new FleetPublisher(this); }
        }

        public new FleetCmdModuleDesign CmdModuleDesign { get { return base.CmdModuleDesign as FleetCmdModuleDesign; } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDamper">The FTL damper.</param>
        /// <param name="cmdModDesign">The cmd module design.</param>
        public FleetCmdData(IFleetCmd fleetCmd, Player owner, IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper, FleetCmdModuleDesign cmdModDesign)
            : this(fleetCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), sensors, ftlDamper, cmdModDesign) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDamper">The FTL damper.</param>
        /// <param name="cmdModDesign">The cmd module design.</param>
        public FleetCmdData(IFleetCmd fleetCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDamper ftlDamper, FleetCmdModuleDesign cmdModDesign)
            : base(fleetCmd, owner, passiveCMs, sensors, ftlDamper, cmdModDesign) {
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.None);
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new FleetInfoAccessController(this);
        }

        #endregion

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
            __CheckFtlStatus(elementData as ShipData);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public FleetCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

        public override void ReplaceCmdModuleWith(AUnitCmdModuleDesign cmdModuleDesign, IEnumerable<PassiveCountermeasure> passiveCMs,
            IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper) {
            base.ReplaceCmdModuleWith(cmdModuleDesign, passiveCMs, sensors, ftlDamper);
            // No FleetCmdModule-specific values to consider dealing with
        }

        /// <summary>
        /// Activates all the ship sensors in the fleet. Does nothing if they are already activated.
        /// <remarks>11.3.17 Handles case where ships added to a brand new, non-operational fleetCmd come from Hanger
        /// with their sensors off. When Ship's Command property was changed to this non-operational FleetCmd, the ship
        /// did not activate its sensors because there was no initialized UnifiedSRSensorRangeMonitor to receive the results.
        /// Accordingly, when this FleetCmd CommencesOperations, it initializes the Monitor, then uses this method
        /// to activate all ship sensors.</remarks>
        /// </summary>
        public void ActivateShipSensors() {
            ElementsData.ForAll(sData => sData.ActivateSRSensors());
            RecalcUnitSensorRange();
        }

        public bool TryGetSectorID(out IntVector3 sectorID) {
            return HQElementData.TryGetSectorID(out sectorID);
        }

        protected override OutputsYield RecalcUnitOutputs() {
            var unitOutputs = ElementsData.Select(ed => ed.Outputs).Sum();
            return unitOutputs;
        }

        protected override void RefreshComposition() {
            var elementCategories = ElementsData.Select(sData => sData.HullCategory);
            UnitComposition = new FleetComposition(elementCategories);
        }

        protected override void RecalcPropertiesDerivedFromCombinedElements() {
            base.RecalcPropertiesDerivedFromCombinedElements();
            RefreshFullSpeed();
            RefreshMaxTurnRate();
            AssessIsFtlCapable();
            AssessIsFtlDamaged();
            AssessIsFtlDampedByField();
            AssessIsFtlOperational();
        }

        private void RefreshFullSpeed() {
            if (ElementCount > Constants.Zero) {
                //D.Log(ShowDebugLog, "{0}.{1}.RefreshFullSpeed() called.", DebugName, GetType().Name);
                UnitFullSpeedValue = ElementsData.Min(sData => sData.FullSpeedValue);
            }
        }

        private void RefreshMaxTurnRate() {
            if (ElementCount > Constants.Zero) {
                UnitMaxTurnRate = ElementsData.Min(sData => sData.TurnRate);
            }
        }

        private void AssessIsFtlCapable() {
            bool isAllShipsFtlCapable = ElementsData.All(sData => sData.IsFtlCapable);
            IsFtlCapable = isAllShipsFtlCapable;
        }

        private void AssessIsFtlDamaged() {
            bool isAnyShipsFtlEngineDamaged = ElementsData.Any(sData => sData.IsFtlDamaged);
            IsFtlDamaged = isAnyShipsFtlEngineDamaged;
        }

        private void AssessIsFtlDampedByField() {
            bool isAnyShipsFtlEngineDampedByField = ElementsData.Any(sData => sData.IsFtlDampedByField);
            IsFtlDampedByField = isAnyShipsFtlEngineDampedByField;
        }

        private void AssessIsFtlOperational() {
            bool isAllShipsEnginesFtlCapableAndOperational = ElementsData.All(sData => sData.IsFtlOperational);
            IsFtlOperational = isAllShipsEnginesFtlCapableAndOperational;
        }

        protected override void Subscribe(AUnitElementData elementData) {
            base.Subscribe(elementData);
            IList<IDisposable> anElementsSubscriptions = _elementSubscriptionsLookup[elementData];
            ShipData shipData = elementData as ShipData;
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.FullSpeedValue, ShipFullSpeedPropChangedHandler));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, bool>(ed => ed.IsFtlDamaged, ShipIsFtlDamagedPropChangedHandler));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, bool>(ed => ed.IsFtlDampedByField, ShipIsFtlDampedByFieldPropChangedHandler));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, bool>(ed => ed.IsFtlOperational, ShipIsFtlOperationalPropChangedHandler));
        }

        public FleetCategory GenerateCmdCategory(FleetComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            //D.Log(ShowDebugLog, "{0}'s known elements count = {1}.", DebugName, elementCount);
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

        private void ShipFullSpeedPropChangedHandler() {
            RefreshFullSpeed();
        }

        private void ShipIsFtlDamagedPropChangedHandler() {
            AssessIsFtlDamaged();
        }

        private void ShipIsFtlDampedByFieldPropChangedHandler() {
            AssessIsFtlDampedByField();
        }

        private void ShipIsFtlOperationalPropChangedHandler() {
            AssessIsFtlOperational();
        }

        #endregion

        protected override void HandleUnitWeaponsRangeChanged() {
            if (UnitWeaponsRange.Max > TempGameValues.__MaxFleetWeaponsRangeDistance) {
                D.Warn("{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.", DebugName, UnitWeaponsRange.Max, TempGameValues.__MaxFleetWeaponsRangeDistance);
            }
        }

        protected override void HandleHQElementDataChanging(AUnitElementData newHQElementData) {
            base.HandleHQElementDataChanging(newHQElementData);
            //D.Log(ShowDebugLog, "{0}: new HQ {1} is being assigned a CombatStance of {2}.", DebugName, newHQElementData.Name, ShipCombatStance.Defensive.GetValueName());
            (newHQElementData as ShipData).CombatStance = ShipCombatStance.Defensive;
        }

        protected override void HandleHQElementDataChanged() {
            base.HandleHQElementDataChanged();
            D.Assert(HQElementData.CombatStance == ShipCombatStance.Defensive, HQElementData.CombatStance.GetValueName());
        }

        protected override void HandleHQElementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (HQElementData.IsOwnerChgUnderway && ElementCount > Constants.One) {
                // 5.17.17 HQElement is changing owner and I'm not going to go with it to that owner
                // so don't follow its IntelCoverage change. I'll pick up my IntelCoverage as soon as
                // I get my newly assigned HQElement. Following its change when not going with it results
                // in telling others of my very temp change which will throw errors when the chg doesn't make sense
                // - e.g. tracking its change to IntelCoverage.None with an ally.
                // 5.20.17 This combination of criteria can never occur for a BaseCmd as an element owner change
                // is only possible when it is the only element.
                // 6.20.18 Use of IsOwnerChgUnderway is needed to filter out condition where HQElement is about to depart fleet
                return;
            }
            base.HandleHQElementIntelCoverageChanged(playerWhosCoverageChgd);
        }

        protected override Topography GetTopography() {
            if (!IsOperational) {
                // if not yet operational, the Flagship does not yet know its topography
                return _gameMgr.GameKnowledge.GetSpaceTopography(Position);
            }
            return base.GetTopography();
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __CheckFtlStatus(ShipData data) {
            if (!data.IsFtlCapable) {
                D.Error("{0} is adding {1} that isn't FtlCapable?", DebugName, data.DebugName); // TEMP until I make STL-only ships
                return;
            }
            if (data.IsFtlDamaged) {
                var otherUndamagedFtlCapableShips = ElementsData.Where(sData => sData.IsFtlCapable && !sData.IsFtlDamaged);
                if (otherUndamagedFtlCapableShips.Any()) {
                    D.Warn("{0} is adding {1} with damaged FTL Engines?", DebugName, data.DebugName);
                }
            }
        }

        protected override void __ValidateUnitMaxFormationRadius() {
            D.Assert(UnitMaxFormationRadius <= TempGameValues.MaxFleetFormationRadius);
        }

        #endregion

        #region Nested Classes

        #endregion

    }
}

