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
    /// Class for Data associated with a SystemItem.
    /// </summary>
    public class SystemData : AIntelItemData, IDisposable {

        public float Radius { get { return TempGameValues.SystemRadius; } }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourcesYield _resources;
        public ResourcesYield Resources {
            get { return _resources; }
            private set { SetProperty<ResourcesYield>(ref _resources, value, "Resources"); }
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

        public IntVector3 SectorID { get { return _sectorData.SectorID; } }

        public new SystemInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SystemInfoAccessController; } }

        public IEnumerable<PlanetoidData> AllPlanetoidData { get { return _allPlanetoidData; } }

        private SystemPublisher _publisher;
        public SystemPublisher Publisher {
            get { return _publisher = _publisher ?? new SystemPublisher(this); }
        }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        private IList<PlanetoidData> _allPlanetoidData = new List<PlanetoidData>();
        private IDictionary<PlanetoidData, IList<IDisposable>> _planetoidSubscriptions;
        private IList<IDisposable> _starSubscriptions;
        private IList<IDisposable> _settlementSubscriptions;
        private SectorData _sectorData;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="sectorData">The sector data.</param>
        public SystemData(ISystem system, SectorData sectorData)
            : this(system, sectorData, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData" /> class.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="sectorData">The sector data.</param>
        /// <param name="owner">The owner.</param>
        public SystemData(ISystem system, SectorData sectorData, Player owner)
            : base(system, owner) {
            _sectorData = sectorData;
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
            planetSubscriber.Add(pData.SubscribeToPropertyChanged<PlanetoidData, ResourcesYield>(pd => pd.Resources, PlanetoidResourceYieldPropChangedHandler));
            pData.intelCoverageChanged += PlanetoidIntelCoverageChangedEventHandler;
        }

        private void SubscribeToStarDataValueChanges() {
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, int>(sd => sd.Capacity, StarCapacityPropChangedHandler));
            _starSubscriptions.Add(StarData.SubscribeToPropertyChanged<StarData, ResourcesYield>(sd => sd.Resources, StarResourceYieldPropChangedHandler));
            StarData.intelCoverageChanged += StarIntelCoverageChangedEventHandler;
        }

        private void SubscribeToSettlementDataValueChanges() {
            _settlementSubscriptions = _settlementSubscriptions ?? new List<IDisposable>();
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
            if (IsOperational) {    // 6.18.18 Currently never occurs in runtime. Here in case in future rogue planetoids can 'join' systems
                RecalcAllProperties();
                AssessIntelCoverage();
            }
        }

        public void RemovePlanetoidData(PlanetoidData data) {
            D.Assert(IsOperational);
            bool isRemoved = _allPlanetoidData.Remove(data);
            D.Assert(isRemoved, data.DebugName);

            UnsubscribeToPlanetoidDataValueChanges(data);
            RecalcAllProperties();
            AssessIntelCoverage();
        }

        protected override AIntel MakeIntelInstance() {
            return new NonRegressibleIntel();
        }

        public SystemReport GetReport(Player player) { return Publisher.GetReport(player); }

        #region Assess System IntelCoverage

        /// <summary>
        /// Static list used to collect all system member's IntelCoverage values for a player when
        /// re-assessing the IntelCoverage value for the system. Avoids creating a temporary
        /// list for every system and every player every time a re-assessment is done, significantly 
        /// reducing heap memory allocations.
        /// </summary>
        private static IList<IntelCoverage> _allMemberIntelCoverages = new List<IntelCoverage>();

        private void AssessIntelCoverage() {
            foreach (Player player in _gameMgr.AllPlayers) {
                AssessIntelCoverageFor(player);
            }
        }

        private void AssessIntelCoverageFor(Player player) {
            if (__debugCntls.IsAllIntelCoverageComprehensive) {
                D.AssertEqual(GetIntelCoverage(player), IntelCoverage.Comprehensive);
                return;
            }

            if (Owner == player) {
                // 7.30.18 Can't Assert Comprehensive here as this assessment can be initiated by the coverage change of a 
                // System member (planet) to Comprehensive (via AssessAssigningOwnerAndAlliesComprehensiveCoverage)
                // before the System itself is set to Comprehensive later by the same method. This is because this System
                // propagates its owner change to its members causing the member to AssessSettingToComprehensive before the System does.
                return;
            }

            if (GetIntelCoverage(player) == IntelCoverage.Comprehensive) {
                return; // no point in continuing as Coverage can't regress
            }

            _allMemberIntelCoverages.Clear();
            foreach (var pData in _allPlanetoidData) {
                IntelCoverage pCoverage = pData.GetIntelCoverage(player);
                _allMemberIntelCoverages.Add(pCoverage);
            }

            IntelCoverage starCoverage = StarData.GetIntelCoverage(player);
            _allMemberIntelCoverages.Add(starCoverage);

            // 7.30.18 Not currently including Settlement because it can't be 'fully explored' by a ship.

            IntelCoverage currentCoverage = GetIntelCoverage(player);

            IntelCoverage lowestMemberCoverage = GameUtility.GetLowestCommonCoverage(_allMemberIntelCoverages);
            SetIntelCoverage(player, lowestMemberCoverage);
        }

        #endregion

        #region Event and Property Change Handlers

        private void StarDataPropSetHandler() {
            D.Assert(!IsOperational);
            SubscribeToStarDataValueChanges();
        }

        private void SettlementDataPropChangedHandler() {
            HandleSettlementDataChanged();
        }

        private void PlanetoidIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandlePlanetoidIntelCoverageChanged(e.Player);
        }

        private void SettlementIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleSettlementIntelCoverageChanged(e.Player);
        }

        private void StarIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleStarIntelCoverageChanged(e.Player);
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

        private void HandlePlanetoidIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        private void HandleSettlementDataChanged() {
            // Existing settlements will always be destroyed (data = null) before a new settlement is founded
            if (SettlementData != null) {
                SubscribeToSettlementDataValueChanges();
            }
            else {
                UnsubscribeToSettlementDataValueChanges();
            }
            _sectorData.AssessSectorAndSystemOwnership();

            if (IsOperational) {
                RecalcAllProperties();
                AssessIntelCoverage();
            }
        }

        private void HandleSettlementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        private void HandleStarIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        protected override void PropagateOwnerChange() {
            base.PropagateOwnerChange();
            _allPlanetoidData.ForAll(pd => pd.Owner = Owner);
            StarData.Owner = Owner;
            if (SettlementData != null) {
                // 7.30.18 no reason to propagate to Settlement. If present, it is the driving force behind the System's owner change
                D.AssertEqual(Owner, SettlementData.Owner);
            }
        }

        private void RecalcAllProperties() {
            UpdateCapacity();
            UpdateResources();
        }

        private void UpdateCapacity() {
            Capacity = _allPlanetoidData.Sum(pData => pData.Capacity) + StarData.Capacity;
        }

        private void UpdateResources() {
            var defaultValueIfEmpty = default(ResourcesYield);
            IEnumerable<ResourcesYield> allPlanetoidResources = _allPlanetoidData.Select(pd => pd.Resources);
            ResourcesYield totalResourcesFromPlanets = allPlanetoidResources.Aggregate(defaultValueIfEmpty, (accumulator, res) => accumulator + res);
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

        private void Cleanup() {
            Unsubscribe();
        }

        #endregion

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

