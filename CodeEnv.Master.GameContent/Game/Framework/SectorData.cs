// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorData.cs
// Class for Data associated with a SectorItem.
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
    using MoreLinq;

    /// <summary>
    /// Class for Data associated with a SectorItem.
    /// </summary>
    public class SectorData : AIntelItemData, IDisposable {

        public IntVector3 SectorID { get; private set; }

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

        private SystemData _systemData;
        public SystemData SystemData {
            get { return _systemData; }
            set {
                D.AssertNull(_systemData);  // one time only if at all
                SetProperty<SystemData>(ref _systemData, value, "SystemData", SystemDataPropSetHandler);
            }
        }

        public new SectorInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SectorInfoAccessController; } }

        private SectorPublisher _publisher;
        public SectorPublisher Publisher {
            get { return _publisher = _publisher ?? new SectorPublisher(this); }
        }

        internal IEnumerable<StarbaseCmdData> AllStarbasesData {
            get {
                if (_allStarbasesData == null) {
                    return Enumerable.Empty<StarbaseCmdData>();
                }
                return _allStarbasesData;
            }
        }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        private IList<IDisposable> _systemDataSubscriptions;
        private IList<StarbaseCmdData> _allStarbasesData;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="sector">The sector.</param>
        /// <param name="sectorID">The sectorID.</param>
        public SectorData(ISector sector, IntVector3 sectorID)
            : this(sector, sectorID, TempGameValues.NoPlayer) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class.
        /// <remarks>Private as Sector's will never begin with an owner besides NoPlayer.</remarks>
        /// </summary>
        /// <param name="sector">The sector.</param>
        /// <param name="sectorID">The sectorID.</param>
        /// <param name="owner">The owner.</param>
        public SectorData(ISector sector, IntVector3 sectorID, Player owner)
            : base(sector, owner) {
            SectorID = sectorID;
            Topography = Topography.OpenSpace;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new SectorInfoAccessController(this);
        }

        private void SubscribeToSystemDataValueChanges() {
            _systemDataSubscriptions = new List<IDisposable>();
            _systemDataSubscriptions.Add(SystemData.SubscribeToPropertyChanged<SystemData, int>(sd => sd.Capacity, SystemCapacityPropChangedHandler));
            _systemDataSubscriptions.Add(SystemData.SubscribeToPropertyChanged<SystemData, ResourcesYield>(sd => sd.Resources, SystemResourcesPropChangedHandler));
            SystemData.intelCoverageChanged += SystemIntelCoverageChangedEventHandler;
            // 6.18.18 No need to monitor System Owner changes as SystemData will call SectorData.AssessOwnership when its owner has changed.
            // Handled this way so all ownership changes propagate through to all dependent items BEFORE IntelCoverage (and InfoAccess) states
            // are evaluated for change. This makes sure all owners are properly set before any other changes that would rely on the right owner.
        }

        private IDictionary<StarbaseCmdData, IList<IDisposable>> _starbaseSubscriptions;

        private void SubscribeToStarbaseDataValueChanges(StarbaseCmdData starbaseData) {
            _starbaseSubscriptions = _starbaseSubscriptions ?? new Dictionary<StarbaseCmdData, IList<IDisposable>>();
            _starbaseSubscriptions.Add(starbaseData, new List<IDisposable>() {
                starbaseData.SubscribeToPropertyChanged<StarbaseCmdData, bool>(sBaseData => sBaseData.IsEstablished, StarbaseIsEstablishedPropChangedHandler),
                starbaseData.SubscribeToPropertyChanged<StarbaseCmdData, Player>(sBaseData => sBaseData.Owner, StarbaseOwnerPropChangedHandler),
            });
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            RecalcAllProperties();
            AssessIntelCoverage();
            AssessSectorAndSystemOwnership();
            AssessStarbasesCrippledState();
        }

        #endregion

        protected override AIntel MakeIntelInstance() {
            return new NonRegressibleIntel();
        }

        /// <summary>
        /// Static list used to collect all sector member's IntelCoverage values for a player when
        /// re-assessing the IntelCoverage value for the sector. Avoids creating a temporary
        /// list for every sector and every player every time a re-assessment is done, significantly 
        /// reducing heap memory allocations.
        /// </summary>
        private static IList<IntelCoverage> _allMemberIntelCoverages = new List<IntelCoverage>();


        public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

        public void Add(StarbaseCmdData starbaseData) {
            _allStarbasesData = _allStarbasesData ?? new List<StarbaseCmdData>();
            _allStarbasesData.Add(starbaseData);
            SubscribeToStarbaseDataValueChanges(starbaseData);
            AssessSectorAndSystemOwnership();
            AssessStarbasesCrippledState();
        }

        public void Remove(StarbaseCmdData starbaseData) {
            bool isRemoved = _allStarbasesData.Remove(starbaseData);
            D.Assert(isRemoved);
            UnsubscribeFromStarbaseDataValueChanges(starbaseData);
            AssessSectorAndSystemOwnership();
        }

        #region Event and Property Change Handlers

        private void StarbaseOwnerPropChangedHandler() {
            HandleStarbaseOwnerChanged();
        }

        private void StarbaseIsEstablishedPropChangedHandler() {
            HandleStarbaseIsEstablishedChanged();
        }

        private void SystemDataPropSetHandler() {
            HandleSystemDataSet();
        }

        private void SystemIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleSystemIntelCoverageChanged(e.Player);
        }

        private void SystemCapacityPropChangedHandler() {
            UpdateCapacity();
        }

        private void SystemResourcesPropChangedHandler() {
            UpdateResources();
        }

        #endregion

        private void HandleSystemIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (!IsOperational) {
                return;
            }
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        protected override void HandleOwnerChangesComplete() {
            base.HandleOwnerChangesComplete();
            AssessIntelCoverage();
            AssessStarbasesCrippledState();
        }

        private void HandleSystemDataSet() {
            D.Assert(!IsOperational);
            SubscribeToSystemDataValueChanges();
        }

        private void HandleStarbaseOwnerChanged() {
            AssessSectorAndSystemOwnership();
            AssessIntelCoverage();
            AssessStarbasesCrippledState();
        }

        private void HandleStarbaseIsEstablishedChanged() {
            AssessSectorAndSystemOwnership();
        }

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
                D.AssertEqual(GetIntelCoverage(player), IntelCoverage.Comprehensive);
                return;
            }

            if (GetIntelCoverage(player) == IntelCoverage.Comprehensive) {
                return; // no point in continuing as Coverage can't regress
            }

            _allMemberIntelCoverages.Clear();
            if (SystemData != null) {
                IntelCoverage playerSystemCoverage = SystemData.GetIntelCoverage(player);
                _allMemberIntelCoverages.Add(playerSystemCoverage);
            }

            if (_allStarbasesData != null) {
                foreach (var sBaseData in _allStarbasesData) {
                    _allMemberIntelCoverages.Add(sBaseData.GetIntelCoverage(player));
                }
            }

            // TODO add other members when they are incorporated into Sectors

            IntelCoverage currentCoverage = GetIntelCoverage(player);

            IntelCoverage lowestMemberCoverage = GameUtility.GetLowestCommonCoverage(_allMemberIntelCoverages);
            SetIntelCoverage(player, lowestMemberCoverage);
        }

        /// <summary>
        /// Assesses whether to change the ownership of the sector and its system if present, and if so, changes it.
        /// <remarks>7.31.18 If the System is settled, that owner owns both the system and sector. If not settled, then 
        /// the owner of both is the owner of the largest established Starbase in the Sector, if any. If neither
        /// of these conditions are true, then the owner is NoPlayer.</remarks>
        /// <remarks>Internal to allow SystemData to call it when it needs to determine its owner.</remarks>
        /// </summary>
        internal void AssessSectorAndSystemOwnership() {
            bool hasSystem = SystemData != null;
            Player sectorAndSystemOwner = Owner;
            if (hasSystem && SystemData.SettlementData != null) {
                // Sector has a System with a Settlement so that specifies Sector owner
                sectorAndSystemOwner = SystemData.SettlementData.Owner;
            }
            else {
                // No System or System but no Settlement so sector and system Owner determined by largest established Starbase, if any
                if (_allStarbasesData != null) {
                    var establishedStarbases = _allStarbasesData.Where(sBaseData => sBaseData.IsEstablished);
                    if (establishedStarbases.Any()) {
                        var maxCategory = establishedStarbases.Max(sBaseData => sBaseData.Category);
                        var maxCategoryStarbases = establishedStarbases.Where(sBaseData => sBaseData.Category == maxCategory);
                        D.Assert(maxCategoryStarbases.Any());
                        sectorAndSystemOwner = maxCategoryStarbases.MaxBy(sBaseData => sBaseData.Population).Owner;
                    }
                }
            }

            if (Owner != sectorAndSystemOwner) {
                Owner = sectorAndSystemOwner;
            }
            if (hasSystem && SystemData.Owner != sectorAndSystemOwner) {
                SystemData.Owner = sectorAndSystemOwner;
                D.AssertEqual(Owner, SystemData.Owner);
            }
        }

        private void AssessStarbasesCrippledState() {
            if (_allStarbasesData != null) {
                if (_allStarbasesData.Any()) {
                    if (Owner != TempGameValues.NoPlayer) {
                        var ownerSbDatas = _allStarbasesData.Where(sbData => sbData.Owner == Owner);
                        var opponentSbDatas = _allStarbasesData.Except(ownerSbDatas);
                        ownerSbDatas.ForAll(sbData => sbData.IsCrippled = false);
                        opponentSbDatas.ForAll(sbData => sbData.IsCrippled = true);
                    }
                    else {
                        _allStarbasesData.ForAll(sbData => sbData.IsCrippled = false);
                    }
                }
            }
        }

        protected override void PropagateOwnerChange() {
            base.PropagateOwnerChange();
            // Starbase ownership is not determined by the owner of the Sector.
            // If System is present and settled, SystemOwner will already be the same owner. 
            // If System is present and not settled SystemOwner has already been changed by AssessSectorAndSystemOwnership().
        }

        private void RecalcAllProperties() {
            UpdateCapacity();
            UpdateResources();
        }

        private void UpdateCapacity() {
            if (SystemData != null) {
                Capacity = SystemData.Capacity;
            }
            // FIXME: Starbases have Capacity values so need to be included, but as Capacity is not yet used, 
            // which starbases should be included into the sector's capacity? - starbases with same owner as
            // Sector? All Starbases? Also, what about System Capacity with or without an owner?
        }

        private void UpdateResources() {
            if (SystemData != null) {
                Resources = SystemData.Resources;
            }
            // 6.19.18 Starbases aren't a source of Resources. They have a Resources property but it represents
            // the resources they have access to in the sector where they are located.
        }

        #region Cleanup

        private void UnsubscribeFromStarbaseDataValueChanges(StarbaseCmdData starbaseData) {
            IList<IDisposable> starbaseSubscriptions = _starbaseSubscriptions[starbaseData];
            starbaseSubscriptions.ForAll(d => d.Dispose());
            starbaseSubscriptions.Clear();
            _starbaseSubscriptions.Remove(starbaseData);
        }

        private void UnsubscribeFromSystemDataValueChanges() {
            if (SystemData != null) {
                D.AssertNotNull(_systemDataSubscriptions);
                _systemDataSubscriptions.ForAll(sds => sds.Dispose());
                _systemDataSubscriptions.Clear();
                SystemData.intelCoverageChanged -= SystemIntelCoverageChangedEventHandler;
            }
        }

        private void Unsubscribe() {
            UnsubscribeFromSystemDataValueChanges();
            if (_allStarbasesData != null) {
                foreach (var sBaseData in _allStarbasesData) {
                    UnsubscribeFromStarbaseDataValueChanges(sBaseData);
                }
            }
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

