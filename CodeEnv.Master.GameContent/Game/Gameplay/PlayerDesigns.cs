// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerDesigns.cs
// Provides access to UnitDesigns and EquipmentStats for a player.
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

    /// <summary>
    /// Provides access to UnitDesigns and EquipmentStats for a player.
    /// </summary>
    public class PlayerDesigns {

        private const string DebugNameFormat = "{0}{1}";

        public string DebugName { get { return DebugNameFormat.Inject(_player.DebugName, GetType().Name); } }

        private IDictionary<string, StarbaseCmdDesign> _starbaseCmdDesignLookupByName;
        private IDictionary<string, FleetCmdDesign> _fleetCmdDesignLookupByName;
        private IDictionary<string, SettlementCmdDesign> _settlementCmdDesignLookupByName;

        private IDictionary<string, ShipDesign> _shipDesignLookupByName;
        private IDictionary<string, FacilityDesign> _facilityDesignLookupByName;

        private IDictionary<ShipHullCategory, IList<ShipDesign>> _shipDesignsLookupByHull;
        private IDictionary<FacilityHullCategory, IList<FacilityDesign>> _facilityDesignsLookupByHull;

        private HashSet<string> _designNamesInUseLookup;

        // UNDONE Once techs can be researched by players, these levels will need to be updated via a tech researched event
        private IDictionary<EquipmentCategory, Level> _currentEquipLevelLookup;

        private EquipmentStatFactory _eStatFactory;
        private Player _player;

        public PlayerDesigns(Player player) {
            _player = player;
            InitializeValuesAndReferences();
            InitializeEquipLevelLookup();
        }

        private void InitializeValuesAndReferences() {
            _shipDesignLookupByName = new Dictionary<string, ShipDesign>();
            _facilityDesignLookupByName = new Dictionary<string, FacilityDesign>();
            _starbaseCmdDesignLookupByName = new Dictionary<string, StarbaseCmdDesign>();
            _fleetCmdDesignLookupByName = new Dictionary<string, FleetCmdDesign>();
            _settlementCmdDesignLookupByName = new Dictionary<string, SettlementCmdDesign>();

            _shipDesignsLookupByHull = new Dictionary<ShipHullCategory, IList<ShipDesign>>();
            _facilityDesignsLookupByHull = new Dictionary<FacilityHullCategory, IList<FacilityDesign>>();

            _designNamesInUseLookup = new HashSet<string>();

            var shipHullCats = Enums<ShipHullCategory>.GetValues(excludeDefault: true);
            foreach (var hull in shipHullCats) {
                _shipDesignsLookupByHull.Add(hull, new List<ShipDesign>());
            }
            var facHullCats = Enums<FacilityHullCategory>.GetValues(excludeDefault: true);
            foreach (var hull in facHullCats) {
                _facilityDesignsLookupByHull.Add(hull, new List<FacilityDesign>());
            }
            _eStatFactory = EquipmentStatFactory.Instance;
        }

        #region Current Equipment Stats

        private void InitializeEquipLevelLookup() {
            _currentEquipLevelLookup = new Dictionary<EquipmentCategory, Level>();
            var allEquipCats = Enums<EquipmentCategory>.GetValues(excludeDefault: true);
            foreach (var eCat in allEquipCats) {
                _currentEquipLevelLookup.Add(eCat, Level.One);
            }
        }

        public IEnumerable<AEquipmentStat> GetCurrentEquipmentStats(IEnumerable<EquipmentCategory> supportedEquipCats) {
            List<AEquipmentStat> allAvailableStats = new List<AEquipmentStat>();
            foreach (var eCat in supportedEquipCats) {
                switch (eCat) {
                    case EquipmentCategory.PassiveCountermeasure:
                        allAvailableStats.Add(GetCurrentPassiveCmStat());
                        break;
                    case EquipmentCategory.ActiveCountermeasure:
                        allAvailableStats.AddRange(GetAllCurrentCmdSensorStats().Cast<AEquipmentStat>());
                        break;
                    case EquipmentCategory.BeamWeapon:
                    case EquipmentCategory.AssaultWeapon:
                    case EquipmentCategory.MissileWeapon:
                    case EquipmentCategory.ProjectileWeapon:
                        allAvailableStats.Add(GetCurrentWeaponStatFor(eCat));
                        break;
                    case EquipmentCategory.ElementSensor:
                        allAvailableStats.Add(GetCurrentElementSensorStat());
                        break;
                    case EquipmentCategory.CommandSensor:
                        allAvailableStats.AddRange(GetAllCurrentCmdSensorStats().Cast<AEquipmentStat>());
                        break;
                    case EquipmentCategory.ShieldGenerator:
                        allAvailableStats.Add(GetCurrentShieldGeneratorStat());
                        break;
                    case EquipmentCategory.Propulsion:
                    case EquipmentCategory.CommandModule:
                    case EquipmentCategory.FtlDampener:
                    case EquipmentCategory.Hull:
                    case EquipmentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eCat));
                }
            }
            return allAvailableStats;
        }

        public SensorStat GetCurrentElementSensorStat() {
            Level playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.ElementSensor);
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.ElementSensor, playerLevel) as SensorStat;
        }

        public SensorStat GetCurrentCmdSensorStat(RangeCategory rangeCat) {
            Level playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.CommandSensor);
            return _eStatFactory.MakeInstances(_player, EquipmentCategory.CommandSensor, playerLevel)
                .Single(stat => (stat as SensorStat).RangeCategory == rangeCat) as SensorStat;
        }

        public IEnumerable<SensorStat> GetAllCurrentCmdSensorStats() {
            Level playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.CommandSensor);
            return _eStatFactory.MakeInstances(_player, EquipmentCategory.CommandSensor, playerLevel).Cast<SensorStat>();
        }

        public EngineStat GetCurrentEngineStat(ShipHullCategory hullCat, bool isFtlEngine) {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.Propulsion);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.Propulsion, playerLevel);
            return aStats.Select(stat => new { eStat = (stat as EngineStat) })
                .Single(anony => anony.eStat.HullCategory == hullCat && anony.eStat.IsFtlEngine == isFtlEngine).eStat;
        }

        public ShipHullStat GetCurrentHullStat(ShipHullCategory hullCat) {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.Hull);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.Hull, playerLevel);
            return aStats.Select(stat => new { shStat = (stat as ShipHullStat) })
                .Single(anony => anony.shStat != null && anony.shStat.HullCategory == hullCat).shStat;
        }

        public IEnumerable<ShipHullStat> GetAllCurrentShipHullStats() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.Hull);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.Hull, playerLevel);
            return aStats.Where(stat => stat is ShipHullStat).Cast<ShipHullStat>();
        }

        public FacilityHullStat GetCurrentHullStat(FacilityHullCategory hullCat) {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.Hull);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.Hull, playerLevel);
            return aStats.Select(stat => new { fhStat = (stat as FacilityHullStat) })
                .Single(anony => anony.fhStat != null && anony.fhStat.HullCategory == hullCat).fhStat;
        }

        public IEnumerable<FacilityHullStat> GetAllCurrentFacilityHullStats() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.Hull);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.Hull, playerLevel);
            return aStats.Where(stat => stat is FacilityHullStat).Cast<FacilityHullStat>();
        }

        public ShieldGeneratorStat GetCurrentShieldGeneratorStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.ShieldGenerator);
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.ShieldGenerator, playerLevel) as ShieldGeneratorStat;
        }

        public FtlDampenerStat GetCurrentFtlDampenerStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.FtlDampener);
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.FtlDampener, playerLevel) as FtlDampenerStat;
        }

        public FleetCmdModuleStat GetCurrentFleetCmdModuleStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.CommandModule);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.CommandModule, playerLevel);
            return aStats.Single(stat => stat is FleetCmdModuleStat) as FleetCmdModuleStat;
        }

        public StarbaseCmdModuleStat GetCurrentStarbaseCmdModuleStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.CommandModule);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.CommandModule, playerLevel);
            return aStats.Single(stat => stat is StarbaseCmdModuleStat) as StarbaseCmdModuleStat;
        }

        public SettlementCmdModuleStat GetCurrentSettlementCmdModuleStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.CommandModule);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.CommandModule, playerLevel);
            return aStats.Single(stat => stat is SettlementCmdModuleStat) as SettlementCmdModuleStat;
        }

        public PassiveCountermeasureStat GetCurrentPassiveCmStat() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.PassiveCountermeasure);
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.PassiveCountermeasure, playerLevel) as PassiveCountermeasureStat;
        }

        public ActiveCountermeasureStat GetCurrentActiveCmStat(RangeCategory rangeCat) {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.ActiveCountermeasure);
            var aStats = _eStatFactory.MakeInstances(_player, EquipmentCategory.ActiveCountermeasure, playerLevel);
            return aStats.Single(stat => (stat as ActiveCountermeasureStat).RangeCategory == rangeCat) as ActiveCountermeasureStat;
        }

        public IEnumerable<ActiveCountermeasureStat> GetAllCurrentActiveCmStats() {
            var playerLevel = GetCurrentEquipLevelFor(EquipmentCategory.ActiveCountermeasure);
            return _eStatFactory.MakeInstances(_player, EquipmentCategory.ActiveCountermeasure, playerLevel).Cast<ActiveCountermeasureStat>();
        }

        public AWeaponStat GetCurrentWeaponStatFor(EquipmentCategory eCat) {
            D.Assert(eCat == EquipmentCategory.AssaultWeapon || eCat == EquipmentCategory.BeamWeapon
                        || eCat == EquipmentCategory.MissileWeapon || eCat == EquipmentCategory.ProjectileWeapon);
            var playerLevel = GetCurrentEquipLevelFor(eCat);
            return _eStatFactory.MakeInstance(_player, eCat, playerLevel) as AWeaponStat;
        }

        private Level GetCurrentEquipLevelFor(EquipmentCategory equipCat) {
            return _currentEquipLevelLookup[equipCat];
        }

        #endregion

        #region Add Design

        public void Add(ShipDesign design) {
            D.AssertEqual(_player, design.Player);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            IList<ShipDesign> hullDesigns;
            if (_shipDesignsLookupByHull.TryGetValue(design.HullCategory, out hullDesigns)) {
                var designsThatShouldBeObsolete = hullDesigns.Where(d => d.RootDesignName == design.RootDesignName
                    && d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteShipDesign(d.DesignName));
                }
            }

            _shipDesignLookupByName.Add(designName, design);

            _shipDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FacilityDesign design) {
            D.AssertEqual(_player, design.Player);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            IList<FacilityDesign> hullDesigns;
            if (_facilityDesignsLookupByHull.TryGetValue(design.HullCategory, out hullDesigns)) {
                var designsThatShouldBeObsolete = hullDesigns.Where(d => d.RootDesignName == design.RootDesignName
                && d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteFacilityDesign(d.DesignName));
                }
            }

            _facilityDesignLookupByName.Add(designName, design);

            _facilityDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(StarbaseCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }
            _starbaseCmdDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FleetCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }
            _fleetCmdDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(SettlementCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }
            _settlementCmdDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        #endregion

        #region Obsolete Design

        public void ObsoleteShipDesign(string designName) {
            ShipDesign design;
            if (!_shipDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(ShipDesign).Name, designName, _shipDesignLookupByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteFacilityDesign(string designName) {
            FacilityDesign design;
            if (!_facilityDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteStarbaseCmdDesign(string designName) {
            StarbaseCmdDesign design;
            if (!_starbaseCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdDesign).Name, designName, _starbaseCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteSettlementCmdDesign(string designName) {
            SettlementCmdDesign design;
            if (!_settlementCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdDesign).Name, designName, _settlementCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        public void ObsoleteFleetCmdDesign(string designName) {
            FleetCmdDesign design;
            if (!_fleetCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FleetCmdDesign).Name, designName, _fleetCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertEqual(AUnitMemberDesign.SourceAndStatus.Player_Current, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.Player_Obsolete;
        }

        #endregion

        #region Design Presence

        public bool IsDesignNameInUse(string designName) {
            return _designNamesInUseLookup.Contains(designName);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(ShipDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _shipDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsUpgradeDesignPresent(ShipDesign designToUpgrade) {
            ShipDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(FacilityDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _facilityDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsUpgradeDesignPresent(FacilityDesign designToUpgrade) {
            FacilityDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(FleetCmdDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _fleetCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(FleetCmdDesign designToUpgrade) {
            FleetCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(StarbaseCmdDesign design, out string designName) {
            var designsPresent = _starbaseCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(StarbaseCmdDesign designToUpgrade) {
            StarbaseCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in active designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns>
        ///   <c>true</c> if [is design present] [the specified design]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDesignPresent(SettlementCmdDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _settlementCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(SettlementCmdDesign designToUpgrade) {
            SettlementCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        #endregion

        #region Get Design

        public IEnumerable<ShipDesign> GetAllShipDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _shipDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _shipDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public ShipDesign GetShipDesign(string designName) {
            if (!_shipDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(ShipDesign).Name, designName, _shipDesignLookupByName.Keys.Concatenate());
            }
            return _shipDesignLookupByName[designName];
        }

        public bool TryGetDesign(string designName, out ShipDesign design) {
            return _shipDesignLookupByName.TryGetValue(designName, out design);
        }

        public bool TryGetDesigns(ShipHullCategory hullCategory, out IList<ShipDesign> designs) {
            return _shipDesignsLookupByHull.TryGetValue(hullCategory, out designs);
        }

        public bool TryGetUpgradeDesign(ShipDesign designToUpgrade, out ShipDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            IList<ShipDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(Constants.One, candidateDesigns.Count());
                upgradeDesign = candidateDesigns.First();
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        public IEnumerable<FacilityDesign> GetAllFacilityDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _facilityDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _facilityDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public FacilityDesign GetFacilityDesign(string designName) {
            if (!_facilityDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            return _facilityDesignLookupByName[designName];
        }

        public bool TryGetDesign(string designName, out FacilityDesign design) {
            return _facilityDesignLookupByName.TryGetValue(designName, out design);
        }

        public bool TryGetDesigns(FacilityHullCategory hullCategory, out IList<FacilityDesign> designs) {
            return _facilityDesignsLookupByHull.TryGetValue(hullCategory, out designs);
        }

        public bool TryGetUpgradeDesign(FacilityDesign designToUpgrade, out FacilityDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            IList<FacilityDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(Constants.One, candidateDesigns.Count());
                upgradeDesign = candidateDesigns.First();
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        public IEnumerable<StarbaseCmdDesign> GetAllStarbaseCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _starbaseCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _starbaseCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public StarbaseCmdDesign GetStarbaseCmdDesign(string designName) {
            if (!_starbaseCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdDesign).Name, designName, _starbaseCmdDesignLookupByName.Keys.Concatenate());
            }
            return _starbaseCmdDesignLookupByName[designName];
        }

        public bool TryGetDesign(string designName, out StarbaseCmdDesign design) {
            return _starbaseCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(StarbaseCmdDesign designToUpgrade, out StarbaseCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _starbaseCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(Constants.One, candidateDesigns.Count());
                upgradeDesign = candidateDesigns.First();
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        public IEnumerable<FleetCmdDesign> GetAllFleetCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _fleetCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _fleetCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public FleetCmdDesign GetFleetCmdDesign(string designName) {
            if (!_fleetCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(FleetCmdDesign).Name, designName, _fleetCmdDesignLookupByName.Keys.Concatenate());
            }
            return _fleetCmdDesignLookupByName[designName];
        }

        public bool TryGetDesign(string designName, out FleetCmdDesign design) {
            return _fleetCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(FleetCmdDesign designToUpgrade, out FleetCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _fleetCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(Constants.One, candidateDesigns.Count());
                upgradeDesign = candidateDesigns.First();
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        public IEnumerable<SettlementCmdDesign> GetAllSettlementCmdDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _settlementCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.Player_Obsolete);
            }
            return _settlementCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.Player_Current);
        }

        public SettlementCmdDesign GetSettlementCmdDesign(string designName) {
            if (!_settlementCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdDesign).Name, designName, _settlementCmdDesignLookupByName.Keys.Concatenate());
            }
            return _settlementCmdDesignLookupByName[designName];
        }

        public bool TryGetDesign(string designName, out SettlementCmdDesign design) {
            return _settlementCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(SettlementCmdDesign designToUpgrade, out SettlementCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _settlementCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.Player_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(Constants.One, candidateDesigns.Count());
                upgradeDesign = candidateDesigns.First();
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        #endregion

        #region Debug

        #endregion

        public override string ToString() {
            return DebugName;
        }


    }
}

