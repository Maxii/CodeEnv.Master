// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RecurringDateMinder.cs
// Monitors the current date, calling the IRecurringDateMinderClient's HandleRecurringDateReached() callback
// when the dates determined by its RecurringClientDuration have been reached.// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Monitors the current date, calling the IRecurringDateMinderClient's HandleRecurringDateReached() callback
    /// when the dates determined by its RecurringClientDuration have been reached.
    /// <remarks>IMPROVE Possible improvement would be to replace the client callback with an Action
    /// to execute when the dates are reached. This is a bit more intuitive to follow and
    /// flexible to use but has the distinct downside of using Actions which create a lot of heap garbage,
    /// especially when closure is reqd.</remarks>
    /// </summary>
    public class RecurringDateMinder {

        public string DebugName { get { return GetType().Name; } }

        private IDictionary<GameDate, HashSet<DateMinderDuration>> _activeDurationsLookup = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<GameDate, HashSet<DateMinderDuration>> _durationsToAddLookup = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<GameDate, HashSet<DateMinderDuration>> _durationsToRemoveLookup = new Dictionary<GameDate, HashSet<DateMinderDuration>>();
        private IDictionary<DateMinderDuration, GameDate> _durationToDateLookup = new Dictionary<DateMinderDuration, GameDate>();

        // OPTIMIZE 3.17.17 remove and use _durationToDateLookup if Warnings in IsScheduledForRemoval never occur
        private HashSet<DateMinderDuration> _durationsScheduledForRemoval = new HashSet<DateMinderDuration>();
        private List<GameDate> _allDates = new List<GameDate>();

        public RecurringDateMinder() { }

        /// <summary>
        /// Check for IDateMinderClients of currentDate and inform them if currentDate
        /// is equal to or later than the date(s) they may have registered. If isDatesSkipped
        /// is <c>true</c>, then there can be older client dates present that need to be checked.
        /// This is because dates can be skipped when FPS drops below desired levels.
        /// </summary>
        /// <param name="currentDate">The current date.</param>
        /// <param name="isDatesSkipped">if set to <c>true</c> [dates skipped].</param>
        public void CheckForClients(GameDate currentDate, bool isDatesSkipped) {
            HashSet<DateMinderDuration> activeDurations;

            // Add before removing in case Remove was called after Add during period between CheckForClients calls
            if (_durationsToAddLookup.Count > Constants.Zero) {
                foreach (var date in _durationsToAddLookup.Keys) {
                    if (!_activeDurationsLookup.TryGetValue(date, out activeDurations)) {
                        activeDurations = GetEmptySet();
                        _activeDurationsLookup.Add(date, activeDurations);
                        //D.Log("{0}: {1} was added.", DebugName, date);
                    }
                    var durationsToAdd = _durationsToAddLookup[date];

                    foreach (var duration in durationsToAdd) {
                        //D.Log("{0} is adding {1} to {2}. Frame {3}. OtherDurationsPresent: {4}.", DebugName, duration, date, Time.frameCount, activeDurations.Concatenate());
                        bool isAdded = activeDurations.Add(duration);
                        D.Assert(isAdded);
                    }
                    RecycleSet(durationsToAdd);
                }
                _durationsToAddLookup.Clear();
            }

            // Remove
            if (_durationsToRemoveLookup.Count > Constants.Zero) {
                foreach (var date in _durationsToRemoveLookup.Keys) {
                    bool isDateFound = _activeDurationsLookup.TryGetValue(date, out activeDurations);
                    D.Assert(isDateFound, date.ToString());

                    var durationsToRemove = _durationsToRemoveLookup[date];
                    foreach (var durationToRemove in durationsToRemove) {
                        //D.Log("{0} is removing {1} from {2}. Frame {3}. OtherDurationsPresent: {4}.", DebugName, durationToRemove, date, Time.frameCount, activeDurations.Concatenate());
                        bool isRemoved = activeDurations.Remove(durationToRemove);
                        D.Assert(isRemoved, "{0} failed to find/remove {1} from {2}. Frame: {3}.".Inject(DebugName, durationToRemove, activeDurations.Concatenate(), Time.frameCount));
                    }
                    RecycleSet(durationsToRemove);

                    if (activeDurations.Count == Constants.Zero) {
                        bool isDateRemoved = _activeDurationsLookup.Remove(date);
                        D.Assert(isDateRemoved);
                        //D.Log("{0}: {1} was removed from _activeDurationsLookup. Frame {2}.", DebugName, date, Time.frameCount);
                        RecycleSet(activeDurations);
                    }
                }
                _durationsToRemoveLookup.Clear();
                _durationsScheduledForRemoval.Clear();
            }

            // Check for Clients
            if (_activeDurationsLookup.TryGetValue(currentDate, out activeDurations)) {
                D.Assert(!_durationsToRemoveLookup.ContainsKey(currentDate));    // removals just processed and client not yet informed

                var activeDurationsCopy = GetSetCopy(activeDurations);
                foreach (var duration in activeDurationsCopy) {
                    D.Assert(!IsScheduledForRemoval(duration)); // removals just processed and duration is unique to a date
                    InformClient(duration);
                    if (!IsScheduledForRemoval(duration)) {
                        UpdateDate(duration, currentDate);
                    }
                    else {
                        // scheduled for removal so will be removed the next time through
                        //D.Log("{0} is not updating {1}'s date because it is scheduled for removal.", DebugName, duration);
                    }
                }
                RecycleSet(activeDurationsCopy);

                if (!HasDurationsScheduledForRemoval(currentDate)) {
                    CleanupAfterClientsInformed(currentDate);
                }
                else {
                    //D.Log("{0} is deferring cleaning up date {1} as it has duration removals to process.", DebugName, currentDate);
                }
            }

            if (isDatesSkipped) {
                if (_activeDurationsLookup.Keys.Count > Constants.Zero) {
                    D.AssertEqual(Constants.Zero, _allDates.Count);
                    _allDates.AddRange(_activeDurationsLookup.Keys.Except(currentDate));    // currentDate won't always be removed above immediately
                    //D.Log("{0}: CurrentDate = {1}, Ordered dates before sort = {2}.", DebugName, currentDate, _allDates.Concatenate());
                    _allDates.Sort();
                    //D.Log("{0}: CurrentDate = {1}, Ordered dates after sort = {2}.", DebugName, currentDate, _allDates.Concatenate());
                    int olderDateCount = Constants.Zero;
                    foreach (var date in _allDates) {
                        if (date > currentDate) {
                            if (olderDateCount > Constants.Zero) {
                                //D.Log("{0} had to sort to look for dates older than {1} and found {2}.", DebugName, currentDate, olderDateCount);
                            }
                            break;
                        }
                        D.Assert(date < currentDate, "{0}: {1} !< {2}. Frame: {3}.".Inject(DebugName, date, currentDate, Time.frameCount));
                        olderDateCount++;

                        activeDurations = _activeDurationsLookup[date];

                        D.Assert(!HasDurationsScheduledForRemoval(date));   // removals just processed and client not yet informed

                        var activeDurationsCopy = GetSetCopy(activeDurations);
                        foreach (var duration in activeDurationsCopy) {
                            D.Assert(!IsScheduledForRemoval(duration)); // removals just processed and duration is unique to a date
                            InformClient(duration);
                            if (!IsScheduledForRemoval(duration)) {
                                UpdateDate(duration, date);
                            }
                            else {
                                // scheduled for removal so will be removed the next time through
                                //D.Log("{0} is not updating {1}'s date because it is scheduled for removal.", DebugName, duration);
                            }
                        }
                        RecycleSet(activeDurationsCopy);

                        if (!HasDurationsScheduledForRemoval(date)) {
                            CleanupAfterClientsInformed(date);
                        }
                        else {
                            //D.Log("{0} is deferring cleaning up date {1} as it has duration removals to process.", DebugName, date);
                        }
                    }
                    _allDates.Clear();
                }
            }
        }

        /// <summary>
        /// Informs the clients that own the active duration that the date
        /// specified by the duration has been reached.
        /// <remarks>Warning: Clients can immediately use Add or Remove as a result of being informed.</remarks>
        /// </summary>
        /// <param name="activeDuration">Active Duration.</param>
        private void InformClient(DateMinderDuration activeDuration) {
            D.Assert(!IsScheduledForRemoval(activeDuration));

            var client = activeDuration.Client;
            //D.Log("{0}: InformClient({1}). Date = {2}.", DebugName, clientDuration, date);
            client.HandleDateReached(activeDuration);
        }

        /// <summary>
        /// Updates the duration's date to a future date derived from the same duration, records it
        /// in _activeDurationsLookup[updatedDate] and _durationToDateLookup, and removes it from
        /// _activeDurationsLookup[oldDate].
        /// </summary>
        /// <param name="activeDuration">The active duration.</param>
        /// <param name="oldDate">The old date associated with this activeDuration.</param>
        private void UpdateDate(DateMinderDuration activeDuration, GameDate oldDate) {
            D.Assert(!IsScheduledForRemoval(activeDuration));

            GameDate updatedDate = new GameDate(activeDuration.Duration);

            HashSet<DateMinderDuration> activeDurations;
            if (!_activeDurationsLookup.TryGetValue(updatedDate, out activeDurations)) {
                activeDurations = GetEmptySet();
                _activeDurationsLookup.Add(updatedDate, activeDurations);
                //D.Log("{0}: {1} was added.", DebugName, updatedDate);
            }

            bool isAdded = activeDurations.Add(activeDuration);
            D.Assert(isAdded);

            _durationToDateLookup[activeDuration] = updatedDate;    // will throw exception if key not there

            // BIG AHA! Now remove the duration that was just updated with a new date. This is how the BUG occurred
            // where the old date duration hung around ready to be found and communicated to its client when it was no longer current.
            HashSet<DateMinderDuration> oldDateDurations = _activeDurationsLookup[oldDate];
            bool isRemoved = oldDateDurations.Remove(activeDuration);
            D.Assert(isRemoved);

            if (oldDateDurations.Count == Constants.Zero) {
                RecycleSet(oldDateDurations);
            }
            // oldDate will be removed from _activeDurationsLookup, if allowed, in CleanupAfterClientsInformed
        }

        /// <summary>
        /// Returns <c>true</c> if this duration is scheduled for removal, <c>false</c> otherwise.
        /// <remarks>OPTIMIZE use _durationToDateLookup if Assert in IsActivelyChecking confirms it.</remarks>
        /// <remarks>Warning: Using _durationToDateLookup only works if this method stays private, aka
        /// duration must already be in use inside Minder.</remarks>
        /// </summary>
        /// <param name="duration">Duration of the client.</param>
        private bool IsScheduledForRemoval(DateMinderDuration duration) {
            bool isScheduledForRemoval = _durationsScheduledForRemoval.Contains(duration);
            bool isScheduledForRemovalDerivedFromLookup = !_durationToDateLookup.ContainsKey(duration);
            if (isScheduledForRemoval != isScheduledForRemovalDerivedFromLookup) {
                D.Warn("{0}.IsScheduledForRemoval has inconsistent results. IsScheduledForRemoval = {1}, IsScheduledForRemovalDerivedFromLookup = {2}.",
                    DebugName, isScheduledForRemoval, isScheduledForRemovalDerivedFromLookup);
            }
            return isScheduledForRemoval;
        }

        /// <summary>
        /// Returns <c>true</c> if there are durations scheduled for removal that are assigned to this date,
        /// <c>false</c> otherwise.
        /// </summary>
        /// <param name="date">The date.</param>
        private bool HasDurationsScheduledForRemoval(GameDate date) {
            return _durationsToRemoveLookup.ContainsKey(date);
        }

        /// <summary>
        /// Cleanups the _activeDurationLookup after the clients of this date have been informed.
        /// </summary>
        /// <param name="date">The date.</param>
        private void CleanupAfterClientsInformed(GameDate date) {
            D.Assert(!HasDurationsScheduledForRemoval(date));
            D.AssertEqual(Constants.Zero, _activeDurationsLookup[date].Count);

            bool isRemoved = _activeDurationsLookup.Remove(date);
            D.Assert(isRemoved);
            //D.Log("{0}: {1} was removed from _activeDurationsLookup.", DebugName, date);
        }

        /// <summary>
        /// Returns <c>true</c> if the provided duration is actively being checked. A duration is actively being checked 
        /// if it is being checked or scheduled to be added to be checked, and not scheduled for removal. It is not actively
        /// being checked if it is not being checked and not scheduled to be added to be checked, or is scheduled for removal.
        /// <remarks>3.17.17 Added in an attempt to allow clients to use the same, unchanging instance of DateMinderDuration.
        /// Unfortunately, when a Remove/Add of the same instance occurs between checks, the instance that was Added after the
        /// instance that was Removed will still be removed and never be processed. This is because they are the same instance.</remarks>
        /// </summary>
        /// <param name="duration">The duration.</param>
        [System.Obsolete("See <remarks>")]
        public bool IsActivelyChecking(DateMinderDuration duration) {
            bool isInLookup = _durationToDateLookup.ContainsKey(duration);
            if (isInLookup) {
                D.Assert(!IsScheduledForRemoval(duration));
            }
            return isInLookup;
        }

        /// <summary>
        /// Adds the duration to the durations waiting for the future date generated by this duration to be reached.
        /// </summary>
        /// <param name="duration">The duration.</param>
        public void Add(DateMinderDuration duration) {
            D.AssertNotDefault(duration.Duration);

            GameDate date = new GameDate(duration.Duration);

            //D.Log("{0}: Scheduling addition of {1} for {2}. Frame {3}.", DebugName, duration, date, Time.frameCount);
            HashSet<DateMinderDuration> durationsToAdd;
            if (!_durationsToAddLookup.TryGetValue(date, out durationsToAdd)) {
                durationsToAdd = GetEmptySet();
                _durationsToAddLookup.Add(date, durationsToAdd);
            }
            bool isAdded = durationsToAdd.Add(duration);
            D.Assert(isAdded);

            _durationToDateLookup.Add(duration, date);
        }

        /// <summary>
        /// Removes the specified date.
        /// <remarks>Warning: Use only when removing a date before Client.HandleDateReached() is called as once
        /// the call is made, this DateMinder automatically removes the date.</remarks>
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="duration">The client that provided the date.</param>
        public void Remove(DateMinderDuration duration) {
            GameDate date = _durationToDateLookup[duration];

            //D.Log("{0}: Scheduling removal of {1} from {2}. Frame: {3}.", DebugName, duration, date, Time.frameCount);
            _durationToDateLookup.Remove(duration);

            HashSet<DateMinderDuration> durationsToRemove;
            if (!_durationsToRemoveLookup.TryGetValue(date, out durationsToRemove)) {
                durationsToRemove = GetEmptySet();
                _durationsToRemoveLookup.Add(date, durationsToRemove);
            }
            bool isAdded = durationsToRemove.Add(duration);
            D.Assert(isAdded);

            isAdded = _durationsScheduledForRemoval.Add(duration);
            D.Assert(isAdded);
        }

        public void Clear() {
            RecycleAllSets();
            __newSetsCreated = Constants.Zero;
            __recycledSetsUseCount = Constants.Zero;

            _activeDurationsLookup.Clear();
            _durationsToAddLookup.Clear();
            _durationsToRemoveLookup.Clear();
            _durationToDateLookup.Clear();
            _durationsScheduledForRemoval.Clear();
            _allDates.Clear();
        }

        private void RecycleAllSets() {
            foreach (var set in _activeDurationsLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _durationsToAddLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _durationsToRemoveLookup.Values) {
                RecycleSet(set);
            }
        }

        #region Recycle Sets System

        private Stack<HashSet<DateMinderDuration>> _reusableDurationSets = new Stack<HashSet<DateMinderDuration>>(100);

        private HashSet<DateMinderDuration> GetEmptySet() {
            if (_reusableDurationSets.Count == Constants.Zero) {
                __newSetsCreated++;
                return new HashSet<DateMinderDuration>();
            }
            __recycledSetsUseCount++;
            return _reusableDurationSets.Pop();
        }

        private HashSet<DateMinderDuration> GetSetCopy(HashSet<DateMinderDuration> set) {
            var copy = GetEmptySet();
            foreach (var member in set) {
                copy.Add(member);
            }
            return copy;
        }

        private void RecycleSet(HashSet<DateMinderDuration> set) {
            set.Clear();
            _reusableDurationSets.Push(set);
        }

        #endregion

        #region Debug

        private int __newSetsCreated;
        private int __recycledSetsUseCount;
        // max size/capacity of a HashSet is irrelevant as there is no constructor that sets capacity

        public void __ReportUsage() {
            Debug.LogFormat("{0} reuse statistics: {1} reuses of {2} Sets.", DebugName, __recycledSetsUseCount, __newSetsCreated);
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

