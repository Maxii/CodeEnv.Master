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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Holds the current knowledge of a player about items in the universe.
    /// What is known by the player about each item is available through the item from Reports.
    /// </summary>
    public class PlayerKnowledge : IDisposable {

        private const string DebugNameFormat = "{0}'s {1}";

        public Player Owner { get; private set; }

        public IUniverseCenter_Ltd UniverseCenter { get; private set; }

        public IEnumerable<IMoon> OwnerMoons {
            get {
                var ownerMoons = new List<IMoon>();
                Player moonOwner;
                Moons.ForAll(m => {
                    if (m.TryGetOwner(Owner, out moonOwner) && moonOwner == Owner) {
                        ownerMoons.Add(m as IMoon);
                    }
                });
                return ownerMoons;
            }
        }

        public IEnumerable<IPlanet> OwnerPlanets {
            get {
                var ownerPlanets = new List<IPlanet>();
                Player planetOwner;
                Planets.ForAll(p => {
                    if (p.TryGetOwner(Owner, out planetOwner) && planetOwner == Owner) {
                        ownerPlanets.Add(p as IPlanet);
                    }
                });
                return ownerPlanets;
            }
        }

        public IEnumerable<IPlanetoid> OwnerPlanetoids {
            get {
                var ownerPlanetoids = new List<IPlanetoid>();
                Player planetoidOwner;
                Planetoids.ForAll(p => {
                    if (p.TryGetOwner(Owner, out planetoidOwner) && planetoidOwner == Owner) {
                        ownerPlanetoids.Add(p as IPlanetoid);
                    }
                });
                return ownerPlanetoids;
            }
        }

        public IEnumerable<IStar> OwnerStars {
            get {
                var ownerStars = new List<IStar>();
                Player starOwner;
                Stars.ForAll(s => {
                    if (s.TryGetOwner(Owner, out starOwner) && starOwner == Owner) {
                        ownerStars.Add(s as IStar);
                    }
                });
                return ownerStars;
            }
        }

        public IEnumerable<ISystem> OwnerSystems {
            get {
                var ownerSystems = new List<ISystem>();
                Player systemOwner;
                Systems.ForAll(s => {
                    if (s.TryGetOwner(Owner, out systemOwner) && systemOwner == Owner) {
                        ownerSystems.Add(s as ISystem);
                    }
                });
                return ownerSystems;
            }
        }

        public IEnumerable<IUnitCmd> OwnerCommands {
            get {
                var ownerCmds = new List<IUnitCmd>();
                Player cmdOwner;
                _commands.ForAll(cmd => {
                    if (cmd.TryGetOwner(Owner, out cmdOwner) && cmdOwner == Owner) {
                        ownerCmds.Add(cmd as IUnitCmd);
                    }
                });
                return ownerCmds;
            }
        }

        public IEnumerable<IFleetCmd> OwnerFleets {
            get {
                var ownerFleets = new List<IFleetCmd>();
                Player fleetOwner;
                Fleets.ForAll(f => {
                    if (f.TryGetOwner(Owner, out fleetOwner) && fleetOwner == Owner) {
                        ownerFleets.Add(f as IFleetCmd);
                    }
                });
                return ownerFleets;
            }
        }

        public IEnumerable<ISettlementCmd> OwnerSettlements {
            get {
                var ownerSettlements = new List<ISettlementCmd>();
                Player settlementOwner;
                Settlements.ForAll(s => {
                    if (s.TryGetOwner(Owner, out settlementOwner) && settlementOwner == Owner) {
                        ownerSettlements.Add(s as ISettlementCmd);
                    }
                });
                return ownerSettlements;
            }
        }

        public IEnumerable<IStarbaseCmd> OwnerStarbases {
            get {
                var ownerStarbases = new List<IStarbaseCmd>();
                Player starbaseOwner;
                Starbases.ForAll(s => {
                    if (s.TryGetOwner(Owner, out starbaseOwner) && starbaseOwner == Owner) {
                        ownerStarbases.Add(s as IStarbaseCmd);
                    }
                });
                return ownerStarbases;
            }
        }

        public IEnumerable<IUnitBaseCmd> OwnerBases {
            get {
                var ownerBases = new List<IUnitBaseCmd>();
                Player baseOwner;
                Bases.ForAll(b => {
                    if (b.TryGetOwner(Owner, out baseOwner) && baseOwner == Owner) {
                        ownerBases.Add(b as IUnitBaseCmd);
                    }
                });
                return ownerBases;
            }
        }

        public IEnumerable<IUnitElement> OwnerElements {
            get {
                var ownerElements = new List<IUnitElement>();
                Player elementOwner;
                Elements.ForAll(e => {
                    if (e.TryGetOwner(Owner, out elementOwner) && elementOwner == Owner) {
                        ownerElements.Add(e as IUnitElement);
                    }
                });
                return ownerElements;
            }
        }

        public IEnumerable<IShip> OwnerShips {
            get {
                var ownerShips = new List<IShip>();
                Player shipOwner;
                Ships.ForAll(s => {
                    if (s.TryGetOwner(Owner, out shipOwner) && shipOwner == Owner) {
                        ownerShips.Add(s as IShip);
                    }
                });
                return ownerShips;
            }
        }

        public IEnumerable<IFacility> OwnerFacilities {
            get {
                var ownerFacilities = new List<IFacility>();
                Player facilityOwner;
                Facilities.ForAll(f => {
                    if (f.TryGetOwner(Owner, out facilityOwner) && facilityOwner == Owner) {
                        ownerFacilities.Add(f as IFacility);
                    }
                });
                return ownerFacilities;
            }
        }

        public IEnumerable<IItem> OwnerItems {
            get { return GetItemsOwnedBy(Owner).Cast<IItem>(); }
        }

        [Obsolete]
        public IEnumerable<ISensorDetectable> MySensorDetectableItems {
            get { return OwnerItems.Where(myItem => (myItem is ISensorDetectable)).Cast<ISensorDetectable>(); }
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
        public IEnumerable<ISystem_Ltd> Systems { get { return _systemLookupBySectorID.Values; } }

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
        public IEnumerable<ISettlementCmd_Ltd> Settlements { get { return _settlementLookupBySectorID.Values; } }

        /// <summary>
        /// The Starbases this player has knowledge of.
        /// </summary>
        public IEnumerable<IStarbaseCmd_Ltd> Starbases {
            get {
                if (_starbasesLookupBySectorID.Values.Count > Constants.Zero) {
                    IList<IStarbaseCmd_Ltd> firstSectorsBases = _starbasesLookupBySectorID.Values.First();
                    IEnumerable<IList<IStarbaseCmd_Ltd>> otherSectorsBases = _starbasesLookupBySectorID.Values.Except(firstSectorsBases);
                    return firstSectorsBases.UnionBy(otherSectorsBases.ToArray());
                }
                return Enumerable.Empty<IStarbaseCmd_Ltd>();
            }
        }

        private string _debugName;
        private string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Owner.DebugName, typeof(PlayerKnowledge).Name);
                }
                return _debugName;
            }
        }

        // Note: Other players this Player has met is held by the Player

        private IDictionary<IntVector3, ISystem_Ltd> _systemLookupBySectorID;
        private IDictionary<IntVector3, IList<IStarbaseCmd_Ltd>> _starbasesLookupBySectorID = new Dictionary<IntVector3, IList<IStarbaseCmd_Ltd>>();
        private IDictionary<IntVector3, ISettlementCmd_Ltd> _settlementLookupBySectorID = new Dictionary<IntVector3, ISettlementCmd_Ltd>();

        private HashSet<IPlanetoid_Ltd> _planetoids = new HashSet<IPlanetoid_Ltd>();
        private HashSet<IStar_Ltd> _stars = new HashSet<IStar_Ltd>();
        private HashSet<IUnitElement_Ltd> _elements = new HashSet<IUnitElement_Ltd>();
        private HashSet<IUnitCmd_Ltd> _commands = new HashSet<IUnitCmd_Ltd>();
        private HashSet<IItem_Ltd> _items = new HashSet<IItem_Ltd>();

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
            D.AssertNotNull(player);
            D.AssertNotEqual(TempGameValues.NoPlayer, player);
            Owner = player;
            InitializeValuesAndReferences();
            AddUniverseCenter(uCenter);
            allStars.ForAll(star => AddStar(star));
            AddAllSystems(allStars);
            __InitializeValidatePlayerKnowledge();
        }

        private void InitializeValuesAndReferences() { }

        /// <summary>
        /// Returns true if the sector indicated by sectorID contains a System.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="system">The system if present in the sector.</param>
        /// <returns></returns>
        public bool TryGetSystem(IntVector3 sectorID, out ISystem_Ltd system) {
            bool isSystemFound = _systemLookupBySectorID.TryGetValue(sectorID, out system);
            if (isSystemFound) {
                //D.Log("{0} found System {1} in Sector {2}.", DebugName, system.DebugName, sectorID);
            }
            return isSystemFound;
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorID contains a Settlement, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="starbasesInSector">The resulting settlement in the sector.</param>
        /// <returns></returns>
        public bool TryGetSettlement(IntVector3 sectorID, out ISettlementCmd_Ltd settlementInSector) {
            D.AssertNotDefault(sectorID);
            ISettlementCmd_Ltd settlements;
            if (_settlementLookupBySectorID.TryGetValue(sectorID, out settlements)) {
                settlementInSector = settlements;
                return true;
            }
            settlementInSector = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorID contains one or more Starbases, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="starbasesInSector">The resulting starbases in sector.</param>
        /// <returns></returns>
        public bool TryGetStarbases(IntVector3 sectorID, out IEnumerable<IStarbaseCmd_Ltd> starbasesInSector) {
            D.AssertNotDefault(sectorID);
            IList<IStarbaseCmd_Ltd> sBases;
            if (_starbasesLookupBySectorID.TryGetValue(sectorID, out sBases)) {
                starbasesInSector = sBases;
                return true;
            }
            starbasesInSector = Enumerable.Empty<IStarbaseCmd_Ltd>();
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorID contains one or more Fleets, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">ID of the sector.</param>
        /// <param name="fleetsInSector">The resulting fleets present in the sector.</param>
        /// <returns></returns>
        public bool TryGetFleets(IntVector3 sectorID, out IEnumerable<IFleetCmd_Ltd> fleetsInSector) {
            D.AssertNotDefault(sectorID);
            fleetsInSector = Fleets.Where(fleet => fleet.SectorID == sectorID);
            if (fleetsInSector.Any()) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether the Owner has knowledge of the provided item.
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
                D.AssertEqual(UniverseCenter, item);
                D.Warn("{0}: unnecessary check for knowledge of {1}.", DebugName, item.DebugName);
                return true;
            }
            if (item is IStar_Ltd) {
                D.Assert(_stars.Contains(item as IStar_Ltd));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", DebugName, item.DebugName);
                return true;
            }
            if (item is ISystem_Ltd) {
                D.Assert(_systemLookupBySectorID.Values.Contains(item as ISystem_Ltd));
                D.Warn("{0}: unnecessary check for knowledge of {1}.", DebugName, item.DebugName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the items that we know about and to which we have 
        /// owner access that are owned by player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<IItem_Ltd> GetItemsOwnedBy(Player player) {
            var playerOwnedItems = new List<IItem_Ltd>();
            _items.ForAll(item => {
                Player itemOwner;
                if (item.TryGetOwner(Owner, out itemOwner) && itemOwner == player) {
                    playerOwnedItems.Add(item);
                }
            });
            return playerOwnedItems;
        }

        private void AddUniverseCenter(IUniverseCenter_Ltd universeCenter) {
            D.AssertNull(UniverseCenter);   // should only be added once when all players get Basic IntelCoverage of UCenter
            _items.Add(universeCenter);
            UniverseCenter = universeCenter;
        }

        private void AddStar(IStar_Ltd star) {
            // A Star should only be added once when all players get Basic IntelCoverage of all stars
            bool isAdded = _stars.Add(star);
            isAdded = isAdded & _items.Add(star);
            D.Assert(isAdded, star.DebugName);
        }

        private void AddAllSystems(IEnumerable<IStar_Ltd> allStars) {   // properly sizes the dictionary
            _systemLookupBySectorID = allStars.ToDictionary(star => star.SectorID, star => star.ParentSystem);
            allStars.ForAll(star => _items.Add(star.ParentSystem));
        }

        internal bool AddPlanetoid(IPlanetoid_Ltd planetoid) {
            bool isAdded = _planetoids.Add(planetoid);
            isAdded = isAdded & _items.Add(planetoid);
            if (!isAdded) {
                //D.Log("{0} tried to add Planetoid {1} it already has.", DebugName, planetoid.DebugName);
                return false;
            }
            //D.Log("{0} is adding {1} to its knowledge base.", DebugName, planetoid.DebugName);

            planetoid.deathOneShot += ItemDeathEventHandler;
            return true;
        }

        /// <summary>
        /// Attempts to add the provided element to this player's knowledge, returning
        /// <c>true</c> if it was not present and therefore added, <c>false</c> if it was
        /// already present.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        internal bool AddElement(IUnitElement_Ltd element) {
            var isAdded = _elements.Add(element);
            isAdded = isAdded & _items.Add(element);
            if (!isAdded) {
                //D.Log("{0} tried to add Element {1} it already has.", DebugName, element.DebugName);
                return false;
            }
            //D.Log("{0} is adding {1} to its knowledge base.", DebugName, element.DebugName);
            element.deathOneShot += ItemDeathEventHandler;  // 8.21.16 was missing?
            return true;
        }

        #region Event and Property Change Handlers

        private void ItemDeathEventHandler(object sender, EventArgs e) {
            IMortalItem_Ltd deadItem = sender as IMortalItem_Ltd;
            D.AssertNotNull(deadItem);
            HandleItemDeath(deadItem);
        }

        #endregion

        private void HandleItemDeath(IMortalItem_Ltd deadItem) {
            D.Assert(!deadItem.IsOperational, DebugName);
            IUnitElement_Ltd deadElement = deadItem as IUnitElement_Ltd;
            if (deadElement != null) {
                RemoveElement(deadElement);
            }
            else {
                IPlanetoid_Ltd deadPlanetoid = deadItem as IPlanetoid_Ltd;
                if (deadPlanetoid != null) {
                    RemoveDeadPlanetoid(deadPlanetoid);
                }
                else {
                    IUnitCmd_Ltd deadCmd = deadItem as IUnitCmd_Ltd;
                    D.AssertNotNull(deadCmd, DebugName);
                    RemoveCommand(deadCmd);
                }
            }
        }

        /// <summary>
        /// Attempts to add the provided command to this player's knowledge, returning
        /// <c>true</c> if it was not present and therefore added, <c>false</c> if it was
        /// already present.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        internal bool AddCommand(IUnitCmd_Ltd command) {
            var isAdded = _commands.Add(command);
            isAdded = isAdded & _items.Add(command);
            if (!isAdded) {
                //D.Log("{0} tried to add Command {1} it already has.", DebugName, command.DebugName);
                return false;
            }
            //D.Log("{0} is adding {1} to its knowledge base.", DebugName, command.DebugName);

            IStarbaseCmd_Ltd sbCmd = command as IStarbaseCmd_Ltd;
            if (sbCmd != null) {
                var sbSectorID = sbCmd.SectorID;

                IList<IStarbaseCmd_Ltd> sbCmds;
                if (!_starbasesLookupBySectorID.TryGetValue(sbSectorID, out sbCmds)) {
                    sbCmds = new List<IStarbaseCmd_Ltd>(2);
                    _starbasesLookupBySectorID.Add(sbSectorID, sbCmds);
                }
                D.Assert(!sbCmds.Contains(sbCmd));
                sbCmds.Add(sbCmd);
            }
            else {
                ISettlementCmd_Ltd settlementCmd = command as ISettlementCmd_Ltd;
                if (settlementCmd != null) {
                    var sSectorID = settlementCmd.SectorID;
                    _settlementLookupBySectorID.Add(sSectorID, settlementCmd);
                }
            }

            command.deathOneShot += ItemDeathEventHandler;
            return true;
        }

        /// <summary>
        /// Removes the provided command from this player's knowledge. Throws
        /// an error if not present.
        /// </summary>
        /// <param name="command">The command.</param>
        internal void RemoveCommand(IUnitCmd_Ltd command) {
            var isRemoved = _commands.Remove(command);
            isRemoved = isRemoved & _items.Remove(command);
            D.Assert(isRemoved);
            //D.Log("{0} has removed Command {1}.", DebugName, command.DebugName);

            IStarbaseCmd_Ltd sbCmd = command as IStarbaseCmd_Ltd;
            if (sbCmd != null) {
                var sbSectorID = sbCmd.SectorID;

                IList<IStarbaseCmd_Ltd> sbCmds = _starbasesLookupBySectorID[sbSectorID];
                isRemoved = sbCmds.Remove(sbCmd);
                D.Assert(isRemoved);
                if (sbCmds.Count == Constants.Zero) {
                    _starbasesLookupBySectorID.Remove(sbSectorID);
                }
            }
            else {
                ISettlementCmd_Ltd settlement = command as ISettlementCmd_Ltd;
                if (settlement != null) {
                    var sSectorID = settlement.SectorID;
                    isRemoved = _settlementLookupBySectorID.Remove(sSectorID);
                    D.Assert(isRemoved);
                }
            }

            if (command.IsOperational && command.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Alliance)) {
                D.Error("{0}: {1} is alive and being removed while in Alliance!", DebugName, command.DebugName);
            }
            command.deathOneShot -= ItemDeathEventHandler;
        }

        /// <summary>
        /// Removes the element from this player's knowledge. Element's are
        /// removed when this Player's IntelCoverage of the element changes to None from another value.
        /// This only happens to ships and only when Player loses detection of the element,
        /// aka when no Player Cmds remain within sensor range. Removal because of death is handled by this Knowledge.
        /// </summary>
        /// <param name="element">The element.</param>
        internal void RemoveElement(IUnitElement_Ltd element) {
            D.Assert(element is IShip_Ltd, element.DebugName);
            var isRemoved = _elements.Remove(element);
            isRemoved = isRemoved & _items.Remove(element);
            D.Assert(isRemoved, element.DebugName);

            if (element.IsOperational && (element as IUnitElement).Owner.IsRelationshipWith(Owner, DiplomaticRelationship.Alliance)) {
                D.Error("{0}: {1} is alive and being removed while in Alliance!", DebugName, element.DebugName);
            }
            element.deathOneShot -= ItemDeathEventHandler;
        }

        /// <summary>
        /// Removes the dead planetoid from the knowledge of this player.
        /// <remarks>Knowledge of the existence of a system is not effected, 
        /// even if this is the only planetoid in the system the player has knowledge of.
        /// </remarks>
        /// </summary>
        /// <param name="deadPlanetoid">The dead planetoid.</param>
        private void RemoveDeadPlanetoid(IPlanetoid_Ltd deadPlanetoid) {
            D.Assert(!deadPlanetoid.IsOperational);
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            isRemoved = isRemoved & _items.Remove(deadPlanetoid);
            D.Assert(isRemoved, deadPlanetoid.DebugName);
        }

        private void Cleanup() {
            __CleanupValidatePlayerKnowledge();
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

        /// <summary>
        /// Debug. Returns the items that we know about that are owned by player.
        /// No Owner-access restrictions.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<IItem> __GetItemsOwnedBy(Player player) {
            var playerOwnedItems = new List<IItem>();
            _items.Cast<IItem>().ForAll(item => {
                if (item.Owner == player) {
                    playerOwnedItems.Add(item);
                }
            });
            return playerOwnedItems;
        }

        private void __InitializeValidatePlayerKnowledge() {
            References.DebugControls.validatePlayerKnowledgeNow += __ValidatePlayerKnowledgeNowEventHandler;
        }

        private void __ValidatePlayerKnowledgeNowEventHandler(object sender, EventArgs e) {
            __ValidatePlayerKnowledgeNow();
        }

        private void __ValidatePlayerKnowledgeNow() {
            D.Log("{0} is validating all Player Knowledge.", DebugName);
            IList<IItem> myItems = OwnerItems.ToList();
            foreach (var item in _items) {
                D.Assert(item.IsOperational, item.DebugName);
                IntelCoverage coverage = (item as IIntelItem).GetIntelCoverage(Owner);
                if (myItems.Contains(item as IItem)) {
                    // item is mine so should be comprehensive
                    D.AssertEqual(IntelCoverage.Comprehensive, coverage, coverage.GetValueName());
                    continue;
                }
                D.AssertNotDefault((int)coverage, item.DebugName);
            }
        }

        private void __CleanupValidatePlayerKnowledge() {
            References.DebugControls.validatePlayerKnowledgeNow -= __ValidatePlayerKnowledgeNowEventHandler;
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

