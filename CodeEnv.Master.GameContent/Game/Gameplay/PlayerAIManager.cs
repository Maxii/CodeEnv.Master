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

        private string Name { get { return NameFormat.Inject(Player.LeaderName, GetType().Name); } }

        public PlayerKnowledge Knowledge { get; private set; }

        public Player Player { get; private set; }

        public IEnumerable<Player> OtherKnownPlayers { get { return Player.OtherKnownPlayers; } }

        private DebugSettings _debugSettings;

        public PlayerAIManager(Player player, PlayerKnowledge knowledge) {
            Player = player;
            Knowledge = knowledge;
            _debugSettings = DebugSettings.Instance;
            Subscribe();
        }

        private void Subscribe() {
            SubscribeToPlayerRelationsChange(Player);
        }

        private void SubscribeToPlayerRelationsChange(Player player) {
            player.relationsChanged += RelationsChangedEventHandler;
        }

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
                itemCandidates = Knowledge.MyStarbases.Cast<T>();
            }
            else if (tType == typeof(ISettlementCmd)) {
                itemCandidates = Knowledge.MySettlements.Cast<T>();
            }
            else if (tType == typeof(IUnitBaseCmd)) {
                itemCandidates = Knowledge.MyBases.Cast<T>();
            }
            else if (tType == typeof(IFleetCmd)) {
                itemCandidates = Knowledge.MyFleets.Cast<T>();
            }
            else if (tType == typeof(ISystem)) {
                itemCandidates = Knowledge.MySystems.Cast<T>();
            }
            else if (tType == typeof(IPlanet)) {
                itemCandidates = Knowledge.MyPlanets.Cast<T>();
            }
            else if (tType == typeof(IMoon)) {
                itemCandidates = Knowledge.MyMoons.Cast<T>();
            }
            else if (tType == typeof(IPlanetoid)) {
                itemCandidates = Knowledge.MyPlanetoids.Cast<T>();
            }
            else if (tType == typeof(IStar)) {
                itemCandidates = Knowledge.MyStars.Cast<T>();
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

        /// <summary>
        /// Called whenever an Item is detected by <c>Player</c>, no matter how
        /// many times the item has been detected previously. This method ignores
        /// items that this player already has knowledge of.
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        public void HandleItemDetection(ISensorDetectable detectedItem) {
            if (detectedItem is IStar_Ltd || detectedItem is IUniverseCenter_Ltd) {
                return; // these are added at startup and never removed so no need to add again
            }
            D.Assert(detectedItem.IsOperational, "{0}: NonOperational Item {1} erroneously detected.", Name, detectedItem.FullName);

            var element = detectedItem as IUnitElement_Ltd;
            if (element != null) {
                // 7.14.16 Eliminated rqmt to be newly discovered element as most newly discovered elements will be at LongRange with no access to Owner
                // bool isNewlyDiscoveredElement = Knowledge.AddElement(element);  
                Knowledge.AddElement(element);

                // Note: even if _debugSettings.AllIntelCoverageComprehensive = true, elements are not pre-populated into Knowledge 
                // since they can be created in runtime.
                if (element.IsHQ) {
                    CheckForDiscoveryOfNewPlayer(element);
                }
            }
            else {
                var planetoid = detectedItem as IPlanetoid_Ltd;
                D.Assert(planetoid != null, "{0}: Unanticipated Type {1} attempting to add {2}.", Name, detectedItem.GetType().Name, detectedItem.FullName);
                if (_debugSettings.AllIntelCoverageComprehensive) {
                    // all planetoids are already known as each Knowledge was fully populated with them before game start
                    D.Assert(Knowledge.HasKnowledgeOf(planetoid));
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
                D.Assert(planetoid != null, "{0}: Unanticipated Type {1} attempting to remove {2}.", Name, detectedItem.GetType().Name, detectedItem.FullName);
                // planetoids are not removed when they lose detection as they can't regress IntelCoverage
                D.Assert(planetoid.IsOperational);
            }
        }

        private void CheckForDiscoveryOfNewPlayer(IUnitElement_Ltd element) {
            Player newlyDiscoveredPlayerCandidate;
            if (element.TryGetOwner(Player, out newlyDiscoveredPlayerCandidate)) {
                if (newlyDiscoveredPlayerCandidate == Player) {
                    // Note: The new HQ element just detected that generated this check is one of our own. 
                    // This typically occurs during the initial process of detecting what is in range of sensors when the game first starts.
                    return;
                }
                bool isAlreadyKnown = Player.IsKnown(newlyDiscoveredPlayerCandidate);
                if (!isAlreadyKnown) {
                    Player newlyDiscoveredPlayer = newlyDiscoveredPlayerCandidate;
                    D.LogBold("{0} discovered new player {1}.", Name, newlyDiscoveredPlayer.LeaderName);
                    SubscribeToPlayerRelationsChange(newlyDiscoveredPlayer);

                    Player.AddNewlyDiscovered(newlyDiscoveredPlayer, __GetInitialRelationship(newlyDiscoveredPlayer));
                }
            }
        }

        #region Event and Property Change Event Handlers

        private void RelationsChangedEventHandler(object sender, RelationsChangedEventArgs e) {
            Player sendingPlayer = sender as Player;
            // Only send one of the (always) two events to our Cmds
            if (sendingPlayer == Player) {
                HandleRelationsChanged(e.EffectedPlayer, e.PriorRelationship, e.NewRelationship);
            }
        }

        #endregion

        private void HandleRelationsChanged(Player otherPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
            D.Assert(otherPlayer != Player);
            D.Log("Relations have changed from {0} to {1} between {2} and {3}.", priorRelationship.GetValueName(), newRelationship.GetValueName(), Player.LeaderName, otherPlayer.LeaderName);
            Knowledge.MyCommands.ForAll(myCmd => myCmd.HandleRelationsChanged(otherPlayer, priorRelationship, newRelationship));
        }

        private void Cleanup() {
            Unsubscribe();
            Knowledge.Dispose();
        }

        private void Unsubscribe() {
            Player.relationsChanged -= RelationsChangedEventHandler;
            OtherKnownPlayers.ForAll(player => player.relationsChanged -= RelationsChangedEventHandler);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug Initial Diplomatic Relationships

        public IEnumerable<Player> __PlayersWithAssignedInitialRelationships { get { return __initialDiploRelationLookup.Keys; } }

        private IDictionary<Player, DiplomaticRelationship> __initialDiploRelationLookup = new Dictionary<Player, DiplomaticRelationship>(5);

        public bool __TryGetPlayersWithAssignedInitialRelationship(DiplomaticRelationship initialRelationship, out IEnumerable<Player> players) {
            if (!__initialDiploRelationLookup.Values.Contains(initialRelationship)) {
                players = Enumerable.Empty<Player>();
                return false;
            }
            IList<Player> relationshipPlayers = new List<Player>(5);
            foreach (var player in __initialDiploRelationLookup.Keys) {
                if (__initialDiploRelationLookup[player] == initialRelationship) {
                    relationshipPlayers.Add(player);
                }
            }
            players = relationshipPlayers;
            return true;
        }

        /// <summary>
        /// Workaround while using UnitCreators to record the initial relationship between the User
        /// and the AIPlayers in the game (those that have instantiated Commands). This initial 
        /// relationship is applied when the players first meet.
        /// <remarks>I want the initial relationship to be assigned to the players WHEN THEY MEET.
        /// Using Player.SetRelations() from UnitCreators has protection against setting a relationship
        /// with a player not yet met, and Player.AddNewlyDiscovered(player) results in the players
        /// meeting before the game starts.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="relationship">The relationship.</param>
        public void __AssignInitialDiploRelation(Player player, DiplomaticRelationship relationship) {
            D.Assert(!__initialDiploRelationLookup.ContainsKey(player));
            __initialDiploRelationLookup[player] = relationship;
        }

        private DiplomaticRelationship __GetInitialRelationship(Player newlyDiscoveredPlayer) {
            DiplomaticRelationship initialRelationship = DiplomaticRelationship.Neutral;
            DiplomaticRelationship storedInitialRelationship;
            if (__initialDiploRelationLookup.TryGetValue(newlyDiscoveredPlayer, out storedInitialRelationship)) {
                initialRelationship = storedInitialRelationship;
            }
            return initialRelationship;
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

    }
}

