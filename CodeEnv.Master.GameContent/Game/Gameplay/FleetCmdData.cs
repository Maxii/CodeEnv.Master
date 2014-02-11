﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdData.cs
// All the data associated with a particular fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular fleet.
    /// </summary>
    public class FleetCmdData : ACommandData {

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
        /// Readonly. The current speed of the LeadShip of the Fleet
        /// in Units per day, normalized for game speed.
        /// </summary>
        public float CurrentSpeed { get { return HQElementData.CurrentSpeed; } }

        /// <summary>
        /// Readonly. The requested speed of the LeadShip in Units per day.
        /// </summary>
        public float RequestedSpeed { get { return HQElementData.RequestedSpeed; } }

        /// <summary>
        /// Readonly. The normalized requested heading of the LeadShip in worldspace coordinates.
        /// </summary>
        public Vector3 RequestedHeading { get { return HQElementData.RequestedHeading; } }

        /// <summary>
        /// Readonly. The real-time, normalized heading of the LeadShip in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return HQElementData.CurrentHeading; } }

        private float _maxSpeed;
        /// <summary>
        /// Gets the maximum speed of the fleet in units per day.
        /// </summary>
        public float MaxSpeed {
            get {
                return _maxSpeed;
            }
            private set {
                SetProperty<float>(ref _maxSpeed, value, "MaxSpeed");
            }
        }

        private float _maxTurnRate;
        /// <summary>
        /// Gets the maximum turn rate of the fleet in radians per day.
        /// </summary>
        public float MaxTurnRate {
            get {
                return _maxTurnRate;
            }
            private set {
                SetProperty<float>(ref _maxTurnRate, value, "MaxTurnRate");
            }
        }

        public FleetComposition Composition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdData"/> class.
        /// </summary>
        /// <param name="fleetName">Name of the fleet.</param>
        /// <param name="cmdMaxHitPoints">The command maximum hit points.</param>
        public FleetCmdData(string fleetName, float cmdMaxHitPoints) : base(fleetName, cmdMaxHitPoints) { }

        protected override void InitializeComposition() {
            Composition = new FleetComposition();
        }

        /// <summary>
        /// Adds or removes shipData from the Composition.
        /// </summary>
        /// <param name="elementData">The ship data.</param>
        /// <param name="toAdd">if set to <c>true</c> add the ship, otherwise remove it.</param>
        protected override void ChangeComposition(AElementData elementData, bool toAdd) {
            bool isChanged = toAdd ? Composition.Add(elementData as ShipData) : Composition.Remove(elementData as ShipData);
            if (isChanged) {
                AssessCommandCategory();
                OnCompositionChanged();
            }
        }

        protected override void UpdatePropertiesDerivedFromCombinedElements() {
            base.UpdatePropertiesDerivedFromCombinedElements();
            UpdateMaxSpeed();
            UpdateMaxTurnRate();
        }

        private void UpdateMaxSpeed() {
            // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
            // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
            if (ElementsData.IsNullOrEmpty()) {
                MaxSpeed = Constants.ZeroF;
                return;
            }
            MaxSpeed = (ElementsData.MinBy(data => (data as ShipData).MaxSpeed) as ShipData).MaxSpeed;
        }

        private void UpdateMaxTurnRate() {
            // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
            // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
            if (ElementsData.IsNullOrEmpty()) {
                MaxTurnRate = Constants.ZeroF;
                return;
            }
            MaxTurnRate = (ElementsData.MinBy(data => (data as ShipData).MaxTurnRate) as ShipData).MaxTurnRate;
        }

        protected override void Subscribe(AElementData elementData) {
            base.Subscribe(elementData);
            IList<IDisposable> anElementsSubscriptions = _subscribers[elementData];
            ShipData shipData = elementData as ShipData;
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.MaxSpeed, OnShipElementMaxSpeedChanged));
            anElementsSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(ed => ed.MaxTurnRate, OnShipElementMaxTurnRateChanged));
        }

        private void OnShipElementMaxSpeedChanged() {
            UpdateMaxSpeed();
        }

        private void OnShipElementMaxTurnRateChanged() {
            UpdateMaxTurnRate();
        }

        private void AssessCommandCategory() {
            if (Composition.ElementCount >= 22) {
                Category = FleetCategory.Armada;
                return;
            }
            if (Composition.ElementCount >= 15) {
                Category = FleetCategory.BattleGroup;
                return;
            }
            if (Composition.ElementCount >= 9) {
                Category = FleetCategory.TaskForce;
                return;
            }
            if (Composition.ElementCount >= 4) {
                Category = FleetCategory.Squadron;
                return;
            }
            if (Composition.ElementCount >= 1) {
                Category = FleetCategory.Flotilla;
                return;
            }
            // element count of 0 = dead, so don't generate a change to be handled
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

