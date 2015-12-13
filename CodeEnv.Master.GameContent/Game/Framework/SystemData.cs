﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemData.cs
// Class for Data associated with a SystemItem.
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
    /// Class for Data associated with a SystemItem.
    /// </summary>
    public class SystemData : ADiscernibleItemData, IDisposable {   //AItemData, IDisposable {

        /// <summary>
        ///  The orbit slot within this system that any current or future settlement can occupy. 
        /// </summary>
        public CelestialOrbitSlot SettlementOrbitSlot { get; set; }

        public float Radius { get { return TempGameValues.SystemRadius; } }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourceYield _resources;
        public ResourceYield Resources {
            get { return _resources; }
            private set { SetProperty<ResourceYield>(ref _resources, value, "Resources"); }
        }

        private SettlementCmdData _settlementData;
        public SettlementCmdData SettlementData {
            get { return _settlementData; }
            set { SetProperty<SettlementCmdData>(ref _settlementData, value, "SettlementData", SettlementDataPropChangedHandler); }
        }

        private StarData _starData;
        public StarData StarData {
            get { return _starData; }
            set { SetProperty<StarData>(ref _starData, value, "StarData", StarDataPropChangedHandler, StarDataPropChangingHandler); }
        }

        public Index3D SectorIndex { get; private set; }

        public sealed override Topography Topography {  // avoids CA2214
            get { return base.Topography; }
            set { base.Topography = value; }
        }

        private IList<APlanetoidData> _allPlanetoidData = new List<APlanetoidData>();

        private IDictionary<APlanetoidData, IList<IDisposable>> _planetoidSubscriptions;
        private IList<IDisposable> _starSubscriptions;
        private IList<IDisposable> _settlementSubscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="systemTransform">The system transform.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="systemName">Name of the system.</param>
        public SystemData(Transform systemTransform, CameraFocusableStat cameraStat, string systemName)
            : this(systemTransform, cameraStat, systemName, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class.
        /// </summary>
        /// <param name="systemTransform">The system transform.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="owner">The owner.</param>
        public SystemData(Transform systemTransform, CameraFocusableStat cameraStat, string systemName, Player owner)
            : base(systemTransform, systemName, owner, cameraStat) {
            SectorIndex = References.SectorGrid.GetSectorIndex(Position);
            Topography = Topography.System;
            Subscribe();
        }

        private void Subscribe() {
            _planetoidSubscriptions = new Dictionary<APlanetoidData, IList<IDisposable>>();
            _starSubscriptions = new List<IDisposable>();
        }

        private void SubscribeToPlanetoidDataValueChanges(APlanetoidData data) {
            if (!_planetoidSubscriptions.ContainsKey(data)) {
                _planetoidSubscriptions.Add(data, new List<IDisposable>());
            }
            var planetSubscriber = _planetoidSubscriptions[data];
            planetSubscriber.Add(data.SubscribeToPropertyChanged<APlanetoidData, int>(pd => pd.Capacity, PlanetoidCapacityPropChangedHandler));
            planetSubscriber.Add(data.SubscribeToPropertyChanged<APlanetoidData, ResourceYield>(pd => pd.Resources, PlanetoidResourceYieldPropChangedHandler));
        }

        private void SubscribeToStarDataValueChanges() {
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, int>(sd => sd.Capacity, StarCapacityPropChangedHandler));
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, ResourceYield>(sd => sd.Resources, StarResourceYieldPropChangedHandler));
        }

        private void SubscribeToSettlementDataValueChanges() {
            if (_settlementSubscriptions == null) {
                _settlementSubscriptions = new List<IDisposable>();
            }
            _settlementSubscriptions.Add(SettlementData.SubscribeToPropertyChanged<SettlementCmdData, Player>(sd => sd.Owner, SettlementOwnerPropChangedHandler));
        }

        public void AddPlanetoid(APlanetoidData data) {
            _allPlanetoidData.Add(data);
            SubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
        }

        public bool RemovePlanetoid(APlanetoidData data) {
            bool isRemoved = _allPlanetoidData.Remove(data);
            if (!isRemoved) {
                D.Warn("Attempting to remove {0}.{1} that is not present.", data.FullName, typeof(APlanetoidData));
                return false;
            }
            UnsubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
            return true;
        }

        #region Event and Property Change Handlers

        private void PlanetoidCapacityPropChangedHandler() {
            UpdateCapacity();
        }

        private void StarCapacityPropChangedHandler() {
            UpdateCapacity();
        }

        private void PlanetoidResourceYieldPropChangedHandler() {
            UpdateResources();
        }

        private void StarResourceYieldPropChangedHandler() {
            UpdateResources();
        }

        private void SettlementDataPropChangedHandler() {
            // Existing settlements will always be destroyed (data = null) before changing to a new settlement
            if (SettlementData != null) {
                SubscribeToSettlementDataValueChanges();
                SettlementOwnerPropChangedHandler();
            }
            else {
                Owner = TempGameValues.NoPlayer;
                UnsubscribeToSettlementDataValueChanges();
            }
            RecalcAllProperties();
        }

        private void StarDataPropChangingHandler(StarData newData) {
            // Existing stars will simply be swapped for another if they change so unsubscribe from the previous first
            if (StarData != null) {
                UnsubscribeToStarDataValueChanges();
            }
        }

        private void StarDataPropChangedHandler() {
            SubscribeToStarDataValueChanges();
            RecalcAllProperties();
        }

        private void SettlementOwnerPropChangedHandler() {
            Owner = SettlementData.Owner;
        }

        protected override void OwnerPropChangedHandler() {
            base.OwnerPropChangedHandler();
            PropogateOwnerChange();
        }

        #endregion

        private void PropogateOwnerChange() {
            _allPlanetoidData.ForAll(pd => pd.Owner = Owner);
            StarData.Owner = Owner;
        }

        private void RecalcAllProperties() {
            UpdateCapacity();
            UpdateResources();
        }

        private void UpdateCapacity() {
            Capacity = _allPlanetoidData.Sum(pData => pData.Capacity) + StarData.Capacity;
        }

        private void UpdateResources() {
            var defaultValueIfEmpty = default(ResourceYield);
            var resources = _allPlanetoidData.Select(pd => pd.Resources);
            ResourceYield totalResourcesFromPlanets = resources.Aggregate(defaultValueIfEmpty, (accumulator, res) => accumulator + res);
            Resources = totalResourcesFromPlanets + StarData.Resources;
        }

        private void UnsubscribeToPlanetoidDataValueChanges(APlanetoidData data) {
            _planetoidSubscriptions[data].ForAll<IDisposable>(d => d.Dispose());
            _planetoidSubscriptions.Remove(data);
        }

        private void UnsubscribeToStarDataValueChanges() {
            _starSubscriptions.ForAll(d => d.Dispose());
            _starSubscriptions.Clear();
        }

        private void UnsubscribeToSettlementDataValueChanges() {
            if (_settlementSubscriptions != null) {
                _settlementSubscriptions.ForAll(d => d.Dispose());
                _settlementSubscriptions.Clear();
            }
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<APlanetoidData> pSubscriptionKeys = new List<APlanetoidData>(_planetoidSubscriptions.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (APlanetoidData pData in pSubscriptionKeys) {
                UnsubscribeToPlanetoidDataValueChanges(pData);
            }
            _planetoidSubscriptions.Clear();

            _starSubscriptions.ForAll(ss => ss.Dispose());
            _starSubscriptions.Clear();

            UnsubscribeToSettlementDataValueChanges();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

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

