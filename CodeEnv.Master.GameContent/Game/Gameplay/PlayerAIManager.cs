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
    public class PlayerAIManager : APropertyChangeTracking, IDisposable {

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

        public bool IsOperational { get; private set; }

        private bool _isPolicyToEngageColdWarEnemies = false;
        /// <summary>
        /// Indicates whether policy is to engage qualified ColdWarEnemy targets.
        /// <remarks>A ColdWarEnemy target is qualified if it is located in our territory.</remarks>
        /// </summary>
        public bool IsPolicyToEngageColdWarEnemies {
            get { return _isPolicyToEngageColdWarEnemies; }
            set { SetProperty<bool>(ref _isPolicyToEngageColdWarEnemies, value, "IsPolicyToEngageColdWarEnemies", IsPolicyToEngageColdWarEnemiesChangedHandler); }
        }

        public PlayerKnowledge Knowledge { get; private set; }

        public Player Owner { get; private set; }

        public IEnumerable<Player> OtherKnownPlayers { get { return Owner.OtherKnownPlayers; } }

        // TODO use these when needing to search for commands to take an action
        private IList<IUnitCmd> _availableCmds;
        private IList<IUnitCmd> _unavailableCmds;
        private bool _areAllPlayersDiscovered;

        private IFpsReadout _fpsReadout;
        private IGameManager _gameMgr;
        private IDebugControls _debugControls;

        public PlayerAIManager(Player owner, PlayerKnowledge knowledge) {
            Owner = owner;
            Knowledge = knowledge;
            InitializeValuesAndReferences();
        }

        private void InitializeValuesAndReferences() {
            _debugControls = GameReferences.DebugControls;
            _gameMgr = GameReferences.GameManager;
            _fpsReadout = GameReferences.FpsReadout;
            _availableCmds = new List<IUnitCmd>();
            _unavailableCmds = new List<IUnitCmd>();
        }

        public void CommenceOperations() {
            IsOperational = true;

            var myAvailableFleetCmds = _availableCmds.Where(cmd => cmd is IFleetCmd).Cast<IFleetCmd>();
            if (!myAvailableFleetCmds.Any()) {
                D.Warn("{0} had no fleets available to issue initial orders.", DebugName);
                return;
            }
            __SpreadInitialFleetOrders(myAvailableFleetCmds);
        }

        /// <summary>
        /// Indicates whether the PlayerAIMgr Owner has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IOwnerItem_Ltd item) {
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
        public bool TryFindClosestKnownItem<T>(Vector3 worldPosition, out T closestItem, params T[] excludedItems) where T : IOwnerItem_Ltd {
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
        public void AssessAwarenessOf(IOwnerItem_Ltd item) {
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
                            CheckForUnknownPlayer(cmd);
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

        private void CheckForUnknownPlayer(IUnitCmd_Ltd cmd) {
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
            D.Assert(!_unavailableCmds.Contains(myUnitCmd));
            D.Assert(!_availableCmds.Contains(myUnitCmd));
            if (myUnitCmd.IsAvailable) {
                _availableCmds.Add(myUnitCmd);
            }
            else {
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

        #region Event and Property Change Handlers

        private void IsPolicyToEngageColdWarEnemiesChangedHandler() {
            Knowledge.OwnerCommands.ForAll(cmd => cmd.HandleColdWarEnemyEngagementPolicyChanged());
        }

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
            UpdateCmdAvailability(myCmd);

            if (IsOperational && myCmd.IsAvailable) {
                IFleetCmd fleetCmd = myCmd as IFleetCmd;
                if (fleetCmd != null) {
                    __AssessAndRecordUnitBaseVisit(fleetCmd);
                    //D.Log("{0} is issuing order to {1} with no delay.", DebugName, fleetCmd.DebugName);
                    __IssueFleetOrder(fleetCmd);
                }
            }
        }

        #endregion

        private void UpdateCmdAvailability(IUnitCmd myCmd) {
            if (myCmd.IsAvailable) {
                bool isRemoved = _unavailableCmds.Remove(myCmd);
                D.Assert(isRemoved);
                _availableCmds.Add(myCmd);
            }
            else {
                bool isRemoved = _availableCmds.Remove(myCmd);
                D.Assert(isRemoved);
                _unavailableCmds.Add(myCmd);
            }
        }

        public void HandleOwnerRelationsChangedWith(Player player) {
            D.AssertNotEqual(Owner, player);
            var priorRelationship = Owner.GetPriorRelations(player);
            var newRelationship = Owner.GetCurrentRelations(player);
            D.AssertNotEqual(priorRelationship, newRelationship);
            //D.Log("Relations have changed from {0} to {1} between {2} and {3}.", priorRelationship.GetValueName(), newRelationship.GetValueName(), Owner, player);
            if (priorRelationship == DiplomaticRelationship.Alliance) {
                HandleLostAllianceWith(player);
            }
            else if (newRelationship == DiplomaticRelationship.Alliance) {
                HandleGainedAllianceWith(player);
            }
            Knowledge.OwnerCommands.ForAll(myCmd => myCmd.HandleRelationsChangedWith(player));
            // Note: 7.15.16 Cmds currently propagate this to Elements and RangeMonitors
        }

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
            Knowledge.Dispose();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug Issue Fleet Orders

        private IList<IUnitBaseCmd> __basesVisited = new List<IUnitBaseCmd>();
        private bool __isUniverseFullyExplored = false; // avoids duplicate logging
        private bool __areAllBasesVisited = false;      // avoids duplicate logging
        private bool __areAllTargetsAttacked = false;
        private Job __spreadInitialFleetOrdersJob;

        private void __SpreadInitialFleetOrders(IEnumerable<IFleetCmd> myAvailableFleetCmds) {
            Stack<IFleetCmd> myAvailableFleetCmdsStack = new Stack<IFleetCmd>(myAvailableFleetCmds);
            int randomInitialWait = RandomExtended.Range(1, 3);
            __spreadInitialFleetOrdersJob = GameReferences.JobManager.RecurringWaitForGameplaySeconds(randomInitialWait, recurringWait: 1F,
                jobName: "InitialOrderSpreadJob", waitMilestone: () => {
                    if (myAvailableFleetCmdsStack.Count == Constants.Zero) {
                        __spreadInitialFleetOrdersJob.Kill();
                        return;
                    }
                    __IssueFleetOrder(myAvailableFleetCmdsStack.Pop());
                });
        }

        private void __AssessAndRecordUnitBaseVisit(IFleetCmd fleetCmd) {
            if (fleetCmd.IsCurrentOrderDirectiveAnyOf(FleetDirective.Move, FleetDirective.FullSpeedMove)) {
                var unitBaseTgt = fleetCmd.CurrentOrder.Target as IUnitBaseCmd;
                if (unitBaseTgt != null) {
                    if (__basesVisited.Contains(unitBaseTgt)) {
                        D.Log("{0}: {1} just completed visiting {2} which was previously visited.",
                            DebugName, fleetCmd.DebugName, unitBaseTgt.DebugName);
                    }
                    else {
                        __basesVisited.Add(unitBaseTgt);
                    }
                }
            }
        }

        ////private int __myActiveFleetCount;
        private IList<IFleetCmd> __myAttackingFleets;

        private void __MyAttackingFleetDeathEventHandler(object sender, EventArgs e) {
            __myAttackingFleets.Remove(sender as IFleetCmd);
        }

        /// <summary>
        /// Issues an order to the fleet.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        private void __IssueFleetOrder(IFleetCmd fleetCmd) {
            D.Assert(_availableCmds.Contains(fleetCmd));
            D.Assert(!_unavailableCmds.Contains(fleetCmd));

            if (fleetCmd.IsFerryFleet) {
                IFleetNavigable closestFleet = null;
                var fleets = _availableCmds.Where(cmd => cmd is IFleetCmd).Where(cmd => !(cmd as IFleetCmd).IsFerryFleet).Cast<IFleetNavigable>();
                if (fleets.Any()) {
                    closestFleet = GameUtility.GetClosest(fleetCmd.Position, fleets);
                }
                else {
                    fleets = _unavailableCmds.Where(cmd => cmd is IFleetCmd).Where(cmd => !(cmd as IFleetCmd).IsFerryFleet).Cast<IFleetNavigable>();
                    if (fleets.Any()) {
                        closestFleet = GameUtility.GetClosest(fleetCmd.Position, fleets);
                    }
                }
                if (closestFleet != null) {
                    D.Log("{0} is issuing an order to {1} to Join {2}.", DebugName, fleetCmd.DebugName, closestFleet.DebugName);
                    FleetOrder order = new FleetOrder(FleetDirective.Join, OrderSource.PlayerAI, closestFleet);
                    bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
                    if (!isOrderInitiated) {
                        D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                            DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
                    }
                    return;
                }
            }

            if (GameReferences.DebugControls.FleetsAutoAttackAsDefault) {
                if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
                    return;
                }

                if (__myAttackingFleets == null) {
                    __myAttackingFleets = new List<IFleetCmd>();
                }

                ////__myActiveFleetCount = Knowledge.OwnerFleets.Count();

                if (__myAttackingFleets.Count < _debugControls.MaxAttackingFleetsPerPlayer) {
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
                            //D.Log("{0}: Fleet {1} can't find an attack target so will explore another part of universe.", 
                            //DebugName, fleetCmd.DebugName);
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
                        //D.Log("{0}: Fleet {1} is not allowed to attack so will explore another part of universe.", DebugName, fleetCmd.DebugName);
                    }
                }

                if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
                    D.LogBold("{0}: Fleet {1} has run out of attack targets and unexplored explorable destinations in universe.",
                        DebugName, fleetCmd.DebugName);
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

                if (GameReferences.DebugControls.FleetsAutoExploreAsDefault) {
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
                        D.LogBold("{0}: Fleet {1} has run out of unexplored explorable or unvisited visitable destinations in universe.",
                            DebugName, fleetCmd.DebugName);
                    }
                }
            }
        }

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
                    D.Log(!isOrderIssued, "{0}: {1} can find no WarAttackTargets of any sort.", DebugName, fleetCmd.DebugName);
                }
                else {
                    isOrderIssued = __IssueFleetMoveOrder(fleetCmd, editorSettings.FindFarthest);
                    D.Log(!isOrderIssued, "{0}: {1} can find no MoveTargets that meet the selection criteria.",
                        DebugName, fleetCmd.DebugName);
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
            List<IUnitAttackable> attackTgts = Knowledge.Fleets.Cast<IUnitAttackable>().Where(f => f.IsWarAttackAllowedBy(Owner)).ToList();
            attackTgts.AddRange(Knowledge.Starbases.Cast<IUnitAttackable>().Where(sb => sb.IsWarAttackAllowedBy(Owner)));
            attackTgts.AddRange(Knowledge.Settlements.Cast<IUnitAttackable>().Where(s => s.IsWarAttackAllowedBy(Owner)));
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
            D.LogBold("{0} is issuing {1} an ATTACK order against {2} in Frame {3}. FPS = {4:0.#}.",
                DebugName, fleetCmd.DebugName, attackTgt.DebugName, Time.frameCount, _fpsReadout.FramesPerSecond);
            FleetOrder order = new FleetOrder(FleetDirective.Attack, OrderSource.PlayerAI, attackTgt);
            bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
            if (!isOrderInitiated) {
                D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                    DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
            }
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
            D.Log("{0} is issuing {1} a MOVE order to {2} in Frame {3}. FPS = {4:0.#}.",
                DebugName, fleetCmd.DebugName, destination.DebugName, Time.frameCount, _fpsReadout.FramesPerSecond);
            var order = new FleetOrder(FleetDirective.Move, OrderSource.PlayerAI, destination);
            bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
            if (!isOrderInitiated) {
                D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                    DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
            }
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
                D.Log("{0} is issuing {1} an EXPLORE order to {2} in Frame {3}. FPS = {4:0.#}. IsOwnerAccessible = {5}.",
                    DebugName, fleetCmd.DebugName, closestUnexploredSystem.DebugName, Time.frameCount, _fpsReadout.FramesPerSecond, closestUnexploredSystem.IsOwnerAccessibleTo(Owner));
                var order = new FleetOrder(FleetDirective.Explore, OrderSource.PlayerAI, closestUnexploredSystem);
                bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
                if (!isOrderInitiated) {
                    D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                        DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
                }
            }
            else {
                IFleetExplorable uCenter = Knowledge.UniverseCenter as IFleetExplorable;
                if (!uCenter.IsFullyExploredBy(Owner)) {
                    var order = new FleetOrder(FleetDirective.Explore, OrderSource.PlayerAI, uCenter);
                    bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
                    if (!isOrderInitiated) {
                        D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                            DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
                    }
                }
                else {
                    D.LogBold("{0}: Fleet {1} has completed {2}'s exploration of explorable universe.",
                        DebugName, fleetCmd.DebugName, Owner.DebugName);
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
                D.Log("{0} is issuing {1} a MOVE order to {2} in Frame {3}. FPS = {4:0.#}.",
                    DebugName, fleetCmd.DebugName, closestVisitableBase.DebugName, Time.frameCount, _fpsReadout.FramesPerSecond);
                var order = new FleetOrder(FleetDirective.Move, OrderSource.PlayerAI, closestVisitableBase as IFleetNavigable);
                bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
                if (!isOrderInitiated) {
                    D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                        DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
                }
            }
            else {
                D.LogBold("{0}: Fleet {1} has completed {2}'s visits to all visitable unvisited bases in known universe.",
                    DebugName, fleetCmd.DebugName, Owner.DebugName);
                isMoveOrderIssued = false;
            }
            return isMoveOrderIssued;
        }

        #endregion

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


        #endregion

        #region Obsolete Archive

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

