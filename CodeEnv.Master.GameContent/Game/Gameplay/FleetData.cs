﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetData.cs
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
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular fleet.
    /// </summary>
    public class FleetData : Data, IDisposable {

        /// <summary>
        /// Readonly. The current speed of the LeadShip of the Fleet
        /// in Units per day, normalized for game speed.
        /// </summary>
        public float CurrentSpeed { get { return LeadShipData.CurrentSpeed; } }

        /// <summary>
        /// Readonly. The requested speed of the LeadShip in Units per day.
        /// </summary>
        public float RequestedSpeed { get { return LeadShipData.RequestedSpeed; } }

        /// <summary>
        /// Readonly. The normalized requested heading of the LeadShip in worldspace coordinates.
        /// </summary>
        public Vector3 RequestedHeading { get { return LeadShipData.RequestedHeading; } }

        /// <summary>
        /// Readonly. The real-time, normalized heading of the LeadShip in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return LeadShipData.CurrentHeading; } }

        private ShipData _leadShipData;
        public ShipData LeadShipData {
            get {
                return _leadShipData;
            }
            set {
                SetProperty<ShipData>(ref _leadShipData, value, "LeadShipData", OnLeadShipChanged);
            }
        }

        private void OnLeadShipChanged() {
            if (!_shipsData.Contains(_leadShipData)) {
                D.Error("LeadShip {0} assigned not present in Fleet {1}.", _leadShipData.OptionalParentName, OptionalParentName);
            }
        }

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

        private float _health;
        public float Health {
            get {
                return _health;
            }
            private set {
                SetProperty<float>(ref _health, value, "Health");
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

        private float _maxHitPoints;
        public float MaxHitPoints {
            get {
                return _maxHitPoints;
            }
            private set {
                SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints");
            }
        }

        private CombatStrength _strength;
        public CombatStrength Strength {
            get {
                return _strength;
            }
            private set {
                SetProperty<CombatStrength>(ref _strength, value, "Strength");
            }
        }

        private FleetComposition _composition;
        public FleetComposition Composition {
            get { return _composition; }
            private set { SetProperty<FleetComposition>(ref _composition, value, "Composition"); }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        private IList<ShipData> _shipsData;
        private IDictionary<ShipData, IList<IDisposable>> _subscribers;

        public FleetData(Transform fleetCommand, string fleetName)
            : base(fleetCommand, fleetName) {
            UnityUtility.ValidateInterfacePresence<IFleetCommand>(fleetCommand);
            FixNames(fleetName);
            InitializeCollections();
        }

        private void FixNames(string fleetName) {
            _transform.name = "FleetCommand";
            _transform.parent.name = fleetName;
        }

        private void InitializeCollections() {
            _shipsData = new List<ShipData>();
            Composition = new FleetComposition();
        }

        protected override void OnNameChanged() {
            FixNames(Name);
        }

        private void OnOwnerChanged() {
            // TODO What feature in the game allows the change in ownership of an entire fleet at once? Takeover?
            // UNDONE change all fleet ship owners?
            D.Log("Fleet Owner has changed to {0}.", Owner.LeaderName);
        }

        public void AddShip(ShipData shipData) {
            if (!_shipsData.Contains(shipData)) {
                ValidateOwner(shipData.Owner);
                SetFleetAssignment(shipData);
                _shipsData.Add(shipData);

                ChangeComposition(shipData, toAdd: true);
                Subscribe(shipData);
                UpdatePropertiesDerivedFromTotalFleet();
                return;
            }
            D.Warn("Attempting to add {0} {1} that is already present.", typeof(ShipData), shipData.OptionalParentName);
        }

        private void ValidateOwner(IPlayer owner) {
            if (Owner == null) {
                // first setting of owner by first ship added. Not broadcast
                _owner = owner;
            }
            D.Assert(Owner == owner, "Owners {0} and {1} are different.".Inject(Owner.LeaderName, owner.LeaderName));
        }

        private void SetFleetAssignment(ShipData shipData) {
            shipData.OptionalParentName = Name;
        }

        /// <summary>
        /// Adds or removes shipData from the Composition.
        /// </summary>
        /// <param name="shipData">The ship data.</param>
        /// <param name="toAdd">if set to <c>true</c> add the ship, otherwise remove it.</param>
        private void ChangeComposition(ShipData shipData, bool toAdd) {
            bool isChanged = false;
            if (toAdd) {
                isChanged = Composition.AddShip(shipData);
            }
            else {
                isChanged = Composition.RemoveShip(shipData);
            }

            if (isChanged) {
                Composition = new FleetComposition(Composition);
            }
        }

        public bool RemoveShip(ShipData shipData) {
            if (_shipsData.Contains(shipData)) {
                bool isRemoved = _shipsData.Remove(shipData);

                ChangeComposition(shipData, toAdd: false);
                Unsubscribe(shipData);
                UpdatePropertiesDerivedFromTotalFleet();
                return isRemoved;
            }
            D.Warn("Attempting to remove {0} {1} that is not present.", typeof(ShipData), shipData.OptionalParentName);
            return false;
        }

        /// <summary>
        /// Recalculates any properties that are dependant upon the total
        /// ship population of the fleet.
        /// </summary>
        private void UpdatePropertiesDerivedFromTotalFleet() {
            UpdateHealth();
            UpdateStrength();
            UpdateMaxHitPoints();
            UpdateMaxSpeed();
            UpdateMaxTurnRate();
        }

        private void UpdateHealth() {
            Health = _shipsData.Sum<ShipData>(data => data.Health);
        }

        private void UpdateStrength() {
            CombatStrength sum = new CombatStrength();
            foreach (var ship in _shipsData) {
                sum.AddToTotal(ship.Strength);
            }
            Strength = sum;
        }

        private void UpdateMaxHitPoints() {
            MaxHitPoints = _shipsData.Sum<ShipData>(data => data.MaxHitPoints);
        }

        private void UpdateMaxSpeed() {
            // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
            // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
            if (_shipsData.IsNullOrEmpty()) {
                MaxSpeed = Constants.ZeroF;
                return;
            }
            MaxSpeed = _shipsData.MinBy(data => data.MaxSpeed).MaxSpeed;
        }

        private void UpdateMaxTurnRate() {
            // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
            // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
            if (_shipsData.IsNullOrEmpty()) {
                MaxTurnRate = Constants.ZeroF;
                return;
            }
            MaxTurnRate = _shipsData.MinBy(data => data.MaxTurnRate).MaxTurnRate;
        }

        #region ShipData PropertyChanged Subscription and Methods
        private void Subscribe(ShipData shipData) {
            if (_subscribers == null) {
                _subscribers = new Dictionary<ShipData, IList<IDisposable>>();
            }
            _subscribers.Add(shipData, new List<IDisposable>());
            IList<IDisposable> shipSubscriptions = _subscribers[shipData];
            shipSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(sd => sd.Health, OnShipHealthChanged));
            shipSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(sd => sd.MaxHitPoints, OnShipMaxHitPointsChanged));
            shipSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(sd => sd.MaxSpeed, OnShipMaxSpeedChanged));
            shipSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, float>(sd => sd.MaxTurnRate, OnShipMaxTurnRateChanged));
            shipSubscriptions.Add(shipData.SubscribeToPropertyChanged<ShipData, CombatStrength>(sd => sd.Strength, OnShipStrengthChanged));
        }

        private void OnShipHealthChanged() {
            UpdateHealth();
        }

        private void OnShipStrengthChanged() {
            UpdateStrength();
        }

        private void OnShipMaxHitPointsChanged() {
            UpdateMaxHitPoints();
        }

        private void OnShipMaxSpeedChanged() {
            UpdateMaxSpeed();
        }

        private void OnShipMaxTurnRateChanged() {
            UpdateMaxTurnRate();
        }

        private void Unsubscribe(ShipData shipData) {
            _subscribers[shipData].ForAll<IDisposable>(d => d.Dispose());
            _subscribers.Remove(shipData);
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<ShipData> subscriberKeys = new List<ShipData>(_subscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (ShipData shipData in subscriberKeys) {
                Unsubscribe(shipData);
            }
            _subscribers.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

