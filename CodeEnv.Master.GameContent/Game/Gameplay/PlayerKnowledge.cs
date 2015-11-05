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

        public Player Player { get; private set; }

        public IUniverseCenterItem UniverseCenter { get; private set; }

        /// <summary>
        /// The Planetoids this player has knowledge of.
        /// </summary>
        public IEnumerable<IPlanetoidItem> Planetoids { get { return _planetoids; } }

        public IEnumerable<IPlanetoidItem> MyPlanetoids { get { return _planetoids.Where(p => p.Owner == Player); } }

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

        public IEnumerable<IShipItem> Ships { get { return _elements.Where(e => e is IShipItem).Cast<IShipItem>(); } }

        public IEnumerable<IFacilityItem> Facilities { get { return _elements.Where(e => e is IFacilityItem).Cast<IFacilityItem>(); } }

        /// <summary>
        /// The Commands this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitCmdItem> Commands { get { return _commands; } }

        public IEnumerable<IBaseCmdItem> Bases { get { return _commands.Where(cmd => cmd is IBaseCmdItem).Cast<IBaseCmdItem>(); } }

        public IEnumerable<IFleetCmdItem> Fleets { get { return _commands.Where(cmd => cmd is IFleetCmdItem).Cast<IFleetCmdItem>(); } }

        public IEnumerable<ISettlementCmdItem> Settlements { get { return _commands.Where(cmd => cmd is ISettlementCmdItem).Cast<ISettlementCmdItem>(); } }

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

        public PlayerKnowledge(Player player) {
            D.Assert(player != null && player != TempGameValues.NoPlayer);
            Player = player;
        }

        /// <summary>
        /// Indicates whether the player has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IDiscernibleItem item) {
            if (item is ISystemItem) {
                return _systems.Contains(item as ISystemItem);
            }
            if (item is IStarItem) {
                return _stars.Contains(item as IStarItem);
            }
            if (item is IPlanetoidItem) {
                return _planetoids.Contains(item as IPlanetoidItem);
            }
            if (item is IUnitElementItem) {
                return _elements.Contains(item as IUnitElementItem);
            }
            if (item is IUnitCmdItem) {
                return _commands.Contains(item as IUnitCmdItem);
            }
            return false;
        }

        /// <summary>
        /// Gets the closest base owned by this player to <c>worldPosition</c>, if any. Can be null.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="exludedBases">The bases to exclude from consideration, if any.</param>
        /// <returns></returns>
        public IBaseCmdItem GetMyClosestBase(Vector3 worldPosition, params IBaseCmdItem[] exludedBases) {
            IBaseCmdItem closestBase = null;
            var candidates = MyBases.Except(exludedBases);
            if (candidates.Any()) {
                closestBase = candidates.MinBy(cmd => Vector3.SqrMagnitude(cmd.Position - worldPosition));
            }
            return closestBase;
        }

        /// <summary>
        /// Called whenever an Item is detected by <c>Player</c>, no matter how
        /// many times the item has been detected previously. This method ignores
        /// items that this player already has knowledge of.
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        public void OnItemDetected(IIntelItem detectedItem) {
            D.Log("{0}'s {1} is adding {2}.", Player.LeaderName, GetType().Name, detectedItem.FullName);
            if (detectedItem is IUnitElementItem) {
                AddElement(detectedItem as IUnitElementItem);
            }
            else if (detectedItem is IPlanetoidItem) {
                AddPlanetoid(detectedItem as IPlanetoidItem);
            }
            else if (detectedItem is IStarItem) {
                AddStar(detectedItem as IStarItem);
            }
            else if (detectedItem is IUniverseCenterItem) {
                AddUniverseCenter(detectedItem as IUniverseCenterItem);
            }
            else {
                D.Warn("{0}'s {1} cannot yet add {2}.", Player.LeaderName, GetType().Name, detectedItem.FullName);
            }
        }

        /// <summary>
        /// Called when an item that was detected by <c>Player</c> is no 
        /// longer detected by <c>Player</c> at all. 
        /// </summary>
        /// <param name="detectedItem">The detected item.</param>
        public void OnItemDetectionLost(IIntelItem detectedItem) {
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
            var playerIntelCoverage = star.GetIntelCoverage(Player);
            D.Assert(playerIntelCoverage != IntelCoverage.None);
            if (playerIntelCoverage > IntelCoverage.Basic) {
                D.Log("{0}'s {1}: IntelCoverage for {2} = {3}.", Player.LeaderName, GetType().Name, star.FullName, playerIntelCoverage.GetValueName());
                AddSystem(star.System);
            }

            bool isAdded = _stars.Add(star);
            if (!isAdded) {
                D.Log("{0}'s {1} tried to add Star {2} it already has.", Player.LeaderName, GetType().Name, star.FullName);
                return;
            }
        }

        public void AddUniverseCenter(IUniverseCenterItem universeCenter) {
            if (UniverseCenter != null) {
                D.Log("{0}'s {1} tried to add {2} it already has.", Player.LeaderName, GetType().Name, universeCenter.FullName);
                return;
            }
            UniverseCenter = universeCenter;
        }

        private void AddPlanetoid(IPlanetoidItem planetoid) {
            bool isAdded = _planetoids.Add(planetoid);
            if (!isAdded) {
                D.Log("{0}'s {1} tried to add Planet {2} it already has.", Player.LeaderName, GetType().Name, planetoid.FullName);
                return;
            }

            D.Assert(planetoid.GetIntelCoverage(Player) > IntelCoverage.None);
            AddSystem(planetoid.System);
        }

        private void AddElement(IUnitElementItem element) {
            var isAdded = _elements.Add(element);
            if (!isAdded) {
                D.Log("{0}'s {1} tried to add Element {2} it already has.", Player.LeaderName, GetType().Name, element.FullName);
                return;
            }

            element.onIsHQChanged += OnElementIsHQChanged;

            D.Assert(element.GetIntelCoverage(Player) > IntelCoverage.None);
            if (element.IsHQ) {
                AddCommand(element.Command);
            }
        }

        private void OnElementIsHQChanged(IUnitElementItem element) {
            if (element.IsHQ) {
                // this known element is now a HQ
                AddCommand(element.Command);
            }
            else {
                // this known element is no longer a HQ
                RemoveCommand(element.Command);
            }
        }

        private void AddCommand(IUnitCmdItem command) {
            var isAdded = _commands.Add(command);
            D.Assert(isAdded);  // Cmd cannot already be present. If adding due to OnElementIsHQChanged(), then previous HQElement removed Cmd before this Add
            D.Log("{0}'s {1} has added Command {2}.", Player.LeaderName, GetType().Name, command.FullName);
        }

        private void RemoveCommand(IUnitCmdItem command) {
            var isRemoved = _commands.Remove(command);
            D.Assert(isRemoved);
            D.Log("{0}'s {1} has removed Command {2}.", Player.LeaderName, GetType().Name, command.FullName);
        }

        private void AddSystem(ISystemItem system) {
            var isAdded = _systems.Add(system); // adding system can fail as it can already be present from the discovery of other members
            if (isAdded) {
                D.Log("{0}'s {1} has added System {2}.", Player.LeaderName, GetType().Name, system.FullName);
                if (Player.IsUser) {
                    // the User just discovered this system for the first time
                    system.OnUserDiscoveredSystem();
                }
            }
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
            D.Assert(isRemoved, "{0}'s {1} could not remove Element {2}.".Inject(Player.LeaderName, GetType().Name, element.FullName));

            element.onIsHQChanged -= OnElementIsHQChanged;
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
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            D.Assert(isRemoved, "{0}'s {1} could not remove Planetoid {2}.".Inject(Player.LeaderName, GetType().Name, deadPlanetoid.FullName));
            var system = deadPlanetoid.System;
            if (ShouldSystemBeRemoved(system)) {
                _systems.Remove(system);
            }
        }

        /// <summary>
        /// Determines if the System should be removed from the player's knowledge.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <returns></returns>
        private bool ShouldSystemBeRemoved(ISystemItem system) {
            var remainingKnownPlanetoidsInSystem = _planetoids.Where(p => p.System == system);
            var knownStarInSystem = _stars.SingleOrDefault(star => star.System == system);
            return knownStarInSystem == null && !remainingKnownPlanetoidsInSystem.Any();
        }

        private void Cleanup() {
            Unsubscribe();
            // other cleanup here including any tracking Gui2D elements
        }

        private void Unsubscribe() {
            _elements.ForAll(e => e.onIsHQChanged -= OnElementIsHQChanged);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

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

    }
}

