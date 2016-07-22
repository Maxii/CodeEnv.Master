// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerKnowledge.cs
// Holds the current knowledge of a player about items in the universe.
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
    /// Holds the current knowledge of a player about items in the universe.
    /// What is known by the player about each item is available through the item from Reports.
    /// </summary>
    public class PlayerKnowledge : IDisposable {

        private const string NameFormat = "{0}'s {1}";

        private string Name { get { return NameFormat.Inject(Player.LeaderName, typeof(PlayerKnowledge).Name); } }

        public Player Player { get; private set; }

        public IUniverseCenter_Ltd UniverseCenter { get; private set; }

        public IEnumerable<IMoon> MyMoons {
            get {
                var myMoons = new List<IMoon>();
                Player moonOwner;
                Moons.ForAll(m => {
                    if (m.TryGetOwner(Player, out moonOwner) && moonOwner == Player) {
                        myMoons.Add(m as IMoon);
                    }
                });
                return myMoons;
            }
        }

        public IEnumerable<IPlanet> MyPlanets {
            get {
                var myPlanets = new List<IPlanet>();
                Player planetOwner;
                Planets.ForAll(p => {
                    if (p.TryGetOwner(Player, out planetOwner) && planetOwner == Player) {
                        myPlanets.Add(p as IPlanet);
                    }
                });
                return myPlanets;
            }
        }

        public IEnumerable<IPlanetoid> MyPlanetoids {
            get {
                var myPlanetoids = new List<IPlanetoid>();
                Player planetoidOwner;
                Planetoids.ForAll(p => {
                    if (p.TryGetOwner(Player, out planetoidOwner) && planetoidOwner == Player) {
                        myPlanetoids.Add(p as IPlanetoid);
                    }
                });
                return myPlanetoids;
            }
        }

        public IEnumerable<IStar> MyStars {
            get {
                var myStars = new List<IStar>();
                Player starOwner;
                Stars.ForAll(s => {
                    if (s.TryGetOwner(Player, out starOwner) && starOwner == Player) {
                        myStars.Add(s as IStar);
                    }
                });
                return myStars;
            }
        }

        public IEnumerable<ISystem> MySystems {
            get {
                var mySystems = new List<ISystem>();
                Player systemOwner;
                Systems.ForAll(s => {
                    if (s.TryGetOwner(Player, out systemOwner) && systemOwner == Player) {
                        mySystems.Add(s as ISystem);
                    }
                });
                return mySystems;
            }
        }

        public IEnumerable<IUnitCmd> MyCommands {
            get {
                var myCmds = new List<IUnitCmd>();
                Player cmdOwner;
                _commands.ForAll(cmd => {
                    if (cmd.TryGetOwner(Player, out cmdOwner) && cmdOwner == Player) {
                        myCmds.Add(cmd as IUnitCmd);
                    }
                });
                return myCmds;
            }
        }

        public IEnumerable<IFleetCmd> MyFleets {
            get {
                var myFleets = new List<IFleetCmd>();
                Player fleetOwner;
                Fleets.ForAll(f => {
                    if (f.TryGetOwner(Player, out fleetOwner) && fleetOwner == Player) {
                        myFleets.Add(f as IFleetCmd);
                    }
                });
                return myFleets;
            }
        }

        public IEnumerable<ISettlementCmd> MySettlements {
            get {
                var mySettlements = new List<ISettlementCmd>();
                Player settlementOwner;
                Settlements.ForAll(s => {
                    if (s.TryGetOwner(Player, out settlementOwner) && settlementOwner == Player) {
                        mySettlements.Add(s as ISettlementCmd);
                    }
                });
                return mySettlements;
            }
        }

        public IEnumerable<IStarbaseCmd> MyStarbases {
            get {
                var myStarbases = new List<IStarbaseCmd>();
                Player starbaseOwner;
                Starbases.ForAll(s => {
                    if (s.TryGetOwner(Player, out starbaseOwner) && starbaseOwner == Player) {
                        myStarbases.Add(s as IStarbaseCmd);
                    }
                });
                return myStarbases;
            }
        }

        public IEnumerable<IUnitBaseCmd> MyBases {
            get {
                var myBases = new List<IUnitBaseCmd>();
                Player baseOwner;
                Bases.ForAll(b => {
                    if (b.TryGetOwner(Player, out baseOwner) && baseOwner == Player) {
                        myBases.Add(b as IUnitBaseCmd);
                    }
                });
                return myBases;
            }
        }

        public IEnumerable<IUnitElement> MyElements {
            get {
                var myElements = new List<IUnitElement>();
                Player elementOwner;
                Elements.ForAll(e => {
                    if (e.TryGetOwner(Player, out elementOwner) && elementOwner == Player) {
                        myElements.Add(e as IUnitElement);
                    }
                });
                return myElements;
            }
        }

        public IEnumerable<IShip> MyShips {
            get {
                var myShips = new List<IShip>();
                Player shipOwner;
                Ships.ForAll(s => {
                    if (s.TryGetOwner(Player, out shipOwner) && shipOwner == Player) {
                        myShips.Add(s as IShip);
                    }
                });
                return myShips;
            }
        }

        public IEnumerable<IFacility> MyFacilities {
            get {
                var myFacilities = new List<IFacility>();
                Player facilityOwner;
                Facilities.ForAll(f => {
                    if (f.TryGetOwner(Player, out facilityOwner) && facilityOwner == Player) {
                        myFacilities.Add(f as IFacility);
                    }
                });
                return myFacilities;
            }
        }

        /// <summary>
        /// The Moons this player has knowledge of.
        /// </summary>
        public IEnumerable<IMoon_Ltd> Moons { get { return _planetoids.Where(p => p is IMoon_Ltd).Cast<IMoon_Ltd>(); } }

        /// <summary>
        /// The Planets this player has knowledge of.
        /// </summary>
        public IEnumerable<IPlanet_Ltd> Planets { get { return _planetoids.Where(p => p is IPlanet_Ltd).Cast<IPlanet_Ltd>(); } }

        /// <summary>
        /// The Planetoids this player has knowledge of.
        /// </summary>
        public IEnumerable<IPlanetoid_Ltd> Planetoids { get { return _planetoids; } }

        /// <summary>
        /// The Stars this player has knowledge of.
        /// </summary>
        public IEnumerable<IStar_Ltd> Stars { get { return _stars; } }

        /// <summary>
        /// The Systems this player has knowledge of.
        /// </summary>
        public IEnumerable<ISystem_Ltd> Systems { get { return _systems; } }

        /// <summary>
        /// The Elements this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitElement_Ltd> Elements { get { return _elements; } }

        /// <summary>
        /// The Ships this player has knowledge of.
        /// </summary>
        public IEnumerable<IShip_Ltd> Ships { get { return _elements.Where(e => e is IShip_Ltd).Cast<IShip_Ltd>(); } }

        /// <summary>
        /// The Facilities this player has knowledge of.
        /// </summary>
        public IEnumerable<IFacility_Ltd> Facilities { get { return _elements.Where(e => e is IFacility_Ltd).Cast<IFacility_Ltd>(); } }

        /// <summary>
        /// The Commands this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitCmd_Ltd> Commands { get { return _commands; } }

        /// <summary>
        /// The Bases this player has knowledge of.
        /// </summary>
        public IEnumerable<IUnitBaseCmd_Ltd> Bases { get { return _commands.Where(cmd => cmd is IUnitBaseCmd_Ltd).Cast<IUnitBaseCmd_Ltd>(); } }

        /// <summary>
        /// The Fleets this player has knowledge of.
        /// </summary>
        public IEnumerable<IFleetCmd_Ltd> Fleets { get { return _commands.Where(cmd => cmd is IFleetCmd_Ltd).Cast<IFleetCmd_Ltd>(); } }

        /// <summary>
        /// The Settlements this player has knowledge of.
        /// </summary>
        public IEnumerable<ISettlementCmd_Ltd> Settlements { get { return _commands.Where(cmd => cmd is ISettlementCmd_Ltd).Cast<ISettlementCmd_Ltd>(); } }

        /// <summary>
        /// The Starbases this player has knowledge of.
        /// </summary>
        public IEnumerable<IStarbaseCmd_Ltd> Starbases { get { return _commands.Where(cmd => cmd is IStarbaseCmd_Ltd).Cast<IStarbaseCmd_Ltd>(); } }

        // Note: Other players this Player has met is held by the Player

        private HashSet<IPlanetoid_Ltd> _planetoids = new HashSet<IPlanetoid_Ltd>();
        private HashSet<IStar_Ltd> _stars = new HashSet<IStar_Ltd>();
        private HashSet<ISystem_Ltd> _systems = new HashSet<ISystem_Ltd>();
        private HashSet<IUnitElement_Ltd> _elements = new HashSet<IUnitElement_Ltd>();
        private HashSet<IUnitCmd_Ltd> _commands = new HashSet<IUnitCmd_Ltd>();
        private DebugSettings _debugSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerKnowledge" /> class.
        /// <remarks>Used to create the instance when DebugSettings.AllIntelCoverageComprehensive = true.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="uCenter">The UniverseCenter.</param>
        /// <param name="allStars">All stars.</param>
        /// <param name="allPlanetoids">All planetoids.</param>
        public PlayerKnowledge(Player player, IUniverseCenter_Ltd uCenter, IEnumerable<IStar_Ltd> allStars, IEnumerable<IPlanetoid_Ltd> allPlanetoids)
            : this(player, uCenter, allStars) {
            allPlanetoids.ForAll(planetoid => AddPlanetoid(planetoid));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerKnowledge"/> class.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="uCenter">The UniverseCenter.</param>
        /// <param name="allStars">All stars.</param>
        public PlayerKnowledge(Player player, IUniverseCenter_Ltd uCenter, IEnumerable<IStar_Ltd> allStars) {
            D.Assert(player != null && player != TempGameValues.NoPlayer);
            Player = player;
            _debugSettings = DebugSettings.Instance;
            AddUniverseCenter(uCenter);
            allStars.ForAll(star => AddStar(star));
        }

        /// <summary>
        /// Indicates whether the player has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IItem_Ltd item) {
            Utility.ValidateNotNull(item);
            if (item is IPlanetoid_Ltd) {
                return _planetoids.Contains(item as IPlanetoid_Ltd);
            }
            if (item is IUnitElement_Ltd) {
                return _elements.Contains(item as IUnitElement_Ltd);
            }
            if (item is IUnitCmd_Ltd) {
                return _commands.Contains(item as IUnitCmd_Ltd);
            }
            if (item is IUniverseCenter_Ltd) {
                D.Assert(UniverseCenter == item);
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            if (item is IStar_Ltd) {
                D.Assert(_stars.Contains(item as IStar_Ltd));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            if (item is ISystem_Ltd) {
                D.Assert(_systems.Contains(item as ISystem_Ltd));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", Name, item.FullName);
                return true;
            }
            return false;
        }

        private void AddStar(IStar_Ltd star) {
            // A Star should only be added once when all players get Basic IntelCoverage of all stars
            bool isAdded = _stars.Add(star);
            D.Assert(isAdded, "{0} tried to add Star {1} it already has.", Name, star.FullName);
            AddSystem(star.ParentSystem);
        }

        private void AddUniverseCenter(IUniverseCenter_Ltd universeCenter) {
            D.Assert(UniverseCenter == null);   // should only be added once when all players get Basic IntelCoverage of UCenter
            UniverseCenter = universeCenter;
        }

        internal void AddPlanetoid(IPlanetoid_Ltd planetoid) {
            bool isAdded = _planetoids.Add(planetoid);
            if (!isAdded) {
                D.Log("{0} tried to add Planet {1} it already has.", Name, planetoid.FullName);
                return;
            }
            planetoid.deathOneShot += ItemDeathEventHandler;
        }

        internal void AddElement(IUnitElement_Ltd element) {
            var isAdded = _elements.Add(element);
            if (!isAdded) {
                D.Log("{0} tried to add Element {1} it already has.", Name, element.FullName);
                return;
            }
            element.isHQChanged += ElementIsHQChangedEventHandler;

            if (element.IsHQ) {
                AddCommand(element.Command);
            }
        }

        #region Event and Property Change Handlers

        private void ElementIsHQChangedEventHandler(object sender, EventArgs e) {
            IUnitElement_Ltd element = sender as IUnitElement_Ltd;
            if (element.IsHQ) {
                // this known element is now a HQ
                AddCommand(element.Command);
            }
            else {
                // this known element is no longer a HQ
                RemoveCommand(element.Command);
            }
        }

        private void ItemDeathEventHandler(object sender, EventArgs e) {
            IMortalItem_Ltd deadItem = sender as IMortalItem_Ltd;
            IUnitElement_Ltd deadElement = deadItem as IUnitElement_Ltd;
            if (deadElement != null) {
                RemoveElement(deadElement);
            }
            else {
                IPlanetoid_Ltd deadPlanetoid = deadItem as IPlanetoid_Ltd;
                D.Assert(deadPlanetoid != null);
                RemoveDeadPlanetoid(deadPlanetoid);
            }
        }

        #endregion

        private void AddCommand(IUnitCmd_Ltd command) {
            var isAdded = _commands.Add(command);
            D.Assert(isAdded);  // Cmd cannot already be present. If adding due to a change in an element's IsHQ state, then previous HQElement removed Cmd before this Add
            D.Log("{0} has added Command {1}.", Name, command.FullName);
        }

        private void RemoveCommand(IUnitCmd_Ltd command) {
            var isRemoved = _commands.Remove(command);
            D.Assert(isRemoved);
            D.Log("{0} has removed Command {1}.", Name, command.FullName);
        }

        private void AddSystem(ISystem_Ltd system) {
            bool isAdded = _systems.Add(system);
            D.Assert(isAdded, "{0} tried to add System {1} it already has.", Name, system.FullName);
        }

        /// <summary>
        /// Removes the element from this player's knowledge. Element's are
        /// removed when they lose all <c>Player</c> detection. Losing all
        /// detection by <c>Player</c> occurs for 1 reason - no Player Cmds
        /// in sensor range. Removal because of death is handled by this Knowledge.
        /// </summary>
        /// <param name="element">The element.</param>
        internal void RemoveElement(IUnitElement_Ltd element) {
            var isRemoved = _elements.Remove(element);
            D.Assert(isRemoved, "{0} could not remove Element {1}.", Name, element.FullName);

            element.isHQChanged -= ElementIsHQChangedEventHandler;
            element.deathOneShot -= ItemDeathEventHandler;
            if (element.IsHQ) {
                RemoveCommand(element.Command);
            }
        }

        /// <summary>
        /// Removes the dead planetoid from the knowledge of this player.
        /// <remarks>Knowledge of the existance of a system is not effected, 
        /// even if this is the only planetoid in the system the player has knowledge of.
        /// </remarks>
        /// </summary>
        /// <param name="deadPlanetoid">The dead planetoid.</param>
        private void RemoveDeadPlanetoid(IPlanetoid_Ltd deadPlanetoid) {
            D.Assert(!deadPlanetoid.IsOperational);
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            D.Assert(isRemoved, "{0} could not remove Planetoid {1}.", Name, deadPlanetoid.FullName);
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _elements.ForAll(e => {
                e.isHQChanged -= ElementIsHQChangedEventHandler;
                e.deathOneShot -= ItemDeathEventHandler;
            });
            _planetoids.ForAll(p => p.deathOneShot -= ItemDeathEventHandler);
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

