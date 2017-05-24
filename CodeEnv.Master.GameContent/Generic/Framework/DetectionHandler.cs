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
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Component that handles detection events for IDetectable items.
    /// </summary>
    public class DetectionHandler : IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public string DebugName { get { return DebugNameFormat.Inject(_item.DebugName, typeof(DetectionHandler).Name); } }

        private bool ShowDebugLog { get { return _item.ShowDebugLog; } }

        /// <summary>
        /// Lookup that holds a list of the Cmds that have detected this Item, organized by the range
        /// of the sensor used and the owner of the Cmd.
        /// </summary>
        private IDictionary<Player, IDictionary<RangeCategory, IList<ISensorDetector>>> _detectionLookup;
        private IDetectionHandlerClient _item;
        private IGameManager _gameMgr;
        private IDebugControls _debugControls;

        public DetectionHandler(IDetectionHandlerClient item) {
            _item = item;
            _gameMgr = GameReferences.GameManager;
            _debugControls = GameReferences.DebugControls;
            _detectionLookup = new Dictionary<Player, IDictionary<RangeCategory, IList<ISensorDetector>>>(_gameMgr.AllPlayers.Count);
        }

        /// <summary>
        /// Handles detection by a ISensorDetector.
        /// <remarks>2.6.17 Removed use of obsolete UpdatePlayerKnowledge(). Knowledge now updated as a result of IntelCoverage changes.</remarks>
        /// </summary>
        /// <param name="detector">The command item.</param>
        /// <param name="sensorRange">The sensor range.</param>
        public void HandleDetectionBy(ISensorDetector detector, RangeCategory sensorRange) {
            if (!_item.IsOperational) {
                D.Error("{0} should not be detected by {1} when dead!", _item.DebugName, detector.DebugName);
            }
            //D.Log(ShowDebugLog, "{0}.HandleDetectionBy called. Detector: {1}, SensorRange: {2}.", DebugName, detector.DebugName, sensorRange.GetValueName());
            Player detectingPlayer = detector.Owner;
            IDictionary<RangeCategory, IList<ISensorDetector>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {

                Profiler.BeginSample("Proper Dictionary allocation", (_item as Component).gameObject);
                rangeLookup = new Dictionary<RangeCategory, IList<ISensorDetector>>(3, RangeCategoryEqualityComparer.Default); // OPTIMIZE check size
                Profiler.EndSample();

                _detectionLookup.Add(detectingPlayer, rangeLookup);
            }

            IList<ISensorDetector> detectors;
            if (!rangeLookup.TryGetValue(sensorRange, out detectors)) {

                Profiler.BeginSample("Proper List allocation", (_item as Component).gameObject);
                detectors = new List<ISensorDetector>();
                Profiler.EndSample();

                rangeLookup.Add(sensorRange, detectors);
            }
            D.Assert(!detectors.Contains(detector), detector.DebugName);
            detectors.Add(detector);

            // The following returns can not move earlier in method as _detectionLookup must be kept current to support Reset
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Should already be set to comprehensive during game startup
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            if (_item.Owner.IsRelationshipWith(detectingPlayer, DiplomaticRelationship.Alliance, DiplomaticRelationship.Self)) {
                // Should already be set to comprehensive when became self or alliance took place
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            Profiler.BeginSample("AssignDetectingPlayerIntelCoverage", (_item as Component).gameObject);
            AssignDetectingPlayerIntelCoverage(detectingPlayer);
            Profiler.EndSample();
        }

        /// <summary>
        /// Handles detection lost by a ISensorDetector.
        /// <remarks>2.6.17 Removed use of obsolete UpdatePlayerKnowledge(). Knowledge now updated as a result of IntelCoverage changes.</remarks>
        /// </summary>
        /// <param name="detector">The detector item.</param>
        /// <param name="detectorOwner">The detector owner.</param>
        /// <param name="sensorRange">The sensor range.</param>
        public void HandleDetectionLostBy(ISensorDetector detector, Player detectorOwner, RangeCategory sensorRange) {
            D.AssertNotDefault((int)sensorRange);
            if (!_item.IsOperational) {    // 7.20.16 detected items no longer notified of lost detection when they die
                D.Error("{0} should not be notified by {1} of detection lost when dead!", DebugName, detector.DebugName);
            }
            //D.Log(ShowDebugLog, "{0}.HandleDetectionLostBy called. Detector: {1}, SensorRange: {2}.", DebugName, detector.DebugName, sensorRange.GetValueName());
            Player detectingPlayer = detectorOwner;
            IDictionary<RangeCategory, IList<ISensorDetector>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                D.Error("{0} found no Sensor Range lookup. Detecting Cmd: {1}.", DebugName, detector.DebugName);
                return;
            }

            IList<ISensorDetector> detectors;
            if (!rangeLookup.TryGetValue(sensorRange, out detectors)) {
                D.Error("{0} found no List of Commands. Detector: {1}, SensorRange: {2}.", DebugName, detector.DebugName, sensorRange.GetValueName());
                return;
            }

            bool isRemoved = detectors.Remove(detector);
            D.Assert(isRemoved, detector.DebugName);
            if (detectors.Count == Constants.Zero) {
                rangeLookup.Remove(sensorRange);
                if (rangeLookup.Count == Constants.Zero) {
                    _detectionLookup.Remove(detectingPlayer);
                }
            }

            // The following returns can not move earlier in method as detection state must be kept current to support Reset
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Should already be set to comprehensive during game startup
                __ValidatePlayerIntelCoverageOfItemIsComprehensive(detectingPlayer);
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return; // continuing could regress coverage on items that allow it
            }

            if (_item.GetIntelCoverage(detectingPlayer) == IntelCoverage.Comprehensive) {
                bool isComprehensiveExpected = (_item as IIntelItem).__IsPlayerEntitledToComprehensiveRelationship(detectingPlayer);

                if (!isComprehensiveExpected) {
                    D.Error("{0} found {1} has IntelCoverage.{2} with Relationship {3} in Frame {4}.", DebugName, detectingPlayer.DebugName,
                        IntelCoverage.Comprehensive.GetValueName(), _item.Owner.GetCurrentRelations(detectingPlayer).GetValueName(), Time.frameCount);
                }
                __ValidatePlayerKnowledgeOfItem(detectingPlayer);
                return;
            }

            // IntelCoverage < Comprehensive
            if (_item.Owner == detectingPlayer) {
                // 4.22.17 The only way the owner and detectingPlayer could be the same with IntelCoverage < Comprehensive, 
                // is when the ElementSensorMonitor attached to this same element is telling us of lost detection as part of 
                // its reset and reacquire process when the element's owner is changing. Confirmed this path is used when changing owner.
                D.Assert(_item is IUnitElement);
                D.Assert((_item as IUnitElement).IsOwnerChangeUnderway);
                // 4.22.17 ResetBasedOnCurrentDetection has already run (from xxxItem.HandleOwnerChanging) which is what reduced 
                // IntelCoverage below Comprehensive. This current pass is from the SensorRangeMonitor of this same element. 
                // If it is the only remaining monitor detecting this element and its telling it of lost detection, then it could 
                // drop to None (if a ship). The previous ResetBasedOnCurrentDetection call could not result in None 
                // as this Item is still detecting itself.
                D.AssertNotEqual(IntelCoverage.None, _item.GetIntelCoverage(detectingPlayer));
                AssignIntelCoverage(detectingPlayer);
                if (_item.GetIntelCoverage(detectingPlayer) == IntelCoverage.None) {
                    D.Assert(_item is IShip);   // Very rare I expect   
                    // 5.19.17 Confirmed occurred without error!
                }
                return;
            }

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
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
        /// <remarks>Player can be self when the current owner is losing ownership and needs to 
        /// reassess their coverage of their [about to be former] item.</remarks>
        /// <remarks>4.22.17 Player can be an ally of current owner when the current owner is losing ownership and has each ally
        /// reassess their coverage of this item that is about to be owned by someone else.</remarks>
        /// <remarks>5.5.17 Changes in Item IntelCoverage generated by a broad player relationship change to/from Ally
        /// is handled entirely by PlayerAIMgrs.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        private void AssignIntelCoverage(Player player) {
            D.AssertNotNull(player);
            D.AssertNotEqual(TempGameValues.NoPlayer, player);
            D.Assert(!_debugControls.IsAllIntelCoverageComprehensive);

            IntelCoverage newCoverage;
            IDictionary<RangeCategory, IList<ISensorDetector>> rangeLookup;
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

            IntelCoverage resultingCoverage;
            if (_item.TrySetIntelCoverage(player, newCoverage, out resultingCoverage)) {
                //D.Log(ShowDebugLog, "{0} set {1}'s IntelCoverage to {2}.", DebugName, player, resultingCoverage.GetValueName());
            }
        }

        #region Event and Property Change Handlers

        // 8.3.16 Owner changing/changed now handled by Items

        #endregion

        /// <summary>
        /// Reassesses the player's IntelCoverage of this item based on current detection levels.
        /// <remarks>If IntelCoverage is changed as a result, this can affect <c>player</c>'s knowledge of the item.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        public void ResetBasedOnCurrentDetection(Player player) {
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // player must already know about this item to call this so can't lose knowledge once Comprehensive and aware of it
                return;
            }
            // by definition, the player must have knowledge of the item before reassessing that knowledge
            __ValidatePlayerKnowledgeOfItem(player);

            AssignIntelCoverage(player);
        }

        private void Cleanup() { }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        private void __ValidatePlayerIntelCoverageOfItemIsComprehensive(Player player) {
            if (_item.GetIntelCoverage(player) != IntelCoverage.Comprehensive) {
                D.Error("{0}: {1}'s IntelCoverage of {2} should be Comprehensive rather than {3}.", DebugName, player, _item.DebugName, _item.GetIntelCoverage(player));
            }
        }

        private void __ValidatePlayerKnowledgeOfItem(Player player) {
            if (_item is IStar || _item is IUniverseCenter) {
                // unnecessary check as all players have knowledge of these sensor detectable items
                return;
            }
            var playerAIMgr = _gameMgr.GetAIManagerFor(player);
            if (!playerAIMgr.HasKnowledgeOf(_item as IIntelItem_Ltd)) {
                string itemsKnownText = playerAIMgr.__GetItemsOwnedBy(_item.Owner).Select(item => item.DebugName).Concatenate();
                D.Error("{0}: {1} has no knowledge of {2}. IsOperational = {3}. Items known: {4}.", DebugName, player, _item.DebugName, _item.IsOperational, itemsKnownText);
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

        #endregion

    }
}

