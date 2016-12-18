// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AllKnowledge.cs
// Singleton. All the item knowledge in a Game instance.
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
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Singleton. All the item knowledge in a Game instance.
    /// </summary>
    public class AllKnowledge : AGenericSingleton<AllKnowledge> {

        public IUniverseCenter UniverseCenter { get; private set; }

        /// <summary>
        /// The Moons currently present in the game.
        /// </summary>
        public IEnumerable<IMoon> Moons { get { return _planetoids.Where(p => p is IMoon).Cast<IMoon>(); } }

        /// <summary>
        /// The Planets currently present in the game.
        /// </summary>
        public IEnumerable<IPlanet> Planets { get { return _planetoids.Where(p => p is IPlanet).Cast<IPlanet>(); } }

        /// <summary>
        /// The Planetoids currently present in the game.
        /// </summary>
        public IEnumerable<IPlanetoid> Planetoids { get { return _planetoids; } }

        /// <summary>
        /// The Stars currently present in the game.
        /// </summary>
        public IEnumerable<IStar> Stars { get { return _stars; } }

        /// <summary>
        /// The Systems currently present in the game.
        /// </summary>
        public IEnumerable<ISystem> Systems { get { return _systemLookupBySectorID.Values; } }

        /// <summary>
        /// The Elements currently present in the game.
        /// </summary>
        public IEnumerable<IUnitElement> Elements { get { return _elements; } }

        /// <summary>
        /// The Ships currently present in the game.
        /// </summary>
        public IEnumerable<IShip> Ships { get { return _elements.Where(e => e is IShip).Cast<IShip>(); } }

        /// <summary>
        /// The Facilities currently present in the game.
        /// </summary>
        public IEnumerable<IFacility> Facilities { get { return _elements.Where(e => e is IFacility).Cast<IFacility>(); } }

        /// <summary>
        /// The Commands currently present in the game.
        /// </summary>
        public IEnumerable<IUnitCmd> Commands { get { return _commands; } }

        /// <summary>
        /// The Bases currently present in the game.
        /// </summary>
        public IEnumerable<IUnitBaseCmd> Bases { get { return _commands.Where(cmd => cmd is IUnitBaseCmd).Cast<IUnitBaseCmd>(); } }

        /// <summary>
        /// The Fleets currently present in the game.
        /// </summary>
        public IEnumerable<IFleetCmd> Fleets { get { return _commands.Where(cmd => cmd is IFleetCmd).Cast<IFleetCmd>(); } }

        /// <summary>
        /// The Settlements currently present in the game.
        /// </summary>
        public IEnumerable<ISettlementCmd> Settlements { get { return _commands.Where(cmd => cmd is ISettlementCmd).Cast<ISettlementCmd>(); } }

        /// <summary>
        /// The Starbases currently present in the game.
        /// </summary>
        public IEnumerable<IStarbaseCmd> Starbases {
            get {
                IList<IStarbaseCmd> firstSectorBases = _starbasesLookupBySectorID.Values.First();
                IEnumerable<IList<IStarbaseCmd>> otherSectorsBases = _starbasesLookupBySectorID.Values.Except(firstSectorBases);
                return firstSectorBases.UnionBy(otherSectorsBases.ToArray());
                //return _commands.Where(cmd => cmd is IStarbaseCmd).Cast<IStarbaseCmd>();
            }
        }

        private IDictionary<IntVector3, ISystem> _systemLookupBySectorID = new Dictionary<IntVector3, ISystem>();
        private IDictionary<IntVector3, IList<IStarbaseCmd>> _starbasesLookupBySectorID = new Dictionary<IntVector3, IList<IStarbaseCmd>>();

        private HashSet<IPlanetoid> _planetoids = new HashSet<IPlanetoid>();
        private HashSet<IStar> _stars = new HashSet<IStar>();
        private HashSet<IUnitElement> _elements = new HashSet<IUnitElement>();
        private HashSet<IUnitCmd> _commands = new HashSet<IUnitCmd>();
        private HashSet<IItem> _items = new HashSet<IItem>();
        private DebugSettings _debugSettings;

        private AllKnowledge() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            InitializeValuesAndReferences();
        }

        private void InitializeValuesAndReferences() {
            _debugSettings = DebugSettings.Instance;
        }

        public void Initialize(IUniverseCenter uCenter) {
            D.AssertEqual(Constants.Zero, _items.Count);
            __InitializeValidateKnowledge();

            AddUniverseCenter(uCenter);
        }

        private void AddUniverseCenter(IUniverseCenter universeCenter) {
            D.AssertNull(UniverseCenter);
            if (universeCenter != null) {
                _items.Add(universeCenter);
                UniverseCenter = universeCenter;
            }
        }

        public void AddSystem(IStar star, IEnumerable<IPlanetoid> planetoids) {
            AddStar(star);
            foreach (var planetoid in planetoids) {
                AddPlanetoid(planetoid);
            }
        }

        public void AddUnit(IUnitCmd unitCmd, IEnumerable<IUnitElement> unitElements) {
            AddCommand(unitCmd);
            foreach (var element in unitElements) {
                AddElement(element);
            }
        }

        /// <summary>
        /// Returns the items owned by player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<IItem> GetItemsOwnedBy(Player player) {
            var playerOwnedItems = new List<IItem>();
            _items.ForAll(item => {
                if (item.Owner == player) {
                    playerOwnedItems.Add(item);
                }
            });
            return playerOwnedItems;
        }

        /// <summary>
        /// Tries to get a random system, returning <c>true</c> if a system was found that is
        /// not in <c>excludedSystems</c>, <c>false</c> otherwise.
        /// </summary>
        /// <param name="excludedSystems">The excluded systems.</param>
        /// <param name="randomSystem">The random system.</param>
        /// <returns></returns>
        public bool TryGetRandomSystem(IEnumerable<ISystem> excludedSystems, out ISystem randomSystem) {
            var availableSystems = _systemLookupBySectorID.Values.Except(excludedSystems);
            if (availableSystems.Any()) {
                randomSystem = RandomExtended.Choice(availableSystems);
                return true;
            }
            randomSystem = null;
            return false;
        }

        public bool TryGetSystem(IntVector3 sectorID, out ISystem system) {
            D.AssertNotDefault(sectorID);
            return _systemLookupBySectorID.TryGetValue(sectorID, out system);
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorID contains one or more Starbases, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="starbasesInSector">The resulting starbases in sector.</param>
        /// <returns></returns>
        public bool TryGetStarbases(IntVector3 sectorID, out IEnumerable<IStarbaseCmd> starbasesInSector) {
            D.AssertNotDefault(sectorID);
            IList<IStarbaseCmd> sBases;
            if (_starbasesLookupBySectorID.TryGetValue(sectorID, out sBases)) {
                starbasesInSector = sBases;
                return true;
            }
            starbasesInSector = Enumerable.Empty<IStarbaseCmd>();
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorID contains one or more Fleets, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="fleetsInSector">The resulting fleets present in the sector.</param>
        /// <returns></returns>
        public bool TryGetFleets(IntVector3 sectorID, out IEnumerable<IFleetCmd> fleetsInSector) {
            D.AssertNotDefault(sectorID);
            fleetsInSector = Fleets.Where(fleet => fleet.SectorID == sectorID);
            if (fleetsInSector.Any()) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the SpaceTopography value associated with this location in worldspace.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns></returns>
        public Topography GetSpaceTopography(Vector3 worldLocation) {
            IntVector3 sectorID = References.SectorGrid.GetSectorIdThatContains(worldLocation);
            ISystem system;
            if (_systemLookupBySectorID.TryGetValue(sectorID, out system)) {
                // the sector containing worldLocation has a system
                if (Vector3.SqrMagnitude(worldLocation - system.Position) < system.Radius * system.Radius) {
                    return Topography.System;
                }
            }
            //TODO add Nebula and DeepNebula
            return Topography.OpenSpace;
        }

        private void AddStar(IStar star) {
            // A Star should only be added once when all players get Basic IntelCoverage of all stars
            bool isAdded = _stars.Add(star);
            isAdded = isAdded & _items.Add(star);
            D.Assert(isAdded, star.DebugName);
            AddSystem(star.ParentSystem);
        }

        private void AddPlanetoid(IPlanetoid planetoid) {
            bool isAdded = _planetoids.Add(planetoid);
            isAdded = isAdded & _items.Add(planetoid);
            D.Assert(isAdded, planetoid.DebugName);
            planetoid.deathOneShot += ItemDeathEventHandler;
        }

        private void AddElement(IUnitElement element) {
            var isAdded = _elements.Add(element);
            isAdded = isAdded & _items.Add(element);
            D.Assert(isAdded, element.DebugName);
            element.deathOneShot += ItemDeathEventHandler;
        }

        #region Event and Property Change Handlers

        private void ItemDeathEventHandler(object sender, EventArgs e) {
            IMortalItem deadItem = sender as IMortalItem;
            IUnitElement deadElement = deadItem as IUnitElement;
            if (deadElement != null) {
                RemoveDeadElement(deadElement);
            }
            else {
                IUnitCmd deadCmd = deadItem as IUnitCmd;
                if (deadCmd != null) {
                    RemoveDeadCommand(deadCmd);
                }
                else {
                    IPlanetoid deadPlanetoid = deadItem as IPlanetoid;
                    D.AssertNotNull(deadPlanetoid); // UNCLEAR could this interface indicate null for dead(destroyed) Planetoid?
                    RemoveDeadPlanetoid(deadPlanetoid);
                }
            }
        }

        #endregion

        private void AddCommand(IUnitCmd command) {
            var isAdded = _commands.Add(command);
            isAdded = isAdded & _items.Add(command);
            D.Assert(isAdded);
            command.deathOneShot += ItemDeathEventHandler;
            //D.Log("{0} has added Command {1}.", DebugName, command.DebugName);

            // populate Starbase lookup
            IStarbaseCmd sbCmd = command as IStarbaseCmd;
            if (sbCmd != null) {
                var sbSectorID = sbCmd.SectorID;

                IList<IStarbaseCmd> sbCmds;
                if (!_starbasesLookupBySectorID.TryGetValue(sbSectorID, out sbCmds)) {
                    sbCmds = new List<IStarbaseCmd>(2);
                    _starbasesLookupBySectorID.Add(sbSectorID, sbCmds);
                }
                sbCmds.Add(sbCmd);
            }
        }

        private void RemoveDeadCommand(IUnitCmd deadCmd) {
            D.Assert(!deadCmd.IsOperational);
            var isRemoved = _commands.Remove(deadCmd);
            isRemoved = isRemoved & _items.Remove(deadCmd);
            D.Assert(isRemoved);
            deadCmd.deathOneShot -= ItemDeathEventHandler;  // OPTIMIZE not really necessary as only way Cmd is removed is via death
            //D.Log("{0} has removed Command {1}.", DebugName, deadCmd.DebugName);

            // remove from Starbase lookup
            IStarbaseCmd sbCmd = deadCmd as IStarbaseCmd;
            if (sbCmd != null) {
                var sbSectorID = sbCmd.SectorID;

                IList<IStarbaseCmd> sbCmds = _starbasesLookupBySectorID[sbSectorID];
                sbCmds.Remove(sbCmd);
                if (sbCmds.Count == Constants.Zero) {
                    _starbasesLookupBySectorID.Remove(sbSectorID);
                }
            }
        }

        private void AddSystem(ISystem system) {
            _systemLookupBySectorID.Add(system.SectorID, system);
            bool isAdded = _items.Add(system);
            D.Assert(isAdded, system.DebugName);
        }

        /// <summary>
        /// Removes the element from this player's knowledge. Element's are
        /// removed when they die.
        /// </summary>
        /// <param name="deadElement">The element.</param>
        private void RemoveDeadElement(IUnitElement deadElement) {
            D.Assert(!deadElement.IsOperational);
            var isRemoved = _elements.Remove(deadElement);
            isRemoved = isRemoved & _items.Remove(deadElement);
            D.Assert(isRemoved, deadElement.DebugName);
            deadElement.deathOneShot -= ItemDeathEventHandler;  // OPTIMIZE not really necessary as only way element is removed is via death
        }

        /// <summary>
        /// Removes the dead planetoid from the knowledge of this player.
        /// <remarks>Knowledge of the existence of a system is not effected, 
        /// even if this is the only planetoid in the system the player has knowledge of.
        /// </remarks>
        /// </summary>
        /// <param name="deadPlanetoid">The dead planetoid.</param>
        private void RemoveDeadPlanetoid(IPlanetoid deadPlanetoid) {
            D.Assert(!deadPlanetoid.IsOperational);
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            isRemoved = isRemoved & _items.Remove(deadPlanetoid);
            D.Assert(isRemoved, deadPlanetoid.DebugName);
            deadPlanetoid.deathOneShot -= ItemDeathEventHandler;  // OPTIMIZE not really necessary as only way planetoid is removed is via death
        }

        /// <summary>
        /// Clears all knowledge in preparation for re-populating it for a new game using Initialize().
        /// </summary>
        public void Reset() {
            Cleanup();
            ClearItemCollections();
        }

        private void ClearItemCollections() {
            UniverseCenter = null;
            _planetoids.Clear();
            _stars.Clear();
            _systemLookupBySectorID.Clear();
            _elements.Clear();
            _commands.Clear();
            _starbasesLookupBySectorID.Clear();
            _items.Clear();
        }

        private void Cleanup() {
            __CleanupValidateKnowledge();
            Unsubscribe();
        }

        private void Unsubscribe() {
            _elements.ForAll(e => e.deathOneShot -= ItemDeathEventHandler);
            _commands.ForAll(cmd => cmd.deathOneShot -= ItemDeathEventHandler);
            _planetoids.ForAll(p => p.deathOneShot -= ItemDeathEventHandler);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug 

        private void __InitializeValidateKnowledge() {
            References.DebugControls.validatePlayerKnowledgeNow += __ValidateKnowledgeNowEventHandler;
        }

        private void __ValidateKnowledgeNowEventHandler(object sender, EventArgs e) {
            __ValidateKnowledgeNow();
        }

        private void __ValidateKnowledgeNow() {
            D.Log("{0} is validating all Knowledge.", DebugName);
            foreach (var item in _items) {
                if (!item.IsOperational) {
                    D.Error("{0} has retained knowledge of dead {1}.", DebugName, item.DebugName);
                }
            }
        }

        private void __CleanupValidateKnowledge() {
            References.DebugControls.validatePlayerKnowledgeNow -= __ValidateKnowledgeNowEventHandler;
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
                CallOnDispose();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion


    }
}


