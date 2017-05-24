﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DateMinder.cs
// Monitors the current date, calling the IDateMinderClient's DateReached() callback
// when the client's designated date has been reached.
// </summary> 
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
    /// Monitors the current date, calling the IDateMinderClient's DateReached() callback
    /// when the client's designated date has been reached.
    /// <remarks>IMPROVE Possible improvement would be to use a DateMinderTask holding the date
    /// and an Action to execute when the date is reached. This is a bit more intuitive to follow and
    /// flexible to use but has the distinct downside of instantiating a lot of DateMinderTasks
    /// containing Actions which create a lot of heap garbage, especially when closure is reqd.</remarks>
    /// </summary>
    public class DateMinder {

        public string DebugName { get { return GetType().Name; } }

        private IDictionary<GameDate, HashSet<IDateMinderClient>> _clientsLookup = new Dictionary<GameDate, HashSet<IDateMinderClient>>();
        private IDictionary<GameDate, HashSet<IDateMinderClient>> _clientsToAddLookup = new Dictionary<GameDate, HashSet<IDateMinderClient>>();
        private IDictionary<GameDate, HashSet<IDateMinderClient>> _clientsToRemoveLookup = new Dictionary<GameDate, HashSet<IDateMinderClient>>();

        /// <summary>
        /// The clients currently scheduled for removal the next time CheckForClients is called.
        /// <remarks>5.18.17 OPTIMIZE This is not the collection that determines what clients get removed. 
        /// That is handled by _clientsToRemoveLookup. It is principally used as a fast access check to 
        /// confirm that a particular client is scheduled for removal, accessed thru IsScheduledForRemoval(client).</remarks>
        /// </summary>
        private HashSet<IDateMinderClient> __clientsScheduledForRemoval = new HashSet<IDateMinderClient>();
        private List<GameDate> _allDates = new List<GameDate>(50);

        private GameTime _gameTime;

        public DateMinder(GameTime gameTime) {
            _gameTime = gameTime;
        }

        /// <summary>
        /// Check for IDateMinderClients of currentDate and inform them if currentDate
        /// is equal to or later than the date(s) they may have registered. If isDatesSkipped
        /// is <c>true</c>, then there can be older client dates present that need to be checked.
        /// This is because dates can be skipped when FPS drops below desired levels.
        /// </summary>
        /// <param name="currentDate">The current date.</param>
        /// <param name="isDatesSkipped">if set to <c>true</c> [dates skipped].</param>
        public void CheckForClients(GameDate currentDate, bool isDatesSkipped) {
            HashSet<IDateMinderClient> clients;

            // Add before removing in case Remove was called after Add during period between CheckForClients calls
            if (_clientsToAddLookup.Count > Constants.Zero) {
                foreach (var date in _clientsToAddLookup.Keys) {
                    if (!_clientsLookup.TryGetValue(date, out clients)) {
                        clients = GetEmptySet();
                        _clientsLookup.Add(date, clients);
                    }
                    var clientsToAdd = _clientsToAddLookup[date];
                    foreach (var client in clientsToAdd) {
                        clients.Add(client);
                    }
                    RecycleSet(clientsToAdd);
                }
                _clientsToAddLookup.Clear();
            }

            // Remove
            if (_clientsToRemoveLookup.Count > Constants.Zero) {
                foreach (var date in _clientsToRemoveLookup.Keys) {
                    bool isDateFound = _clientsLookup.TryGetValue(date, out clients);
                    D.Assert(isDateFound);

                    var clientsToRemove = _clientsToRemoveLookup[date];
                    foreach (var clientToRemove in clientsToRemove) {
                        bool isClientRemoved = clients.Remove(clientToRemove);
                        D.Assert(isClientRemoved, "{0} failed to find/remove {1} from {2}.".Inject(DebugName, clientToRemove, clients.Concatenate()));
                    }
                    RecycleSet(clientsToRemove);

                    if (clients.Count == Constants.Zero) {
                        bool isDateRemoved = _clientsLookup.Remove(date);
                        D.Assert(isDateRemoved);
                        RecycleSet(clients);
                    }
                }
                _clientsToRemoveLookup.Clear();
                __clientsScheduledForRemoval.Clear();
            }

            // Check for Clients
            if (_clientsLookup.TryGetValue(currentDate, out clients)) {
                InformClients(clients, currentDate);

                // OPTIMIZE While clients can be removed, informing clients of a date reached won't generate an immediate Remove(date, client) 
                // since dates have no value once reached and can't recur. They can generate an immediate Add(date, client).
                foreach (var client in clients) {
                    D.Assert(!__IsScheduledForRemoval(client));
                }
                D.Assert(!HasClientsScheduledForRemoval(currentDate));
                // Accordingly the set can be recycled.
                RecycleSet(clients);

                CleanupAfterClientsInformedOf(currentDate);
            }

            if (isDatesSkipped) {
                if (_clientsLookup.Keys.Count > Constants.Zero) {
                    D.AssertEqual(Constants.Zero, _allDates.Count); // unlike RecurringDurations, currentDate will always be removed by Cleanup
                    _allDates.AddRange(_clientsLookup.Keys);
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

                        clients = _clientsLookup[date];
                        InformClients(clients, date);

                        // OPTIMIZE While clients can be removed, informing clients of a date reached won't generate an immediate Remove(date, client) 
                        // since dates have no value once reached and can't recur. They can generate an immediate Add(date, client).
                        foreach (var client in clients) {
                            D.Assert(!__IsScheduledForRemoval(client));
                        }
                        D.Assert(!HasClientsScheduledForRemoval(date));
                        // Accordingly the set can be recycled.
                        RecycleSet(clients);

                        CleanupAfterClientsInformedOf(date);
                    }
                    _allDates.Clear();
                }
            }
        }

        /// <summary>
        /// Informs the clients that that the date they specified has been reached.
        /// <remarks>Warning: Clients can immediately use Add as a result of being informed.</remarks>
        /// </summary>
        /// <param name="clients">The clients.</param>
        /// <param name="date">The date.</param>
        private void InformClients(HashSet<IDateMinderClient> clients, GameDate date) {
            foreach (var client in clients) {
                client.HandleDateReached(date);
            }
        }

        /// <summary>
        /// Cleanups the _clientsLookup after the clients of this date have been informed of reaching it.
        /// </summary>
        /// <param name="date">The date.</param>
        private void CleanupAfterClientsInformedOf(GameDate date) {
            D.AssertEqual(Constants.Zero, _clientsLookup[date].Count);

            bool isRemoved = _clientsLookup.Remove(date);
            D.Assert(isRemoved);
            //D.Log("{0}: {1} was removed from _clientsLookup.", DebugName, date);
        }

        /// <summary>
        /// Adds the specified client to the list of clients waiting for this date to be reached.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="client">The client.</param>
        public void Add(GameDate date, IDateMinderClient client) {
            D.Assert(date > _gameTime.CurrentDate, "{0}: DateToAdd {1} <= CurrentDate {2}. Client: {3}.".Inject(DebugName, date, _gameTime.CurrentDate, client.DebugName));

            HashSet<IDateMinderClient> clients;
            if (!_clientsToAddLookup.TryGetValue(date, out clients)) {
                clients = GetEmptySet();
                _clientsToAddLookup.Add(date, clients);
            }
            bool isAdded = clients.Add(client);
            if (!isAdded) {
                // 3.17.17 So far, I am limited to 1 use per client due to the use of Sets. 
                // Scenario 1: Using Sets, the same client is already waiting to be added on the same date so there must be a Remove 
                // scheduled for this client and date, trying to remove the already existing client and date. For now, I can proceed
                // by getting rid of the scheduled Remove and ignoring this new Add. 
                //
                // 4.9.17 This occurred for a Unit's element's ActiveCMs reload process. CommenceOperations AssessedAlertStatus on startup
                // and found it needed to set YellowAlert. That started the reload process which added reload dates to DateMinder. 
                // During the same frame, the owner of the enemy that caused YellowAlert became known, firing OnRelationsChanged. 
                // That caused the RangeMonitor to re-categorize all detected items en-masse. That re-categorization removed, 
                // then re-added the reload dates all within the same frame, thus generating 65 warnings from here. I'm going to keep
                // the warning for now to get a sense of frequency, but it is warning about acceptable behaviour.
                // 5.7.17 This acceptable reload event now occurring more commonly since I added automatic relations changes.
                // Changing warning to log.
                // 5.18.17 Happening in bunches now when auto relations changes cause AlertStatus to change.

                // Scenario2: the same client gets an Add scheduled but for a different
                // dates, creating no immediate conflict here. There must be another Remove scheduled for the first date to allow the second
                // date to be added. This will work out when processed - the two adds will be included, then the remove will remove the first.
                D.Assert(HasClientsScheduledForRemoval(date));
                D.Assert(__IsScheduledForRemoval(client));
                bool isRemoved = _clientsToRemoveLookup[date].Remove(client);
                D.Assert(isRemoved);
                // no reason to remove the date if its clients is now empty as that will happen during CheckForClients
                //D.Log("{0} had to remove Client {1} from scheduled removal as the same client attempted a second Add on the same date.", DebugName, client.DebugName);
                // No need to remove and re-add client to _clientsToAddLookup as both date and client are the same
            }
        }

        /// <summary>
        /// Removes the specified date.
        /// <remarks>Warning: Use only when removing a date before Client.HandleDateReached() is called as once
        /// the call is made, this DateMinder automatically removes the date.</remarks>
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="client">The client that provided the date.</param>
        public void Remove(GameDate date, IDateMinderClient client) {
            if (date < _gameTime.CurrentDate) {
                // 5.15.17 Occurred with equal date when reload date was removed as a result of ownerChg due to attack by assaultShuttle.
                // AssaultShuttleSim is currently an atomic attack and result. Root start was DateMinder initiating firing sequence.
                // AHA! Since attack and result is atomic, DateMinder is still processing currentDate. This Removal is legal! When
                // attempting to InformClient of this removedData, it will find scheduled for removal and not InformClient. New criteria
                // must allow equal to currentDate as this could happen to ActiveCMs too which are atomic.
                D.Error("{0}: DateToRemove {1} <= CurrentDate {2}. Client: {3}.", DebugName, date, _gameTime.CurrentDate, client.DebugName);
            }
            HashSet<IDateMinderClient> clients;
            if (!_clientsToRemoveLookup.TryGetValue(date, out clients)) {
                clients = GetEmptySet();
                _clientsToRemoveLookup.Add(date, clients);
            }
            bool isAdded = clients.Add(client);
            D.Assert(isAdded);

            isAdded = __clientsScheduledForRemoval.Add(client);
            if (!isAdded) {
                // 5.17.17, 5.22.17 Source was ShieldGenerator KillReloadProcess. This is not an error as it can occur when 
                // the same client removes 2 separate dates between CheckForClients() calls. Not adding the second instance 
                // of client won't affect the role it plays as it is simply a fast access check for debugging.
                //D.Log("{0} was unable to add {1} for scheduled removal in Frame {2} because it is already there.", DebugName, client.DebugName, Time.frameCount);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if this client is scheduled for removal, <c>false</c> otherwise.
        /// </summary>
        /// <param name="client">The client.</param>
        private bool __IsScheduledForRemoval(IDateMinderClient client) {
            bool isScheduledForRemoval = __clientsScheduledForRemoval.Contains(client);
            return isScheduledForRemoval;
        }

        /// <summary>
        /// Returns <c>true</c> if there are clients scheduled for removal that are assigned to this date,
        /// <c>false</c> otherwise.
        /// </summary>
        /// <param name="date">The date.</param>
        private bool HasClientsScheduledForRemoval(GameDate date) {
            return _clientsToRemoveLookup.ContainsKey(date);
        }

        public void Clear() {
            RecycleAllSets();
            __newSetsCreated = Constants.Zero;
            __recycledSetsUseCount = Constants.Zero;

            _clientsLookup.Clear();
            _clientsToAddLookup.Clear();
            _clientsToRemoveLookup.Clear();
            __clientsScheduledForRemoval.Clear();
            _allDates.Clear();
        }

        private void RecycleAllSets() {
            foreach (var set in _clientsLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _clientsToAddLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _clientsToRemoveLookup.Values) {
                RecycleSet(set);
            }
        }

        #region Recycle Sets System

        private Stack<HashSet<IDateMinderClient>> _reusableClientSets = new Stack<HashSet<IDateMinderClient>>(100);

        private HashSet<IDateMinderClient> GetEmptySet() {
            if (_reusableClientSets.Count == Constants.Zero) {
                __newSetsCreated++;
                return new HashSet<IDateMinderClient>();
            }
            __recycledSetsUseCount++;
            return _reusableClientSets.Pop();
        }

        private void RecycleSet(HashSet<IDateMinderClient> set) {
            set.Clear();
            _reusableClientSets.Push(set);
        }

        #endregion

        #region Debug

        private int __newSetsCreated;
        private int __recycledSetsUseCount;
        // max size/capacity of a HashSet is irrelevant as there is no constructor that sets capacity.

        public void __ReportUsage() {
            Debug.LogFormat("{0} reuse statistics: {1} reuses of {2} Sets.", DebugName, __recycledSetsUseCount, __newSetsCreated);
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

    }
}

