// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerKnowledge.cs
// Holds the current knowledge base for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
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
    /// Holds the current knowledge base for a player.
    /// </summary>
    public class PlayerKnowledge : IDisposable {

        private const string NameFormat = "{0}'s {1}";

        private string Name { get { return NameFormat.Inject(Player.LeaderName, typeof(PlayerKnowledge).Name); } }

        public Player Player { get; private set; }

        public IUniverseCenterItem UniverseCenter { get; private set; }

        public IEnumerable<IMoonItem> MyMoons { get { return Moons.Where(p => p.Owner == Player); } }

        /// <summary>
        /// The Moons this player has knowledge of.
        /// </summary>
        public IEnumerable<IMoonItem> Moons { get { return _planetoids.Where(p => p is IMoonItem).Cast<IMoonItem>(); } }

        public IEnumerable<IPlanetItem> MyPlanets { get { return Planets.Where(p => p.Owner == Player); } }

        /// <summary>
        /// The Planets this player has knowledge of.
        /// </summary>
        public IEnumerable<IPlanetItem> Planets { get { return _planetoids.Where(p => p is IPlanetItem).Cast<IPlanetItem>(); } }

        /// <summary>
        /// The Planetoids this player has knowledge of.
        /// </summary>
        public IEnumerable<IPlanetoidItem> Planetoids { get { return _planetoids; } }

        public IEnumerable<IPlanetoidItem> MyPlanetoids { get { return _planetoids.Where(p => p.Owner == Player); } }

        public IEnumerable<IStarItem> MyStars { get { return _stars.Where(s => s.Owner == Player); } }

        /// <summary>
        /// The Stars this player has knowledge of.
        /// </summary>
        public IEnumerable<IStarItem> Stars { get { return _stars; } }

        /// <summary>
        /// The Systems this player has knowledge of.
        /// </summary>
        public IEnumerable<ISystemItem> Systems { get { return _systems; } }

        public IEnumerable<ISystemItem> MySystems { get { return _systems.Where(sys => sys.Owner == Player); } }

        /// <summary>
        /// The Elements this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitElementItem> Elements { get { return _elements; } }

        /// <summary>
        /// The Ships this player has knowledge of.
        /// </summary>
        public IEnumerable<IShipItem> Ships { get { return _elements.Where(e => e is IShipItem).Cast<IShipItem>(); } }

        /// <summary>
        /// The Facilities this player has knowledge of.
        /// </summary>
        public IEnumerable<IFacilityItem> Facilities { get { return _elements.Where(e => e is IFacilityItem).Cast<IFacilityItem>(); } }

        /// <summary>
        /// The Commands this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitCmdItem> Commands { get { return _commands; } }

        /// <summary>
        /// The Bases this player has knowledge of.
        /// </summary>
        public IEnumerable<IBaseCmdItem> Bases { get { return _commands.Where(cmd => cmd is IBaseCmdItem).Cast<IBaseCmdItem>(); } }

        /// <summary>
        /// The Fleets this player has knowledge of.
        /// </summary>
        public IEnumerable<IFleetCmdItem> Fleets { get { return _commands.Where(cmd => cmd is IFleetCmdItem).Cast<IFleetCmdItem>(); } }

        /// <summary>
        /// The Settlements this player has knowledge of.
        /// </summary>
        public IEnumerable<ISettlementCmdItem> Settlements { get { return _commands.Where(cmd => cmd is ISettlementCmdItem).Cast<ISettlementCmdItem>(); } }

        /// <summary>
        /// The Starbases this player has knowledge of.
        /// </summary>
        public IEnumerable<IStarbaseCmdItem> Starbases { get { return _commands.Where(cmd => cmd is IStarbaseCmdItem).Cast<IStarbaseCmdItem>(); } }

        public IEnumerable<IFleetCmdItem> MyFleets { get { return Fleets.Where(cmd => cmd.Owner == Player); } }

        public IEnumerable<ISettlementCmdItem> MySettlements { get { return Settlements.Where(cmd => cmd.Owner == Player); } }

        public IEnumerable<IStarbaseCmdItem> MyStarbases { get { return Starbases.Where(cmd => cmd.Owner == Player); } }

        public IEnumerable<IBaseCmdItem> MyBases { get { return Bases.Where(cmd => cmd.Owner == Player); } }

        private HashSet<IPlanetoidItem> _planetoids = new HashSet<IPlanetoidItem>();
        private HashSet<IStarItem> _stars = new HashSet<IStarItem>();
        private HashSet<ISystemItem> _systems = new HashSet<ISystemItem>();
        private HashSet<IUnitElementItem> _elements = new HashSet<IUnitElementItem>();
        private HashSet<IUnitCmdItem> _commands = new HashSet<IUnitCmdItem>();

        private DebugSettings _debugSettings;

        public PlayerKnowledge(Player player) {
            D.Assert(player != null && player != TempGameValues.NoPlayer);
            Player = player;
            _debugSettings = DebugSettings.Instance;
        }

        /// <summary>
        /// Indicates whether the player has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IDiscernibleItem item) {
            Utility.ValidateNotNull(item);
            if (item is IPlanetoidItem) {
                return _planetoids.Contains(item as IPlanetoidItem);
            }
            if (item is IUnitElementItem) {
                return _elements.Contains(item as IUnitElementItem);
            }
            if (item is IUnitCmdItem) {
                return _commands.Contains(item as IUnitCmdItem);
            }
            if (item is IUniverseCenterItem) {
                D.Assert(UniverseCenter == item);
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            if (item is IStarItem) {
                D.Assert(_stars.Contains(item as IStarItem));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            if (item is ISystemItem) {
                D.Assert(_systems.Contains(item as ISystemItem));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            return false;
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
            if (tType == typeof(IStarbaseCmdItem)) {
                itemCandidates = MyStarbases.Cast<T>();
            }
            else if (tType == typeof(ISettlementCmdItem)) {
                itemCandidates = MySettlements.Cast<T>();
            }
            else if (tType == typeof(IBaseCmdItem)) {
                itemCandidates = MyBases.Cast<T>();
            }
            else if (tType == typeof(IFleetCmdItem)) {
                itemCandidates = MyFleets.Cast<T>();
            }
            else if (tType == typeof(ISystemItem)) {
                itemCandidates = MySystems.Cast<T>();
            }
            else if (tType == typeof(IPlanetItem)) {
                itemCandidates = MyPlanets.Cast<T>();
            }
            else if (tType == typeof(IMoonItem)) {
                itemCandidates = MyMoons.Cast<T>();
            }
            else if (tType == typeof(IPlanetoidItem)) {
                itemCandidates = MyPlanetoids.Cast<T>();
            }
            else if (tType == typeof(IStarItem)) {
                itemCandidates = MyStars.Cast<T>();
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
        /// Returns <c>true</c> if one was found, <c>false</c> otherwise. The result will be 
        /// an item owned by the Player if that is closest.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="closestItem">The returned closest item. Null if returns <c>false</c>.</param>
        /// <param name="excludedItems">The items to exclude, if any.</param>
        /// <returns></returns>
        public bool TryFindClosestKnownItem<T>(Vector3 worldPosition, out T closestItem, params T[] excludedItems) where T : IItem {
            Type tType = typeof(T);
            IEnumerable<T> itemCandidates = null;
            if (tType == typeof(IStarbaseCmdItem)) {
                itemCandidates = Starbases.Cast<T>();
            }
            else if (tType == typeof(ISettlementCmdItem)) {
                itemCandidates = Settlements.Cast<T>();
            }
            else if (tType == typeof(IBaseCmdItem)) {
                itemCandidates = Bases.Cast<T>();
            }
            else if (tType == typeof(IFleetCmdItem)) {
                itemCandidates = Fleets.Cast<T>();
            }
            else if (tType == typeof(ISystemItem)) {
                itemCandidates = Systems.Cast<T>();
            }
            else if (tType == typeof(IPlanetItem)) {
                itemCandidates = Planets.Cast<T>();
            }
            else if (tType == typeof(IMoonItem)) {
                itemCandidates = Moons.Cast<T>();
            }
            else if (tType == typeof(IPlanetoidItem)) {
                itemCandidates = Planetoids.Cast<T>();
            }
            else if (tType == typeof(IStarItem)) {
                itemCandidates = Stars.Cast<T>();
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
        public void HandleItemDetection(IIntelItem detectedItem) {
            D.Assert(detectedItem is ISensorDetectable);
            D.Assert(detectedItem.IsOperational, "{0}: NonOperational Item {1} erroneously detected.", Name, detectedItem.FullName);
            if (detectedItem is IStarItem || detectedItem is IUniverseCenterItem) {
                return; // these are added at startup and never removed so no need to add again
            }
            D.Log("{0} is adding {1}.", Name, detectedItem.FullName);
            if (detectedItem is IUnitElementItem) {
                AddElement(detectedItem as IUnitElementItem);
            }
            else if (detectedItem is IPlanetoidItem) {
                AddPlanetoid(detectedItem as IPlanetoidItem);
            }
            else {
                D.Warn("{0} cannot yet add {1}.", Name, detectedItem.FullName);
            }
        }

        /// <summary>
        /// Called when an item that was detected by <c>Player</c> is no 
        /// longer detected by <c>Player</c> at all. 
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        public void HandleItemDetectionLost(IIntelItem detectedItem) {
            D.Assert(detectedItem is ISensorDetectable);
            var element = detectedItem as IUnitElementItem;
            if (element != null) {
                // no need to test element for death as it gets removed when
                // it loses detection no matter the cause
                RemoveElement(element);
            }
            else {
                var planetoid = detectedItem as IPlanetoidItem;
                if (planetoid != null && !planetoid.IsOperational) {
                    // planetoids are only removed when they lose detection because they are dying
                    RemoveDeadPlanetoid(planetoid);
                }
            }
        }

        public void AddStar(IStarItem star) {
            // A Star should only be added once when all players get Basic IntelCoverage of all stars
            D.Assert(_stars.Add(star), "{0} tried to add Star {1} it already has.", Name, star.FullName);
            var starIntelCoverage = _debugSettings.AllIntelCoverageComprehensive ? IntelCoverage.Comprehensive : IntelCoverage.Basic;
            D.Assert(star.GetIntelCoverage(Player) == starIntelCoverage);
            AddSystem(star.System);
        }

        public void AddUniverseCenter(IUniverseCenterItem universeCenter) {
            D.Assert(UniverseCenter == null);   // should only be added once when all players get Basic IntelCoverage of UCenter
            var ucIntelCoverage = _debugSettings.AllIntelCoverageComprehensive ? IntelCoverage.Comprehensive : IntelCoverage.Basic;
            D.Assert(universeCenter.GetIntelCoverage(Player) == ucIntelCoverage);
            UniverseCenter = universeCenter;
        }

        private void AddPlanetoid(IPlanetoidItem planetoid) {
            bool isAdded = _planetoids.Add(planetoid);
            if (!isAdded) {
                D.Log("{0} tried to add Planet {1} it already has.", Name, planetoid.FullName);
                return;
            }
            D.Assert(planetoid.GetIntelCoverage(Player) > IntelCoverage.None);
        }

        private void AddElement(IUnitElementItem element) {
            var isAdded = _elements.Add(element);
            if (!isAdded) {
                D.Log("{0} tried to add Element {1} it already has.", Name, element.FullName);
                return;
            }
            element.isHQChanged += ElementIsHQChangedEventHandler;

            D.Assert(element.GetIntelCoverage(Player) > IntelCoverage.None);
            if (element.IsHQ) {
                AddCommand(element.Command);
            }
        }

        #region Event and Property Change Handlers

        private void ElementIsHQChangedEventHandler(object sender, EventArgs e) {
            IUnitElementItem element = sender as IUnitElementItem;
            if (element.IsHQ) {
                // this known element is now a HQ
                AddCommand(element.Command);
            }
            else {
                // this known element is no longer a HQ
                RemoveCommand(element.Command);
            }
        }

        #endregion

        private void AddCommand(IUnitCmdItem command) {
            var isAdded = _commands.Add(command);
            D.Assert(isAdded);  // Cmd cannot already be present. If adding due to a change in an element's IsHQ state, then previous HQElement removed Cmd before this Add
            D.Log("{0} has added Command {1}.", Name, command.FullName);
        }

        private void RemoveCommand(IUnitCmdItem command) {
            var isRemoved = _commands.Remove(command);
            D.Assert(isRemoved);
            D.Log("{0} has removed Command {1}.", Name, command.FullName);
        }

        private void AddSystem(ISystemItem system) {
            D.Assert(_systems.Add(system), "{0} tried to add System {1} it already has.", Name, system.FullName);
        }

        /// <summary>
        /// Removes the element from this player's knowledge. Element's are
        /// removed when they lose all <c>Player</c> detection. Losing all
        /// detection by <c>Player</c> occurs for 2 reasons - no Player Cmds
        /// in sensor range and death.
        /// </summary>
        /// <param name="element">The element.</param>
        private void RemoveElement(IUnitElementItem element) {
            var isRemoved = _elements.Remove(element);
            D.Assert(isRemoved, "{0} could not remove Element {1}.", Name, element.FullName);

            element.isHQChanged -= ElementIsHQChangedEventHandler;
            if (element.IsHQ) {
                _commands.Remove(element.Command);
            }
        }

        /// <summary>
        /// Removes the dead planetoid from the knowledge of all players.
        /// If the deadPlanetoid is the only source of knowledge a player has
        /// about a System, then that System is also removed from that player's
        /// knowledge.
        /// </summary>
        /// <param name="deadPlanetoid">The dead planetoid.</param>
        private void RemoveDeadPlanetoid(IPlanetoidItem deadPlanetoid) {
            D.Assert(!deadPlanetoid.IsOperational);
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            D.Assert(isRemoved, "{0} could not remove Planetoid {1}.", Name, deadPlanetoid.FullName);
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _elements.ForAll(e => e.isHQChanged -= ElementIsHQChangedEventHandler);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

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

