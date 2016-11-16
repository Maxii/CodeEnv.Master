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
    public class SystemData : AIntelItemData, IDisposable {

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
            set {
                D.AssertNull(_starData);
                SetProperty<StarData>(ref _starData, value, "StarData", StarDataPropSetHandler);
            }
        }

        public IntVector3 SectorID { get; private set; }

        public new SystemInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SystemInfoAccessController; } }

        public IEnumerable<PlanetoidData> AllPlanetoidData { get { return _allPlanetoidData; } }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        private IList<PlanetoidData> _allPlanetoidData = new List<PlanetoidData>();
        private IDictionary<PlanetoidData, IList<IDisposable>> _planetoidSubscriptions;
        private IList<IDisposable> _starSubscriptions;
        private IList<IDisposable> _settlementSubscriptions;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="system">The system.</param>
        public SystemData(ISystem system)
            : this(system, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="owner">The owner.</param>
        public SystemData(ISystem system, Player owner)
            : base(system, owner) {
            SectorID = References.SectorGrid.GetSectorIdThatContains(Position);
            Topography = Topography.System;
            Subscribe();
        }

        private void Subscribe() {
            _planetoidSubscriptions = new Dictionary<PlanetoidData, IList<IDisposable>>();
            _starSubscriptions = new List<IDisposable>();
        }

        private void SubscribeToPlanetoidDataValueChanges(PlanetoidData pData) {
            if (!_planetoidSubscriptions.ContainsKey(pData)) {
                _planetoidSubscriptions.Add(pData, new List<IDisposable>());
            }
            var planetSubscriber = _planetoidSubscriptions[pData];
            planetSubscriber.Add(pData.SubscribeToPropertyChanged<PlanetoidData, int>(pd => pd.Capacity, PlanetoidCapacityPropChangedHandler));
            planetSubscriber.Add(pData.SubscribeToPropertyChanged<PlanetoidData, ResourceYield>(pd => pd.Resources, PlanetoidResourceYieldPropChangedHandler));
            pData.intelCoverageChanged += PlanetoidIntelCoverageChangedEventHandler;
        }

        private void SubscribeToStarDataValueChanges() {
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, int>(sd => sd.Capacity, StarCapacityPropChangedHandler));
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, ResourceYield>(sd => sd.Resources, StarResourceYieldPropChangedHandler));
            StarData.intelCoverageChanged += StarIntelCoverageChangedEventHandler;
        }

        private void SubscribeToSettlementDataValueChanges() {
            if (_settlementSubscriptions == null) {
                _settlementSubscriptions = new List<IDisposable>();
            }
            _settlementSubscriptions.Add(SettlementData.SubscribeToPropertyChanged<SettlementCmdData, Player>(sd => sd.Owner, SettlementOwnerPropChangedHandler));
            SettlementData.intelCoverageChanged += SettlementIntelCoverageChangedEventHandler;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new SystemInfoAccessController(this);
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            RecalcAllProperties();
            AssessIntelCoverage();
        }

        #endregion

        public void AddPlanetoidData(PlanetoidData data) {
            _allPlanetoidData.Add(data);
            SubscribeToPlanetoidDataValueChanges(data);
            if (IsOperational) {
                RecalcAllProperties();
                AssessIntelCoverage();
            }
        }

        public void RemovePlanetoidData(PlanetoidData data) {
            D.Assert(IsOperational);
            bool isRemoved = _allPlanetoidData.Remove(data);
            D.Assert(isRemoved, data.FullName);

            UnsubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
            AssessIntelCoverage();
        }

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        private void AssessIntelCoverage() {
            foreach (Player player in _gameMgr.AllPlayers) {
                AssessIntelCoverageFor(player);
            }
        }

        private void AssessIntelCoverageFor(Player player) {
            var starCoverage = StarData.GetIntelCoverage(player);
            var planetoidCoverages = _allPlanetoidData.Select(pd => pd.GetIntelCoverage(player));
            List<IntelCoverage> allMemberCoverages = new List<IntelCoverage>();
            allMemberCoverages.Add(starCoverage);
            allMemberCoverages.AddRange(planetoidCoverages);

            if (SettlementData != null) {
                IntelCoverage settlementCoverage = SettlementData.GetIntelCoverage(player);
                allMemberCoverages.Add(settlementCoverage);
            }

            IntelCoverage currentCoverage = GetIntelCoverage(player);

            IntelCoverage lowestCommonCoverage = GetLowestCommonCoverage(allMemberCoverages);
            var isCoverageSet = SetIntelCoverage(player, lowestCommonCoverage);
            if (isCoverageSet) {
                //D.Log(ShowDebugLog, "{0} has assessed its IntelCoverage for {1} and changed it from {2} to the lowest common member value {3}.",
                //    FullName, player.Name, currentCoverage.GetValueName(), lowestCommonCoverage.GetValueName());
            }
            else {
                //D.Log(ShowDebugLog, "{0} has assessed its IntelCoverage for {1} and declined to change it from {2} to the lowest common member value {3}.",
                //    FullName, player.Name, currentCoverage.GetValueName(), lowestCommonCoverage.GetValueName());
            }
        }

        private IntelCoverage GetLowestCommonCoverage(IEnumerable<IntelCoverage> intelCoverages) {
            IntelCoverage lowestCommonCoverage = IntelCoverage.Comprehensive;
            foreach (var coverage in intelCoverages) {
                if (coverage < lowestCommonCoverage) {
                    lowestCommonCoverage = coverage;
                }
            }
            return lowestCommonCoverage;
        }

        #region Event and Property Change Handlers

        private void StarDataPropSetHandler() {
            D.Assert(!IsOperational);
            SubscribeToStarDataValueChanges();
        }

        private void SettlementDataPropChangedHandler() {
            HandleSettlementDataChanged();
        }

        private void HandleSettlementDataChanged() {
            // Existing settlements will always be destroyed (data = null) before changing to a new settlement
            if (SettlementData != null) {
                SubscribeToSettlementDataValueChanges();
                //D.Log("{0} is about to have its owner changed to {1}'s Owner {2}.", FullName, SettlementData.FullName, SettlementData.Owner);
                Owner = SettlementData.Owner;
            }
            else {
                Owner = TempGameValues.NoPlayer;
                UnsubscribeToSettlementDataValueChanges();
            }
            if (IsOperational) {
                RecalcAllProperties();
                AssessIntelCoverage();
            }
        }

        protected override void HandleOwnerChanged() {
            base.HandleOwnerChanged();
            PropagateOwnerChange();
        }

        private void PlanetoidIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandlePlanetoidIntelCoverageChanged(e.Player);
        }

        private void HandlePlanetoidIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        private void SettlementIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleSettlementIntelCoverageChanged(e.Player);
        }

        private void HandleSettlementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        private void StarIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleStarIntelCoverageChanged(e.Player);
        }

        private void HandleStarIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

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

        private void SettlementOwnerPropChangedHandler() {
            Owner = SettlementData.Owner;
        }

        #endregion

        private void PropagateOwnerChange() {
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

        #region Cleanup

        private void UnsubscribeToPlanetoidDataValueChanges(PlanetoidData pData) {
            _planetoidSubscriptions[pData].ForAll<IDisposable>(d => d.Dispose());
            _planetoidSubscriptions.Remove(pData);
            pData.intelCoverageChanged -= PlanetoidIntelCoverageChangedEventHandler;
        }

        private void UnsubscribeToStarDataValueChanges() {
            _starSubscriptions.ForAll(d => d.Dispose());
            _starSubscriptions.Clear();
            StarData.intelCoverageChanged -= StarIntelCoverageChangedEventHandler;
        }

        private void UnsubscribeToSettlementDataValueChanges() {
            if (SettlementData != null) {
                D.AssertNotNull(_settlementSubscriptions);
                _settlementSubscriptions.ForAll(d => d.Dispose());
                _settlementSubscriptions.Clear();
                SettlementData.intelCoverageChanged -= SettlementIntelCoverageChangedEventHandler;
            }
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<PlanetoidData> pSubscriptionKeys = new List<PlanetoidData>(_planetoidSubscriptions.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (PlanetoidData pData in pSubscriptionKeys) {
                UnsubscribeToPlanetoidDataValueChanges(pData);
            }
            _planetoidSubscriptions.Clear();

            UnsubscribeToStarDataValueChanges();
            UnsubscribeToSettlementDataValueChanges();
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}

