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
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a SectorItem.
    /// </summary>
    public class SectorData : AItemData, IDisposable {

        public Index3D SectorIndex { get; private set; }

        private SystemData _systemData;
        public SystemData SystemData {
            get { return _systemData; }
            set {
                D.Assert(_systemData == null);  // one time only if at all
                SetProperty<SystemData>(ref _systemData, value, "SystemData", SystemDataPropChangedHandler);
            }
        }

        /// <summary>
        /// UNDONE
        /// The density of matter in space in this sector. Intended to be
        /// applied to pathfinding points in the sector as a 'penalty' to
        /// influence path creation. Should also increase drag on a ship
        /// in the sector to reduce its speed for a given thrust. The value
        /// should probably be a function of the OpeYield in the sector.
        /// </summary>
        [System.Obsolete]
        public float Density { get; set; }

        private IList<IDisposable> _systemDataSubscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        public SectorData(Transform sectorTransform, Index3D index)
            : this(sectorTransform, index, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        /// <param name="owner">The owner.</param>
        public SectorData(Transform sectorTransform, Index3D index, Player owner)
            : base(sectorTransform, "Sector {0}".Inject(index), owner) {
            SectorIndex = index;
            Topography = Topography.OpenSpace;
        }

        private void SubscribeToSystemDataValueChanges() {
            _systemDataSubscribers = new List<IDisposable>();
            _systemDataSubscribers.Add(SystemData.SubscribeToPropertyChanged<SystemData, Player>(sd => sd.Owner, SystemOwnerPropChangedHandler));
        }

        #region Event and Property Change Handlers

        private void SystemDataPropChangedHandler() {
            SubscribeToSystemDataValueChanges();
            if (Owner != SystemData.Owner) { // avoids PropChange equal warning when System owner is NoPlayer
                Owner = SystemData.Owner;
            }
        }

        private void SystemOwnerPropChangedHandler() {
            Owner = SystemData.Owner;
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            if (_systemDataSubscribers != null) {
                _systemDataSubscribers.ForAll(sds => sds.Dispose());
                _systemDataSubscribers.Clear();
            }
        }

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

