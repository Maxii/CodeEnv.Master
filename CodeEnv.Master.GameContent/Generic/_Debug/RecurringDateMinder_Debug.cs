// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RecurringDateMinder2.cs
// RecurringDateMinder version with all debugging additions while searching for gnarly bug.
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
    /// RecurringDateMinder version with all debugging additions while searching for gnarly bug.
    /// </summary>
    [Obsolete]
    public class RecurringDateMinder_Debug {

        public string DebugName { get { return GetType().Name; } }

        private IDictionary<GameDate, HashSet<DateMinderDuration>> _clientDurationsLookup = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<GameDate, HashSet<DateMinderDuration>> _datesAndClientDurationsToAdd = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<GameDate, HashSet<DateMinderDuration>> _datesAndClientDurationsToRemove = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<DateMinderDuration, GameDate> _dateLookup = new Dictionary<DateMinderDuration, GameDate>();

        private Stack<HashSet<DateMinderDuration>> _reusableClientDurationLists = new Stack<HashSet<DateMinderDuration>>(50);
        private List<GameDate> _allDates = new List<GameDate>();

        public RecurringDateMinder_Debug() { }

        private GameDate __lastCurrentDate = GameTime.GameStartDate;


        private HashSet<DateMinderDuration> __removedDurations = new HashSet<DateMinderDuration>();

        private HashSet<DateMinderDuration> __addedDurations = new HashSet<DateMinderDuration>();

        /// <summary>
        /// Check for IDateMinderClients of currentDate and inform them if currentDate
        /// is equal to or later than the date(s) they may have registered. If isDatesSkipped
        /// is <c>true</c>, then there can be older client dates present that need to be checked.
        /// This is because dates can be skipped when FPS drops below desired levels.
        /// </summary>
        /// <param name="currentDate">The current date.</param>
        /// <param name="isDatesSkipped">if set to <c>true</c> [dates skipped].</param>
        public void CheckForClients(GameDate currentDate, bool isDatesSkipped) {
            HashSet<DateMinderDuration> clientDurations;

            if (_clientDurationsLookup.TryGetValue(currentDate, out clientDurations)) {
                foreach (var clientDuration in clientDurations) {
                    D.Assert(!__removedDurations.Contains(clientDuration), "{0} from {1} in Frame {2}.".Inject(clientDuration, currentDate, Time.frameCount));
                }
            }

            clientDurations = null;
            // Add before removing in case Remove was called after Add during period between CheckForClients calls
            if (_datesAndClientDurationsToAdd.Count > Constants.Zero) {
                foreach (var date in _datesAndClientDurationsToAdd.Keys) {
                    if (!_clientDurationsLookup.TryGetValue(date, out clientDurations)) {
                        clientDurations = new HashSet<DateMinderDuration>();  //= GetEmptyList();
                        _clientDurationsLookup.Add(date, clientDurations);
                        //D.Log("{0}: {1} was added.", DebugName, date);
                    }
                    var clientDurationsToAdd = _datesAndClientDurationsToAdd[date];

                    foreach (var dToAdd in clientDurationsToAdd) {
                        __WarnIfPresentInOtherDates(dToAdd);
                        D.Log("{0} is adding {1} to {2}. Frame {3}. OtherDurationsPresent: {4}.", DebugName, dToAdd, date, Time.frameCount, clientDurations.Concatenate());
                        D.Assert(!__removedDurations.Contains(dToAdd), "{0} in Frame {1}.".Inject(dToAdd, Time.frameCount));
                        bool isAdded = clientDurations.Add(dToAdd);
                        D.Assert(isAdded);
                    }

                }
                _datesAndClientDurationsToAdd.Clear();
            }

            clientDurations = null;
            // Remove
            if (_datesAndClientDurationsToRemove.Count > Constants.Zero) {
                foreach (var date in _datesAndClientDurationsToRemove.Keys) {
                    bool isDateFound = _clientDurationsLookup.TryGetValue(date, out clientDurations);
                    D.Assert(isDateFound, date.ToString());

                    var clientsToRemove = _datesAndClientDurationsToRemove[date];
                    foreach (var clientToRemove in clientsToRemove) {
                        D.Log("{0} is removing {1} from {2}. Frame {3}. OtherDurationsPresent: {4}.", DebugName, clientToRemove, date, Time.frameCount, clientDurations.Concatenate());
                        bool isClientRemoved = clientDurations.Remove(clientToRemove);
                        D.Assert(isClientRemoved, "{0} failed to find/remove {1} from {2}. Frame: {3}.".Inject(DebugName, clientToRemove, clientDurations.Concatenate(), Time.frameCount));
                        ////isClientRemoved = _dateLookup.Remove(clientToRemove);
                        ////D.Assert(isClientRemoved);
                        D.Assert(!clientDurations.Contains(clientToRemove), clientToRemove.ToString());


                        D.Assert(!__removedDurations.Contains(clientToRemove), "{0} in Frame {1}.".Inject(clientToRemove, Time.frameCount));
                        bool isAdded = __removedDurations.Add(clientToRemove);
                        D.Assert(isAdded);
                    }
                    //RecycleList(clientsToRemove);

                    if (clientDurations.Count == Constants.Zero) {
                        bool isDateRemoved = _clientDurationsLookup.Remove(date);
                        D.Log("{0}: {1} was removed from _clientDurationsLookup. Frame {2}.", DebugName, date, Time.frameCount);
                        D.Assert(isDateRemoved);
                        //RecycleList(clientDurations);
                    }

                }

                //// if this passes and it fails after clear then clear is the problem
                // AHA! This failed right after it was removed above, almost certainly from another date. That means its present
                // in more than one date! How?
                clientDurations = null;
                if (_clientDurationsLookup.TryGetValue(currentDate, out clientDurations)) {
                    foreach (var clientDuration in clientDurations) {
                        D.Assert(!__removedDurations.Contains(clientDuration), "{0} from {1} in Frame {2}.".Inject(clientDuration, currentDate, Time.frameCount));
                    }
                }

                _datesAndClientDurationsToRemove.Clear();

                clientDurations = null;
                if (_clientDurationsLookup.TryGetValue(currentDate, out clientDurations)) {
                    foreach (var clientDuration in clientDurations) {
                        D.Assert(!__removedDurations.Contains(clientDuration), "{0} from {1} in Frame {2}.".Inject(clientDuration, currentDate, Time.frameCount));
                    }
                }
            }

            clientDurations = null;
            if (_clientDurationsLookup.TryGetValue(currentDate, out clientDurations)) {
                foreach (var clientDuration in clientDurations) {
                    D.Assert(!__removedDurations.Contains(clientDuration), "{0} from {1} in Frame {2}.".Inject(clientDuration, currentDate, Time.frameCount));
                }
            }


            clientDurations = null;
            D.Assert(__lastCurrentDate < currentDate, currentDate.ToString());

            // Check for Clients
            if (_clientDurationsLookup.TryGetValue(currentDate, out clientDurations)) {

                foreach (var clientDuration in clientDurations) {
                    D.Assert(!__removedDurations.Contains(clientDuration), "{0} from {1} in Frame {2}.".Inject(clientDuration, currentDate, Time.frameCount));
                }

                D.Assert(!_datesAndClientDurationsToRemove.ContainsKey(currentDate));   // removals just processed, how can it be until informed?
                HashSet<DateMinderDuration> clientDurationsCopy = new HashSet<DateMinderDuration>(clientDurations);
                foreach (var duration in clientDurationsCopy) {
                    D.Assert(!IsScheduledForRemoval(duration)); // removals just processed, how can it be until informed?
                    InformClient(duration);
                    if (!IsScheduledForRemoval(duration)) {
                        UpdateDate(duration, currentDate);
                        //// AHA! now remove the duration from its old date, aka currentDate. Not removing it was the bug
                        ////bool isRemoved = clientDurations.Remove(duration);
                        ////D.Assert(isRemoved);
                    }
                    else {
                        // if it is scheduled for removal, it will be removed the next time through
                        D.Log("{0} is not updating {1}'s date because it is scheduled for removal.", DebugName, duration);
                    }
                }

                if (!_datesAndClientDurationsToRemove.ContainsKey(currentDate)) {
                    CleanupAfterClientsInformed(currentDate);
                }
                else {
                    D.Log("{0} is deferring cleaning up date {1} as it has duration removals to process.", DebugName, currentDate);
                }
            }

            clientDurations = null;
            if (isDatesSkipped) {
                if (_clientDurationsLookup.Keys.Count > Constants.Zero) {
                    //var allDates = _clientDurationsLookup.Keys.ToList();
                    D.AssertEqual(Constants.Zero, _allDates.Count);
                    _allDates.AddRange(_clientDurationsLookup.Keys.Except(currentDate));    // currentDate won't always be removed above until next call
                    //D.Log("{0}: CurrentDate = {1}, Ordered dates before sort = {2}.", DebugName, currentDate, _allDates.Concatenate());
                    _allDates.Sort();
                    //D.Log("{0}: CurrentDate = {1}, Ordered dates after sort = {2}.", DebugName, currentDate, _allDates.Concatenate());
                    int olderDateCount = Constants.Zero;
                    foreach (var date in _allDates) {
                        if (date > currentDate) {  // date >= currentDate when cleaning up dates at bottom
                            if (olderDateCount > Constants.Zero) {
                                //D.Log("{0} had to sort to look for dates older than {1} and found {2}.", DebugName, currentDate, olderDateCount);
                            }
                            break;
                        }
                        D.Assert(date < currentDate, "{0}: {1} >= {2}. Frame: {3}.".Inject(DebugName, date, currentDate, Time.frameCount));
                        D.Assert(date >= __lastCurrentDate, "{0}: {1} < {2}. Frame: {3}.".Inject(DebugName, date, __lastCurrentDate, Time.frameCount));
                        olderDateCount++;

                        clientDurations = _clientDurationsLookup[date];

                        D.Assert(!_datesAndClientDurationsToRemove.ContainsKey(date));   // removals just processed, how can it be until informed?
                        HashSet<DateMinderDuration> clientDurationsCopy = new HashSet<DateMinderDuration>(clientDurations);
                        foreach (var duration in clientDurationsCopy) {
                            D.Assert(!IsScheduledForRemoval(duration)); // removals just processed, how can it be until informed?
                            InformClient(duration);
                            if (!IsScheduledForRemoval(duration)) {
                                UpdateDate(duration, date);
                                //// AHA! now remove the duration from its old date, aka date. Not removing it was the bug
                                ////bool isRemoved = clientDurations.Remove(duration);
                                ////D.Assert(isRemoved);
                            }
                            else {
                                // if it is scheduled for removal, it will be removed the next time through
                                D.Log("{0} is not updating {1}'s date because it is scheduled for removal.", DebugName, duration);
                            }
                        }

                        if (!_datesAndClientDurationsToRemove.ContainsKey(date)) {
                            CleanupAfterClientsInformed(date);
                        }
                        else {
                            D.Log("{0} is deferring cleaning up date {1} as it has duration removals to process.", DebugName, date);
                        }
                    }
                    _allDates.Clear();
                }
            }

            __lastCurrentDate = currentDate;
        }

        /// <summary>
        /// Informs the clients that own the recurring duration that the date
        /// specified by the duration has been reached.
        /// <remarks>Warning: Clients can immediately use Add or Remove as a result of being informed.</remarks>
        /// </summary>
        /// <param name="clientDurations">The client durations.</param>
        private void InformClient(DateMinderDuration clientDuration) { // TEMP date
            D.Assert(!__removedDurations.Contains(clientDuration), "{0} in Frame {1}.".Inject(clientDuration, Time.frameCount));
            D.Assert(!IsScheduledForRemoval(clientDuration));
            var client = clientDuration.Client;
            //D.Log("{0}: InformClient({1}). Date = {2}.", DebugName, clientDuration, date);
            client.HandleDateReached(clientDuration);
        }

        private void UpdateDate(DateMinderDuration clientDuration, GameDate oldDate) {

            D.Assert(!__removedDurations.Contains(clientDuration), "{0} in Frame {1}.".Inject(clientDuration, Time.frameCount));

            D.Assert(!IsScheduledForRemoval(clientDuration));

            GameDate updatedDate = new GameDate(clientDuration.Duration);
            D.Assert(updatedDate >= __lastCurrentDate, "{0}: {1} < {2}. Frame: {3}.".Inject(DebugName, updatedDate, __lastCurrentDate, Time.frameCount));

            HashSet<DateMinderDuration> updatedDateClientDurations;
            if (!_clientDurationsLookup.TryGetValue(updatedDate, out updatedDateClientDurations)) {
                updatedDateClientDurations = new HashSet<DateMinderDuration>();
                _clientDurationsLookup.Add(updatedDate, updatedDateClientDurations);
                //D.Log("{0}: {1} was added.", DebugName, updatedDate);
            }

            bool isAdded = updatedDateClientDurations.Add(clientDuration);
            D.Assert(isAdded);

            D.Assert(_dateLookup.ContainsKey(clientDuration), clientDuration.ToString());  // TEMP you've got to have a key to update it?
            _dateLookup[clientDuration] = updatedDate;

            // BIG AHA! Now remove the duration that was just updated with a new date from the old date it previously had. This is how
            // an old date hangs around ready to be found in the next pass when older dates are processed.

            HashSet<DateMinderDuration> oldDateClientDurations = _clientDurationsLookup[oldDate];
            bool isRemoved = oldDateClientDurations.Remove(clientDuration);
            D.Assert(isRemoved);
        }

        private void __WarnIfPresentInOtherDates(DateMinderDuration duration) {
            IList<GameDate> datesPresent = new List<GameDate>();
            var allDates = _clientDurationsLookup.Keys.ToList();
            foreach (var date in allDates) {
                var durations = _clientDurationsLookup[date];
                if (durations.Contains(duration)) {
                    datesPresent.Add(date);
                }
            }

            if (datesPresent.Any()) {
                D.Warn("{0}: {1} found in dates: {2}.", DebugName, duration, datesPresent.Concatenate());
            }
        }

        private bool IsScheduledForRemoval(DateMinderDuration clientDuration) {
            // Remove immediately removes the ClientDuration key after gaining the date associated with this duration
            bool isScheduledForRemoval = !_dateLookup.ContainsKey(clientDuration);

            bool isClientDurationFoundInRemovals = false;
            //if (isScheduledForRemoval) {
            foreach (var date in _datesAndClientDurationsToRemove.Keys) {
                var clientDurations = _datesAndClientDurationsToRemove[date];

                DateMinderDuration duplicateDuration;
                if (clientDurations.ContainsDuplicates(out duplicateDuration)) {
                    D.Error("{0}: IsScheduledForRemoval({1}) found a duplicate: {2}. Frame {3}.", DebugName, clientDuration, duplicateDuration, Time.frameCount);
                }

                if (clientDurations.Contains(clientDuration)) {
                    isClientDurationFoundInRemovals = true;
                    break;
                }
            }

            if (isScheduledForRemoval != isClientDurationFoundInRemovals) {
                // This inconsistency when not in lookup  and not found in removals means it was removed, but in a frame prior to this check
                D.Log("{0}: IsScheduledForRemoval({1}) has inconsistent results. Frame {2}.", DebugName, clientDuration, Time.frameCount);
                D.Log("IsScheduledForRemoval from Lookup = {0}, IsClientDurationFoundInRemovals = {1}.", isScheduledForRemoval, isClientDurationFoundInRemovals);
                isScheduledForRemoval = isClientDurationFoundInRemovals;
            }
            return isScheduledForRemoval;
        }

        private void CleanupAfterClientsInformed(GameDate date) {
            D.Assert(!_datesAndClientDurationsToRemove.ContainsKey(date));
            D.Assert(!_clientDurationsLookup[date].Any());
            bool isRemoved = _clientDurationsLookup.Remove(date);
            D.Assert(isRemoved);
            D.Log("{0}: {1} was removed from _clientDurationsLookup.", DebugName, date);
        }

        /// <summary>
        /// Adds the specified client to the list of clients waiting for this date to be reached.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="clientDurationToAdd">The client.</param>
        public void Add(DateMinderDuration clientDurationToAdd) {
            D.AssertNotDefault(clientDurationToAdd.Duration);

            bool isAdded = __addedDurations.Add(clientDurationToAdd);
            D.Assert(isAdded, "{0} attempting to add {1} that has already been added. Frame {2}.".Inject(DebugName, clientDurationToAdd, Time.frameCount));

            GameDate date = new GameDate(clientDurationToAdd.Duration);
            D.Assert(date >= __lastCurrentDate, "{0}: {1} < {2}. Frame: {3}.".Inject(DebugName, date, __lastCurrentDate, Time.frameCount));

            //D.Log("{0}: Scheduling addition of {1} for {2}. Frame {3}.", DebugName, clientDurationToAdd, date, Time.frameCount);
            HashSet<DateMinderDuration> clientDurationsToAdd;
            if (!_datesAndClientDurationsToAdd.TryGetValue(date, out clientDurationsToAdd)) {
                clientDurationsToAdd = new HashSet<DateMinderDuration>();
                _datesAndClientDurationsToAdd.Add(date, clientDurationsToAdd);
            }
            isAdded = clientDurationsToAdd.Add(clientDurationToAdd);
            D.Assert(isAdded);

            _dateLookup.Add(clientDurationToAdd, date);
        }

        /// <summary>
        /// Removes the specified date.
        /// <remarks>Warning: Use only when removing a date before Client.HandleDateReached() is called as once
        /// the call is made, this DateMinder automatically removes the date.</remarks>
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="clientDurationToRemove">The client that provided the date.</param>
        public void Remove(DateMinderDuration clientDurationToRemove) {
            GameDate date = _dateLookup[clientDurationToRemove];

            D.Assert(date >= __lastCurrentDate, "{0}: {1} < {2}. Frame: {3}.".Inject(DebugName, date, __lastCurrentDate, Time.frameCount));

            //D.Log("{0}: Scheduling removal of {1} from {2}. Frame: {3}.", DebugName, clientDurationToRemove, date, Time.frameCount);
            //// remove from dateLookup when removing in CheckForClients
            bool isRemoved = _dateLookup.Remove(clientDurationToRemove);
            D.Assert(isRemoved);    //OPTIMIZE not necessary

            HashSet<DateMinderDuration> clientDurationsToRemove;
            if (!_datesAndClientDurationsToRemove.TryGetValue(date, out clientDurationsToRemove)) {
                clientDurationsToRemove = new HashSet<DateMinderDuration>();  //= GetEmptyList();
                _datesAndClientDurationsToRemove.Add(date, clientDurationsToRemove);
            }
            bool isAdded = clientDurationsToRemove.Add(clientDurationToRemove);
            D.Assert(isAdded);
        }

        [Obsolete]
        private HashSet<DateMinderDuration> GetEmptyList() {

            if (_reusableClientDurationLists.Count == Constants.Zero) {
                __newListsCreated++;
                return new HashSet<DateMinderDuration>();
            }
            __recycledListsUseCount++;
            return _reusableClientDurationLists.Pop();
        }

        [Obsolete]
        private void RecycleList(HashSet<DateMinderDuration> list) { // IMPROVE keep list capacity under control
            list.Clear();
            _reusableClientDurationLists.Push(list);
        }

        #region Debug

        private int __newListsCreated;
        private int __recycledListsUseCount;
#pragma warning disable 0649
        private int __maxListSizeEncountered;
#pragma warning restore 0649

        public void __ReportUsage() {
            Debug.LogFormat(@"{0} List reuse statistics: {1} reuses of {2} total Lists created to meet peak demand. /n
                MaxListSizeEncountered = {3}.",
                DebugName, __recycledListsUseCount, __newListsCreated, __maxListSizeEncountered);
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

