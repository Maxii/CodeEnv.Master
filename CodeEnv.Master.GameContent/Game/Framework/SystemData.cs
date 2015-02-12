// --------------------------------------------------------------------------------------------------------------------
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
    public class SystemData : AItemData, IDisposable {

        public Index3D SectorIndex { get; private set; }

        /// <summary>
        ///  The orbit slot within this system that any current or future settlement can occupy. 
        /// </summary>
        public CelestialOrbitSlot SettlementOrbitSlot { get; set; }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private OpeYield _resources;
        public OpeYield Resources {
            get { return _resources; }
            private set { SetProperty<OpeYield>(ref _resources, value, "Resources"); }
        }

        private XYield _specialResources;
        public XYield SpecialResources {
            get { return _specialResources; }
            private set { SetProperty<XYield>(ref _specialResources, value, "SpecialResources"); }
        }

        private SettlementCmdData _settlementData;
        public SettlementCmdData SettlementData {
            get { return _settlementData; }
            set { SetProperty<SettlementCmdData>(ref _settlementData, value, "SettlementData", OnSettlementDataChanged); }
        }

        private StarData _starData;
        public StarData StarData {
            get { return _starData; }
            set { SetProperty<StarData>(ref _starData, value, "StarData", OnStarDataChanged, OnStarDataChanging); }
        }

        private IList<PlanetoidData> _allPlanetoidData = new List<PlanetoidData>();

        private IDictionary<PlanetoidData, IList<IDisposable>> _planetoidSubscribers;
        private IList<IDisposable> _starSubscribers;
        private IList<IDisposable> _settlementSubscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="systemTransform">The system transform.</param>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="sectorIndex">Index of the sector.</param>
        /// <param name="topography">The topography.</param>
        public SystemData(Transform systemTransform, string systemName, Index3D sectorIndex, Topography topography)
            : this(systemTransform, systemName, sectorIndex, topography, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class.
        /// </summary>
        /// <param name="systemTransform">The system transform.</param>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="sectorIndex">Index of the sector.</param>
        /// <param name="topography">The topography.</param>
        /// <param name="owner">The owner.</param>
        public SystemData(Transform systemTransform, string systemName, Index3D sectorIndex, Topography topography, Player owner)
            : base(systemTransform, systemName, owner) {
            SectorIndex = sectorIndex;
            base.Topography = topography;
            Subscribe();
        }

        private void Subscribe() {
            _planetoidSubscribers = new Dictionary<PlanetoidData, IList<IDisposable>>();
            _starSubscribers = new List<IDisposable>();
        }

        public void AddPlanetoid(PlanetoidData data) {
            _allPlanetoidData.Add(data);
            SubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
        }

        public bool RemovePlanetoid(PlanetoidData data) {
            bool isRemoved = _allPlanetoidData.Remove(data);
            if (!isRemoved) {
                D.Warn("Attempting to remove {0}.{1} that is not present.", data.FullName, typeof(PlanetoidData));
                return false;
            }
            UnsubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
            return true;
        }

        private void OnSystemMemberCapacityChanged() {
            UpdateCapacity();
        }

        private void OnSystemMemberResourceValueChanged() {
            UpdateResources();
        }

        private void OnSystemMemberSpecialResourceValueChanged() {
            UpdateSpecialResources();
        }

        private void OnSettlementDataChanged() {
            // Existing settlements will always be destroyed (data = null) before changing to a new settlement
            if (SettlementData != null) {
                SubscribeToSettlementDataValueChanges();
                OnSettlementOwnerChanged();
            }
            else {
                Owner = TempGameValues.NoPlayer;
                UnsubscribeToSettlementDataValueChanges();
            }
            RecalcAllProperties();
        }

        private void OnStarDataChanging(StarData newData) {
            // Existing stars will simply be swapped for another if they change so unsubscribe from the previous first
            if (StarData != null) {
                UnsubscribeToStarDataValueChanges();
            }
        }

        private void OnStarDataChanged() {
            SubscribeToStarDataValueChanges();
            RecalcAllProperties();
        }

        private void OnSettlementOwnerChanged() {
            Owner = SettlementData.Owner;
        }

        protected override void OnOwnerChanged() {
            base.OnOwnerChanged();
            PropogateOwnerChange();
        }

        private void PropogateOwnerChange() {
            _allPlanetoidData.ForAll(pd => pd.Owner = Owner);
            StarData.Owner = Owner;
        }

        private void RecalcAllProperties() {
            UpdateCapacity();
            UpdateResources();
            UpdateSpecialResources();
        }

        private void UpdateCapacity() {
            Capacity = _allPlanetoidData.Sum(pData => pData.Capacity) + StarData.Capacity;
        }

        private void UpdateResources() {
            var defaultValueIfEmpty = default(OpeYield);
            var resources = _allPlanetoidData.Select(pd => pd.Resources);
            OpeYield totalResourcesFromPlanets = resources.Aggregate(defaultValueIfEmpty, (accumulator, ope) => accumulator + ope);
            Resources = totalResourcesFromPlanets + StarData.Resources;
        }

        private void UpdateSpecialResources() {
            var defaultValueIfEmpty = default(XYield);
            var resources = _allPlanetoidData.Select(pd => pd.SpecialResources);
            XYield totalResourcesFromPlanets = resources.Aggregate(defaultValueIfEmpty, (accumulator, ope) => accumulator + ope);
            SpecialResources = totalResourcesFromPlanets + StarData.SpecialResources;
        }

        private void SubscribeToPlanetoidDataValueChanges(PlanetoidData data) {
            if (!_planetoidSubscribers.ContainsKey(data)) {
                _planetoidSubscribers.Add(data, new List<IDisposable>());
            }
            var planetSubscriber = _planetoidSubscribers[data];
            planetSubscriber.Add(data.SubscribeToPropertyChanged<PlanetoidData, int>(pd => pd.Capacity, OnSystemMemberCapacityChanged));
            planetSubscriber.Add(data.SubscribeToPropertyChanged<PlanetoidData, OpeYield>(pd => pd.Resources, OnSystemMemberResourceValueChanged));
            planetSubscriber.Add(data.SubscribeToPropertyChanged<PlanetoidData, XYield>(pd => pd.SpecialResources, OnSystemMemberSpecialResourceValueChanged));
        }

        private void SubscribeToStarDataValueChanges() {
            _starSubscribers.Add(StarData.SubscribeToPropertyChanged<StarData, int>(sd => sd.Capacity, OnSystemMemberCapacityChanged));
            _starSubscribers.Add(StarData.SubscribeToPropertyChanged<StarData, OpeYield>(sd => sd.Resources, OnSystemMemberResourceValueChanged));
            _starSubscribers.Add(StarData.SubscribeToPropertyChanged<StarData, XYield>(sd => sd.SpecialResources, OnSystemMemberSpecialResourceValueChanged));
        }

        private void SubscribeToSettlementDataValueChanges() {
            if (_settlementSubscribers == null) {
                _settlementSubscribers = new List<IDisposable>();
            }
            _settlementSubscribers.Add(SettlementData.SubscribeToPropertyChanged<SettlementCmdData, Player>(sd => sd.Owner, OnSettlementOwnerChanged));
        }

        private void UnsubscribeToPlanetoidDataValueChanges(PlanetoidData data) {
            _planetoidSubscribers[data].ForAll<IDisposable>(d => d.Dispose());
            _planetoidSubscribers.Remove(data);
        }

        private void UnsubscribeToStarDataValueChanges() {
            _starSubscribers.ForAll(d => d.Dispose());
            _starSubscribers.Clear();
        }

        private void UnsubscribeToSettlementDataValueChanges() {
            if (_settlementSubscribers != null) {
                _settlementSubscribers.ForAll(d => d.Dispose());
                _settlementSubscribers.Clear();
            }
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<PlanetoidData> subscriberKeys = new List<PlanetoidData>(_planetoidSubscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (PlanetoidData data in subscriberKeys) {
                UnsubscribeToPlanetoidDataValueChanges(data);
            }
            _planetoidSubscribers.Clear();

            _starSubscribers.ForAll(ss => ss.Dispose());
            _starSubscribers.Clear();

            UnsubscribeToSettlementDataValueChanges();
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

