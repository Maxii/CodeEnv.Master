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
    /// Class for Data associated with a SectorItem.
    /// </summary>
    public class SectorData : AIntelItemData, IDisposable {

        public Index3D SectorIndex { get; private set; }

        private SystemData _systemData;
        public SystemData SystemData {
            get { return _systemData; }
            set {
                D.Assert(_systemData == null);  // one time only if at all
                SetProperty<SystemData>(ref _systemData, value, "SystemData", SystemDataPropSetHandler);
            }
        }

        public new SectorInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SectorInfoAccessController; } }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        private IList<IDisposable> _systemDataSubscribers;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        public SectorData(ISector sector, Index3D index)
            : this(sector, index, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class.
        /// </summary>
        /// <param name="sector">The sector.</param>
        /// <param name="index">The index.</param>
        /// <param name="owner">The owner.</param>
        public SectorData(ISector sector, Index3D index, Player owner)
            : base(sector, owner) {
            SectorIndex = index;
            Topography = Topography.OpenSpace;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new SectorInfoAccessController(this);
        }

        private void SubscribeToSystemDataValueChanges() {
            _systemDataSubscribers = new List<IDisposable>();
            _systemDataSubscribers.Add(SystemData.SubscribeToPropertyChanged<SystemData, Player>(sd => sd.Owner, SystemOwnerPropChangedHandler));
            SystemData.intelCoverageChanged += SystemIntelCoverageChangedEventHandler;
        }

        protected override void FinalInitialize() {
            base.FinalInitialize();
            AssessIntelCoverage();
        }

        #endregion

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
            List<IntelCoverage> allMemberCoverages = new List<IntelCoverage>();

            if (SystemData != null) {
                IntelCoverage systemCoverage = SystemData.GetIntelCoverage(player);
                allMemberCoverages.Add(systemCoverage);
            }
            // TODO add other members when they are incorporated into Sectors

            if (!allMemberCoverages.Any()) {
                // TEMP there are no members so give player Comprehensive
                var isSet = SetIntelCoverage(player, IntelCoverage.Comprehensive);
                D.Assert(isSet);
                return;
            }

            IntelCoverage currentCoverage = GetIntelCoverage(player);

            IntelCoverage lowestCommonCoverage = GetLowestCommonCoverage(allMemberCoverages);
            var isCoverageSet = SetIntelCoverage(player, lowestCommonCoverage);
            if (isCoverageSet) {
                D.Log(ShowDebugLog, "{0} has assessed its IntelCoverage for {1} and changed it from {2} to the lowest common member value {3}.",
                    FullName, player.Name, currentCoverage.GetValueName(), lowestCommonCoverage.GetValueName());
            }
            else {
                D.Log(ShowDebugLog, "{0} has assessed its IntelCoverage for {1} and declined to change it from {2} to the lowest common member value {3}.",
                    FullName, player.Name, currentCoverage.GetValueName(), lowestCommonCoverage.GetValueName());
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

        private void SystemDataPropSetHandler() {
            D.Assert(!IsOperational);
            SubscribeToSystemDataValueChanges();
            if (Owner != SystemData.Owner) { // avoids PropChange equal warning when System owner is NoPlayer
                Owner = SystemData.Owner;
            }
        }

        private void SystemIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            if (!IsOperational) {
                return;
            }
            var playerWhosCoverageChgd = e.Player;
            AssessIntelCoverageFor(playerWhosCoverageChgd);
        }

        private void SystemOwnerPropChangedHandler() {
            Owner = SystemData.Owner;
        }

        protected override void OwnerPropChangedHandler() {
            base.OwnerPropChangedHandler();
            PropogateOwnerChange();
        }

        #endregion

        private void PropogateOwnerChange() {
            // TODO nothing to propogate to yet as System.Owner is the source of the change
        }

        #region Cleanup

        private void UnsubscribeFromSystemDataValueChanges() {
            if (SystemData != null) {
                D.Assert(_systemDataSubscribers != null);
                _systemDataSubscribers.ForAll(sds => sds.Dispose());
                _systemDataSubscribers.Clear();
                SystemData.intelCoverageChanged -= SystemIntelCoverageChangedEventHandler;
            }
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            UnsubscribeFromSystemDataValueChanges();
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

