// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemData.cs
// All the data associated with a particular system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular system.
    /// </summary>
    public class SystemData : AItemData, IDisposable {

        public event Action onCompositionChanged;
        private void OnCompositionChanged() {
            var temp = onCompositionChanged;
            if (temp != null) {
                temp();
            }
        }

        private Vector3 _settlementOrbitSlot;
        /// <summary>
        ///  The orbital start position (in local space) of any current or
        /// future settlement. The transform holding the SettlementCreator (whose
        /// children are SettlementCmd and the Settlement's Facilities) has
        /// its localPosition assigned this value. It is then attached to an orbit
        /// object which is parented to the System.
        /// </summary>
        public Vector3 SettlementOrbitSlot {
            get { return _settlementOrbitSlot; }
            set {
                _settlementOrbitSlot = value;
                D.Log("System's SettlementOrbitSlot set to {0}.", _settlementOrbitSlot);
            }
        }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set {
                SetProperty<int>(ref _capacity, value, "Capacity");
            }
        }

        private OpeYield _resources;
        public OpeYield Resources {
            get { return _resources; }
            private set {
                SetProperty<OpeYield>(ref _resources, value, "Resources");
            }
        }

        private XYield _specialResources;
        public XYield SpecialResources {
            get { return _specialResources; }
            private set {
                SetProperty<XYield>(ref _specialResources, value, "SpecialResources");
            }
        }

        private SettlementCmdData _settlementData;
        public SettlementCmdData SettlementData {
            get { return _settlementData; }
            set {
                SetProperty<SettlementCmdData>(ref _settlementData, value, "SettlementData", OnSettlementDataChanged);
            }
        }

        public SystemComposition Composition { get; private set; }

        private IDictionary<PlanetoidData, IList<IDisposable>> _planetSubscribers;
        private IList<IDisposable> _starSubscribers;
        private IList<IDisposable> _settlementSubscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData"/> class.
        /// </summary>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="composition">The composition.</param>
        public SystemData(string systemName, SystemComposition composition)
            : base(systemName) {
            Composition = composition;
            Subscribe();
            UpdateProperties();
        }

        private void Subscribe() {
            _planetSubscribers = new Dictionary<PlanetoidData, IList<IDisposable>>();
            foreach (var planetData in Composition.GetPlanetData()) {
                _planetSubscribers.Add(planetData, new List<IDisposable>());
                IList<IDisposable> planetSubscriptions = _planetSubscribers[planetData];
                planetSubscriptions.Add(planetData.SubscribeToPropertyChanged<PlanetoidData, int>(pd => pd.Capacity, OnCapacityChangedInComposition));
                planetSubscriptions.Add(planetData.SubscribeToPropertyChanged<PlanetoidData, OpeYield>(pd => pd.Resources, OnResourcesChangedInComposition));
                planetSubscriptions.Add(planetData.SubscribeToPropertyChanged<PlanetoidData, XYield>(pd => pd.SpecialResources, OnSpecialResourcesChangedInComposition));
            }

            StarData starData = Composition.StarData;
            _starSubscribers = new List<IDisposable>();
            _starSubscribers.Add(starData.SubscribeToPropertyChanged<StarData, int>(sd => sd.Capacity, OnCapacityChangedInComposition));
            _starSubscribers.Add(starData.SubscribeToPropertyChanged<StarData, OpeYield>(sd => sd.Resources, OnResourcesChangedInComposition));
            _starSubscribers.Add(starData.SubscribeToPropertyChanged<StarData, XYield>(sd => sd.SpecialResources, OnSpecialResourcesChangedInComposition));

            _settlementSubscribers = new List<IDisposable>();
        }

        private void SubscribeToSettlementDataValuesChanged() {
            _settlementSubscribers.Add(SettlementData.SubscribeToPropertyChanged<SettlementCmdData, IPlayer>(sd => sd.Owner, OnSettlementOwnerChanged));
        }

        public bool RemovePlanet(PlanetoidData data) {
            if (Composition.RemovePlanet(data)) {
                Unsubscribe(data);
                UpdateProperties();
                OnCompositionChanged();
                return true;
            }
            D.Warn("Attempting to remove {0} {1} that is not present.", typeof(PlanetoidData), data.ParentName);
            return false;
        }

        private void OnCapacityChangedInComposition() {
            UpdateCapacity();
        }

        private void OnResourcesChangedInComposition() {
            UpdateResources();
        }

        private void OnSpecialResourcesChangedInComposition() {
            UpdateSpecialResources();
        }

        private void OnSettlementDataChanged() {
            if (SettlementData != null) {
                SubscribeToSettlementDataValuesChanged();
                OnSettlementOwnerChanged();
            }
            else {
                Owner = null;
                UnsubscribeSettlement();
            }
        }

        private void OnSettlementOwnerChanged() {
            Owner = SettlementData.Owner;
        }

        protected override void OnOwnerChanged() {
            base.OnOwnerChanged();
            PropogateOwnerChange();
        }

        private void PropogateOwnerChange() {
            Composition.GetPlanetData().ForAll(pd => pd.Owner = Owner);
            Composition.StarData.Owner = Owner;
        }

        /// <summary>
        /// Recalculates any properties that are dependant upon the 
        /// composition of the system
        /// </summary>
        private void UpdateProperties() {
            UpdateCapacity();
            UpdateResources();
            UpdateSpecialResources();
        }

        private void UpdateCapacity() {
            Capacity = Composition.GetPlanetData().Sum<PlanetoidData>(data => data.Capacity) + Composition.StarData.Capacity;
        }

        private void UpdateResources() {
            OpeYield totalResource = new OpeYield();
            var resources = Composition.GetPlanetData().Select(pd => pd.Resources);
            if (!resources.IsNullOrEmpty()) {   // Aggregate throws up if source enumerable is empty
                var totalResourceFromPlanets = resources.Aggregate<OpeYield>(
                    (accumulator, ope) => new OpeYield() {
                        Organics = accumulator.Organics + ope.Organics,
                        Particulates = accumulator.Particulates + ope.Particulates,
                        Energy = accumulator.Energy + ope.Energy
                    });
                totalResource.Add(totalResourceFromPlanets);
            }
            totalResource.Add(Composition.StarData.Resources);
            Resources = totalResource;
        }

        private void UpdateSpecialResources() {
            XYield totalSpecial = new XYield();
            var specialResources = Composition.GetPlanetData().Select(pd => pd.SpecialResources);
            if (!specialResources.IsNullOrEmpty()) {
                var totalSpecialFromPlanets = specialResources.Aggregate<XYield>(
                    (accumulator, x) => new XYield() {   // Aggregate throws up if source enumerable is empty
                        Special_1 = accumulator.Special_1 + x.Special_1,
                        Special_2 = accumulator.Special_2 + x.Special_2,
                        Special_3 = accumulator.Special_3 + x.Special_3
                    });
                totalSpecial.Add(totalSpecialFromPlanets);
            }
            totalSpecial.Add(Composition.StarData.SpecialResources);
            SpecialResources = totalSpecial;
        }

        private void Unsubscribe(PlanetoidData planetData) {
            _planetSubscribers[planetData].ForAll<IDisposable>(d => d.Dispose());
            _planetSubscribers.Remove(planetData);
        }

        private void UnsubscribeSettlement() {
            _settlementSubscribers.ForAll(d => d.Dispose());
            _settlementSubscribers.Clear();
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<PlanetoidData> subscriberKeys = new List<PlanetoidData>(_planetSubscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (PlanetoidData data in subscriberKeys) {
                Unsubscribe(data);
            }
            _planetSubscribers.Clear();

            _starSubscribers.ForAll(ss => ss.Dispose());
            _starSubscribers.Clear();

            UnsubscribeSettlement();
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

