﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DetectionHandler.cs
// Component that handles detection events for IDetectable items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Component that handles detection events for IDetectable items.
    /// </summary>
    public class DetectionHandler : IDisposable {

        /// <summary>
        /// Lookup that holds a list of the Cmds that have detected this Item, organized by the range
        /// of the sensor used and the owner of the Cmd.
        /// </summary>
        private IDictionary<Player, IDictionary<RangeCategory, IList<IUnitCmdItem>>> _detectionLookup = new Dictionary<Player, IDictionary<RangeCategory, IList<IUnitCmdItem>>>();
        private IIntelItem _item;
        private IGameManager _gameMgr;

        public DetectionHandler(IIntelItem item) {
            _item = item;
            _gameMgr = References.GameManager;
            Subscribe();
        }

        private void Subscribe() {
            _item.ownerChanging += OwnerChangingEventHandler;
            _item.ownerChanged += OwnerChangedEventHandler;
        }

        public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
            D.Log("{0}.{1}.HandleDetectionBy called. Detecting Cmd: {2}, SensorRange: {3}.", _item.FullName, GetType().Name, cmdItem.FullName, sensorRange.GetValueName());
            Player detectingPlayer = cmdItem.Owner;

            IDictionary<RangeCategory, IList<IUnitCmdItem>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                rangeLookup = new Dictionary<RangeCategory, IList<IUnitCmdItem>>();
                _detectionLookup.Add(detectingPlayer, rangeLookup);
            }

            IList<IUnitCmdItem> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                cmds = new List<IUnitCmdItem>();
                rangeLookup.Add(sensorRange, cmds);
            }
            D.Assert(!cmds.Contains(cmdItem), "{0} attempted to add duplicate {1}.".Inject(_item.FullName, cmdItem.FullName));
            cmds.Add(cmdItem);

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
            UpdatePlayerKnowledge(detectingPlayer);
        }

        public void HandleDetectionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
            D.Assert(sensorRange != RangeCategory.None);
            D.Log("{0}.{1}.HandleDetectionLostBy called. Detecting Cmd: {2}, SensorRange: {3}.", _item.FullName, GetType().Name, cmdItem.FullName, sensorRange.GetValueName());
            Player detectingPlayer = cmdItem.Owner;

            IDictionary<RangeCategory, IList<IUnitCmdItem>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                D.Error("{0} found no Sensor Range lookup. Detecting Cmd: {1}.", _item.FullName, cmdItem.FullName);
                return;
            }

            IList<IUnitCmdItem> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                D.Error("{0} found no List of Commands. Detecting Cmd: {1}, SensorRange: {2}.", _item.FullName, cmdItem.FullName, sensorRange.GetValueName());
                return;
            }

            bool isRemoved = cmds.Remove(cmdItem);
            D.Assert(isRemoved, "{0} could not find {1} to remove.".Inject(_item.FullName, cmdItem.FullName));
            if (cmds.Count == Constants.Zero) {
                rangeLookup.Remove(sensorRange);
            }

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
            UpdatePlayerKnowledge(detectingPlayer);
        }

        /// <summary>
        /// Assesses and assigns the Item's IntelCoverage for the provided <c>detectingPlayer</c>.
        /// </summary>
        /// <param name="detectingPlayer">The detecting player.</param>
        private void AssignDetectingPlayerIntelCoverage(Player detectingPlayer) {
            AssignIntelCoverage(detectingPlayer, _item.Owner);
        }

        /// <summary>
        /// Assesses and assigns the Item's IntelCoverage for this <c>player</c>.
        /// </summary>
        /// <remarks>
        /// This version is used in cases where the <c>itemOwner</c> is changing,
        /// thereby not allowing an effective comparison to _data.Owner.
        /// </remarks>
        /// <param name="player">The player.</param>
        /// <param name="itemOwner">The item owner.</param>
        private void AssignIntelCoverage(Player player, Player itemOwner) {
            D.Assert(player != null && player != TempGameValues.NoPlayer);
            D.Assert(itemOwner != null);

            IntelCoverage newCoverage;
            if (DebugSettings.Instance.AllIntelCoverageComprehensive || player == itemOwner) {
                newCoverage = IntelCoverage.Comprehensive;
            }
            else {
                IDictionary<RangeCategory, IList<IUnitCmdItem>> rangeLookup;
                if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
                    D.Error("{0} found no Sensor Range lookup. Player: {1}.", _item.FullName, player.LeaderName);
                    return;
                }
                if (rangeLookup.ContainsKey(RangeCategory.Short)) {
                    newCoverage = IntelCoverage.Broad;
                }
                else if (rangeLookup.ContainsKey(RangeCategory.Medium)) {
                    newCoverage = IntelCoverage.Essential;
                }
                else if (rangeLookup.ContainsKey(RangeCategory.Long)) {
                    newCoverage = IntelCoverage.Basic;
                }
                else {
                    newCoverage = IntelCoverage.None;
                }
            }

            if (_item.SetIntelCoverage(player, newCoverage)) {
                D.Log("{0} successfully set {1}'s IntelCoverage to {2}.", _item.FullName, player.LeaderName, newCoverage.GetValueName());
            }
        }

        private void UpdatePlayerKnowledge(Player player) {
            IDictionary<RangeCategory, IList<IUnitCmdItem>> rangeLookup;
            if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
                D.Error("{0} found no Sensor Range lookup. Player: {1}.", _item.FullName, player.LeaderName);
                return;
            }

            var playerKnowledge = _gameMgr.PlayersKnowledge.GetKnowledge(player);
            if (rangeLookup.Keys.Count > Constants.Zero) {
                // there are one or more DistanceRange keys so some Cmd of player has this item in sensor range
                playerKnowledge.HandleItemDetection(_item);
            }
            else {
                // there are no DistanceRange keys so player has no Cmds in sensor range of this item
                playerKnowledge.HandleItemDetectionLost(_item);
            }
        }

        #region Event and Property Change Handlers

        private void OwnerChangingEventHandler(object sender, OwnerChangingEventArgs e) {
            var outgoingOwner = (sender as IItem).Owner;
            D.Assert(outgoingOwner != null);    // default should be NoPlayer rather than null
            if (outgoingOwner == TempGameValues.NoPlayer) {
                return; // NoPlayer is not a player in the game with IntelCoverage to track
            }
            Player newOwner = e.IncomingOwner;
            AssignIntelCoverage(outgoingOwner, newOwner);
        }

        private void OwnerChangedEventHandler(object sender, EventArgs e) {
            var newOwner = (sender as IItem).Owner;
            if (newOwner == TempGameValues.NoPlayer) {
                return; // NoPlayer is not a player in the game with IntelCoverage to track
            }
            AssignIntelCoverage(newOwner, newOwner);
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _item.ownerChanging -= OwnerChangingEventHandler;
            _item.ownerChanged -= OwnerChangedEventHandler;
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


        #region Archive

        //private void AssessPlayerIntelState(Player player) {
        //    IDictionary<DistanceRange, IList<ICommandItem>> rangeLookup;
        //    if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
        //        D.Error("{0} found no Sensor Range lookup. Player: {1}.", _data.FullName, player.LeaderName);
        //        return;
        //    }
        //    IntelCoverage newCoverage;
        //    if (player == _data.Owner) {
        //        newCoverage = IntelCoverage.Comprehensive;
        //    }
        //    else if (rangeLookup.ContainsKey(DistanceRange.Short)) {
        //        newCoverage = IntelCoverage.Moderate;
        //    }
        //    else if (rangeLookup.ContainsKey(DistanceRange.Medium)) {
        //        newCoverage = IntelCoverage.Minimal;
        //    }
        //    else if (rangeLookup.ContainsKey(DistanceRange.Long)) {
        //        newCoverage = IntelCoverage.Aware;
        //    }
        //    else {
        //        newCoverage = IntelCoverage.None;
        //    }

        //    if (_data.TrySetIntelCoverage(player, newCoverage)) {
        //        D.Log("{0} successfully set {1}'s IntelCoverage to {2}.", _data.FullName, player.LeaderName, newCoverage.GetName());
        //    }
        //}

        #endregion

    }
}

