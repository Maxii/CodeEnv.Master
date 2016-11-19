// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Component that handles detection events for IDetectable items.
    /// </summary>
    public class DetectionHandler : IDisposable {

        private const string FullNameFormat = "{0}.{1}";

        public string FullName { get { return FullNameFormat.Inject(_item.FullName, typeof(DetectionHandler).Name); } }

        private bool ShowDebugLog { get { return _item.ShowDebugLog; } }

        /// <summary>
        /// Lookup that holds a list of the Cmds that have detected this Item, organized by the range
        /// of the sensor used and the owner of the Cmd.
        /// </summary>
        private IDictionary<Player, IDictionary<RangeCategory, IList<IUnitCmd_Ltd>>> _detectionLookup;
        private IIntelItem _item;
        private IGameManager _gameMgr;
        private IDebugControls _debugControls;

        public DetectionHandler(IIntelItem item) {
            _item = item;
            _gameMgr = References.GameManager;
            _debugControls = References.DebugControls;
            _detectionLookup = new Dictionary<Player, IDictionary<RangeCategory, IList<IUnitCmd_Ltd>>>(_gameMgr.AllPlayers.Count);
        }

        public void HandleDetectionBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRange) {
            if (!_item.IsOperational) {
                D.Error("{0} should not be detected by {1} when dead!", _item.FullName, cmdItem.FullName);
            }
            //D.Log(ShowDebugLog, "{0}.HandleDetectionBy called. Detecting Cmd: {1}, SensorRange: {2}.", FullName, cmdItem.FullName, sensorRange.GetValueName());

            IDictionary<RangeCategory, IList<IUnitCmd_Ltd>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                rangeLookup = new Dictionary<RangeCategory, IList<IUnitCmd_Ltd>>(RangeCategoryEqualityComparer.Default);
                _detectionLookup.Add(detectingPlayer, rangeLookup);
            }

            IList<IUnitCmd_Ltd> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                cmds = new List<IUnitCmd_Ltd>();
                rangeLookup.Add(sensorRange, cmds);
            }
            D.Assert(!cmds.Contains(cmdItem), cmdItem.FullName);
            cmds.Add(cmdItem);

            // The following returns can not move earlier in method as detection state must be kept current to support Reset
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Should already be set to comprehensive during game startup
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                // Note: this debug setting DOES NOT pre-populate all player's knowledge
                if (_item.Owner.IsRelationshipWith(detectingPlayer, DiplomaticRelationship.Alliance, DiplomaticRelationship.Self)) {
                    // even with DebugSettings all coverage comprehensive, still no reason to update if Alliance or Self
                    __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                    __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                    return;
                }
                UpdatePlayerKnowledge(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            if (_item.Owner.IsRelationshipWith(detectingPlayer, DiplomaticRelationship.Alliance, DiplomaticRelationship.Self)) {
                // Should already be set to comprehensive when became self or alliance took place
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
            UpdatePlayerKnowledge(detectingPlayer);
        }

        public void HandleDetectionLostBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRange) {
            D.AssertNotDefault((int)sensorRange);
            if (!_item.IsOperational) {    // 7.20.16 detected items no longer notified of lost detection when they die
                D.Error("{0} should not be notified by {1} of detection lost when dead!", FullName, cmdItem.FullName);
            }
            //D.Log(ShowDebugLog, "{0}.HandleDetectionLostBy called. Detecting Cmd: {1}, SensorRange: {2}.", FullName, cmdItem.FullName, sensorRange.GetValueName());

            IDictionary<RangeCategory, IList<IUnitCmd_Ltd>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                D.Error("{0} found no Sensor Range lookup. Detecting Cmd: {1}.", FullName, cmdItem.FullName);
                return;
            }

            IList<IUnitCmd_Ltd> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                D.Error("{0} found no List of Commands. Detecting Cmd: {1}, SensorRange: {2}.", FullName, cmdItem.FullName, sensorRange.GetValueName());
                return;
            }

            bool isRemoved = cmds.Remove(cmdItem);
            D.Assert(isRemoved, cmdItem.FullName);
            if (cmds.Count == Constants.Zero) {
                rangeLookup.Remove(sensorRange);
                if (rangeLookup.Count == Constants.Zero) {
                    _detectionLookup.Remove(detectingPlayer);
                }
            }

            // The following returns can not move earlier in method as detection state must be kept current to support Reset
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Should already be set to comprehensive during game startup
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);   // must have already detected item to have lost it
                return; // continuing could regress coverage on items that allow it
            }

            if (_item.Owner.IsRelationshipWith(detectingPlayer, DiplomaticRelationship.Alliance, DiplomaticRelationship.Self)) {
                // Should already be set to comprehensive when became self or alliance took place
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
            UpdatePlayerKnowledge(detectingPlayer);
        }

        /// <summary>
        /// Assesses and assigns the Item's IntelCoverage for the provided <c>detectingPlayer</c>.
        /// </summary>
        /// <param name="detectingPlayer">The detecting player.</param>
        private void AssignDetectingPlayerIntelCoverage(Player detectingPlayer) {
            D.AssertNotEqual(_item.Owner, detectingPlayer);
            AssignIntelCoverage(detectingPlayer);
        }

        /// <summary>
        /// Assesses and assigns the Item's IntelCoverage for this <c>player</c>.
        /// Makes no changes if item Owner is allied with player.
        /// </summary>
        /// <param name="player">The player.</param>
        private void AssignIntelCoverage(Player player) {
            D.AssertNotNull(player);
            D.AssertNotEqual(TempGameValues.NoPlayer, player);
            D.Assert(!_debugControls.IsAllIntelCoverageComprehensive);
            D.Assert(!_item.Owner.IsRelationshipWith(player, DiplomaticRelationship.Alliance));

            // Note: Player can be self when the current owner is losing ownership and needs to reassess their coverage
            // and knowledge of their (about to be former) item

            IntelCoverage newCoverage;
            IDictionary<RangeCategory, IList<IUnitCmd_Ltd>> rangeLookup;
            if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
                newCoverage = IntelCoverage.None;
            }
            else if (rangeLookup.ContainsKey(RangeCategory.Short)) {
                newCoverage = IntelCoverage.Broad;
            }
            else if (rangeLookup.ContainsKey(RangeCategory.Medium)) {
                newCoverage = IntelCoverage.Essential;
            }
            else if (rangeLookup.ContainsKey(RangeCategory.Long)) {
                newCoverage = IntelCoverage.Basic;
            }
            else {
                newCoverage = IntelCoverage.None;   // OPTIMIZE needed?
            }

            if (_item.SetIntelCoverage(player, newCoverage)) {
                if (ShowDebugLog) {
                D.Log("{0} successfully set {1}'s IntelCoverage to {2}.", FullName, player, newCoverage.GetValueName());
                }
            }
        }

        private void UpdatePlayerKnowledge(Player player) {
            D.Assert(!_item.Owner.IsRelationshipWith(player, DiplomaticRelationship.Alliance));

            // Notes: 
            // 1)   AllIntelCoverageComprehensive does not pre-populate all player's knowledge so this can still be called 
            //      when player first detects this item
            // 2)   Player can be self when the current owner is losing ownership and needs to reassess their coverage
            //      and knowledge of their (about to be former) item

            var playerAiMgr = _gameMgr.GetAIManagerFor(player);

            IDictionary<RangeCategory, IList<IUnitCmd_Ltd>> rangeLookup;
            if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
                playerAiMgr.HandleItemDetectionLost(_item as ISensorDetectable);
            }
            else {
                // there are one or more DistanceRange keys so some Cmd of player has this item in sensor range
                D.Assert(rangeLookup.Keys.Count > Constants.Zero);
                playerAiMgr.HandleItemDetection(_item as ISensorDetectable);
            }
        }

        #region Event and Property Change Handlers

        // 8.3.16 Owner changing/changed now handled by Items

        #endregion

        /// <summary>
        /// Reassesses the player's coverage and knowledge of this item based on 
        /// current detection levels.
        /// </summary>
        /// <param name="player">The player.</param>
        public void ResetBasedOnCurrentDetection(Player player) {
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // player must already know about this item to call this so can't lose knowledge once Comprehensive and aware of it
                return;
            }
            AssignIntelCoverage(player);

            // by definition, the player must have knowledge of the item before reassessing that knowledge
            __ValidatePlayerKnowledgeOfItem(player);
            UpdatePlayerKnowledge(player);
        }

        private void Cleanup() { }


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        private void __ValidatePlayerIntelCoverageOfItemIsComprehensive(Player player) {
            if (_item.GetIntelCoverage(player) != IntelCoverage.Comprehensive) {
                D.Error("{0}: {1}'s IntelCoverage of {2} should be Comprehensive rather than {3}.", FullName, player, _item.FullName, _item.GetIntelCoverage(player));
            }
        }

        private void __ValidatePlayerKnowledgeOfItem(Player player) {
            if (_item is IStar || _item is IUniverseCenter) {
                // unnecessary check as all players have knowledge of these sensor detectable items
                return;
            }
            var playerAIMgr = _gameMgr.GetAIManagerFor(player);
            if (!playerAIMgr.HasKnowledgeOf(_item as IIntelItem_Ltd)) {
                string itemsKnownText = playerAIMgr.__GetItemsOwnedBy(_item.Owner).Select(item => item.FullName).Concatenate();
                D.Error("{0}: {1} has no knowledge of {2}. IsOperational = {3}. Items known: {4}.", FullName, player, _item.FullName, _item.IsOperational, itemsKnownText);
            }
        }

        #endregion

        // OPTIMIZE IDisposable not really needed when no longer subscribing to owner change events

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
        //        D.Log("{0} successfully set {1}'s IntelCoverage to {2}.", _data.FullName, player.LeaderName, newCoverage.GetValueName());
        //    }
        //}

        #endregion

    }
}

