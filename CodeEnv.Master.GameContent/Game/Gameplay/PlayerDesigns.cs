// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerDesigns.cs
// Provides access to AUnitMemberDesigns and current AEquipmentStats for a player.
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
    using MoreLinq;

    /// <summary>
    /// Provides access to AUnitMemberDesigns and current AEquipmentStats for a player.
    /// </summary>
    public class PlayerDesigns {

        private const string DebugNameFormat = "{0}{1}";

        private static EquipmentCategory[] _equipCatsUsedByFleetCmdTemplateDesign = {
                                                                                        EquipmentCategory.FleetCmdModule,
                                                                                        EquipmentCategory.FtlDampener,
                                                                                        EquipmentCategory.MRSensor
                                                                                    };

        private static EquipmentCategory[] _equipCatsUsedByStarbaseCmdTemplateDesign = {
                                                                                        EquipmentCategory.StarbaseCmdModule,
                                                                                        EquipmentCategory.FtlDampener,
                                                                                        EquipmentCategory.MRSensor
                                                                                    };

        private static EquipmentCategory[] _equipCatsUsedBySettlementCmdTemplateDesign = {
                                                                                        EquipmentCategory.SettlementCmdModule,
                                                                                        EquipmentCategory.FtlDampener,
                                                                                        EquipmentCategory.MRSensor
                                                                                    };

        private static EquipmentCategory[] _equipCatsUsedByFacilityTemplateDesign = {
                                                                                        EquipmentCategory.FHullBarracks,
                                                                                        EquipmentCategory.FHullCentralHub,
                                                                                        EquipmentCategory.FHullColonyHab,
                                                                                        EquipmentCategory.FHullDefense,
                                                                                        EquipmentCategory.FHullEconomic,
                                                                                        EquipmentCategory.FHullFactory,
                                                                                        EquipmentCategory.FHullLaboratory,
                                                                                        EquipmentCategory.SRSensor
                                                                                    };

        private static EquipmentCategory[] _equipCatsUsedByShipTemplateDesign =     {
                                                                                        EquipmentCategory.SHullCarrier,
                                                                                        EquipmentCategory.SHullColonizer,
                                                                                        EquipmentCategory.SHullCruiser,
                                                                                        EquipmentCategory.SHullDestroyer,
                                                                                        EquipmentCategory.SHullDreadnought,
                                                                                        EquipmentCategory.SHullFrigate,
                                                                                        EquipmentCategory.SHullInvestigator,
                                                                                        EquipmentCategory.SHullSupport,
                                                                                        EquipmentCategory.SHullTroop,
                                                                                        EquipmentCategory.SRSensor,
                                                                                        EquipmentCategory.FtlPropulsion,
                                                                                        EquipmentCategory.StlPropulsion
                                                                                    };


        public string DebugName { get { return DebugNameFormat.Inject(_player.DebugName, GetType().Name); } }

        private IDictionary<string, StarbaseCmdModuleDesign> _starbaseCmdModDesignLookupByName;
        private IDictionary<string, FleetCmdModuleDesign> _fleetCmdModDesignLookupByName;
        private IDictionary<string, SettlementCmdModuleDesign> _settlementCmdModDesignLookupByName;

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
            _starbaseCmdModDesignLookupByName = new Dictionary<string, StarbaseCmdModuleDesign>();
            _fleetCmdModDesignLookupByName = new Dictionary<string, FleetCmdModuleDesign>();
            _settlementCmdModDesignLookupByName = new Dictionary<string, SettlementCmdModuleDesign>();

            _shipDesignsLookupByHull = new Dictionary<ShipHullCategory, IList<ShipDesign>>();
            _facilityDesignsLookupByHull = new Dictionary<FacilityHullCategory, IList<FacilityDesign>>();

            _designNamesInUseLookup = new HashSet<string>();

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
            _currentEquipLevelLookup = new Dictionary<EquipmentCategory, Level>();
        }

        /// <summary>
        /// Makes any updates needed to current AEquipmentStats. If any Stat improvement effects
        /// a required Template Design, that Design is also updated.
        /// <remarks>Use of EquipmentStatID is simply a convenient container for passing the EquipmentCategory paired with
        /// its highest Level researched. In this case it is not used as a Stat ID.</remarks>
        /// </summary>
        /// <param name="allEnabledEqCatsWithHighestLevelResearched">All the enabled EquipmentCategories paired with their highest Level researched.</param>
        public void UpdateEquipLevelAndTemplateDesigns(IList<EquipmentStatID> allEnabledEqCatsWithHighestLevelResearched) {
            IList<EquipmentCategory> improvedEqCats = new List<EquipmentCategory>();
            foreach (var ePair in allEnabledEqCatsWithHighestLevelResearched) {
                EquipmentCategory eqCat = ePair.Category;
                Level eqLevel = ePair.Level;

                Level currentLevel;
                if (_currentEquipLevelLookup.TryGetValue(eqCat, out currentLevel)) {
                    D.Assert(eqLevel >= currentLevel);
                    if (eqLevel == currentLevel) {
                        continue;   // Level has not increased
                    }
                }
                _currentEquipLevelLookup[eqCat] = eqLevel;
                improvedEqCats.Add(eqCat);
            }

            if (GameReferences.GameManager.IsRunning) { // If not yet running, NewGameUnitGenerator has not yet created any ReqdDesigns
                if (improvedEqCats.Any()) {
                    UpdateTemplateDesigns(improvedEqCats);
                }
            }
        }

        /// <summary>
        /// Returns ShipHullStats that have been researched. The Level of the HullStat
        /// returned will be the highest level currently researched.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Returns FacilityHullStats that have been researched. The Level of the HullStat
        /// returned will be the highest level currently researched.
        /// </summary>
        /// <returns></returns>
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
        /// Returns the optional EquipmentStats that are currently valid for use when creating a ShipDesign.
        /// <remarks>Used by AUnitDesignWindow to populate the optional EquipmentStats that can be picked to create or modify a design.</remarks>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<AEquipmentStat> GetCurrentShipOptEquipStats() {
            List<AEquipmentStat> currentElementStats = new List<AEquipmentStat>();
            foreach (var eCat in TempGameValues.EquipCatsSupportedByShipDesigner) {
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
                    case EquipmentCategory.FtlPropulsion:
                        EngineStat ftlEngineStat;
                        if (TryGetCurrentFtlEngineStat(out ftlEngineStat)) {
                            currentElementStats.Add(ftlEngineStat);
                        }
                        break;
                    case EquipmentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eCat));
                }
            }
            return currentElementStats;
        }

        /// <summary>
        /// Returns the optional EquipmentStats that are currently valid for use when creating a FacilityDesign.
        /// <remarks>Used by AUnitDesignWindow to populate the optional EquipmentStats that can be picked to create or modify a design.</remarks>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<AEquipmentStat> GetCurrentFacilityOptEquipStats() {
            List<AEquipmentStat> currentElementStats = new List<AEquipmentStat>();
            foreach (var eCat in TempGameValues.EquipCatsSupportedByFacilityDesigner) {
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
                    case EquipmentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eCat));
                }
            }
            return currentElementStats;
        }

        /// <summary>
        /// Returns the optional EquipmentStats that are currently valid for use when creating a CmdModuleDesign.
        /// <remarks>Used by AUnitDesignWindow to populate the optional EquipmentStats that can be picked to create or modify a design.</remarks>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<AEquipmentStat> GetCurrentCmdModuleOptEquipStats() {
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
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.SRSensor];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.SRSensor, playerLevel) as SensorStat;
        }

        public SensorStat GetCurrentMRCmdSensorStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.MRSensor];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.MRSensor, playerLevel) as SensorStat;
        }

        public bool TryGetCurrentLRCmdSensorStat(out SensorStat sStat) {
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(EquipmentCategory.LRSensor, out playerLevel)) {
                sStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.LRSensor, playerLevel) as SensorStat;
                return true;
            }
            sStat = null;
            return false;
        }

        public EngineStat GetCurrentStlEngineStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.StlPropulsion];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.StlPropulsion, playerLevel) as EngineStat;
        }

        public bool TryGetCurrentFtlEngineStat(out EngineStat eStat) {
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(EquipmentCategory.FtlPropulsion, out playerLevel)) {
                eStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.FtlPropulsion, playerLevel) as EngineStat;
                return true;
            }
            eStat = null;
            return false;
        }

        public bool TryGetCurrentHullStat(ShipHullCategory hullCat, out ShipHullStat hullStat) {
            Level playerLevel;
            var hullECat = hullCat.EquipCategory();
            if (_currentEquipLevelLookup.TryGetValue(hullECat, out playerLevel)) {
                hullStat = _eStatFactory.MakeInstance(_player, hullECat, playerLevel) as ShipHullStat;
                return true;
            }
            hullStat = null;
            return false;
        }

        public bool TryGetCurrentHullStat(FacilityHullCategory hullCat, out FacilityHullStat hullStat) {
            Level playerLevel;
            var hullECat = hullCat.EquipCategory();
            if (_currentEquipLevelLookup.TryGetValue(hullECat, out playerLevel)) {
                hullStat = _eStatFactory.MakeInstance(_player, hullECat, playerLevel) as FacilityHullStat;
                return true;
            }
            hullStat = null;
            return false;
        }

        public bool TryGetCurrentShieldGeneratorStat(out ShieldGeneratorStat sgStat) {
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(EquipmentCategory.ShieldGenerator, out playerLevel)) {
                sgStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.ShieldGenerator, playerLevel) as ShieldGeneratorStat;
                return true;
            }
            sgStat = null;
            return false;
        }

        public FtlDampenerStat GetCurrentFtlDampenerStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.FtlDampener];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.FtlDampener, playerLevel) as FtlDampenerStat;
        }

        public FleetCmdModuleStat GetCurrentFleetCmdModuleStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.FleetCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.FleetCmdModule, playerLevel) as FleetCmdModuleStat;
        }

        public SettlementCmdModuleStat GetCurrentSettlementCmdModuleStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.SettlementCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.SettlementCmdModule, playerLevel) as SettlementCmdModuleStat;
        }

        [Obsolete]
        public StarbaseCmdModuleStat GetCurrentStarbaseCmdModuleStat() {
            Level playerLevel = _currentEquipLevelLookup[EquipmentCategory.StarbaseCmdModule];
            return _eStatFactory.MakeInstance(_player, EquipmentCategory.StarbaseCmdModule, playerLevel) as StarbaseCmdModuleStat;
        }

        public bool TryGetCurrentStarbaseCmdModuleStat(out StarbaseCmdModuleStat cmdModuleStat) {
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(EquipmentCategory.StarbaseCmdModule, out playerLevel)) {
                cmdModuleStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.StarbaseCmdModule, playerLevel) as StarbaseCmdModuleStat;
                return true;
            }
            cmdModuleStat = null;
            return false;
        }

        public bool TryGetCurrentPassiveCmStat(out PassiveCountermeasureStat pStat) {
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(EquipmentCategory.PassiveCountermeasure, out playerLevel)) {
                pStat = _eStatFactory.MakeInstance(_player, EquipmentCategory.PassiveCountermeasure, playerLevel) as PassiveCountermeasureStat;
                return true;
            }
            pStat = null;
            return false;
        }

        public bool TryGetCurrentActiveCmStat(EquipmentCategory activeCmCat, out ActiveCountermeasureStat aStat) {
            D.Assert(activeCmCat == EquipmentCategory.SRActiveCountermeasure || activeCmCat == EquipmentCategory.MRActiveCountermeasure);
            Level playerLevel;
            if (_currentEquipLevelLookup.TryGetValue(activeCmCat, out playerLevel)) {
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
            if (_currentEquipLevelLookup.TryGetValue(weapCat, out playerLevel)) {
                wStat = _eStatFactory.MakeInstance(_player, weapCat, playerLevel) as AWeaponStat;
                return true;
            }
            wStat = null;
            return false;
        }

        #endregion

        #region Update Required Designs

        /// <summary>
        /// Makes any updates needed to Required Designs (Templates).
        /// </summary>
        /// <param name="improvedEqCats">The EquipmentCategories that were improved, aka increased their Level.</param>
        private void UpdateTemplateDesigns(IEnumerable<EquipmentCategory> improvedEqCats) {
            List<EquipmentCategory> relevantImprovedEqCats = improvedEqCats.Intersect(_equipCatsUsedByFleetCmdTemplateDesign).ToList();
            if (relevantImprovedEqCats.Any()) {
                UpdateFleetCmdTemplateDesign();
            }

            relevantImprovedEqCats.Clear();
            relevantImprovedEqCats.AddRange(improvedEqCats.Intersect(_equipCatsUsedByStarbaseCmdTemplateDesign));
            if (relevantImprovedEqCats.Any()) {
                UpdateStarbaseCmdTemplateDesign();
            }

            relevantImprovedEqCats.Clear();
            relevantImprovedEqCats.AddRange(improvedEqCats.Intersect(_equipCatsUsedBySettlementCmdTemplateDesign));
            if (relevantImprovedEqCats.Any()) {
                UpdateSettlementCmdTemplateDesign();
            }

            relevantImprovedEqCats.Clear();
            relevantImprovedEqCats.AddRange(improvedEqCats.Intersect(_equipCatsUsedByFacilityTemplateDesign));
            if (relevantImprovedEqCats.Any()) {
                UpdateFacilityTemplateDesigns(relevantImprovedEqCats);
            }

            relevantImprovedEqCats.Clear();
            relevantImprovedEqCats.AddRange(improvedEqCats.Intersect(_equipCatsUsedByShipTemplateDesign));
            if (relevantImprovedEqCats.Any()) {
                UpdateShipTemplateDesigns(relevantImprovedEqCats);
            }
        }

        private void UpdateFleetCmdTemplateDesign() {
            FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
            var cmdModuleStat = GetCurrentFleetCmdModuleStat();
            var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
            FleetCmdModuleDesign cmdModDesign = new FleetCmdModuleDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                RootDesignName = TempGameValues.FleetCmdModTemplateRootDesignName,
                Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
            };

            D.Assert(IsFleetCmdTemplateDesignPresent());    // will always be present as only called after NewGameUnitGenerator makes designs

            FleetCmdModuleDesign existingTemplateDesign = GetCurrentFleetCmdModTemplateDesign();
            int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
            cmdModDesign.IncrementDesignLevelAndName(newDesignLevel);

            Add(cmdModDesign);
        }

        private void UpdateSettlementCmdTemplateDesign() {
            FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
            var cmdModuleStat = GetCurrentSettlementCmdModuleStat();
            var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
            SettlementCmdModuleDesign cmdModDesign = new SettlementCmdModuleDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                RootDesignName = TempGameValues.SettlementCmdModTemplateRootDesignName,
                Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
            };

            D.Assert(IsSettlementCmdTemplateDesignPresent());    // will always be present as only called after NewGameUnitGenerator makes designs

            SettlementCmdModuleDesign existingTemplateDesign = GetCurrentSettlementCmdModTemplateDesign();
            int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
            cmdModDesign.IncrementDesignLevelAndName(newDesignLevel);

            Add(cmdModDesign);
        }

        private void UpdateStarbaseCmdTemplateDesign() {
            StarbaseCmdModuleStat cmdModuleStat;
            if (TryGetCurrentStarbaseCmdModuleStat(out cmdModuleStat)) {
                FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
                var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
                StarbaseCmdModuleDesign cmdModDesign = new StarbaseCmdModuleDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                    RootDesignName = TempGameValues.StarbaseCmdModTemplateRootDesignName,
                    Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
                };

                if (IsStarbaseCmdTemplateDesignPresent()) {  // if CmdModuleStat JUST enabled, there won't yet be a Template design
                    StarbaseCmdModuleDesign existingTemplateDesign = GetCurrentStarbaseCmdModTemplateDesign();
                    int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
                    cmdModDesign.IncrementDesignLevelAndName(newDesignLevel);
                }

                Add(cmdModDesign);
            }
        }

        private void UpdateFacilityTemplateDesigns(IEnumerable<EquipmentCategory> updatedEqCats) {
            D.AssertNotEqual(Constants.Zero, updatedEqCats.Count());

            // 5.1.18 This just reduces which HullCats to consider for updates. UpdateFacilityTemplateDesign(HullCat)
            // will still test for whether there the HullStat for that HullCat is available.
            IEnumerable<FacilityHullCategory> hullCatsToUpdate;
            if (updatedEqCats.Contains(EquipmentCategory.SRSensor)) {
                hullCatsToUpdate = TempGameValues.FacilityHullCategoriesInUse;
            }
            else {
                hullCatsToUpdate = updatedEqCats.Select(eCat => eCat.FacilityHullCat());
            }

            foreach (var hullCat in hullCatsToUpdate) {
                UpdateFacilityTemplateDesign(hullCat);
            }
        }

        private void UpdateFacilityTemplateDesign(FacilityHullCategory hullCat) {
            FacilityHullStat hullStat;
            if (TryGetCurrentHullStat(hullCat, out hullStat)) {
                SensorStat reqdSrSensorStat = GetCurrentSRSensorStat();
                FacilityDesign design = new FacilityDesign(_player, reqdSrSensorStat, hullStat) {
                    RootDesignName = hullCat.GetEmptyTemplateDesignName(),
                    Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
                };

                if (IsTemplateDesignPresent(hullCat)) {  // if hullCat's HullStat JUST enabled, there won't yet be a Template design
                    FacilityDesign existingTemplateDesign = GetCurrentFacilityTemplateDesign(hullCat);
                    int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
                    design.IncrementDesignLevelAndName(newDesignLevel);
                }

                Add(design);
            }
        }

        private void UpdateShipTemplateDesigns(IEnumerable<EquipmentCategory> updatedEqCats) {
            D.AssertNotEqual(Constants.Zero, updatedEqCats.Count());

            // 5.1.18 This just reduces which HullCats to consider for updates. UpdateFacilityTemplateDesign(HullCat)
            // will still test for whether there the HullStat for that HullCat is available.
            IEnumerable<ShipHullCategory> hullCatsToUpdate;
            if (updatedEqCats.Contains(EquipmentCategory.SRSensor) || updatedEqCats.Contains(EquipmentCategory.StlPropulsion)
                || updatedEqCats.Contains(EquipmentCategory.FtlPropulsion)) {
                hullCatsToUpdate = TempGameValues.ShipHullCategoriesInUse;
            }
            else {
                hullCatsToUpdate = updatedEqCats.Select(eCat => eCat.ShipHullCat());
            }

            foreach (var hullCat in hullCatsToUpdate) {
                UpdateShipTemplateDesign(hullCat);
            }
        }

        private void UpdateShipTemplateDesign(ShipHullCategory hullCat) {
            ShipHullStat hullStat;
            if (TryGetCurrentHullStat(hullCat, out hullStat)) {
                SensorStat reqdSrSensorStat = GetCurrentSRSensorStat();
                EngineStat stlEngineStat = GetCurrentStlEngineStat();

                ShipDesign design = new ShipDesign(_player, reqdSrSensorStat, hullStat, stlEngineStat, ShipCombatStance.Disengage) {
                    RootDesignName = hullCat.GetEmptyTemplateDesignName(),
                    Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
                };

                EngineStat ftlEngineStat;
                if (TryGetCurrentFtlEngineStat(out ftlEngineStat)) {
                    OptionalEquipSlotID slotID;
                    bool isEmptySlotFound = design.TryGetEmptySlotIDFor(EquipmentCategory.FtlPropulsion, out slotID);
                    D.Assert(isEmptySlotFound);
                    design.Add(slotID, ftlEngineStat);
                }

                if (IsTemplateDesignPresent(hullCat)) {  // if hullCat's HullStat JUST enabled, there won't yet be a Template design
                    ShipDesign existingTemplateDesign = GetCurrentShipTemplateDesign(hullCat);
                    int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
                    design.IncrementDesignLevelAndName(newDesignLevel);
                }

                Add(design);
            }
        }

        #endregion

        #region Add Design

        public void Add(ShipDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, design.Status);

            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            IList<ShipDesign> hullDesigns;
            if (_shipDesignsLookupByHull.TryGetValue(design.HullCategory, out hullDesigns)) {
                if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                    var designsThatShouldBeObsolete = hullDesigns.Where(d => d.RootDesignName == design.RootDesignName
                        && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                    if (designsThatShouldBeObsolete.Any()) {
                        D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                        designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                    }
                }
                else {
                    ShipDesign existingTemplateDesign = hullDesigns.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                    if (existingTemplateDesign != null) {
                        D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                        D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                        _shipDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                        hullDesigns.Remove(existingTemplateDesign);
                    }
                }
            }
            else {
                _shipDesignsLookupByHull.Add(design.HullCategory, new List<ShipDesign>());
            }

            _shipDesignLookupByName.Add(designName, design);
            _shipDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FacilityDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, design.Status);

            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            IList<FacilityDesign> hullDesigns;
            if (_facilityDesignsLookupByHull.TryGetValue(design.HullCategory, out hullDesigns)) {
                if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                    var designsThatShouldBeObsolete = hullDesigns.Where(d => d.RootDesignName == design.RootDesignName
                        && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                    if (designsThatShouldBeObsolete.Any()) {
                        D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                        designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                    }
                }
                else {
                    FacilityDesign existingTemplateDesign = hullDesigns.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                    if (existingTemplateDesign != null) {
                        D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                        D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                        _facilityDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                        hullDesigns.Remove(existingTemplateDesign);
                    }
                }
            }
            else {
                _facilityDesignsLookupByHull.Add(design.HullCategory, new List<FacilityDesign>());
            }

            _facilityDesignLookupByName.Add(designName, design);
            _facilityDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(StarbaseCmdModuleDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);

            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _starbaseCmdModDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                    && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else if (design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                StarbaseCmdModuleDesign existingTemplateDesign = _starbaseCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _starbaseCmdModDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
            }
            else {
                StarbaseCmdModuleDesign existingDefaultDesign = _starbaseCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
                if (existingDefaultDesign != null) {
                    D.Error("{0} found existing Default Design {1} when adding another {2}.", DebugName, existingDefaultDesign.DebugName, design.DebugName);
                }
            }
            _starbaseCmdModDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FleetCmdModuleDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);

            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }
            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _fleetCmdModDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else if (design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                FleetCmdModuleDesign existingTemplateDesign = _fleetCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _fleetCmdModDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
            }
            else {
                FleetCmdModuleDesign existingDefaultDesign = _fleetCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
                if (existingDefaultDesign != null) {
                    D.Error("{0} found existing Default Design {1} when adding another {2}.", DebugName, existingDefaultDesign.DebugName, design.DebugName);
                }
            }
            _fleetCmdModDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(SettlementCmdModuleDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);

            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _settlementCmdModDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else if (design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                SettlementCmdModuleDesign existingTemplateDesign = _settlementCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _settlementCmdModDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
            }
            else {
                SettlementCmdModuleDesign existingDefaultDesign = _settlementCmdModDesignLookupByName.Values
                    .SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
                if (existingDefaultDesign != null) {
                    D.Error("{0} found existing Default Design {1} when adding another {2}.", DebugName, existingDefaultDesign.DebugName, design.DebugName);
                }
            }
            _settlementCmdModDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        #endregion

        #region Obsolete Design

        [Obsolete("Error prone. Use ObsoleteDesign(design) instead.")]
        public void ObsoleteShipDesign(string designName) {
            ShipDesign design;
            if (!_shipDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(ShipDesign).Name, designName, _shipDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(ShipDesign design) {
            if (!_shipDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _shipDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        [Obsolete("Error prone. Use ObsoleteDesign(design) instead.")]
        public void ObsoleteFacilityDesign(string designName) {
            FacilityDesign design;
            if (!_facilityDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(FacilityDesign design) {
            if (!_facilityDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        [Obsolete("Error prone. Use ObsoleteDesign(design) instead.")]
        public void ObsoleteStarbaseCmdDesign(string designName) {
            StarbaseCmdModuleDesign design;
            if (!_starbaseCmdModDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdModuleDesign).Name, designName, _starbaseCmdModDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(StarbaseCmdModuleDesign design) {
            if (!_starbaseCmdModDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _starbaseCmdModDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        [Obsolete("Error prone. Use ObsoleteDesign(design) instead.")]
        public void ObsoleteSettlementCmdDesign(string designName) {
            SettlementCmdModuleDesign design;
            if (!_settlementCmdModDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdModuleDesign).Name, designName, _settlementCmdModDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(SettlementCmdModuleDesign design) {
            if (!_settlementCmdModDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _settlementCmdModDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        [Obsolete("Error prone. Use ObsoleteDesign(design) instead.")]
        public void ObsoleteFleetCmdDesign(string designName) {
            FleetCmdModuleDesign design;
            if (!_fleetCmdModDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FleetCmdModuleDesign).Name, designName, _fleetCmdModDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(FleetCmdModuleDesign design) {
            if (!_fleetCmdModDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _fleetCmdModDesignLookupByName.Keys.Concatenate());
            }
            // no need to Assert design.Status as existing Status only allowed to change from None or PlayerCreation_Current
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        #endregion

        #region Design Presence

        public bool CanDesignEverBeUpgraded(AUnitMemberDesign design) {
            return design.Player == _player;
        }

        public bool IsDesignNameInUse(string designName) {
            return _designNamesInUseLookup.Contains(designName);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Template or SystemCreation_Default when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(ShipDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _shipDesignLookupByName.Values
                .Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsTemplateDesignPresent(ShipHullCategory hullCat) {
            IList<ShipDesign> designs;
            if (_shipDesignsLookupByHull.TryGetValue(hullCat, out designs)) {
                var templateDesign = designs.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (templateDesign != null) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if a single non-obsolete design with the same RootDesignName is available to upgrade designToUpgrade.
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool IsUpgradeDesignAvailable(ShipDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            ShipDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if one or more non-obsolete designs are available to upgrade designToUpgrade.
        /// <remarks>Does not require the same RootDesignName in the other designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool AreUpgradeDesignsAvailable(ShipDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            IEnumerable<ShipDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(designToUpgrade, out unusedUpgradeDesigns);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Template or SystemCreation_Default when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(FacilityDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _facilityDesignLookupByName.Values
                .Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsTemplateDesignPresent(FacilityHullCategory hullCat) {
            IList<FacilityDesign> designs;
            if (_facilityDesignsLookupByHull.TryGetValue(hullCat, out designs)) {
                var templateDesign = designs.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (templateDesign != null) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if a single non-obsolete design with the same RootDesignName is available to upgrade designToUpgrade.
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool IsUpgradeDesignAvailable(FacilityDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            FacilityDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if one or more non-obsolete designs are available to upgrade designToUpgrade.
        /// <remarks>Does not require the same RootDesignName in the other designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool AreUpgradeDesignsAvailable(FacilityDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            IEnumerable<FacilityDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(designToUpgrade, out unusedUpgradeDesigns);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Template or SystemCreation_Default when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(FleetCmdModuleDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _fleetCmdModDesignLookupByName.Values
                .Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsFleetCmdTemplateDesignPresent() {
            var templateDesign = _fleetCmdModDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        /// <summary>
        /// Returns <c>true</c> if a single non-obsolete design with the same RootDesignName is available to upgrade designToUpgrade.
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool IsUpgradeDesignAvailable(FleetCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            FleetCmdModuleDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if one or more non-obsolete designs are available to upgrade designToUpgrade.
        /// <remarks>Does not require the same RootDesignName in the other designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool AreUpgradeDesignsAvailable(FleetCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            IEnumerable<FleetCmdModuleDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(designToUpgrade, out unusedUpgradeDesigns);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Template or SystemCreation_Default when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(StarbaseCmdModuleDesign design, out string designName) {
            var designsPresent = _starbaseCmdModDesignLookupByName.Values
                .Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsStarbaseCmdTemplateDesignPresent() {
            var templateDesign = _starbaseCmdModDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        /// <summary>
        /// Returns <c>true</c> if a single non-obsolete design with the same RootDesignName is available to upgrade designToUpgrade.
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool IsUpgradeDesignAvailable(StarbaseCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            StarbaseCmdModuleDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if one or more non-obsolete designs are available to upgrade designToUpgrade.
        /// <remarks>Does not require the same RootDesignName in the other designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool AreUpgradeDesignsAvailable(StarbaseCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            IEnumerable<StarbaseCmdModuleDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(designToUpgrade, out unusedUpgradeDesigns);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Template or SystemCreation_Default when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(SettlementCmdModuleDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _settlementCmdModDesignLookupByName.Values
                .Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            foreach (var presentDesign in designsPresent) {
                if (presentDesign.HasEqualContent(design)) {
                    designName = presentDesign.DesignName;
                    return true;
                }
            }
            designName = null;
            return false;
        }

        public bool IsSettlementCmdTemplateDesignPresent() {
            var templateDesign = _settlementCmdModDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        /// <summary>
        /// Returns <c>true</c> if a single non-obsolete design with the same RootDesignName is available to upgrade designToUpgrade.
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool IsUpgradeDesignAvailable(SettlementCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            SettlementCmdModuleDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if one or more non-obsolete designs are available to upgrade designToUpgrade.
        /// <remarks>Does not require the same RootDesignName in the other designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <returns></returns>
        public bool AreUpgradeDesignsAvailable(SettlementCmdModuleDesign designToUpgrade) {
            if (!CanDesignEverBeUpgraded(designToUpgrade)) {
                // 4.30.18 Occurs when the item of a design is taken over and then tests for refit potential
                D.Warn("{0}: An upgrade design presence inquiry occurred using {1} that can never be upgraded.", DebugName, designToUpgrade.DebugName);
                return false;
            }
            IEnumerable<SettlementCmdModuleDesign> unusedUpgradeDesigns;
            return TryGetUpgradeDesigns(designToUpgrade, out unusedUpgradeDesigns);
        }

        #endregion

        #region Get Design

        /// <summary>
        /// Returns all deployable ship designs.
        /// <remarks>Currently includeDefault does nothing as there are no default Element designs.</remarks>
        /// </summary>
        /// <param name="includeObsolete">if set to <c>true</c> [include obsolete].</param>
        /// <param name="includeDefault">if set to <c>true</c> [include default].</param>
        /// <returns></returns>
        public IEnumerable<ShipDesign> GetAllDeployableShipDesigns(bool includeObsolete = false, bool includeDefault = false) {
            var authorizedStatusList = new List<AUnitMemberDesign.SourceAndStatus> { AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current };
            if (includeObsolete) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            if (includeDefault) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            }
            return _shipDesignLookupByName.Values.Where(des => authorizedStatusList.Contains(des.Status));
        }

        public ShipDesign __GetShipDesign(string designName) {
            if (!_shipDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(ShipDesign).Name, designName, _shipDesignLookupByName.Keys.Concatenate());
            }
            return _shipDesignLookupByName[designName];
        }

        public ShipDesign GetCurrentShipTemplateDesign(ShipHullCategory hullCat) {
            return _shipDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        [Obsolete("Elements do not currently support SystemCreation_Default designs")]
        public ShipDesign GetShipDefaultDesign(ShipHullCategory hullCat) {
            return _shipDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
        }

        public bool TryGetDesign(string designName, out ShipDesign design) {
            return _shipDesignLookupByName.TryGetValue(designName, out design);
        }

        public bool TryGetDesigns(ShipHullCategory hullCategory, out IList<ShipDesign> designs) {
            return _shipDesignsLookupByHull.TryGetValue(hullCategory, out designs);
        }

        /// <summary>
        /// Returns <c>true</c> if there is an upgradeDesign available for designToUpgrade that has the same RootDesignName.
        /// <remarks>Used primarily by the AI to allow automatic selection of an upgrade.</remarks>
        /// <remarks>Use TryGetUpgradeDesigns() when either the user or the AI wishes to pick from the available non-obsolete designs
        /// that may have different RootDesignNames.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesign">The upgrade design.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesign(ShipDesign designToUpgrade, out ShipDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, designToUpgrade.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            IList<ShipDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, designToUpgrade.Status, designToUpgrade.DebugName);
                upgradeDesign = candidateDesigns.Single();
                D.Log("{0} has found an upgrade design {1} better than {2}.", DebugName, upgradeDesign.DebugName, designToUpgrade.DebugName);
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            //D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is one or more upgradeDesigns available for designToUpgrade, independent of designToUpgrade's RootDesignName.
        /// <remarks>Use TryGetUpgradeDesign() when either the user or the AI wishes to automatically upgrade within the same 
        /// RootDesignName series of designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesigns">The upgrade designs.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesigns(ShipDesign designToUpgrade, out IEnumerable<ShipDesign> upgradeDesigns) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            IList<ShipDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            upgradeDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current)
                .Except(designToUpgrade);
            return upgradeDesigns.Any();
        }

        /// <summary>
        /// Returns all deployable facility designs.
        /// <remarks>Currently includeDefault does nothing as there are no default Element designs.</remarks>
        /// </summary>
        /// <param name="includeObsolete">if set to <c>true</c> [include obsolete].</param>
        /// <param name="includeDefault">if set to <c>true</c> [include default].</param>
        /// <returns></returns>
        public IEnumerable<FacilityDesign> GetAllDeployableFacilityDesigns(bool includeObsolete = false, bool includeDefault = false) {
            var authorizedStatusList = new List<AUnitMemberDesign.SourceAndStatus> { AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current };
            if (includeObsolete) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            if (includeDefault) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            }
            return _facilityDesignLookupByName.Values.Where(des => authorizedStatusList.Contains(des.Status));
        }

        public FacilityDesign __GetFacilityDesign(string designName) {
            if (!_facilityDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            return _facilityDesignLookupByName[designName];
        }

        public FacilityDesign GetCurrentFacilityTemplateDesign(FacilityHullCategory hullCat) {
            return _facilityDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        [Obsolete("Elements do not currently support SystemCreation_Default designs")]
        public FacilityDesign GetFacilityDefaultDesign(FacilityHullCategory hullCat) {
            return _facilityDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
        }

        public bool TryGetDesign(string designName, out FacilityDesign design) {
            return _facilityDesignLookupByName.TryGetValue(designName, out design);
        }

        public bool TryGetDesigns(FacilityHullCategory hullCategory, out IList<FacilityDesign> designs) {
            return _facilityDesignsLookupByHull.TryGetValue(hullCategory, out designs);
        }

        /// <summary>
        /// Returns <c>true</c> if there is an upgradeDesign available for designToUpgrade that has the same RootDesignName.
        /// <remarks>Used primarily by the AI to allow automatic selection of an upgrade.</remarks>
        /// <remarks>Use TryGetUpgradeDesigns() when either the user or the AI wishes to pick from the available non-obsolete designs
        /// that may have different RootDesignNames.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesign">The upgrade design.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesign(FacilityDesign designToUpgrade, out FacilityDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, designToUpgrade.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            IList<FacilityDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, designToUpgrade.Status, designToUpgrade.DebugName);
                upgradeDesign = candidateDesigns.Single();
                D.Log("{0} has found an upgrade design {1} better than {2}.", DebugName, upgradeDesign.DebugName, designToUpgrade.DebugName);
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            //D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is one or more upgradeDesigns available for designToUpgrade, independent of designToUpgrade's RootDesignName.
        /// <remarks>Use TryGetUpgradeDesign() when either the user or the AI wishes to automatically upgrade within the same 
        /// RootDesignName series of designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesigns">The upgrade designs.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesigns(FacilityDesign designToUpgrade, out IEnumerable<FacilityDesign> upgradeDesigns) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            IList<FacilityDesign> hullDesigns;
            bool areDesignsPresent = TryGetDesigns(designToUpgrade.HullCategory, out hullDesigns);
            D.Assert(areDesignsPresent);

            upgradeDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current)
                .Except(designToUpgrade);
            return upgradeDesigns.Any();
        }

        public IEnumerable<StarbaseCmdModuleDesign> GetAllDeployableStarbaseCmdModDesigns(bool includeObsolete = false, bool includeDefault = false) {
            var authorizedStatusList = new List<AUnitMemberDesign.SourceAndStatus> { AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current };
            if (includeObsolete) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            if (includeDefault) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            }
            return _starbaseCmdModDesignLookupByName.Values.Where(des => authorizedStatusList.Contains(des.Status));
        }

        public StarbaseCmdModuleDesign __GetStarbaseCmdModDesign(string designName) {
            if (!_starbaseCmdModDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdModuleDesign).Name, designName, _starbaseCmdModDesignLookupByName.Keys.Concatenate());
            }
            return _starbaseCmdModDesignLookupByName[designName];
        }

        public StarbaseCmdModuleDesign GetCurrentStarbaseCmdModTemplateDesign() {
            return _starbaseCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        /// <summary>
        /// Gets the default CmdModuleDesign, created at the beginning of the game with Status.SystemCreation_Default.
        /// Typically it is the cheapest, lowest level design that can be created. It will never be Obsoleted.
        /// </summary>
        /// <returns></returns>
        public StarbaseCmdModuleDesign GetStarbaseCmdModDefaultDesign() {
            return _starbaseCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
        }

        /// <summary>
        /// Gets the default StarbaseCmdModuleDesign which will always be the cheapest of the lowest level designs available.
        /// The design returned will typically, but not always, be Obsolete. It will never be a Template design.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetStarbaseCmdModDefaultDesign")]
        public StarbaseCmdModuleDesign GetDefaultStarbaseCmdModDesign() {
            var allDesigns = _starbaseCmdModDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            int minLevel = allDesigns.Min(des => des.DesignLevel);
            D.AssertEqual(Constants.Zero, minLevel);
            return allDesigns.Where(des => des.DesignLevel == minLevel).MinBy(minLevelDes => minLevelDes.BuyoutCost);
        }

        public bool TryGetDesign(string designName, out StarbaseCmdModuleDesign design) {
            return _starbaseCmdModDesignLookupByName.TryGetValue(designName, out design);
        }

        /// <summary>
        /// Returns <c>true</c> if there is an upgradeDesign available for designToUpgrade that has the same RootDesignName.
        /// <remarks>Used primarily by the AI to allow automatic selection of an upgrade.</remarks>
        /// <remarks>Use TryGetUpgradeDesigns() when either the user or the AI wishes to pick from the available non-obsolete designs
        /// that may have different RootDesignNames.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesign">The upgrade design.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesign(StarbaseCmdModuleDesign designToUpgrade, out StarbaseCmdModuleDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, designToUpgrade.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            var candidateDesigns = _starbaseCmdModDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, designToUpgrade.Status, designToUpgrade.DebugName);
                upgradeDesign = candidateDesigns.Single();
                D.Log("{0} has found an upgrade design {1} better than {2}.", DebugName, upgradeDesign.DebugName, designToUpgrade.DebugName);
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            //D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is one or more upgradeDesigns available for designToUpgrade, independent of designToUpgrade's RootDesignName.
        /// <remarks>Use TryGetUpgradeDesign() when either the user or the AI wishes to automatically upgrade within the same 
        /// RootDesignName series of designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesigns">The upgrade designs.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesigns(StarbaseCmdModuleDesign designToUpgrade, out IEnumerable<StarbaseCmdModuleDesign> upgradeDesigns) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            upgradeDesigns = _starbaseCmdModDesignLookupByName.Values
                .Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current).Except(designToUpgrade);
            return upgradeDesigns.Any();
        }

        public IEnumerable<FleetCmdModuleDesign> GetAllDeployableFleetCmdModDesigns(bool includeObsolete = false, bool includeDefault = false) {
            var authorizedStatusList = new List<AUnitMemberDesign.SourceAndStatus> { AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current };
            if (includeObsolete) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            if (includeDefault) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            }
            return _fleetCmdModDesignLookupByName.Values.Where(des => authorizedStatusList.Contains(des.Status));
        }

        public FleetCmdModuleDesign __GetFleetCmdModDesign(string designName) {
            if (!_fleetCmdModDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(FleetCmdModuleDesign).Name, designName, _fleetCmdModDesignLookupByName.Keys.Concatenate());
            }
            return _fleetCmdModDesignLookupByName[designName];
        }

        public FleetCmdModuleDesign GetCurrentFleetCmdModTemplateDesign() {
            return _fleetCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        /// <summary>
        /// Gets the default CmdModuleDesign, created at the beginning of the game with Status.SystemCreation_Default.
        /// Typically it is the cheapest, lowest level design that can be created. It will never be Obsoleted.
        /// </summary>
        /// <returns></returns>
        public FleetCmdModuleDesign GetFleetCmdModDefaultDesign() {
            return _fleetCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
        }

        /// <summary>
        /// Gets the default FleetCmdModuleDesign which will always be the cheapest of the lowest level designs available.
        /// The design returned will typically, but not always, be Obsolete. It will never be a Template design.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetFleetCmdModDefaultDesign")]
        public FleetCmdModuleDesign GetDefaultFleetCmdModDesign() {
            var allDesigns = _fleetCmdModDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            int minLevel = allDesigns.Min(des => des.DesignLevel);
            D.AssertEqual(Constants.Zero, minLevel);
            return allDesigns.Where(des => des.DesignLevel == minLevel).MinBy(minLevelDes => minLevelDes.BuyoutCost);
        }

        public bool TryGetDesign(string designName, out FleetCmdModuleDesign design) {
            return _fleetCmdModDesignLookupByName.TryGetValue(designName, out design);
        }

        /// <summary>
        /// Returns <c>true</c> if there is an upgradeDesign available for designToUpgrade that has the same RootDesignName.
        /// <remarks>Used primarily by the AI to allow automatic selection of an upgrade.</remarks>
        /// <remarks>Use TryGetUpgradeDesigns() when either the user or the AI wishes to pick from the available non-obsolete designs
        /// that may have different RootDesignNames.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesign">The upgrade design.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesign(FleetCmdModuleDesign designToUpgrade, out FleetCmdModuleDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, designToUpgrade.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            var candidateDesigns = _fleetCmdModDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, designToUpgrade.Status, designToUpgrade.DebugName);
                upgradeDesign = candidateDesigns.Single();
                D.Log("{0} has found an upgrade design {1} better than {2}.", DebugName, upgradeDesign.DebugName, designToUpgrade.DebugName);
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            //D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is one or more upgradeDesigns available for designToUpgrade, independent of designToUpgrade's RootDesignName.
        /// <remarks>Use TryGetUpgradeDesign() when either the user or the AI wishes to automatically upgrade within the same 
        /// RootDesignName series of designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesigns">The upgrade designs.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesigns(FleetCmdModuleDesign designToUpgrade, out IEnumerable<FleetCmdModuleDesign> upgradeDesigns) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            upgradeDesigns = _fleetCmdModDesignLookupByName.Values
                .Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current).Except(designToUpgrade);
            return upgradeDesigns.Any();
        }

        public IEnumerable<SettlementCmdModuleDesign> GetAllDeployableSettlementCmdModDesigns(bool includeObsolete = false, bool includeDefault = false) {
            var authorizedStatusList = new List<AUnitMemberDesign.SourceAndStatus> { AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current };
            if (includeObsolete) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            if (includeDefault) {
                authorizedStatusList.Add(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            }
            return _settlementCmdModDesignLookupByName.Values.Where(des => authorizedStatusList.Contains(des.Status));
        }

        public SettlementCmdModuleDesign __GetSettlementCmdModDesign(string designName) {
            if (!_settlementCmdModDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdModuleDesign).Name, designName, _settlementCmdModDesignLookupByName.Keys.Concatenate());
            }
            return _settlementCmdModDesignLookupByName[designName];
        }

        public SettlementCmdModuleDesign GetCurrentSettlementCmdModTemplateDesign() {
            return _settlementCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        /// <summary>
        /// Gets the default CmdModuleDesign, created at the beginning of the game with Status.SystemCreation_Default.
        /// Typically it is the cheapest, lowest level design that can be created. It will never be Obsoleted.
        /// </summary>
        /// <returns></returns>
        public SettlementCmdModuleDesign GetSettlementCmdModDefaultDesign() {
            return _settlementCmdModDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
        }

        /// <summary>
        /// Gets the default SettlementCmdModuleDesign which will always be the cheapest of the lowest level designs available.
        /// The design returned will typically, but not always, be Obsolete. It will never be a Template design.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetSettlementCmdModDefaultDesign")]
        public SettlementCmdModuleDesign GetDefaultSettlementCmdModDesign() {
            var allDesigns = _settlementCmdModDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            int minLevel = allDesigns.Min(des => des.DesignLevel);
            D.AssertEqual(Constants.Zero, minLevel);
            return allDesigns.Where(des => des.DesignLevel == minLevel).MinBy(minLevelDes => minLevelDes.BuyoutCost);
        }

        public bool TryGetDesign(string designName, out SettlementCmdModuleDesign design) {
            return _settlementCmdModDesignLookupByName.TryGetValue(designName, out design);
        }

        /// <summary>
        /// Returns <c>true</c> if there is an upgradeDesign available for designToUpgrade that has the same RootDesignName.
        /// <remarks>Used primarily by the AI to allow automatic selection of an upgrade.</remarks>
        /// <remarks>Use TryGetUpgradeDesigns() when either the user or the AI wishes to pick from the available non-obsolete designs
        /// that may have different RootDesignNames.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesign">The upgrade design.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesign(SettlementCmdModuleDesign designToUpgrade, out SettlementCmdModuleDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, designToUpgrade.Status);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            var candidateDesigns = _settlementCmdModDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                && d.RootDesignName == designToUpgrade.RootDesignName).Except(designToUpgrade);
            if (candidateDesigns.Any()) {
                D.AssertEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, designToUpgrade.Status, designToUpgrade.DebugName);
                upgradeDesign = candidateDesigns.Single();
                D.Log("{0} has found an upgrade design {1} better than {2}.", DebugName, upgradeDesign.DebugName, designToUpgrade.DebugName);
                D.Assert(upgradeDesign.DesignLevel > designToUpgrade.DesignLevel);
                return true;
            }
            //D.Log("{0} has found no upgrade design better than {1}.", DebugName, designToUpgrade.DebugName);
            upgradeDesign = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is one or more upgradeDesigns available for designToUpgrade, independent of designToUpgrade's RootDesignName.
        /// <remarks>Use TryGetUpgradeDesign() when either the user or the AI wishes to automatically upgrade within the same 
        /// RootDesignName series of designs.</remarks>
        /// </summary>
        /// <param name="designToUpgrade">The design to upgrade.</param>
        /// <param name="upgradeDesigns">The upgrade designs.</param>
        /// <returns></returns>
        public bool TryGetUpgradeDesigns(SettlementCmdModuleDesign designToUpgrade, out IEnumerable<SettlementCmdModuleDesign> upgradeDesigns) {
            D.AssertEqual(_player, designToUpgrade.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToUpgrade.Status);

            upgradeDesigns = _settlementCmdModDesignLookupByName.Values
                .Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current).Except(designToUpgrade);
            return upgradeDesigns.Any();
        }

        #endregion

        #region Debug

        #endregion

        public override string ToString() {
            return DebugName;
        }


    }
}

