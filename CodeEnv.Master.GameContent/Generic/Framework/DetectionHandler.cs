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

#define DEBUG_LOG
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

        private IDictionary<Player, IDictionary<DistanceRange, IList<ICommandItem>>> _detectionLookup = new Dictionary<Player, IDictionary<DistanceRange, IList<ICommandItem>>>();
        private AIntelItemData _data;
        private IList<IDisposable> _subscribers;

        public DetectionHandler(AIntelItemData data) {
            _data = data;
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OnOwnerChanging));
            _subscribers.Add(_data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OnOwnerChanged));
        }

        public void OnDetection(ICommandItem cmdItem, DistanceRange sensorRange) {
            Player detectingPlayer = cmdItem.Owner;

            IDictionary<DistanceRange, IList<ICommandItem>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                rangeLookup = new Dictionary<DistanceRange, IList<ICommandItem>>();
                _detectionLookup.Add(detectingPlayer, rangeLookup);
            }

            IList<ICommandItem> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                cmds = new List<ICommandItem>();
                rangeLookup.Add(sensorRange, cmds);
            }
            D.Assert(!cmds.Contains(cmdItem), "{0} attempted to add duplicate {1}.".Inject(_data.FullName, cmdItem.FullName));
            cmds.Add(cmdItem);

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
        }

        public void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange) {
            Player detectingPlayer = cmdItem.Owner;

            IDictionary<DistanceRange, IList<ICommandItem>> rangeLookup;
            if (!_detectionLookup.TryGetValue(detectingPlayer, out rangeLookup)) {
                D.Error("{0} found no Sensor Range lookup. Player: {1}.", _data.FullName, detectingPlayer.LeaderName);
                return;
            }

            IList<ICommandItem> cmds;
            if (!rangeLookup.TryGetValue(sensorRange, out cmds)) {
                D.Error("{0} found no List of Commands. Player: {1}, SensorRange: {2}.", _data.FullName, detectingPlayer.LeaderName, sensorRange.GetName());
                return;
            }

            bool isRemoved = cmds.Remove(cmdItem);
            D.Assert(isRemoved, "{0} could not find {1} to remove.".Inject(_data.FullName, cmdItem.FullName));
            if (cmds.Count == Constants.Zero) {
                rangeLookup.Remove(sensorRange);
            }

            AssignDetectingPlayerIntelCoverage(detectingPlayer);
        }

        /// <summary>
        /// Assesses and assigns the Item's IntelCoverage for the provided <c>detectingPlayer</c>.
        /// </summary>
        /// <param name="detectingPlayer">The detecting player.</param>
        private void AssignDetectingPlayerIntelCoverage(Player detectingPlayer) {
            AssignIntelCoverage(detectingPlayer, _data.Owner);
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
            if (player == itemOwner) {
                newCoverage = IntelCoverage.Comprehensive;
            }
            else {
                IDictionary<DistanceRange, IList<ICommandItem>> rangeLookup;
                if (!_detectionLookup.TryGetValue(player, out rangeLookup)) {
                    D.Error("{0} found no Sensor Range lookup. Player: {1}.", _data.FullName, player.LeaderName);
                    return;
                }
                if (rangeLookup.ContainsKey(DistanceRange.Short)) {
                    newCoverage = IntelCoverage.Moderate;
                }
                else if (rangeLookup.ContainsKey(DistanceRange.Medium)) {
                    newCoverage = IntelCoverage.Minimal;
                }
                else if (rangeLookup.ContainsKey(DistanceRange.Long)) {
                    newCoverage = IntelCoverage.Aware;
                }
                else {
                    newCoverage = IntelCoverage.None;
                }
            }

            if (_data.TrySetIntelCoverage(player, newCoverage)) {
                D.Log("{0} successfully set {1}'s IntelCoverage to {2}.", _data.FullName, player.LeaderName, newCoverage.GetName());
            }
        }


        private void OnOwnerChanging(Player newOwner) {
            var outgoingOwner = _data.Owner;
            D.Assert(outgoingOwner != null);    // default should be NoPlayer rather than null
            if (outgoingOwner == TempGameValues.NoPlayer) {
                return; // NoPlayer is not a player in the game with IntelCoverage to track
            }
            AssignIntelCoverage(outgoingOwner, newOwner);
        }

        private void OnOwnerChanged() {
            var newOwner = _data.Owner;
            if (newOwner == TempGameValues.NoPlayer) {
                return; // NoPlayer is not a player in the game with IntelCoverage to track
            }
            AssignIntelCoverage(newOwner, newOwner);
        }

        private void Cleanup() {
            Unsubscribe();
            // other cleanup here including any tracking Gui2D elements
        }

        private void Unsubscribe() {
            _subscribers.ForAll(d => d.Dispose());
            _subscribers.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

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
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
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

