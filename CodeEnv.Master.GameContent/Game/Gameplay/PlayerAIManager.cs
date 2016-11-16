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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        private const string NameFormat = "{0}'s {1}";

        protected string Name { get { return NameFormat.Inject(Owner.LeaderName, GetType().Name); } }

        public PlayerKnowledge Knowledge { get; private set; }

        public Player Owner { get; private set; }

        public IEnumerable<Player> OtherKnownPlayers { get { return Owner.OtherKnownPlayers; } }

        // TODO use these when needing to search for commands to take an action
        private IList<IUnitCmd> _availableCmds;
        private IList<IUnitCmd> _unavailableCmds;

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
        public bool TryFindMyClosestItem<T>(Vector3 worldPosition, out T closestItem, params T[] excludedItems) where T : IItem {
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

        #region Handle Item Detection and Owner Changes

        /// <summary>
        /// Called whenever an operational ISensorDetectable Item is detected by <c>Owner</c>, no matter how
        /// many times the item has been detected previously. Ignores items that Owner already has knowledge of.
        /// <remarks>The only ISensorDetectables that really get handled here are Elements and Planetoids as
        /// Stars and the UniverseCenter are already known to all players.</remarks>
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        public void HandleItemDetection(ISensorDetectable detectedItem) {
            if (detectedItem is IStar_Ltd || detectedItem is IUniverseCenter_Ltd) {
                return; // these are added at startup and never removed so no need to add again
            }
            D.Assert(detectedItem.IsOperational, detectedItem.FullName);

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
                    D.Error("{0}: Unanticipated Type {1} attempting to add {2}.", Name, detectedItem.GetType().Name, detectedItem.FullName);
                }
                if (_debugControls.IsAllIntelCoverageComprehensive) {
                    // all planetoids are already known as each Knowledge was fully populated with them before game start
                    if (!Knowledge.HasKnowledgeOf(planetoid)) {
                        D.Error("{0} has no knowledge of {1}.", Name, planetoid.FullName);
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
                    D.Error("{0}: Unanticipated Type {1} attempting to remove {2}.", Name, detectedItem.GetType().Name, detectedItem.FullName);
                }
                // planetoids are not removed when they lose detection as they can't regress IntelCoverage
                D.Assert(planetoid.IsOperational);
            }
        }

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
                    D.LogBold("{0} discovered new {1}.", Name, newlyDiscoveredPlayer);
                    SubscribeToPlayerRelationsChange(newlyDiscoveredPlayer);

                    Owner.HandleMetNewPlayer(newlyDiscoveredPlayer);
                }
            }
        }

        /// <summary>
        /// Handles the change of an item's owner to an ally of this AIMgr's owner. This can be called in 2 scenarios:
        /// 1) An existing ally of the owner of this AIMgr just created a new item, and 
        /// 2) an existing item had its owner changed to one of this AIMgr's allies.
        /// </summary>
        /// <param name="allyOwnedItem">The ally owned item.</param>
        public void HandleChgdItemOwnerIsAlly(IItem allyOwnedItem) {
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
        public void HandleGainedItemOwnership(IItem myOwnedItem) {
            //D.Log("{0}.HandleGainedItemOwnership({1}) called.", Name, myOwnedItem.FullName);
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
        public void HandleLosingItemOwnership(IItem losingOwnedItem) {
            D.AssertEqual(Owner, losingOwnedItem.Owner);
            D.Assert(!(losingOwnedItem is IUniverseCenter));

            // Items that are losing their owner call Item.DetectionMgr.Reset() to re-determine the
            // (soon to be) former owner's intel coverage (and if appropriate, de-populate knowledge)
        }

        private void ChangeIntelCoverageToComprehensiveAndPopulateKnowledge(IItem item) {
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
                //D.Log(!isAdded, "{0} tried to add {1} it already has.", Name, element.FullName);
            }
            else {
                var planetoid = item as IPlanetoid;
                if (planetoid != null) {
                    planetoid.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                    bool isAdded = Knowledge.AddPlanetoid(planetoid as IPlanetoid_Ltd);
                    //D.Log(!isAdded, "{0} tried to add {1} it already has.", Name, planetoid.FullName);
                }
                else {
                    var star = item as IStar;
                    D.AssertNotNull(star);
                    star.SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
                    // don't need to add to knowledge as all stars are already known
                }
            }
        }

        #endregion

        /// <summary>
        /// Makes Owner's provided UnitCommand available for orders whenever isAvailableChanged fires.
        /// Called in 2 scenarios: just before commencing operation and after gaining ownership.
        /// </summary>
        /// <param name="myUnitCmd">My unit command.</param>
        public void RegisterForOrders(IUnitCmd myUnitCmd) {
            D.AssertEqual(Owner, myUnitCmd.Owner);
            //D.Log("{0} is registering {1} in prep for being issued orders. IsAvailable = {2}.", Name, myUnitCmd.FullName, myUnitCmd.IsAvailable);
            if (myUnitCmd.IsAvailable) {
                D.Assert(!_availableCmds.Contains(myUnitCmd));
                _availableCmds.Add(myUnitCmd);
            }
            else {
                D.Assert(!_unavailableCmds.Contains(myUnitCmd));
                _unavailableCmds.Add(myUnitCmd);
            }
            myUnitCmd.isAvailableChanged += MyCmdIsAvailableChgdEventHandler;
        }

        /// <summary>
        /// Un-registers Owner's provided UnitCommand from receiving orders whenever isAvailableChanged fires.
        /// Called in 2 scenarios: death and in process of losing ownership.
        /// </summary>
        /// <param name="myUnitCmd">My unit command.</param>
        public void UnregisterForOrders(IUnitCmd myUnitCmd) {
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
                        //D.Log("{0} is issuing order to {1} with a {2} hour delay.", Name, fleetCmd.FullName, hoursDelay);

                        // makes sure Owner's knowledge of universe has been constructed before selecting its target
                        string jobName = "{0}.WaitToIssueFirstOrderJob".Inject(Name);
                        References.JobManager.WaitForHours(hoursDelay, jobName, waitFinished: delegate {
                            __IssueFleetOrder(fleetCmd);
                        });
                    }
                    else {
                        //D.Log("{0} is issuing order to {1} with no delay.", Name, fleetCmd.FullName);
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
                //D.Log("{0} is adding Ally {1}'s item {2} to knowledge with IntelCoverage = Comprehensive.", Name, ally, allyItem.FullName);
                ChangeIntelCoverageToComprehensiveAndPopulateKnowledge(allyItem);
            });
        }

        private void HandleLostAllianceWith(Player formerAlly) {
            D.Assert(Owner.IsPriorRelationshipWith(formerAlly, DiplomaticRelationship.Alliance));

            var formerAllyOwnedItems = Knowledge.GetItemsOwnedBy(formerAlly);
            var formerAllySensorDetectableOwnedItems = formerAllyOwnedItems.Where(item => item is ISensorDetectable).Cast<ISensorDetectable>();
            formerAllySensorDetectableOwnedItems.ForAll(sdItem => sdItem.ResetBasedOnCurrentDetection(Owner));
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
        public IEnumerable<IItem> __GetItemsOwnedBy(Player player) {
            return Knowledge.__GetItemsOwnedBy(player);
        }

        #region Debug Issue Fleet Orders

        /// <summary>
        /// Issues an order to the fleet.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        private void __IssueFleetOrder(IFleetCmd fleetCmd) {
            var debugFleetCreator = fleetCmd.transform.GetComponentInParent<IDebugFleetCreator>();
            if (debugFleetCreator != null) {
                FleetCreatorEditorSettings editorSettings = debugFleetCreator.EditorSettings as FleetCreatorEditorSettings;
                if (__IssueFleetOrderSpecifiedByCreator(fleetCmd, editorSettings)) {
                    return;
                }
            }
            if (References.DebugControls.FleetsAutoExploreAsDefault) {
                __IssueFleetExploreOrder(fleetCmd);
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
                }
                else {
                    isOrderIssued = __IssueFleetMoveOrder(fleetCmd, editorSettings.FindFarthest);
                }
            }
            return isOrderIssued;
        }

        /// <summary>
        /// Tries to issue a fleet move order. Returns
        /// <c>true</c> if the order was issued, <c>false</c> otherwise.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="findFarthestTgt">if set to <c>true</c> [find farthest target].</param>
        /// <returns></returns>
        private bool __IssueFleetAttackOrder(IFleetCmd fleetCmd, bool findFarthestTgt) {
            List<IUnitAttackable> attackTgts = Knowledge.Fleets.Cast<IUnitAttackable>().Where(f => f.IsWarAttackByAllowed(Owner)).ToList();
            attackTgts.AddRange(Knowledge.Starbases.Cast<IUnitAttackable>().Where(sb => sb.IsWarAttackByAllowed(Owner)));
            attackTgts.AddRange(Knowledge.Settlements.Cast<IUnitAttackable>().Where(s => s.IsWarAttackByAllowed(Owner)));
            attackTgts.AddRange(Knowledge.Planets.Cast<IUnitAttackable>().Where(p => p.IsWarAttackByAllowed(Owner)));
            if (!attackTgts.Any()) {
                D.LogBold("{0}: {1} can find no WarAttackTargets of any sort.", Name, fleetCmd.FullName);
                return false;
            }
            IUnitAttackable attackTgt;
            if (findFarthestTgt) {
                attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - fleetCmd.Position));
            }
            else {
                attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - fleetCmd.Position));
            }
            //D.Log("{0} attack target is {1}.", fleetCmd.FullName, attackTgt.FullName);
            fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Attack, OrderSource.CmdStaff, attackTgt);
            return true;
        }

        /// <summary>
        /// Tries to issue a fleet attack order. Returns
        /// <c>true</c> if the order was issued, <c>false</c> otherwise.
        /// </summary>
        /// <param name="fleetCmd">The fleet command.</param>
        /// <param name="findFarthestTgt">if set to <c>true</c> [find farthest target].</param>
        /// <returns></returns>
        private bool __IssueFleetMoveOrder(IFleetCmd fleetCmd, bool findFarthestTgt) {
            List<IFleetNavigable> moveTgts = Knowledge.Starbases.Cast<IFleetNavigable>().ToList();
            moveTgts.AddRange(Knowledge.Settlements.Cast<IFleetNavigable>());
            moveTgts.AddRange(Knowledge.Planets.Cast<IFleetNavigable>());
            //moveTgts.AddRange(Knowledge.Systems.Cast<IFleetNavigable>());   // UNCLEAR or Stars?
            moveTgts.AddRange(Knowledge.Stars.Cast<IFleetNavigable>());
            if (Knowledge.UniverseCenter != null) {
                moveTgts.Add(Knowledge.UniverseCenter as IFleetNavigable);
            }

            if (!moveTgts.Any()) {
                D.LogBold("{0}: {1} can find no MoveTargets that meet the selection criteria.", Name, fleetCmd.FullName);
                return false;
            }
            IFleetNavigable destination;
            if (findFarthestTgt) {
                destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - fleetCmd.Position));
            }
            else {
                destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - fleetCmd.Position));
            }
            //D.Log("{0} move destination is {1}.", fleetCmd.FullName, destination.FullName);
            fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
            return true;
        }

        private void __IssueFleetExploreOrder(IFleetCmd fleetCmd) {
            var explorableUnexploredSystems =
                from sys in Knowledge.Systems
                let eSys = sys as IFleetExplorable
                where eSys.IsExploringAllowedBy(Owner) && !eSys.IsFullyExploredBy(Owner)
                select eSys;
            if (explorableUnexploredSystems.Any()) {
                var closestUnexploredSystem = explorableUnexploredSystems.MinBy(sys => Vector3.SqrMagnitude(fleetCmd.Position - sys.Position));
                //D.Log("{0} is issuing an explore order to {1} with target {2}. IsExploringAllowed = {3}, IsFullyExplored = {4}.",
                //    Name, fleetCmd.FullName, closestUnexploredSystem.FullName, closestUnexploredSystem.IsExploringAllowedBy(Owner), closestUnexploredSystem.IsFullyExploredBy(Owner));
                fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Explore, OrderSource.CmdStaff, closestUnexploredSystem);
            }
            else {
                IFleetExplorable uCenter = Knowledge.UniverseCenter as IFleetExplorable;
                if (!uCenter.IsFullyExploredBy(Owner)) {
                    fleetCmd.CurrentOrder = new FleetOrder(FleetDirective.Explore, OrderSource.CmdStaff, uCenter);
                }
                else {
                    D.LogBold("{0}: Fleet {1} has completed exploration of explorable universe.", Name, fleetCmd.FullName);
                }
            }
        }

        #endregion

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

    }
}

