// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerAIManager.cs
// The AI Manager for each player.
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
    using MoreLinq;
    using UnityEngine;

    /// <summary>
    /// The AI Manager for each player.
    /// </summary>
    public class PlayerAIManager : IDisposable {

        private const string DebugNameFormat = "{0}'s {1}";

        /// <summary>
        /// Occurs when this player's awareness of a fleet has changed.
        /// <remarks>Only fleets have an awareness change event as they are the only Cmd that can have their IntelCoverage regress to the point
        /// where a player is no longer aware of their existence.</remarks>
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of the fleet. Knowledge of a fleet's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwarenessOfFleetChangedEventArgs> awarenessOfFleetChanged;

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Owner.DebugName, GetType().Name);
                }
                return _debugName;
            }
        }

        public PlayerKnowledge Knowledge { get; private set; }

        public Player Owner { get; private set; }

        public IEnumerable<Player> OtherKnownPlayers { get { return Owner.OtherKnownPlayers; } }

        // TODO use these when needing to search for commands to take an action
        private IList<IUnitCmd> _availableCmds;
        private IList<IUnitCmd> _unavailableCmds;
        private bool _areAllPlayersDiscovered;

        private IGameManager _gameMgr;
        private IDebugControls _debugControls;

        public PlayerAIManager(Player owner, PlayerKnowledge knowledge) {
            Owner = owner;
            Knowledge = knowledge;
            InitializeValuesAndReferences();
            Subscribe();
        }

        private void InitializeValuesAndReferences() {
            _debugControls = References.DebugControls;
            _gameMgr = References.GameManager;
            _availableCmds = new List<IUnitCmd>();
            _unavailableCmds = new List<IUnitCmd>();
        }

        private void Subscribe() {
            SubscribeToPlayerRelationsChange(Owner);
        }

        private void SubscribeToPlayerRelationsChange(Player player) {
            player.relationsChanged += RelationsChangedEventHandler;
        }

        /// <summary>
        /// Indicates whether the PlayerAIMgr Owner has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IItem_Ltd item) {
            return Knowledge.HasKnowledgeOf(item);
        }

        #region Find Closest Item

        /// <summary>
        /// Tries to find the closest item of Type T owned by this player to <c>worldPosition</c>, if any. 
        /// Returns <c>true</c> if one was found, <c>false</c> otherwise.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="closestItem">The returned closest item. Null if returns <c>false</c>.</param>
        /// <param name="excludedItems">The items to exclude, if any.</param>
        /// <returns></returns>
        public bool TryFindMyClosestItem<T>(Vector3 worldPosition, out T closestItem, params T[] excludedItems) where T : IOwnerItem {
            Type tType = typeof(T);
            IEnumerable<T> itemCandidates = null;
            if (tType == typeof(IStarbaseCmd)) {
                itemCandidates = Knowledge.OwnerStarbases.Cast<T>();
            }
            else if (tType == typeof(ISettlementCmd)) {
                itemCandidates = Knowledge.OwnerSettlements.Cast<T>();
            }
            else if (tType == typeof(IUnitBaseCmd)) {
                itemCandidates = Knowledge.OwnerBases.Cast<T>();
            }
            else if (tType == typeof(IFleetCmd)) {
                itemCandidates = Knowledge.OwnerFleets.Cast<T>();
            }
            else if (tType == typeof(ISystem)) {
                itemCandidates = Knowledge.OwnerSystems.Cast<T>();
            }
            else if (tType == typeof(IPlanet)) {
                itemCandidates = Knowledge.OwnerPlanets.Cast<T>();
            }
            else if (tType == typeof(IMoon)) {
                itemCandidates = Knowledge.OwnerMoons.Cast<T>();
            }
            else if (tType == typeof(IPlanetoid)) {
                itemCandidates = Knowledge.OwnerPlanetoids.Cast<T>();
            }
            else if (tType == typeof(IStar)) {
                itemCandidates = Knowledge.OwnerStars.Cast<T>();
            }
            else {
                D.Error("Unanticipated Type {0}.", tType.Name);
            }

            itemCandidates = itemCandidates.Except(excludedItems);
            if (itemCandidates.Any()) {
                closestItem = itemCandidates.MinBy(cand => Vector3.SqrMagnitude(cand.Position - worldPosition));
                return true;
            }
            closestItem = default(T);
            return false;
        }

        /// <summary>
        /// Tries to find the closest known item of Type T to <c>worldPosition</c>, if any. 
        /// Returns <c>true</c> if one was found, <c>false</c> otherwise. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="closestItem">The returned closest item. Null if returns <c>false</c>.</param>
        /// <param name="excludedItems">The items to exclude, if any.</param>
        /// <returns></returns>
        public bool TryFindClosestKnownItem<T>(Vector3 worldPosition, out T closestItem, params T[] excludedItems) where T : IItem_Ltd {
            Type tType = typeof(T);
            IEnumerable<T> itemCandidates = null;
            if (tType == typeof(IStarbaseCmd_Ltd)) {
                itemCandidates = Knowledge.Starbases.Cast<T>();
            }
            else if (tType == typeof(ISettlementCmd_Ltd)) {
                itemCandidates = Knowledge.Settlements.Cast<T>();
            }
            else if (tType == typeof(IUnitBaseCmd_Ltd)) {
                itemCandidates = Knowledge.Bases.Cast<T>();
            }
            else if (tType == typeof(IFleetCmd_Ltd)) {
                itemCandidates = Knowledge.Fleets.Cast<T>();
            }
            else if (tType == typeof(ISystem_Ltd)) {
                itemCandidates = Knowledge.Systems.Cast<T>();
            }
            else if (tType == typeof(IPlanet_Ltd)) {
                itemCandidates = Knowledge.Planets.Cast<T>();
            }
            else if (tType == typeof(IMoon_Ltd)) {
                itemCandidates = Knowledge.Moons.Cast<T>();
            }
            else if (tType == typeof(IPlanetoid_Ltd)) {
                itemCandidates = Knowledge.Planetoids.Cast<T>();
            }
            else if (tType == typeof(IStar_Ltd)) {
                itemCandidates = Knowledge.Stars.Cast<T>();
            }
            else {
                D.Error("Unanticipated Type {0}.", tType.Name);
            }

            itemCandidates = itemCandidates.Except(excludedItems);
            if (itemCandidates.Any()) {
                closestItem = itemCandidates.MinBy(cand => Vector3.SqrMagnitude(cand.Position - worldPosition));
                return true;
            }
            closestItem = default(T);
            return false;
        }

        #endregion

        /// <summary>
        /// Assesses whether the [Owner] of this PlayerAIMgr is aware of this Item's existence. If already aware and [Owner] should
        /// lose awareness (IntelCoverage has regressed to None), the [Owner]'s Knowledge of the item is removed.
        /// If not already aware, the knowledge is added and [Owner] becomes aware. If aware of the item,
        /// the item is a Command, and [Owner] has not yet met the Command's owner, then [Owner] has just discovered
        /// a new player. If [Owner] has just gained or lost awareness of a FleetCmd due to an increase or reduction of 
        /// [Owner]'s IntelCoverage, this AIMgr will fire an awarenessOfFleetChgd event. Obviously, if fleet awareness is lost
        /// it won't be owned by [Owner].
        /// <remarks>Called whenever an item has had its IntelCoverage by [Owner] changed. Does nothing if already aware of
        /// the item without losing that awareness.</remarks>
        /// </summary>
        /// <param name = "item" > The item whose IntelCoverage by [Owner] has changed.</param>
        public void AssessAwarenessOf(IItem_Ltd item) {
            // TEMP
            IIntelItem intelItem = item as IIntelItem;
            IntelCoverage intelCoverage = intelItem.GetIntelCoverage(Owner);
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Each and every item should be set to Comprehensive by AIntelItem during FinalInitialization...
                D.AssertEqual(IntelCoverage.Comprehensive, intelCoverage, intelCoverage.GetValueName());
            }

            if (item is IStar_Ltd || item is ISystem_Ltd || item is IUniverseCenter_Ltd) {
                return; // these are added to knowledge at startup and never removed so no need to add again
            }

            // Note: Cleanup of Knowledge on item death handled by Knowledge

            var element = item as IUnitElement_Ltd;
            if (element != null) {
                var ship = element as IShip;
                if (ship != null) {
                    // Ships can regress IntelCoverage to None
                    // intelCoverage = ship.GetIntelCoverage(Owner);            // TEMP
                    if (intelCoverage == IntelCoverage.None) {
                        Knowledge.RemoveElement(element);
                        return;
                    }
                }
                Knowledge.AddElement(element);
            }
            else {
                var planetoid = item as IPlanetoid_Ltd;
                if (planetoid != null) {
                    Knowledge.AddPlanetoid(planetoid);
                }
                else {
                    var cmd = item as IUnitCmd_Ltd;
                    if (cmd != null) {
                        var fleetCmd = cmd as IFleetCmd_Ltd;
                        if (fleetCmd != null) {
                            // Fleets can regress IntelCoverage to None
                            // intelCoverage = fleetCmd.GetIntelCoverage(Owner);            // TEMP
                            if (intelCoverage == IntelCoverage.None) {
                                Knowledge.RemoveCommand(cmd);
                                OnAwarenessOfFleetChanged(fleetCmd, isAware: false);
                                return;
                            }
                        }
                        bool isNewlyAware = Knowledge.AddCommand(cmd);
                        if (fleetCmd != null && isNewlyAware) {
                            OnAwarenessOfFleetChanged(fleetCmd, isAware: true);
                        }
                        // Don't filter for Cmd to be newly aware as most newly discovered Cmds will be at LongRange with no access to Owner
                        if (!_areAllPlayersDiscovered) {
                            CheckForDiscoveryOfNewPlayer(cmd);
                        }
                    }
                    else {
                        D.Error("{0}: Unanticipated Type {1} attempting to add {2}.", DebugName, item.GetType().Name, item.DebugName);
                    }
                }
            }

            // TEMP
            if (intelCoverage == IntelCoverage.None) {
                D.Warn("{0} has added {1} with {2}.{3}!", DebugName, item.DebugName, typeof(IntelCoverage).Name, intelCoverage.GetValueName());
            }

            // TEMP and redundant
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // ... and they should also be known as a result of arriving here as Comprehensive and passing thru above
                if (!Knowledge.HasKnowledgeOf(item)) {
                    D.Error("{0} has no knowledge of {1}.", DebugName, item.DebugName);
                }
            }
        }

        private void CheckForDiscoveryOfNewPlayer(IUnitCmd_Ltd cmd) {
            D.Assert(!_areAllPlayersDiscovered);
            Player newlyDiscoveredPlayerCandidate;
            if (cmd.TryGetOwner(Owner, out newlyDiscoveredPlayerCandidate)) {
                if (newlyDiscoveredPlayerCandidate == Owner) {
                    // Note: The Cmd that generated this check is one of our own. 
                    return;
                }
                bool isAlreadyKnown = Owner.IsKnown(newlyDiscoveredPlayerCandidate);
                if (!isAlreadyKnown) {
                    Player newlyDiscoveredPlayer = newlyDiscoveredPlayerCandidate;
                    D.LogBold("{0} discovered new {1}.", DebugName, newlyDiscoveredPlayer);
                    SubscribeToPlayerRelationsChange(newlyDiscoveredPlayer);

                    Owner.HandleMetNewPlayer(newlyDiscoveredPlayer);
                    _areAllPlayersDiscovered = Owner.OtherKnownPlayers.Count() == _gameMgr.AllPlayers.Count - 1;
                }
            }
        }

        /// <summary>
        /// Makes Owner's provided UnitCommand available for orders whenever isAvailableChanged fires.
        /// Called in 2 scenarios: just before commencing operation and after gaining ownership.
        /// </summary>
        /// <param name="myUnitCmd">My unit command.</param>
        public void RegisterForOrders(IUnitCmd myUnitCmd) {
            D.AssertEqual(Owner, myUnitCmd.Owner);
            //D.Log("{0} is registering {1} as {2} for orders.", DebugName, myUnitCmd.DebugName, myUnitCmd.IsAvailable ? "available" : "unavailable");
            if (myUnitCmd.IsAvailable) {
                D.Assert(!_unavailableCmds.Contains(myUnitCmd));
                D.Assert(!_availableCmds.Contains(myUnitCmd));
                _availableCmds.Add(myUnitCmd);
            }
            else {
                D.Assert(!_availableCmds.Contains(myUnitCmd));
                D.Assert(!_unavailableCmds.Contains(myUnitCmd));
                _unavailableCmds.Add(myUnitCmd);
            }
            myUnitCmd.isAvailableChanged += MyCmdIsAvailableChgdEventHandler;
        }

        /// <summary>
        /// Deregisters Owner's provided UnitCommand from receiving orders whenever isAvailableChanged fires.
        /// Called in 2 scenarios: death and in process of losing ownership.
        /// </summary>
        /// <param name="myUnitCmd">My unit command.</param>
        public void DeregisterForOrders(IUnitCmd myUnitCmd) {
            D.AssertEqual(Owner, myUnitCmd.Owner);
            myUnitCmd.isAvailableChanged -= MyCmdIsAvailableChgdEventHandler;
            if (_availableCmds.Contains(myUnitCmd)) {
                _availableCmds.Remove(myUnitCmd);
            }
            else {
                bool isRemoved = _unavailableCmds.Remove(myUnitCmd);
                D.Assert(isRemoved);
            }
        }

        #region Event and Property Change Event Handlers

        private void OnAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
            if (awarenessOfFleetChanged != null) {
                awarenessOfFleetChanged(this, new AwarenessOfFleetChangedEventArgs(fleet, isAware));
            }
        }

        private void MyCmdIsAvailableChgdEventHandler(object sender, EventArgs e) {
            IUnitCmd myCmd = sender as IUnitCmd;
            HandleMyCmdIsAvailableChanged(myCmd);
        }

        private void HandleMyCmdIsAvailableChanged(IUnitCmd myCmd) {
            if (myCmd.IsAvailable) {
                bool isRemoved = _unavailableCmds.Remove(myCmd);
                D.Assert(isRemoved);
                _availableCmds.Add(myCmd);

                IFleetCmd fleetCmd = myCmd as IFleetCmd;
                if (fleetCmd != null) {
                    if (GameTime.Instance.CurrentDate == GameTime.GameStartDate) {
                        float hoursDelay = 1F;
                        //D.Log("{0} is issuing order to {1} with a {2} hour delay.", DebugName, fleetCmd.DebugName, hoursDelay);

                        // makes sure Owner's knowledge of universe has been constructed before selecting its target
                        string jobName = "{0}.WaitToIssueFirstOrderJob".Inject(DebugName);
                        References.JobManager.WaitForHours(hoursDelay, jobName, waitFinished: (jobWasKilled) => {
                            if (jobWasKilled) {
                                // No local reference to kill so JobManager is only source of kills (during scene transition)
                            }
                            else {
                                __IssueFleetOrder(fleetCmd);
                            }
                        });
                    }
                    else {
                        __AssessAndRecordUnitBaseVisit(fleetCmd);
                        //D.Log("{0} is issuing order to {1} with no delay.", DebugName, fleetCmd.DebugName);
                        __IssueFleetOrder(fleetCmd);
                    }
                }
            }
            else {
                bool isRemoved = _availableCmds.Remove(myCmd);
                D.Assert(isRemoved);
                _unavailableCmds.Add(myCmd);
            }
        }

        private void RelationsChangedEventHandler(object sender, RelationsChangedEventArgs e) {
            Player sendingPlayer = sender as Player;
            // Only send one of the (always) two events to our Cmds
            if (sendingPlayer == Owner) {
                HandleOwnerRelationsChanged(e.ChgdRelationsPlayer);
            }
            // TODO what about relations changes between other players that don't involve Owner?
        }

        private void HandleOwnerRelationsChanged(Player chgdRelationsPlayer) {
            D.AssertNotEqual(Owner, chgdRelationsPlayer);
            var priorRelationship = Owner.GetPriorRelations(chgdRelationsPlayer);
            var newRelationship = Owner.GetCurrentRelations(chgdRelationsPlayer);
            D.AssertNotEqual(priorRelationship, newRelationship);
            //D.Log("Relations have changed from {0} to {1} between {2} and {3}.", priorRelationship.GetValueName(), newRelationship.GetValueName(), Owner.LeaderName, chgdRelationsPlayer.LeaderName);
            if (priorRelationship == DiplomaticRelationship.Alliance) {
                HandleLostAllianceWith(chgdRelationsPlayer);
            }
            else if (newRelationship == DiplomaticRelationship.Alliance) {
                HandleGainedAllianceWith(chgdRelationsPlayer);
            }
            Knowledge.OwnerCommands.ForAll(myCmd => myCmd.HandleRelationsChanged(chgdRelationsPlayer));
            // Note: 7.15.16 Cmds currently propagate this to Elements and RangeMonitors
        }

        #endregion

        private void HandleGainedAllianceWith(Player ally) {
            D.Assert(Owner.IsRelationshipWith(ally, DiplomaticRelationship.Alliance));

            var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
            var allyItems = allyAIMgr.Knowledge.OwnerItems;
            allyItems.ForAll(allyItem => {
                D.Assert(!(allyItem is IUniverseCenter));
                //D.Log("{0} is adding Ally {1}'s item {2} to knowledge with IntelCoverage = Comprehensive.", DebugName, ally, allyItem.DebugName);
                ChangeIntelCoverageToComprehensive(allyItem);
            });
        }

        private void HandleLostAllianceWith(Player formerAlly) {
            D.Assert(Owner.IsPriorRelationshipWith(formerAlly, DiplomaticRelationship.Alliance));

            var formerAllyOwnedItems = Knowledge.GetItemsOwnedBy(formerAlly);
            var formerAllySensorDetectableOwnedItems = formerAllyOwnedItems.Where(item => item is ISensorDetectable).Cast<ISensorDetectable>();
            formerAllySensorDetectableOwnedItems.ForAll(sdItem => sdItem.ResetBasedOnCurrentDetection(Owner));
        }

        /// <summary>
        /// Changes the IntelCoverage of the provided item to Comprehensive.
        /// <remarks>This change will also result in Knowledge becoming aware of the item if it isn't already.</remarks>
        /// </summary>
        /// <param name="item">The item.</param>
        private void ChangeIntelCoverageToComprehensive(IOwnerItem item) {
            if (item is ISystem || item is IUnitCmd) {
                // These will auto change to Comprehensive when their members do 
                return;
            }

            D.Assert(!(item is ISector));  // UNCLEAR Sector IntelCoverage role not yet determined

            var element = item as IUnitElement;
            if (element != null) {
                element.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
            }
            else {
                var planetoid = item as IPlanetoid;
                if (planetoid != null) {
                    planetoid.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                }
                else {
                    var star = item as IStar;
                    D.AssertNotNull(star);
                    star.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                }
            }
        }

        private void Cleanup() {
            Unsubscribe();
            Knowledge.Dispose();
        }

        private void Unsubscribe() {
            Owner.relationsChanged -= RelationsChangedEventHandler;
            OtherKnownPlayers.ForAll(player => player.relationsChanged -= RelationsChangedEventHandler);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        /// <summary>
        /// Debug. Returns the items that Owner knows about that are owned by player.
        /// No owner access restrictions.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<IOwnerItem> __GetItemsOwnedBy(Player player) {
            return Knowledge.__GetItemsOwnedBy(player);
        }

        #region Debug Issue Fleet Orders

        private IList<IUnitBaseCmd> __basesVisited = new List<IUnitBaseCmd>();
        private bool __isUniverseFullyExplored = false; // avoids duplicate logging
        private bool __areAllBasesVisited = false;      // avoids duplicate logging
        private bool __areAllTargetsAttacked = false;

        private void __AssessAndRecordUnitBaseVisit(IFleetCmd fleetCmd) {
            if (fleetCmd.IsCurrentOrderDirectiveAnyOf(FleetDirective.Move, FleetDirective.FullSpeedMove)) {
                var unitBaseTgt = fleetCmd.CurrentOrder.Target as IUnitBaseCmd;
                if (unitBaseTgt != null) {
                    if (__basesVisited.Contains(unitBaseTgt)) {
                        D.Log("{0}: {1} just completed visiting {2} which was previously visited.", DebugName, fleetCmd.DebugName, unitBaseTgt.DebugName);
                    }
                    else {
                        __basesVisited.Add(unitBaseTgt);
                    }
                }
            }
        }

        private int __myActiveFleetCount;
        private IList<IFleetCmd> __myAttackingFleets;

        private void __MyAttackingFleetDeathEventHandler(object sender, EventArgs e) {
            __myAttackingFleets.Remove(sender as IFleetCmd);
        }

        /// <summary>
        /// Issues an order to the fleet.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        private void __IssueFleetOrder(IFleetCmd fleetCmd) {
            if (References.DebugControls.FleetsAutoAttackAsDefault) {
                if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
                    return;
                }

                if (__myAttackingFleets == null) {
                    __myAttackingFleets = new List<IFleetCmd>();
                }

                __myActiveFleetCount = Knowledge.OwnerFleets.Count();

                if (__myAttackingFleets.Count < Mathf.CeilToInt(__myActiveFleetCount / 2F)) {
                    // room to assign another fleet to attack
                    if (__IssueFleetAttackOrder(fleetCmd, findFarthestTgt: false)) {
                        if (!__myAttackingFleets.Contains(fleetCmd)) {
                            __myAttackingFleets.Add(fleetCmd);
                            fleetCmd.deathOneShot += __MyAttackingFleetDeathEventHandler;
                        }
                        return;
                    }
                    else {
                        // no target detected to attack
                        if (__myAttackingFleets.Contains(fleetCmd)) {
                            __myAttackingFleets.Remove(fleetCmd);
                            fleetCmd.deathOneShot -= __MyAttackingFleetDeathEventHandler;
                        }

                        __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
                        if (__isUniverseFullyExplored) {
                            // couldn't find target to attack and universe is fully explored so all targets have been destroyed
                            __areAllTargetsAttacked = true;
                        }
                        else {
                            D.Log("{0}: Fleet {1} can't find an attack target so will explore another part of universe.", DebugName, fleetCmd.DebugName);
                        }
                    }
                }
                else {
                    // no room to assign more fleets so assign to explore
                    if (__myAttackingFleets.Contains(fleetCmd)) {
                        __myAttackingFleets.Remove(fleetCmd);
                        fleetCmd.deathOneShot -= __MyAttackingFleetDeathEventHandler;
                    }

                    __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
                    if (__isUniverseFullyExplored) {
                        // no room for more attacks and universe is fully explored so all targets that can be attacked have been
                        __areAllTargetsAttacked = true;
                    }
                    else {
                        D.Log("{0}: Fleet {1} is not allowed to attack so will explore another part of universe.", DebugName, fleetCmd.DebugName);
                    }
                }

                if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
                    D.LogBold("{0}: Fleet {1} has run out of attack targets and unexplored explorable destinations in universe.", DebugName, fleetCmd.DebugName);
                }
            }
            else {  // FleetsAutoAttackAsDefault precludes all other orders
                var debugFleetCreator = fleetCmd.transform.GetComponentInParent<IDebugFleetCreator>();
                if (debugFleetCreator != null) {
                    FleetCreatorEditorSettings editorSettings = debugFleetCreator.EditorSettings as FleetCreatorEditorSettings;
                    if (__IssueFleetOrderSpecifiedByCreator(fleetCmd, editorSettings)) {
                        return;
                    }
                }

                if (References.DebugControls.FleetsAutoExploreAsDefault) {
                    if (__isUniverseFullyExplored && __areAllBasesVisited) {
                        return;
                    }

                    if (!__isUniverseFullyExplored) {
                        bool tryExplore = RandomExtended.Chance(0.75F);
                        if (tryExplore) {
                            __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
                            if (__isUniverseFullyExplored && !__areAllBasesVisited) {
                                __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
                            };
                        }
                        else {
                            __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
                            if (__areAllBasesVisited) {
                                __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
                            }
                        }
                    }
                    else {
                        if (!__areAllBasesVisited) {
                            __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
                        }
                    }
                    if (__isUniverseFullyExplored && __areAllBasesVisited) {
                        D.LogBold("{0}: Fleet {1} has run out of unexplored explorable or unvisited visitable destinations in universe.", DebugName, fleetCmd.DebugName);
                    }
                }
            }
        }
        //private void __IssueFleetOrder(IFleetCmd fleetCmd) {
        //    if (References.DebugControls.FleetsAutoAttackAsDefault) {
        //        if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
        //            return;
        //        }

        //        if (__IssueFleetAttackOrder(fleetCmd, findFarthestTgt: false)) {
        //            return;
        //        }
        //        else {
        //            // no target detected to attack
        //            __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
        //            if (__isUniverseFullyExplored) {
        //                // couldn't find target to attack and universe is fully explored so all targets have been destroyed
        //                __areAllTargetsAttacked = true;
        //            }
        //            else {
        //                //D.Log("{0}: Fleet {1} can't find an attack target so will explore another part of universe.", DebugName, fleetCmd.DebugName);
        //            }
        //        }
        //        if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
        //            D.LogBold("{0}: Fleet {1} has run out of attack targets and unexplored explorable destinations in universe.", DebugName, fleetCmd.DebugName);
        //        }
        //    }
        //    else {  // FleetsAutoAttackAsDefault precludes all other orders
        //        var debugFleetCreator = fleetCmd.transform.GetComponentInParent<IDebugFleetCreator>();
        //        if (debugFleetCreator != null) {
        //            FleetCreatorEditorSettings editorSettings = debugFleetCreator.EditorSettings as FleetCreatorEditorSettings;
        //            if (__IssueFleetOrderSpecifiedByCreator(fleetCmd, editorSettings)) {
        //                return;
        //            }
        //        }

        //        if (References.DebugControls.FleetsAutoExploreAsDefault) {
        //            if (__isUniverseFullyExplored && __areAllBasesVisited) {
        //                return;
        //            }

        //            if (!__isUniverseFullyExplored) {
        //                bool tryExplore = RandomExtended.Chance(0.75F);
        //                if (tryExplore) {
        //                    __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
        //                    if (__isUniverseFullyExplored && !__areAllBasesVisited) {
        //                        __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
        //                    };
        //                }
        //                else {
        //                    __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
        //                    if (__areAllBasesVisited) {
        //                        __isUniverseFullyExplored = !__IssueFleetExploreOrder(fleetCmd);
        //                    }
        //                }
        //            }
        //            else {
        //                if (!__areAllBasesVisited) {
        //                    __areAllBasesVisited = !__IssueFleetBaseMoveOrder(fleetCmd);
        //                }
        //            }
        //            if (__isUniverseFullyExplored && __areAllBasesVisited) {
        //                D.LogBold("{0}: Fleet {1} has run out of unexplored explorable or unvisited visitable destinations in universe.", DebugName, fleetCmd.DebugName);
        //            }
        //        }
        //    }
        //}


        /// <summary>
        /// Tries to issue the fleet order specified by the DebugUnitCreator. Returns
        /// <c>true</c> if the order was issued, <c>false</c> otherwise.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="editorSettings">The editor settings.</param>
        /// <returns></returns>
        private bool __IssueFleetOrderSpecifiedByCreator(IFleetCmd fleetCmd, FleetCreatorEditorSettings editorSettings) {
            bool isOrderIssued = false;
            if (editorSettings.Move) {
                if (editorSettings.Attack) {
                    isOrderIssued = __IssueFleetAttackOrder(fleetCmd, editorSettings.FindFarthest);
                    D.LogBold(!isOrderIssued, "{0}: {1} can find no WarAttackTargets of any sort.", DebugName, fleetCmd.DebugName);
                }
                else {
                    isOrderIssued = __IssueFleetMoveOrder(fleetCmd, editorSettings.FindFarthest);
                    D.LogBold(!isOrderIssued, "{0}: {1} can find no MoveTargets that meet the selection criteria.", DebugName, fleetCmd.DebugName);
                }
            }
            return isOrderIssued;
        }

        /// <summary>
        /// Tries to issue a fleet attack order. Returns
        /// <c>true</c> if the order was issued, <c>false</c> otherwise.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="findFarthestTgt">if set to <c>true</c> [find farthest target].</param>
        /// <returns></returns>
        private bool __IssueFleetAttackOrder(IFleetCmd fleetCmd, bool findFarthestTgt) {
            List<IUnitAttackable> attackTgts = Knowledge.Fleets.Cast<IUnitAttackable>().Where(f => f.IsWarAttackByAllowed(Owner)).ToList();
            attackTgts.AddRange(Knowledge.Starbases.Cast<IUnitAttackable>().Where(sb => sb.IsWarAttackByAllowed(Owner)));
            attackTgts.AddRange(Knowledge.Settlements.Cast<IUnitAttackable>().Where(s => s.IsWarAttackByAllowed(Owner)));
            ////attackTgts.AddRange(Knowledge.Planets.Cast<IUnitAttackable>().Where(p => p.IsWarAttackByAllowed(Owner)));
            if (!attackTgts.Any()) {
                return false;
            }
            IUnitAttackable attackTgt;
            if (findFarthestTgt) {
                attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - fleetCmd.Position));
            }
            else {
                attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - fleetCmd.Position));
            }
            D.Log("{0} is issuing an attack order against {1}.", fleetCmd.DebugName, attackTgt.DebugName);
            fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Attack, OrderSource.CmdStaff, attackTgt);
            return true;
        }

        /// <summary>
        /// Tries to issue a fleet move order. Returns
        /// <c>true</c> if the order was issued, <c>false</c> otherwise.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="findFarthestTgt">if set to <c>true</c> [find farthest target].</param>
        /// <returns></returns>
        private bool __IssueFleetMoveOrder(IFleetCmd fleetCmd, bool findFarthestTgt) {
            List<IFleetNavigable> moveTgts = Knowledge.Starbases.Cast<IFleetNavigable>().ToList();
            moveTgts.AddRange(Knowledge.Settlements.Cast<IFleetNavigable>());
            moveTgts.AddRange(Knowledge.Planets.Cast<IFleetNavigable>());
            ////moveTgts.AddRange(Knowledge.Systems.Cast<IFleetNavigable>());
            moveTgts.AddRange(Knowledge.Stars.Cast<IFleetNavigable>());
            if (Knowledge.UniverseCenter != null) {
                moveTgts.Add(Knowledge.UniverseCenter as IFleetNavigable);
            }

            if (!moveTgts.Any()) {
                return false;
            }
            IFleetNavigable destination;
            if (findFarthestTgt) {
                destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - fleetCmd.Position));
            }
            else {
                destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - fleetCmd.Position));
            }
            D.Log("{0} is issuing {1} a move order to {2}.", DebugName, fleetCmd.DebugName, destination.DebugName);
            fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
            return true;
        }

        private bool __IssueFleetExploreOrder(IFleetCmd fleetCmd) {
            bool isExploreOrderIssued = true;
            var explorableUnexploredSystems =
                from sys in Knowledge.Systems
                let eSys = sys as IFleetExplorable
                where eSys.IsExploringAllowedBy(Owner) && !eSys.IsFullyExploredBy(Owner)
                select eSys;
            if (explorableUnexploredSystems.Any()) {
                var closestUnexploredSystem = explorableUnexploredSystems.MinBy(sys => Vector3.SqrMagnitude(fleetCmd.Position - sys.Position));
                D.Log("{0} is issuing {1} an explore order to {2}.", DebugName, fleetCmd.DebugName, closestUnexploredSystem.DebugName);
                fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Explore, OrderSource.CmdStaff, closestUnexploredSystem);
            }
            else {
                IFleetExplorable uCenter = Knowledge.UniverseCenter as IFleetExplorable;
                if (!uCenter.IsFullyExploredBy(Owner)) {
                    fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Explore, OrderSource.CmdStaff, uCenter);
                }
                else {
                    D.Log("{0}: Fleet {1} has completed {2}'s exploration of explorable universe.", DebugName, fleetCmd.DebugName, Owner.DebugName);
                    isExploreOrderIssued = false;
                }
            }
            return isExploreOrderIssued;
        }

        private bool __IssueFleetBaseMoveOrder(IFleetCmd fleetCmd) {
            bool isMoveOrderIssued = true;
            var visitableUnvisitedBases =
                from unitBase in Knowledge.Bases
                let ownerAssessibleUnitBase = unitBase as IUnitBaseCmd
                // OK to know owner as can always move to a base. Just don't want to get fired on
                where !ownerAssessibleUnitBase.Owner.IsAtWarWith(Owner) && !__basesVisited.Contains(ownerAssessibleUnitBase)
                select ownerAssessibleUnitBase;
            if (visitableUnvisitedBases.Any()) {
                var closestVisitableBase = visitableUnvisitedBases.MinBy(vBase => Vector3.SqrMagnitude(fleetCmd.Position - vBase.Position));
                D.Log("{0} is issuing {1} a move order to {2}.", DebugName, fleetCmd.DebugName, closestVisitableBase.DebugName);
                fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, closestVisitableBase as IFleetNavigable);
            }
            else {
                D.Log("{0}: Fleet {1} has completed {2}'s visits to all visitable unvisited bases in known universe.", DebugName, fleetCmd.DebugName, Owner.DebugName);
                isMoveOrderIssued = false;
            }
            return isMoveOrderIssued;
        }

        #endregion

        #endregion

        #region Obsolete Archive

        /// <summary>
        /// Handles the change of an item's owner to an ally of this AIMgr's owner. This can be called in 2 scenarios:
        /// 1) An existing ally of the owner of this AIMgr just created a new item, and 
        /// 2) an existing item had its owner changed to one of this AIMgr's allies.
        /// </summary>
        /// <param name="allyOwnedItem">The ally owned item.</param>
        [Obsolete]
        public void HandleChgdItemOwnerIsAlly(IOwnerItem allyOwnedItem) {
            D.Assert(Owner.IsRelationshipWith(allyOwnedItem.Owner, DiplomaticRelationship.Alliance));
            D.Assert(!(allyOwnedItem is IUniverseCenter));

            ChangeIntelCoverageToComprehensiveAndPopulateKnowledge(allyOwnedItem);
        }

        /// <summary>
        /// Handles the change of an item's owner to this AIMgr's owner. This can be called in 2 scenarios:
        /// 1) The owner of this AIMgr just created a new item, and 
        /// 2) an existing item had its owner changed to the owner of this AIMgr.
        /// </summary>
        /// <param name="myOwnedItem">My owned item.</param>
        [Obsolete]
        public void HandleGainedItemOwnership(IOwnerItem myOwnedItem) {
            //D.Log("{0}.HandleGainedItemOwnership({1}) called.", DebugName, myOwnedItem.DebugName);
            D.AssertEqual(Owner, myOwnedItem.Owner);
            D.Assert(!(myOwnedItem is IUniverseCenter));

            ChangeIntelCoverageToComprehensiveAndPopulateKnowledge(myOwnedItem);
        }

        /// <summary>
        /// Handles the changing of an item's owner from this AIMgr's owner. This is called in 1 scenario:
        /// 1) An existing item is in the process of having its owner changed from the owner of this AIMgr.
        /// Warning: Called prior to the actual change of the owner.
        /// </summary>
        /// <param name="losingOwnedItem">The losing owned item.</param>
        [Obsolete]
        public void HandleLosingItemOwnership(IOwnerItem losingOwnedItem) {
            D.AssertEqual(Owner, losingOwnedItem.Owner);
            D.Assert(!(losingOwnedItem is IUniverseCenter));

            // Items that are losing their owner call Item.DetectionHandler.ResetBasedOnCurrentDetection() to re-determine the
            // (soon to be) former owner's intel coverage (and if appropriate, depopulate knowledge)
        }

        [Obsolete]
        private void ChangeIntelCoverageToComprehensiveAndPopulateKnowledge(IOwnerItem item) {
            if (item is ISystem || item is IUnitCmd) {
                // These will auto change to Comprehensive when their members do 
                // and they auto populate knowledge based on their members populating it
                return;
            }

            D.Assert(!(item is ISector));  // UNCLEAR how sectors interact with knowledge not yet determined

            var element = item as IUnitElement;
            if (element != null) {
                element.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                bool isAdded = Knowledge.AddElement(element as IUnitElement_Ltd);
                //D.Log(!isAdded, "{0} tried to add {1} it already has.", DebugName, element.DebugName);
            }
            else {
                var planetoid = item as IPlanetoid;
                if (planetoid != null) {
                    planetoid.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                    bool isAdded = Knowledge.AddPlanetoid(planetoid as IPlanetoid_Ltd);
                    //D.Log(!isAdded, "{0} tried to add {1} it already has.", DebugName, planetoid.DebugName);
                }
                else {
                    var star = item as IStar;
                    D.AssertNotNull(star);
                    star.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                    // don't need to add to knowledge as all stars are already known
                }
            }
        }

        /// <summary>
        /// Called whenever an operational ISensorDetectable Item is detected by <c>Owner</c>, no matter how
        /// many times the item has been detected previously. Ignores items that Owner already has knowledge of.
        /// <remarks>The only ISensorDetectables that really get handled here are Elements and Planetoids as
        /// Stars and the UniverseCenter are already known to all players.</remarks>
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        [Obsolete]
        public void HandleItemDetection(ISensorDetectable detectedItem) {
            if (detectedItem is IStar_Ltd || detectedItem is IUniverseCenter_Ltd) {
                return; // these are added at startup and never removed so no need to add again
            }
            D.Assert(detectedItem.IsOperational, detectedItem.DebugName);

            var element = detectedItem as IUnitElement_Ltd;
            if (element != null) {
                Knowledge.AddElement(element);
                if (element.IsHQ) {
                    // 7.14.16 Eliminated rqmt to be newly discovered element as most newly discovered elements will be at LongRange with no access to Owner
                    CheckForDiscoveryOfNewPlayer(element);
                }
            }
            else {
                var planetoid = detectedItem as IPlanetoid_Ltd;
                if (planetoid == null) {
                    D.Error("{0}: Unanticipated Type {1} attempting to add {2}.", DebugName, detectedItem.GetType().Name, detectedItem.DebugName);
                }
                if (_debugControls.IsAllIntelCoverageComprehensive) {
                    // all planetoids are already known as each Knowledge was fully populated with them before game start
                    if (!Knowledge.HasKnowledgeOf(planetoid)) {
                        D.Error("{0} has no knowledge of {1}.", DebugName, planetoid.DebugName);
                    }
                    return;
                }
                Knowledge.AddPlanetoid(planetoid);
            }
        }

        /// <summary>
        /// Called when an item that was detected by <c>Player</c> is no longer detected by <c>Player</c> at all. 
        /// <remarks>7.20.16 Items no longer informed of loss of detection when they die. 
        /// Cleanup of Knowledge on item death now handled by Knowledge.</remarks>
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        [Obsolete]
        public void HandleItemDetectionLost(ISensorDetectable detectedItem) {
            if (detectedItem is IStar_Ltd || detectedItem is IUniverseCenter_Ltd) {
                return; // these are added at startup and never removed so no need to evaluate
            }

            var element = detectedItem as IUnitElement_Ltd;
            if (element != null) {
                D.Assert(element.IsOperational);
                Knowledge.RemoveElement(element);
            }
            else {
                var planetoid = detectedItem as IPlanetoid_Ltd;
                if (planetoid == null) {
                    D.Error("{0}: Unanticipated Type {1} attempting to remove {2}.", DebugName, detectedItem.GetType().Name, detectedItem.DebugName);
                }
                // planetoids are not removed when they lose detection as they can't regress IntelCoverage
                D.Assert(planetoid.IsOperational);
            }
        }

        [Obsolete]
        private void CheckForDiscoveryOfNewPlayer(IUnitElement_Ltd element) {
            Player newlyDiscoveredPlayerCandidate;
            if (element.TryGetOwner(Owner, out newlyDiscoveredPlayerCandidate)) {
                if (newlyDiscoveredPlayerCandidate == Owner) {
                    // Note: The new HQ element just detected that generated this check is one of our own. 
                    // This typically occurs during the initial process of detecting what is in range of sensors when the game first starts.
                    return;
                }
                bool isAlreadyKnown = Owner.IsKnown(newlyDiscoveredPlayerCandidate);
                if (!isAlreadyKnown) {
                    Player newlyDiscoveredPlayer = newlyDiscoveredPlayerCandidate;
                    D.LogBold("{0} discovered new {1}.", DebugName, newlyDiscoveredPlayer);
                    SubscribeToPlayerRelationsChange(newlyDiscoveredPlayer);

                    Owner.HandleMetNewPlayer(newlyDiscoveredPlayer);
                }
            }
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

        #region Nested Classes

        /// <summary>
        /// Event Args containing info on a change to a player's awareness of a fleet.
        /// <remarks>Used to describe a change in whether the player is or is not aware
        /// of a fleet. It is not used to indicate a change in IntelCoverage level except for to/from None.</remarks>
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        public class AwarenessOfFleetChangedEventArgs : EventArgs {

            public IFleetCmd_Ltd Fleet { get; private set; }

            public bool IsAware { get; private set; }

            public AwarenessOfFleetChangedEventArgs(IFleetCmd_Ltd fleet, bool isAware) {
                Fleet = fleet;
                IsAware = isAware;
            }
        }

        #endregion

    }
}

