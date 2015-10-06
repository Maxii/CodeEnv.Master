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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with an FleetCmdItem.
    /// </summary>
    public class FleetCmdData : AUnitCmdItemData {

        private INavigableTarget _target;
        public INavigableTarget Target {
            get { return _target; }
            set { SetProperty<INavigableTarget>(ref _target, value, "Target"); }
        }

        private FleetCategory _category;
        public FleetCategory Category {
            get { return _category; }
            private set { SetProperty<FleetCategory>(ref _category, value, "Category"); }
        }

        public new ShipData HQElementData {
            get { return base.HQElementData as ShipData; }
            set { base.HQElementData = value; }
        }

        /// <summary>
        /// Readonly. The current speed of the Flagship of the Fleet in Units per hour, normalized for game speed.
        /// </summary>
        public float CurrentSpeed { get { return HQElementData.CurrentSpeed; } }

        /// <summary>
        /// Readonly. The requested speed of the Flagship in Units per hour.
        /// </summary>
        public float RequestedSpeed { get { return HQElementData.RequestedSpeed; } }

        /// <summary>
        /// Readonly. The normalized requested heading of the Flagship in worldspace coordinates.
        /// </summary>
        public Vector3 RequestedHeading { get { return HQElementData.RequestedHeading; } }

        /// <summary>
        /// Readonly. The real-time, normalized heading of the Flagship in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return HQElementData.CurrentHeading; } }

        private float _unitFullSpeed;
        /// <summary>
        /// The maximum sustainable speed of the fleet in units per hour.
        /// </summary>
        public float UnitFullSpeed {
            get { return _unitFullSpeed; }
            private set { SetProperty<float>(ref _unitFullSpeed, value, "UnitFullSpeed"); }
        }

        private float _unitFullStlSpeed;
        /// <summary>
        /// The maximum sustainable STL speed of the fleet in units per hour.
        /// </summary>
        public float UnitFullStlSpeed {
            get { return _unitFullStlSpeed; }
            private set { SetProperty<float>(ref _unitFullStlSpeed, value, "UnitFullStlSpeed"); }
        }

        private float _unitFullFtlSpeed;
        /// <summary>
        /// The maximum sustainable FTL speed of the fleet in units per hour.
        /// </summary>
        public float UnitFullFtlSpeed {
            get { return _unitFullFtlSpeed; }
            private set { SetProperty<float>(ref _unitFullFtlSpeed, value, "UnitFullFtlSpeed"); }
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

        public override Index3D SectorIndex { get { return HQElementData.SectorIndex; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData"/> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public FleetCmdData(Transform cmdTransform, FleetCmdStat stat, Player owner)
            : this(cmdTransform, stat, owner, Enumerable.Empty<PassiveCountermeasure>()) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData"/> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        public FleetCmdData(Transform cmdTransform, FleetCmdStat stat, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(cmdTransform, stat.Name, stat.MaxHitPoints, owner, passiveCMs) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            UnitFormation = stat.UnitFormation;
        }

        public override void AddElement(AUnitElementItemData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementItemData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        protected override void UpdateComposition() {
            var elementCategories = ElementsData.Cast<ShipData>().Select(sd => sd.HullCategory);
            UnitComposition = new FleetComposition(elementCategories);
        }

        protected override void RecalcPropertiesDerivedFromCombinedElements() {
            base.RecalcPropertiesDerivedFromCombinedElements();
            UpdateFullSpeed();
            UpdateMaxTurnRate();
        }

        private void UpdateFullSpeed() {
            if (ElementsData.Any()) {
                //D.Log("{0}.{1}.UpdateFullSpeed() called.", FullName, GetType().Name);
                UnitFullStlSpeed = ElementsData.Min(eData => (eData as ShipData).FullStlSpeed);
                UnitFullFtlSpeed = ElementsData.Min(eData => (eData as ShipData).FullFtlSpeed);
                UnitFullSpeed = ElementsData.Min(eData => (eData as ShipData).FullSpeed);
            }
        }

        private void UpdateMaxTurnRate() {
            if (ElementsData.Any()) {
                UnitMaxTurnRate = ElementsData.Min(data => (data as ShipData).MaxTurnRate);
            }
        }

        protected override void Subscribe(AUnitElementItemData elementData) {
            base.Subscribe(elementData);
            IList<IDisposable> anElementsSubscriptions = _subscriptions[elementData];
            ShipData shipData = elementData as ShipData;
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.FullStlSpeed, OnShipFullSpeedChanged));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.FullFtlSpeed, OnShipFullSpeedChanged));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, bool>(ed => ed.IsFtlAvailableForUse, OnShipFtlAvailableForUseChanged));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.MaxTurnRate, OnShipElementMaxTurnRateChanged));
        }

        private void OnShipFullSpeedChanged() {
            UpdateFullSpeed();
        }

        private void OnShipFtlAvailableForUseChanged() {
            UpdateFullSpeed();
        }

        private void OnShipElementMaxTurnRateChanged() {
            UpdateMaxTurnRate();
        }

        public FleetCategory GenerateCmdCategory(FleetComposition unitComposition) {
            int elementCount = UnitComposition.GetTotalElementsCount();
            D.Log("{0}'s known elements count = {1}.", FullName, elementCount);
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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

