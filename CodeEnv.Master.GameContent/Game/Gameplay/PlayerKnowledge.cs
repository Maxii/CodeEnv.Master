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
    public class PlayerKnowledge : APropertyChangeTracking, IDisposable {

        private const string DebugNameFormat = "{0}'s {1}";

        public Player Owner { get; private set; }

        public decimal __BankBalance { get { return 10000; } }

        private OutputsYield _totalOutputs;
        public OutputsYield TotalOutputs {
            get { return _totalOutputs; }
            private set { SetProperty<OutputsYield>(ref _totalOutputs, value, "TotalOutputs"); }
        }

        private ResourcesYield _totalResources;
        public ResourcesYield TotalResources {
            get { return _totalResources; }
            private set { SetProperty<ResourcesYield>(ref _totalResources, value, "TotalResources"); }
        }

        public bool IsOperational { get; private set; }

        #region Universe Awareness

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

        public IEnumerable<IOwnerItem> OwnerItems {
            get { return GetItemsOwnedBy(Owner).Cast<IOwnerItem>(); }
        }

        [Obsolete]
        public IEnumerable<ISensorDetectable> MySensorDetectableItems {
            get { return OwnerItems.Where(myItem => (myItem is ISensorDetectable)).Cast<ISensorDetectable>(); }
        }

        public bool AreAnyKnownItemsGuardableByOwner { get { return KnownItemsGuardableByOwner.Any(); } }

        public IEnumerable<IGuardable> KnownBasesGuardableByOwner {
            get {
                return Bases.Select(b => new { guardableBase = b as IGuardable, b })
                            .Where(x => x.guardableBase.IsGuardingAllowedBy(Owner))
                            .Select(x => x.guardableBase);
            }
        }

        public IEnumerable<IGuardable> SystemsGuardableByOwner {
            get {
                return Systems.Select(system => new { guardableSystem = system as IGuardable, system })
                              .Where(x => x.guardableSystem.IsGuardingAllowedBy(Owner))
                              .Select(x => x.guardableSystem);
            }
        }

        /// <summary>
        /// Returns all known Items that are currently guardable by Owner.
        /// </summary>
        public IEnumerable<IGuardable> KnownItemsGuardableByOwner {
            get { return KnownGuardableItems.Where(guardableItem => guardableItem.IsGuardingAllowedBy(Owner)); }
        }

        /// <summary>
        /// Returns all known Items that implement IGuardable.
        /// <remarks>The items returned may or may not allow the client owner to guard them.</remarks>
        /// </summary>
        public IEnumerable<IGuardable> KnownGuardableItems {
            get {
                return _items.Select(item => new { guardableItem = item as IGuardable, item })
                             .Where(x => x.guardableItem != null)
                             .Select(x => x.guardableItem);
            }
        }

        public bool AreAnyKnownItemsPatrollableByOwner { get { return KnownItemsPatrollableByOwner.Any(); } }

        public IEnumerable<IPatrollable> KnownBasesPatrollableByOwner {
            get {
                return Bases.Select(b => new { patrollableBase = b as IPatrollable, b })
                            .Where(x => x.patrollableBase.IsPatrollingAllowedBy(Owner))
                            .Select(x => x.patrollableBase);
            }
        }

        public IEnumerable<IPatrollable> SystemsPatrollableByOwner {
            get {
                return Systems.Select(system => new { patrollableSystem = system as IPatrollable, system })
                              .Where(x => x.patrollableSystem.IsPatrollingAllowedBy(Owner))
                              .Select(x => x.patrollableSystem);
            }
        }

        /// <summary>
        /// Returns all known Items that are currently patrollable by Owner.
        /// </summary>
        public IEnumerable<IPatrollable> KnownItemsPatrollableByOwner {
            get { return KnownPatrollableItems.Where(patrollableItem => patrollableItem.IsPatrollingAllowedBy(Owner)); }
        }

        /// <summary>
        /// Returns all known Items that implement IPatrollable.
        /// <remarks>The items returned may or may not allow the client owner to patrol them.</remarks>
        /// <remarks>OPTIMIZE be more selective than _items.</remarks>
        /// </summary>
        public IEnumerable<IPatrollable> KnownPatrollableItems {
            get {
                return _items.Select(item => new { patrollableItem = item as IPatrollable, item })
                             .Where(x => x.patrollableItem != null)
                             .Select(x => x.patrollableItem);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if any known items remain unexplored by Owner fleets.
        /// </summary>
        public bool AreAnyKnownItemsUnexploredByOwnerFleets { get { return KnownItemsUnexploredByOwnerFleets.Any(); } }

        /// <summary>
        /// Returns all known Items that are currently explorable that remain unexplored by Owner fleets.
        /// </summary>
        public IEnumerable<IFleetExplorable> KnownItemsUnexploredByOwnerFleets {
            get {
                return KnownFleetExplorableItems.Where(explorableItem => explorableItem.IsExploringAllowedBy(Owner)
                    && !explorableItem.IsFullyExploredBy(Owner));
            }
        }

        /// <summary>
        /// Returns all known Items that implement IFleetExplorable.
        /// <remarks>The items returned may or may not allow the client owner to explore them.</remarks>
        /// <remarks>OPTIMIZE be more selective than _items.</remarks>
        /// </summary>
        public IEnumerable<IFleetExplorable> KnownFleetExplorableItems {
            get {
                return _items.Select(item => new { explorableItem = item as IFleetExplorable, item })
                             .Where(x => x.explorableItem != null)
                             .Select(x => x.explorableItem);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if any known fleets are attackable by Owner Units.
        /// </summary>
        public bool AreAnyKnownFleetsAttackableByOwnerUnits { get { return KnownFleetsAttackableByOwnerUnits.Any(); } }


        public IEnumerable<IUnitAttackable> KnownFleetsAttackableByOwnerUnits {
            get { return Fleets.Cast<IUnitAttackable>().Where(fleet => fleet.IsAttackAllowedBy(Owner)); }
        }

        /// <summary>
        /// Returns <c>true</c> if any known items are attackable by Owner Units.
        /// </summary>
        public bool AreAnyKnownItemsAttackableByOwnerUnits { get { return KnownItemsAttackableByOwnerUnits.Any(); } }

        /// <summary>
        /// Returns all known Items that are currently attackable by Owner Units.
        /// </summary>
        public IEnumerable<IUnitAttackable> KnownItemsAttackableByOwnerUnits {
            get { return KnownUnitAttackableItems.Where(attackableItem => attackableItem.IsAttackAllowedBy(Owner)); }
        }

        /// <summary>
        /// Returns all known Items that implement IUnitAttackable.
        /// <remarks>The items returned may or may not allow the client owner to attack them.</remarks>
        /// <remarks>OPTIMIZE be more selective than _items.</remarks>
        /// </summary>
        public IEnumerable<IUnitAttackable> KnownUnitAttackableItems {
            get {
                return _items.Select(item => new { attackableItem = item as IUnitAttackable, item })
                             .Where(x => x.attackableItem != null)
                             .Select(x => x.attackableItem);
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

        //[Obsolete("Not currently used, pending Hanger becoming an AItem.")]
        //public IEnumerable<IHanger_Ltd> BaseHangers { get { return Bases.Select(b => b.Hanger); } }

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

        #endregion

        private string _debugName;
        public virtual string DebugName {
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
        private IList<IStar_Ltd> _stars = new List<IStar_Ltd>();
        private HashSet<IUnitElement_Ltd> _elements = new HashSet<IUnitElement_Ltd>();
        private HashSet<IUnitCmd_Ltd> _commands = new HashSet<IUnitCmd_Ltd>();
        private HashSet<IOwnerItem_Ltd> _items = new HashSet<IOwnerItem_Ltd>();

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

        internal void CommenceOperations() {
            IsOperational = true;
            RefreshTotalOutputs();
            RefreshTotalResources();
        }

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
            ISettlementCmd_Ltd settlement;
            if (_settlementLookupBySectorID.TryGetValue(sectorID, out settlement)) {
                settlementInSector = settlement;
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

        public bool AreAnyFleetsJoinableBy(IShip ship) {
            IEnumerable<IFleetCmd> unusedJoinableFleets;
            return TryGetJoinableFleetsFor(ship, out unusedJoinableFleets);
        }

        public bool TryGetJoinableFleetsFor(IShip ship, out IEnumerable<IFleetCmd> joinableFleets) {
            D.Assert(ship.__HasCommand);
            D.AssertEqual(Owner, ship.Owner);
            joinableFleets = OwnerFleets.Where(f => f.IsJoinable).Except(ship.Command);
            return joinableFleets.Any();
        }

        public bool AreAnyFleetsJoinableBy(IFleetCmd potentialJoiningFleet) {
            IEnumerable<IFleetCmd> unusedJoinableFleets;
            return TryGetJoinableFleetsFor(potentialJoiningFleet, out unusedJoinableFleets);
        }

        public bool TryGetJoinableFleetsFor(IFleetCmd potentialJoiningFleet, out IEnumerable<IFleetCmd> joinableFleets) {
            D.AssertEqual(Owner, potentialJoiningFleet.Owner);
            int additionalElementCount = potentialJoiningFleet.ElementCount;
            joinableFleets = OwnerFleets.Where(f => f.IsJoinableBy(additionalElementCount)).Except(potentialJoiningFleet);
            return joinableFleets.Any();
        }

        public bool AreAnyBaseHangersJoinableBy(IShip ship) {
            IEnumerable<IUnitBaseCmd> unusedJoinableHangerBases;
            return TryGetJoinableHangerBasesFor(ship, out unusedJoinableHangerBases);
        }

        public bool TryGetJoinableHangerBasesFor(IShip ship, out IEnumerable<IUnitBaseCmd> joinableHangerBases) {
            D.Assert(ship.__HasCommand);
            D.AssertEqual(Owner, ship.Owner);
            joinableHangerBases = OwnerBases.Where(b => b.Hanger.IsJoinable);
            return joinableHangerBases.Any();
        }

        public bool AreAnyBaseHangersJoinableBy(IFleetCmd potentialJoiningFleet) {
            D.AssertEqual(Owner, potentialJoiningFleet.Owner);
            return AreAnyBaseHangersJoinable(potentialJoiningFleet.ElementCount);
        }

        public bool TryGetJoinableHangerBasesFor(IFleetCmd potentialJoiningFleet, out IEnumerable<IUnitBaseCmd> joinableHangerBases) {
            D.AssertEqual(Owner, potentialJoiningFleet.Owner);
            return TryGetJoinableHangerBases(potentialJoiningFleet.ElementCount, out joinableHangerBases);
        }

        public bool AreAnyBaseHangersJoinable(int reqdHangerSlots) {
            IEnumerable<IUnitBaseCmd> unusedJoinableHangerBases;
            return TryGetJoinableHangerBases(reqdHangerSlots, out unusedJoinableHangerBases);
        }

        public bool TryGetJoinableHangerBases(int reqdHangerSlots, out IEnumerable<IUnitBaseCmd> joinableHangerBases) {
            joinableHangerBases = OwnerBases.Where(b => b.Hanger.IsJoinableBy(reqdHangerSlots));
            return joinableHangerBases.Any();
        }

        /// <summary>
        /// Indicates whether the Owner has knowledge of the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool HasKnowledgeOf(IOwnerItem_Ltd item) {
            D.AssertNotNull(item);
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
        public IEnumerable<IOwnerItem_Ltd> GetItemsOwnedBy(Player player) {
            var playerOwnedItems = new List<IOwnerItem_Ltd>();
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
            D.Assert(!_stars.Contains(star));
            _stars.Add(star);
            bool isAdded = _items.Add(star);
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

        private void OwnerCmdOutputsChangedEventHandler(object sender, EventArgs e) {
            RefreshTotalOutputs();
        }

        private void OwnerBaseCmdResourcesChangedEventHandler(object sender, EventArgs e) {
            RefreshTotalResources();
        }

        private void ItemDeathEventHandler(object sender, EventArgs e) {
            IMortalItem_Ltd deadItem = sender as IMortalItem_Ltd;
            HandleItemDeath(deadItem);
        }

        private void CmdOwnerChangedEventHandler(object sender, EventArgs e) {
            // Only Cmds owned by Owner were subscribed too
            RefreshTotalOutputs();
            RefreshTotalResources();
        }

        #endregion

        private void HandleItemDeath(IMortalItem_Ltd deadItem) {
            D.AssertNotNull(deadItem);
            D.Assert(deadItem.IsDead, DebugName);
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
            //D.Log("{0} has added {1} to its knowledge base.", DebugName, command.DebugName);

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

            bool isOwnedByOwner = Subscribe(command);
            if (isOwnedByOwner && IsOperational) {
                RefreshTotalOutputs();
                RefreshTotalResources();
            }

            return true;
        }

        /// <summary>
        /// Wires subscriptions for the provided cmd.
        /// As a convenience, returns <c>true</c> if the cmd is owned by the Owner of this
        /// Knowledge, <c>false</c> otherwise.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns></returns>
        private bool Subscribe(IUnitCmd_Ltd cmd) {
            cmd.deathOneShot += ItemDeathEventHandler;
            var unitCmd = cmd as IUnitCmd;
            if (unitCmd.Owner == Owner) {
                unitCmd.ownerChanged += CmdOwnerChangedEventHandler;
                unitCmd.unitOutputsChanged += OwnerCmdOutputsChangedEventHandler;
                var unitBaseCmd = unitCmd as IUnitBaseCmd;
                if (unitBaseCmd != null) {
                    unitBaseCmd.resourcesChanged += OwnerBaseCmdResourcesChangedEventHandler;
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// Removes the provided command from this player's knowledge. Throws an error if not present.
        /// </summary>
        /// <param name="command">The command.</param>
        internal void RemoveCommand(IUnitCmd_Ltd command) {
            D.Assert(IsOperational);
            var isRemoved = _commands.Remove(command);
            isRemoved = isRemoved & _items.Remove(command);
            D.Assert(isRemoved);
            //D.Log("{0} has removed {1} from its knowledge base.", DebugName, command.DebugName);

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

            if (!command.IsDead) {
                if (command.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Alliance)) {
                    if (!command.IsOwnerChangeUnderway) {
                        D.Error("{0}: {1} is alive and being removed while in Alliance!", DebugName, command.DebugName);
                    }
                    else {
                        // 5.20.17 This path has not yet occurred
                        D.Warn("FYI. {0} had to rely on testing {1}.IsOwnerChangeUnderway to accept it.", DebugName, command.DebugName);
                    }
                }
            }

            Unsubscribe(command);
            RefreshTotalOutputs();
            RefreshTotalResources();
        }

        private void Unsubscribe(IUnitCmd_Ltd cmd) {
            cmd.deathOneShot -= ItemDeathEventHandler;
            // Can't check if command is owned by Owner as it could be Removed due to an ownership change in which case
            // Cmd.Owner would have already changed. Won't hurt to always remove.
            cmd.ownerChanged -= CmdOwnerChangedEventHandler;
            (cmd as IUnitCmd).unitOutputsChanged -= OwnerCmdOutputsChangedEventHandler;
            var baseCmd = cmd as IUnitBaseCmd;
            if (baseCmd != null) {
                baseCmd.resourcesChanged -= OwnerBaseCmdResourcesChangedEventHandler;
            }
        }

        /// <summary>
        /// Removes the element from this player's knowledge. Element's are
        /// removed when this Player's IntelCoverage of the element changes to None from another value.
        /// This only happens to ships and only when Player loses detection of the element,
        /// aka when no Player Cmds remain within sensor range. Removal because of death is handled by this Knowledge.
        /// </summary>
        /// <param name="element">The element.</param>
        internal void RemoveElement(IUnitElement_Ltd element) {
            var isRemoved = _elements.Remove(element);
            isRemoved = isRemoved & _items.Remove(element);
            D.Assert(isRemoved, element.DebugName);

            if (!element.IsDead) {
                if (element.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Alliance)) {
                    if (!element.IsOwnerChangeUnderway) {
                        D.Error("{0}: {1} is alive and being removed while in Alliance!", DebugName, element.DebugName);
                    }
                    else {
                        D.Log("{0} had to rely on testing {1}.IsOwnerChangeUnderway to accept it.", DebugName, element.DebugName);
                    }
                }
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
            D.Assert(deadPlanetoid.IsDead);
            var isRemoved = _planetoids.Remove(deadPlanetoid);
            isRemoved = isRemoved & _items.Remove(deadPlanetoid);
            D.Assert(isRemoved, deadPlanetoid.DebugName);
        }

        private void RefreshTotalOutputs() {
            TotalOutputs = OwnerCommands.Select(cmd => cmd.UnitOutputs).Sum();
        }

        private void RefreshTotalResources() {
            TotalResources = OwnerBases.Select(cmd => cmd.Resources).Sum();
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

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug 

        /// <summary>
        /// Debug. Returns the items that we know about that are owned by player.
        /// No Owner-access restrictions.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<IOwnerItem> __GetItemsOwnedBy(Player player) {
            var playerOwnedItems = new List<IOwnerItem>();
            _items.Cast<IOwnerItem>().ForAll(item => {
                if (item.Owner == player) {
                    playerOwnedItems.Add(item);
                }
            });
            return playerOwnedItems;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void __InitializeValidatePlayerKnowledge() {
            GameReferences.DebugControls.validatePlayerKnowledgeNow += __ValidatePlayerKnowledgeNowEventHandler;
        }

        private void __ValidatePlayerKnowledgeNowEventHandler(object sender, EventArgs e) {
            __ValidatePlayerKnowledgeNow();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public void __ValidatePlayerKnowledgeNow() {
            //D.Log("{0} is validating its Knowledge.", DebugName);
            foreach (var item in OwnerItems) {
                D.AssertEqual(Owner, item.Owner);
            }
            //D.Log("{0}: All Unit Items owned: {1}.", DebugName, OwnerItems.Where(i => i is IUnitCmd || i is IUnitElement).Select(i => i.DebugName).Concatenate());

            bool isAllIntelCoverageComprehensive = GameReferences.DebugControls.IsAllIntelCoverageComprehensive;
            foreach (var item in _items) {
                var mortalItem = item as IMortalItem;
                if (mortalItem != null) {
                    D.Assert(!mortalItem.IsDead);
                }

                bool isIntelCoverageExpected = true;
                var intelItem = item as IIntelItem;
                IntelCoverage coverage = intelItem.GetIntelCoverage(Owner);
                if (coverage == IntelCoverage.Comprehensive) {
                    isIntelCoverageExpected = intelItem.__IsPlayerEntitledToComprehensiveRelationship(Owner);
                }
                else if (coverage == IntelCoverage.None) {
                    isIntelCoverageExpected = false;
                }

                if (!isIntelCoverageExpected) {
                    D.Warn("{0} has found unexpected IntelCoverage.{1} on {2}.", DebugName, coverage.GetValueName(), item.DebugName);
                }
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void __CleanupValidatePlayerKnowledge() {
            GameReferences.DebugControls.validatePlayerKnowledgeNow -= __ValidatePlayerKnowledgeNowEventHandler;
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

