﻿// --------------------------------------------------------------------------------------------------------------------
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
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigableTarget>(ref _target, value, "Target");
            }
        }

        private FleetCategory _category;
        public FleetCategory Category {
            get { return _category; }
            private set { SetProperty<FleetCategory>(ref _category, value, "Category"); }
        }

        /// <summary>
        /// Readonly. The real-time speed of the Flagship in Units per hour, normalized for game speed.
        /// </summary>
        public float CurrentSpeedValue { get { return HQElementData.CurrentSpeedValue; } }

        /// <summary>
        /// Readonly. The requested speed of the Flagship in Units per hour, normalized for game speed.
        /// </summary>
        public float RequestedSpeedValue { get { return HQElementData.RequestedSpeedValue; } }

        // Note: RequestedSpeed not currently present as it would be confusing to show ShipSpeeds from HQElement

        /// <summary>
        /// Readonly. The requested heading of the Flagship in worldspace coordinates.
        /// </summary>
        public Vector3 RequestedHeading { get { return HQElementData.RequestedHeading; } }

        /// <summary>
        /// Readonly. The real-time heading of the Flagship in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return HQElementData.CurrentHeading; } }

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

        public override Index3D SectorIndex { get { return HQElementData.SectorIndex; } }

        public new ShipData HQElementData {
            protected get { return base.HQElementData as ShipData; }
            set { base.HQElementData = value; }
        }

        public new CameraFleetCmdStat CameraStat { get { return base.CameraStat as CameraFleetCmdStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="cmdStat">The stat.</param>
        public FleetCmdData(IFleetCmdItem fleetCmd, Player owner, CameraFleetCmdStat cameraStat, UnitCmdStat cmdStat)
            : this(fleetCmd, owner, cameraStat, Enumerable.Empty<PassiveCountermeasure>(), cmdStat) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData" /> class.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="cmdStat">The stat.</param>
        public FleetCmdData(IFleetCmdItem fleetCmd, Player owner, CameraFleetCmdStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, UnitCmdStat cmdStat)
            : base(fleetCmd, owner, cameraStat, passiveCMs, cmdStat) {
        }

        public override void AddElement(AUnitElementItemData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementItemData elementData) {
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
                //D.Log("{0}.{1}.RefreshFullSpeed() called.", FullName, GetType().Name);
                UnitFullSpeedValue = _elementsData.Min(eData => (eData as ShipData).FullSpeedValue);
            }
        }

        private void RefreshMaxTurnRate() {
            if (_elementsData.Any()) {
                UnitMaxTurnRate = _elementsData.Min(data => (data as ShipData).MaxTurnRate);
            }
        }

        protected override void Subscribe(AUnitElementItemData elementData) {
            base.Subscribe(elementData);
            IList<IDisposable> anElementsSubscriptions = _subscriptions[elementData];
            ShipData shipData = elementData as ShipData;
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.FullSpeedValue, ShipFullSpeedPropChangedHandler));
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

        #region Event and Property Change Handlers

        private void ShipFullSpeedPropChangedHandler() {
            RefreshFullSpeed();
        }

        #endregion

        protected override Topography GetTopography() {
            if (!IsOperational) {
                // if not yet operational, the Flagship does not yet know its topography
                return References.SectorGrid.GetSpaceTopography(Position);
            }
            return base.GetTopography();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

