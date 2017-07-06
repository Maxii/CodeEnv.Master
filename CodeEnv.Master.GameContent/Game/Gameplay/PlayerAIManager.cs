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
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of a mortalItem. Knowledge of a mortalItem's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwareChgdEventArgs> awareChgd_Fleet;

        /// <summary>
        /// Occurs when this player's awareness of a ship has changed.
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of a mortalItem. Knowledge of a mortalItem's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwareChgdEventArgs> awareChgd_Ship;

        /// <summary>
        /// Occurs when this player's awareness of a base has changed.
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of a mortalItem. Knowledge of a mortalItem's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwareChgdEventArgs> awareChgd_Base;

        /// <summary>
        /// Occurs when this player's awareness of a facility has changed.
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of a mortalItem. Knowledge of a mortalItem's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwareChgdEventArgs> awareChgd_Facility;

        /// <summary>
        /// Occurs when this player's awareness of a planet has changed.
        /// <remarks>This event will not fire when the player loses awareness because of the death 
        /// of a mortalItem. Knowledge of a mortalItem's death should be handled by subscribing to its deathOneShot event.</remarks>
        /// </summary>
        public event EventHandler<AwareChgdEventArgs> awareChgd_Planet;

        private string _debugName;
        public virtual string DebugName {
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
        private IJobManager _jobMgr;

        public PlayerAIManager(Player owner, PlayerKnowledge knowledge) {
            Owner = owner;
            Knowledge = knowledge;
            InitializeValuesAndReferences();
        }

        private void InitializeValuesAndReferences() {
            _debugControls = GameReferences.DebugControls;
            _gameMgr = GameReferences.GameManager;
            _jobMgr = GameReferences.JobManager;
            _fpsReadout = GameReferences.FpsReadout;
            _availableCmds = new List<IUnitCmd>();
            _unavailableCmds = new List<IUnitCmd>();
        }

        public void CommenceOperations() {
            IsOperational = true;

            if (_debugControls.IsAutoRelationsChangeEnabled) {
                __InitializeAutoRelationsChgSystem();
            }

            var myAvailableFleetCmds = _availableCmds.Where(cmd => cmd is IFleetCmd).Cast<IFleetCmd>();
            if (!myAvailableFleetCmds.Any()) {
                D.Log("{0} had no fleets available to issue initial orders.", DebugName);
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
        /// If not already aware, the knowledge is added and [Owner] becomes aware. Each item has their ownership assessed
        /// and if [Owner] has access to the Item's owner, and Item's owner is not known to [Owner], then the two owners 
        /// 'discover' each other. If [Owner] has just gained (or lost in the case of FleetCmd and Ship) awareness of an Item
        /// due to an increase or reduction of [Owner]'s IntelCoverage, this AIMgr will fire an awarenessChgd event. 
        /// Obviously, if fleet awareness is lost it won't be owned by [Owner].
        /// <remarks>Called whenever an item has had its IntelCoverage by [Owner] changed. Does nothing if already aware of
        /// the item without losing that awareness.</remarks>
        /// </summary>
        /// <param name = "item" > The item whose IntelCoverage by [Owner] has changed.</param>
        public void AssessAwarenessOf(IOwnerItem_Ltd item) {
            //D.Log("{0}.AssessAwarenessOf({1}) called. Frame: {2}.", DebugName, item.DebugName, Time.frameCount); 
            IIntelItem intelItem = item as IIntelItem;
            IntelCoverage intelCoverage = intelItem.GetIntelCoverage(Owner);
            if (_debugControls.IsAllIntelCoverageComprehensive) {
                // Each and every item should be set to Comprehensive by AIntelItem during FinalInitialization...
                D.AssertEqual(IntelCoverage.Comprehensive, intelCoverage, intelCoverage.GetValueName());
            }

            if (item is IUniverseCenter_Ltd) {
                // added to knowledge at startup and never removed so no need to attempt add again
                return;
            }

            if (!_areAllPlayersDiscovered) {
                CheckForUndiscoveredPlayer(item);
            }

            if (item is IStar_Ltd || item is ISystem_Ltd) {
                // added to knowledge at startup and never removed so no need to attempt add again
                return;
            }

            // Note: Cleanup of Knowledge on item death handled by Knowledge

            var element = item as IUnitElement_Ltd;
            if (element != null) {
                var ship = element as IShip_Ltd;
                if (ship != null) {
                    // intelCoverage = ship.GetIntelCoverage(Owner);            // TEMP
                    if (intelCoverage == IntelCoverage.None) {
                        D.Assert(ship.IsOperational);   // 4.20.17 This is a revert to None so must be operational
                        Knowledge.RemoveElement(ship);
                        OnAwareChgd_Ship(ship);
                        return;
                    }
                }
                bool isNewlyAware = Knowledge.AddElement(element);
                if (isNewlyAware && element.IsOperational) {
                    // 4.20.17 awareChgd events only raised when item is operational
                    if (ship != null) {
                        OnAwareChgd_Ship(ship);
                    }
                    else {
                        D.Assert(element is IFacility_Ltd);
                        OnAwareChgd_Facility(element as IFacility_Ltd);
                    }
                }
            }
            else {
                var planetoid = item as IPlanetoid_Ltd;
                if (planetoid != null) {
                    bool isNewlyAware = Knowledge.AddPlanetoid(planetoid);
                    if (isNewlyAware && planetoid.IsOperational) {
                        // 4.20.17 awareChgd events only raised when item is operational
                        var planet = planetoid as IPlanet_Ltd;
                        if (planet != null) {
                            OnAwareChgd_Planet(planet);
                        }
                    }
                }
                else {
                    var cmd = item as IUnitCmd_Ltd;
                    if (cmd != null) {
                        var fleetCmd = cmd as IFleetCmd_Ltd;
                        if (fleetCmd != null) {
                            // intelCoverage = fleetCmd.GetIntelCoverage(Owner);            // TEMP
                            if (intelCoverage == IntelCoverage.None) {
                                D.Assert(fleetCmd.IsOperational);   // 4.20.17 This is a revert to None so must be operational
                                Knowledge.RemoveCommand(fleetCmd);
                                OnAwareChgd_Fleet(fleetCmd);
                                return;
                            }
                        }
                        bool isNewlyAware = Knowledge.AddCommand(cmd);
                        if (isNewlyAware && cmd.IsOperational) {
                            // 4.20.17 awareChgd events only raised when item is operational
                            if (fleetCmd != null) {
                                OnAwareChgd_Fleet(fleetCmd);
                            }
                            else {
                                D.Assert(cmd is IUnitBaseCmd_Ltd);
                                OnAwareChgd_Base(cmd as IUnitBaseCmd_Ltd);
                            }
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

        private void CheckForUndiscoveredPlayer(IOwnerItem_Ltd item) {
            D.Assert(!_areAllPlayersDiscovered);
            //D.Log("{0}.CheckForUndiscoveredPlayer({1}) called. Frame: {2}.", DebugName, item.DebugName, Time.frameCount);

            Player newlyDiscoveredPlayerCandidate;
            if (item.TryGetOwner_Debug(Owner, out newlyDiscoveredPlayerCandidate)) {
                if (newlyDiscoveredPlayerCandidate == TempGameValues.NoPlayer || newlyDiscoveredPlayerCandidate == Owner) {
                    return;
                }

                bool isAlreadyKnown = Owner.IsKnown(newlyDiscoveredPlayerCandidate);
                if (!isAlreadyKnown) {
                    Player newlyDiscoveredPlayer = newlyDiscoveredPlayerCandidate;
                    Owner.HandleMetNewPlayer(newlyDiscoveredPlayer);
                    _areAllPlayersDiscovered = Owner.OtherKnownPlayers.Count() == _gameMgr.AllPlayers.Count - 1;
                    if (_areAllPlayersDiscovered) {
                        //D.LogBold("{0}: {1} now knows all players!", DebugName, Owner.DebugName);
                        //D.Log("{0}'s OtherKnownPlayers: {1}, AllPlayers: {2}.", Owner.DebugName,
                        //    Owner.OtherKnownPlayers.Select(p => p.DebugName).Concatenate(), _gameMgr.AllPlayers.Select(p => p.DebugName).Concatenate());
                    }
                }
                //else {
                //    D.Log("{0}: {1} is already known to {2}.", DebugName, newlyDiscoveredPlayerCandidate.DebugName, Owner.DebugName);
                //}
            }
        }

        /// <summary>
        /// Makes Owner's provided UnitCommand available for orders whenever isAvailableChanged fires.
        /// <remarks>Called in 2 scenarios: when commencing operation and after gaining ownership.</remarks>
        /// </summary>
        /// <param name="myUnitCmd">My unit command.</param>
        public void RegisterForOrders(IUnitCmd myUnitCmd) {
            D.AssertEqual(Owner, myUnitCmd.Owner);
            D.Assert(IsOperational || !myUnitCmd.IsAvailable);
            if (!HasKnowledgeOf(myUnitCmd as IUnitCmd_Ltd)) {
                D.Warn("{0} is unaware of {1} when registering for orders in Frame {2}.", DebugName, myUnitCmd.DebugName, Time.frameCount);
            }
            //D.Log("{0} is registering {1} as {2} for orders in Frame {3}.",
            //    DebugName, myUnitCmd.DebugName, myUnitCmd.IsAvailable ? "available" : "unavailable", Time.frameCount);
            D.Assert(!_unavailableCmds.Contains(myUnitCmd));
            D.Assert(!_availableCmds.Contains(myUnitCmd));
            if (myUnitCmd.IsAvailable) {
                var myFleetCmd = myUnitCmd as IFleetCmd;
                if (myFleetCmd != null && IsOperational) {
                    if (__IssueFleetOrder(myFleetCmd)) {
                        _unavailableCmds.Add(myUnitCmd);
                    }
                    else {
                        _availableCmds.Add(myUnitCmd);
                    }
                }
                else {
                    _availableCmds.Add(myUnitCmd);
                }
            }
            else {
                _unavailableCmds.Add(myUnitCmd);
            }
            myUnitCmd.isAvailableChanged += MyCmdIsAvailableChgdEventHandler;
        }

        /// <summary>
        /// Deregisters Owner's provided UnitCommand from receiving orders whenever isAvailableChanged fires.
        /// <remarks>Called in 2 scenarios: death and just before losing ownership.</remarks>
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
                if (!isRemoved) {
                    // 5.16.17 LoneCmd hadn't commenced ops and registered yet. Should be solved now that
                    // this is called from Cmd.HandleOwnerChanging rather than Changed
                    D.Error("{0}: cannot find {1} to DeregisterForOrders.", DebugName, myUnitCmd.DebugName);
                }
            }
        }

        #region Event and Property Change Handlers

        private void IsPolicyToEngageColdWarEnemiesChangedHandler() {
            Knowledge.OwnerCommands.ForAll(cmd => cmd.HandleColdWarEnemyEngagementPolicyChanged());
        }

        private void OnAwareChgd_Fleet(IFleetCmd_Ltd fleet) {
            if (awareChgd_Fleet != null) {
                D.Assert(fleet.IsOperational, fleet.DebugName);
                if (fleet.Owner_Debug == Owner) {
                    // 5.10.17 ship taken over changed Cmd owner. Intel got changed to None due to Sensor IsOperational recycle.
                    D.Error("{0}: Awareness Chg of {1} owned by us. IsLoneCmd = {2}. HQElementOwner = {3}.",
                        DebugName, fleet.DebugName, fleet.IsLoneCmd, (fleet as IUnitCmd).HQElement.Owner);
                }
                awareChgd_Fleet(this, new AwareChgdEventArgs(fleet));
            }
        }

        private void OnAwareChgd_Ship(IShip_Ltd ship) {
            if (awareChgd_Ship != null) {
                D.Assert(ship.IsOperational, ship.DebugName);
                D.AssertNotEqual(ship.Owner_Debug, Owner);
                awareChgd_Ship(this, new AwareChgdEventArgs(ship));
            }
        }

        private void OnAwareChgd_Base(IUnitBaseCmd_Ltd baseCmd) {
            if (awareChgd_Base != null) {
                D.Assert(baseCmd.IsOperational, baseCmd.DebugName);
                D.AssertNotEqual(baseCmd.Owner_Debug, Owner);
                awareChgd_Base(this, new AwareChgdEventArgs(baseCmd));
            }
        }

        private void OnAwareChgd_Facility(IFacility_Ltd facility) {
            if (awareChgd_Facility != null) {
                D.Assert(facility.IsOperational, facility.DebugName);
                D.AssertNotEqual(facility.Owner_Debug, Owner);
                awareChgd_Facility(this, new AwareChgdEventArgs(facility));
            }
        }

        private void OnAwareChgd_Planet(IPlanet_Ltd planet) {
            if (awareChgd_Planet != null) {
                D.Assert(planet.IsOperational, planet.DebugName);
                D.AssertNotEqual(planet.Owner_Debug, Owner);
                awareChgd_Planet(this, new AwareChgdEventArgs(planet));
            }
        }


        private void MyCmdIsAvailableChgdEventHandler(object sender, EventArgs e) {
            IUnitCmd myCmd = sender as IUnitCmd;
            HandleMyCmdIsAvailableChanged(myCmd);
        }

        #endregion

        private void HandleMyCmdIsAvailableChanged(IUnitCmd myCmd) {
            UpdateCmdAvailability(myCmd);

            if (IsOperational) {
                IFleetCmd fleetCmd = myCmd as IFleetCmd;
                if (fleetCmd != null) {
                    if (fleetCmd.IsAvailable) {
                        __IssueFleetOrder(fleetCmd);
                    }
                    else {
                        __AssessAndRecordUpcomingBaseVisit(fleetCmd);
                    }
                }
            }
        }

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
            formerAllySensorDetectableOwnedItems.ForAll(sdItem => {
                //D.Log("{0} is about to call formerAlly {1}'s item {2}.ResetBasedOnCurrentDetection.", DebugName, formerAlly, sdItem.DebugName);
                sdItem.ResetBasedOnCurrentDetection(Owner);
            });
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

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug Auto Relations Change System

        private Job __autoRelationsChgJob;

        private void __InitializeAutoRelationsChgSystem() {
            GameDate startDate = new GameDate(new GameTimeDuration(0F, days: RandomExtended.Range(1, 3)));
            GameTimeDuration durationBetweenChgs = new GameTimeDuration(hours: UnityEngine.Random.Range(0F, 10F), days: RandomExtended.Range(3, 10));
            //D.Log("{0}: Initiating Auto Relations Changes beginning {1} with changes every {2}.", DebugName, startDate, durationBetweenChgs);
            __autoRelationsChgJob = _jobMgr.WaitForDate(startDate, "AutoRelationsChgStartJob", waitFinished: (jobWasKilled) => {
                if (jobWasKilled) {

                }
                else {
                    __autoRelationsChgJob = _jobMgr.RecurringWaitForHours(durationBetweenChgs, "AutoRelationsChgRecurringJob", waitMilestone: () => {
                        if (OtherKnownPlayers.Any()) {
                            Player player = RandomExtended.Choice(OtherKnownPlayers);
                            var currentRelations = Owner.GetCurrentRelations(player);
                            DiplomaticRelationship newRelations = Enums<DiplomaticRelationship>.GetRandomExcept(default(DiplomaticRelationship),
                                currentRelations, DiplomaticRelationship.Self);
                            Owner.SetRelationsWith(player, newRelations);
                        }
                    });
                }
            });
        }

        #endregion

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
                jobName: "__InitialOrderSpreadJob", waitMilestone: () => {
                    if (myAvailableFleetCmdsStack.Count == Constants.Zero) {
                        __spreadInitialFleetOrdersJob.Kill();
                        return;
                    }
                    __IssueFleetOrder(myAvailableFleetCmdsStack.Pop());
                });
        }

        private void __AssessAndRecordUpcomingBaseVisit(IFleetCmd fleetCmd) {
            if (fleetCmd.IsCurrentOrderDirectiveAnyOf(FleetDirective.Move, FleetDirective.FullSpeedMove)) {
                var unitBaseTgt = fleetCmd.CurrentOrder.Target as IUnitBaseCmd;
                if (unitBaseTgt != null) {
                    if (__basesVisited.Contains(unitBaseTgt)) {
                        D.Log("{0}: {1} intends to visit {2} which was previously visited.",
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
        /// Issues an order to the fleet, returning true if an order was issued.
        /// <remarks>5.15.17 Not entirely sure it returns proper value all the time.</remarks>
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <returns></returns>
        private bool __IssueFleetOrder(IFleetCmd fleetCmd) {
            D.Assert(fleetCmd.IsAvailable);
            D.Assert(_availableCmds.Contains(fleetCmd));
            D.Assert(!_unavailableCmds.Contains(fleetCmd));

            if (fleetCmd.IsLoneCmd) {
                IFleetNavigableDestination closestFleet = null;

                IEnumerable<IFleetNavigableDestination> tgtFleets =
                from cmd in _availableCmds
                let fleet = cmd as IFleetCmd
                where fleet != null && !fleet.IsLoneCmd && fleet.IsJoinable
                let tgtFleet = fleet as IFleetNavigableDestination
                select tgtFleet;

                if (tgtFleets.Any()) {
                    closestFleet = GameUtility.GetClosest(fleetCmd.Position, tgtFleets);
                }
                else {
                    tgtFleets =
                        from cmd in _unavailableCmds
                        let fleet = cmd as IFleetCmd
                        where fleet != null && !fleet.IsLoneCmd && fleet.IsJoinable
                        let tgtFleet = fleet as IFleetNavigableDestination
                        select tgtFleet;
                    if (tgtFleets.Any()) {
                        closestFleet = GameUtility.GetClosest(fleetCmd.Position, tgtFleets);
                    }
                }
                if (closestFleet != null) {
                    //D.Log("{0} is issuing an order to {1} to JOIN {2}.", DebugName, fleetCmd.DebugName, closestFleet.DebugName);
                    FleetOrder order = new FleetOrder(FleetDirective.Join, OrderSource.PlayerAI, closestFleet);
                    bool isOrderInitiated = fleetCmd.InitiateNewOrder(order);
                    if (!isOrderInitiated) {
                        D.Warn("{0} was unable to immediately initiate {1} due to CmdStaff's Override order {2}.",
                            DebugName, order.DebugName, fleetCmd.CurrentOrder.DebugName);
                        return false;
                    }
                    return true;
                }
                return false;
            }

            if (GameReferences.DebugControls.FleetsAutoAttackAsDefault) {
                if (__areAllTargetsAttacked && __isUniverseFullyExplored) {
                    return false;
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
                        return true;
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
                    return false;
                }
            }
            else {  // FleetsAutoAttackAsDefault precludes all other orders
                var debugFleetCreator = fleetCmd.transform.GetComponentInParent<IDebugFleetCreator>();
                if (debugFleetCreator != null) {
                    FleetCreatorEditorSettings editorSettings = debugFleetCreator.EditorSettings as FleetCreatorEditorSettings;
                    if (__IssueFleetOrderSpecifiedByCreator(fleetCmd, editorSettings)) {
                        return true;
                    }
                }

                if (GameReferences.DebugControls.FleetsAutoExploreAsDefault) {
                    if (__isUniverseFullyExplored && __areAllBasesVisited) {
                        return false;
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
                        return false;
                    }
                    return true;
                }
                return true;
            }
            return true;
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
            List<IFleetNavigableDestination> moveTgts = Knowledge.Starbases.Cast<IFleetNavigableDestination>().ToList();
            moveTgts.AddRange(Knowledge.Settlements.Cast<IFleetNavigableDestination>());
            moveTgts.AddRange(Knowledge.Planets.Cast<IFleetNavigableDestination>());
            ////moveTgts.AddRange(Knowledge.Systems.Cast<IFleetNavigableDestination>());
            moveTgts.AddRange(Knowledge.Stars.Cast<IFleetNavigableDestination>());
            if (Knowledge.UniverseCenter != null) {
                moveTgts.Add(Knowledge.UniverseCenter as IFleetNavigableDestination);
            }

            if (!moveTgts.Any()) {
                return false;
            }
            IFleetNavigableDestination destination;
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
                var order = new FleetOrder(FleetDirective.Move, OrderSource.PlayerAI, closestVisitableBase as IFleetNavigableDestination);
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
        /// Event Args containing info on a change to a player's awareness of an item.
        /// <remarks>Used to describe a change in whether the player is or is not aware
        /// of an item. It is not used to indicate a change in IntelCoverage level except for to/from None.</remarks>
        /// <remarks>Most items cannot be lost from awareness once this player is aware of them. The exceptions
        /// are Fleets and Ships which can be lost from awareness when they go out of sensor range.</remarks>
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        public class AwareChgdEventArgs : EventArgs {

            public IOwnerItem_Ltd Item { get; private set; }

            public AwareChgdEventArgs(IOwnerItem_Ltd item) {
                Item = item;
            }
        }

        #endregion

    }
}

