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
        private IDictionary<EquipmentCategory, Level> _currentNonHullEquipLevelLookup;

        private IDictionary<ShipHullCategory, Level> _currentShipHullLevelLookup;
        private IDictionary<FacilityHullCategory, Level> _currentFacilityHullLevelLookup;

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

        /// <summary>
        /// Initializes the equip level lookup fields.
        /// <remarks>At first, this also initialized all the CurrentLevel values too. Now that is
        /// handled by the PlayerResearchManager via UpdateCurrentLevel() below. TODO PlayerResearchManager
        /// will need to also use UpdateCurrentLevel when it completes research on a new tech, assuming
        /// new EquipmentStats are enabled in the tech.</remarks>
        /// </summary>
        private void InitializeEquipLevelLookup() {
            _currentNonHullEquipLevelLookup = new Dictionary<EquipmentCategory, Level>();
            _currentFacilityHullLevelLookup = new Dictionary<FacilityHullCategory, Level>();
            _currentShipHullLevelLookup = new Dictionary<ShipHullCategory, Level>();
        }

        public void UpdateCurrentLevel(EquipmentCategory eCat, Level currentLevel) {
            D.AssertNotEqual(EquipmentCategory.Hull, eCat);
            _currentNonHullEquipLevelLookup[eCat] = currentLevel;
        }

        public void UpdateCurrentLevel(ShipHullCategory hullCat, Level currentLevel) {
            _currentShipHullLevelLookup[hullCat] = currentLevel;
        }

        public void UpdateCurrentLevel(FacilityHullCategory hullCat, Level currentLevel) {
            _currentFacilityHullLevelLookup[hullCat] = currentLevel;
        }

        public IEnumerable<ShipHullStat> GetAllCurrentShipHullStats() {
            IList<ShipHullStat> stats = new List<ShipHullStat>();
            var allHullCats = TempGameValues.ShipHullCategoriesInUse;
            ShipHullStat hullStat;
            foreach (var hullCat in allHullCats) {
                if (TryGetCurrentHullStat(hullCat, out hullStat)) {
                    stats.Add(hullStat);
                }
            }
            return stats;
        }

        public IEnumerable<FacilityHullStat> GetAllCurrentFacilityHullStats() {
            IList<FacilityHullStat> stats = new List<FacilityHullStat>();
            var allHullCats = TempGameValues.FacilityHullCategoriesInUse;
            FacilityHullStat hullStat;
            foreach (var hullCat in allHullCats) {
                if (TryGetCurrentHullStat(hullCat, out hullStat)) {
                    stats.Add(hullStat);
                }
            }
            return stats;
        }

        /// <summary>
        /// Returns the EquipmentStats that are currently valid for use when creating an Element Design.
        /// <remarks>Used by AUnitDesignWindow to populate the EquipmentStats that can be picked to create or modify a design.</remarks>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<AEquipmentStat> GetCurrentElementEquipmentStats() {
            List<AEquipmentStat> currentElementStats = new List<AEquipmentStat>();
            foreach (var eCat in TempGameValues.EquipCatsSupportedByElementDesigner) {
                switch (eCat) {
                    case EquipmentCategory.PassiveCountermeasure:
                        PassiveCountermeasureStat pStat;
                        if (TryGetCurrentPassiveCmStat(out pStat)) {
                            currentElementStats.Add(pStat);
                        }
                        break;
                    case EquipmentCategory.SRSensor:
                        currentElementStats.Add(GetCurrentSRSensorStat());
                        break;
                    case EquipmentCategory.SRActiveCountermeasure:
                    case EquipmentCategory.MRActiveCountermeasure:
                        ActiveCountermeasureStat aStat;
                        if (TryGetCurrentActiveCmStat(eCat, out aStat)) {
                            currentElementStats.Add(aStat);
                        }
                        break;
                    case EquipmentCategory.AssaultWeapon:
                    case EquipmentCategory.BeamWeapon:
                    case EquipmentCategory.MissileWeapon:
                    case EquipmentCategory.ProjectileWeapon:
                        AWeaponStat wStat;
                        if (TryGetCurrentWeaponStat(eCat, out wStat)) {
                            currentElementStats.Add(wStat);
                        }
                        break;
                    case EquipmentCategory.ShieldGenerator:
                        ShieldGeneratorStat sgStat;
                        if (TryGetCurrentShieldGeneratorStat(out sgStat)) {
                            currentElementStats.Add(sgStat);
                        }
                        break;
                    case EquipmentCategory.StlPropulsion:
                    case EquipmentCategory.FtlPropulsion:
                    case EquipmentCategory.Hull:
                    case EquipmentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eCat));
                }
            }
            return currentElementStats;
        }

        /// <summary>
        /// Returns the EquipmentStats that are currently valid for use when creating a CmdModule Design.
        /// <remarks>Used by AUnitDesignWindow to populate the EquipmentStats that can be picked to create or modify a design.</remarks>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<AEquipmentStat> GetCurrentCmdModuleEquipmentStats() {
            List<AEquipmentStat> currentCmdModuleStats = new List<AEquipmentStat>();
            foreach (var eCat in TempGameValues.EquipCatsSupportedByCmdModuleDesigner) {
                switch (eCat) {
                    case EquipmentCategory.PassiveCountermeasure:
                        PassiveCountermeasureStat pStat;
                        if (TryGetCurrentPassiveCmStat(out pStat)) {
                            currentCmdModuleStats.Add(pStat);
                        }
                        break;
                    case EquipmentCategory.MRSensor:
                        currentCmdModuleStats.Add(GetCurrentMRCmdSensorStat());
                        break;
                    case EquipmentCategory.LRSensor:
                        SensorStat sStat;
                        if (TryGetCurrentLRCmdSensorStat(out sStat)) {
                            currentCmdModuleStats.Add(sStat);
                        }
                        break;
                    case EquipmentCategory.FtlDampener:
                        currentCmdModuleStats.Add(GetCurrentFtlDampenerStat());
                        break;
                    case EquipmentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eCat));
                }
            }
            return currentCmdModuleStats;
        }

        public SensorStat GetCurrentSRSensorStat() {
            Level playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.SRSensor];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.SRSensor, playerLevel) as SensorStat;
        }

        public SensorStat GetCurrentMRCmdSensorStat() {
            Level playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.MRSensor];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.MRSensor, playerLevel) as SensorStat;
        }

        public bool TryGetCurrentLRCmdSensorStat(out SensorStat sStat) {
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(EquipmentCategory.LRSensor, out playerLevel)) {
                sStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.LRSensor, playerLevel) as SensorStat;
                return true;
            }
            sStat = null;
            return false;
        }


        public bool TryGetCurrentFtlEngineStat(out EngineStat eStat) {
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(EquipmentCategory.FtlPropulsion, out playerLevel)) {
                eStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.FtlPropulsion, playerLevel) as EngineStat;
                return true;
            }
            eStat = null;
            return false;
        }

        public EngineStat GetCurrentStlEngineStat() {
            var playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.StlPropulsion];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.StlPropulsion, playerLevel) as EngineStat;
        }

        public bool TryGetCurrentHullStat(ShipHullCategory hullCat, out ShipHullStat hullStat) {
            Level playerLevel;
            if (_currentShipHullLevelLookup.TryGetValue(hullCat, out playerLevel)) {
                hullStat = _eStatFactory.MakeInstance(_player, hullCat, playerLevel);
                return true;
            }
            hullStat = null;
            return false;
        }

        public bool TryGetCurrentHullStat(FacilityHullCategory hullCat, out FacilityHullStat hullStat) {
            Level playerLevel;
            if (_currentFacilityHullLevelLookup.TryGetValue(hullCat, out playerLevel)) {
                hullStat = _eStatFactory.MakeInstance(_player, hullCat, playerLevel);
                return true;
            }
            hullStat = null;
            return false;
        }

        public bool TryGetCurrentShieldGeneratorStat(out ShieldGeneratorStat sgStat) {
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(EquipmentCategory.ShieldGenerator, out playerLevel)) {
                sgStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.ShieldGenerator, playerLevel) as ShieldGeneratorStat;
                return true;
            }
            sgStat = null;
            return false;
        }

        public FtlDampenerStat GetCurrentFtlDampenerStat() {
            Level playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.FtlDampener];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.FtlDampener, playerLevel) as FtlDampenerStat;
        }

        public FleetCmdModuleStat GetCurrentFleetCmdModuleStat() {
            Level playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.FleetCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.FleetCmdModule, playerLevel) as FleetCmdModuleStat;
        }

        public StarbaseCmdModuleStat GetCurrentStarbaseCmdModuleStat() {
            var playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.StarbaseCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.StarbaseCmdModule, playerLevel) as StarbaseCmdModuleStat;
        }

        public SettlementCmdModuleStat GetCurrentSettlementCmdModuleStat() {
            Level playerLevel = _currentNonHullEquipLevelLookup[EquipmentCategory.SettlementCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.SettlementCmdModule, playerLevel) as SettlementCmdModuleStat;
        }

        public bool TryGetCurrentPassiveCmStat(out PassiveCountermeasureStat pStat) {
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(EquipmentCategory.PassiveCountermeasure, out playerLevel)) {
                pStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.PassiveCountermeasure, playerLevel) as PassiveCountermeasureStat;
                return true;
            }
            pStat = null;
            return false;
        }

        public bool TryGetCurrentActiveCmStat(EquipmentCategory activeCmCat, out ActiveCountermeasureStat aStat) {
            D.Assert(activeCmCat == EquipmentCategory.SRActiveCountermeasure || activeCmCat == EquipmentCategory.MRActiveCountermeasure);
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(activeCmCat, out playerLevel)) {
                aStat = _eStatFactory.MakeInstance(_player, activeCmCat, playerLevel) as ActiveCountermeasureStat;
                return true;
            }
            aStat = null;
            return false;
        }

        public bool TryGetCurrentWeaponStat(EquipmentCategory weapCat, out AWeaponStat wStat) {
            D.Assert(weapCat == EquipmentCategory.AssaultWeapon || weapCat == EquipmentCategory.BeamWeapon
            || weapCat == EquipmentCategory.MissileWeapon || weapCat == EquipmentCategory.ProjectileWeapon);
            Level playerLevel;
            if (_currentNonHullEquipLevelLookup.TryGetValue(weapCat, out playerLevel)) {
                wStat = _eStatFactory.MakeInstance(_player, weapCat, playerLevel) as AWeaponStat;
                return true;
            }
            wStat = null;
            return false;
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

