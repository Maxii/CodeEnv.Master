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

        private static EquipmentCategory[] _equipCatsUsedByFleetCmdDefaultDesign =  {
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
        /// a Required Design (Templates and Fleet's Default Cmd Design), that Design is also updated.
        /// <remarks>Use of EquipmentStatID is simply a convenient container for passing the EquipmentCategory paired with
        /// its highest Level researched. In this case it is not used as a Stat ID.</remarks>
        /// </summary>
        /// <param name="allEnabledEqCatsWithHighestLevelResearched">All the enabled EquipmentCategories paired with their highest Level researched.</param>
        public void UpdateEquipLevelAndReqdDesigns(IList<EquipmentStatID> allEnabledEqCatsWithHighestLevelResearched) {
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
                    UpdateReqdDesigns(improvedEqCats);
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
        /// Makes any updates needed to Required Designs (Templates and Fleet's Default Cmd Design).
        /// </summary>
        /// <param name="improvedEqCats">The EquipmentCategories that were improved, aka increased their Level.</param>
        private void UpdateReqdDesigns(IEnumerable<EquipmentCategory> improvedEqCats) {
            List<EquipmentCategory> relevantImprovedEqCats = improvedEqCats.Intersect(_equipCatsUsedByFleetCmdTemplateDesign).ToList();
            if (relevantImprovedEqCats.Any()) {
                UpdateFleetCmdTemplateDesign();
            }

            relevantImprovedEqCats.Clear();
            relevantImprovedEqCats.AddRange(improvedEqCats.Intersect(_equipCatsUsedByFleetCmdDefaultDesign));
            if (relevantImprovedEqCats.Any()) {
                UpdateFleetCmdDefaultDesign();
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
            FleetCmdDesign cmdDesign = new FleetCmdDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                RootDesignName = TempGameValues.FleetCmdTemplateRootDesignName,
                Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
            };

            D.Assert(IsFleetCmdTemplateDesignPresent());    // will always be present as only called after NewGameUnitGenerator makes designs

            FleetCmdDesign existingTemplateDesign = GetCurrentFleetTemplateDesign();
            int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
            cmdDesign.IncrementDesignLevelAndName(newDesignLevel);

            Add(cmdDesign);
        }

        private void UpdateFleetCmdDefaultDesign() {
            FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
            var cmdModuleStat = GetCurrentFleetCmdModuleStat();
            var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
            FleetCmdDesign cmdDesign = new FleetCmdDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                RootDesignName = TempGameValues.FleetCmdDefaultRootDesignName,
                Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
            };

            D.Assert(IsFleetCmdDefaultDesignPresent());    // will always be present as only called after NewGameUnitGenerator makes designs

            FleetCmdDesign existingDefaultDesign = GetCurrentDefaultFleetCmdDesign();
            int newDesignLevel = existingDefaultDesign.DesignLevel + Constants.One;
            cmdDesign.IncrementDesignLevelAndName(newDesignLevel);

            ObsoleteDesign(existingDefaultDesign);   // avoids warning of need to auto obsolete existing design

            Add(cmdDesign);
        }

        private void UpdateSettlementCmdTemplateDesign() {
            FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
            var cmdModuleStat = GetCurrentSettlementCmdModuleStat();
            var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
            SettlementCmdDesign cmdDesign = new SettlementCmdDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                RootDesignName = TempGameValues.SettlementCmdTemplateRootDesignName,
                Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
            };

            D.Assert(IsSettlementCmdTemplateDesignPresent());    // will always be present as only called after NewGameUnitGenerator makes designs

            SettlementCmdDesign existingTemplateDesign = GetCurrentSettlementTemplateDesign();
            int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
            cmdDesign.IncrementDesignLevelAndName(newDesignLevel);

            Add(cmdDesign);
        }

        private void UpdateStarbaseCmdTemplateDesign() {
            StarbaseCmdModuleStat cmdModuleStat;
            if (TryGetCurrentStarbaseCmdModuleStat(out cmdModuleStat)) {
                FtlDampenerStat ftlDampStat = GetCurrentFtlDampenerStat();
                var reqdMrSensorStat = GetCurrentMRCmdSensorStat();
                StarbaseCmdDesign cmdDesign = new StarbaseCmdDesign(_player, ftlDampStat, cmdModuleStat, reqdMrSensorStat) {
                    RootDesignName = TempGameValues.StarbaseCmdTemplateRootDesignName,
                    Status = AUnitMemberDesign.SourceAndStatus.SystemCreation_Template
                };

                if (IsStarbaseCmdTemplateDesignPresent()) {  // if CmdModuleStat JUST enabled, there won't yet be a Template design
                    StarbaseCmdDesign existingTemplateDesign = GetCurrentStarbaseTemplateDesign();
                    int newDesignLevel = existingTemplateDesign.DesignLevel + Constants.One;
                    cmdDesign.IncrementDesignLevelAndName(newDesignLevel);
                }

                Add(cmdDesign);
            }
        }

        private void UpdateFacilityTemplateDesigns(IEnumerable<EquipmentCategory> updatedEqCats) {
            D.AssertNotEqual(Constants.Zero, updatedEqCats.Count());

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

            IEnumerable<ShipHullCategory> hullCatsToUpdate;
            if (updatedEqCats.Contains(EquipmentCategory.SRSensor) || updatedEqCats.Contains(EquipmentCategory.StlPropulsion) || updatedEqCats.Contains(EquipmentCategory.FtlPropulsion)) {
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

            _shipDesignLookupByName.Add(designName, design);
            _shipDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FacilityDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
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

            _facilityDesignLookupByName.Add(designName, design);
            _facilityDesignsLookupByHull[design.HullCategory].Add(design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(StarbaseCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _starbaseCmdDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                    && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else {
                StarbaseCmdDesign existingTemplateDesign = _starbaseCmdDesignLookupByName.Values.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _starbaseCmdDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
            }
            _starbaseCmdDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(FleetCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }
            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _fleetCmdDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else {
                FleetCmdDesign existingTemplateDesign = _fleetCmdDesignLookupByName.Values.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _fleetCmdDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
            }
            _fleetCmdDesignLookupByName.Add(designName, design);
            //D.Log("{0} added {1}.", DebugName, design.DebugName);
        }

        public void Add(SettlementCmdDesign design) {
            D.AssertEqual(_player, design.Player);
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            string designName = design.DesignName;
            bool isAdded = _designNamesInUseLookup.Add(designName);
            if (!isAdded) {
                D.Warn("{0} was not able to add DesignName {1} as it is already present.", DebugName, designName);
                return;
            }

            if (design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current) {
                var designsThatShouldBeObsolete = _settlementCmdDesignLookupByName.Values.Where(d => d.RootDesignName == design.RootDesignName
                && d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
                if (designsThatShouldBeObsolete.Any()) {
                    D.Warn("{0} found {1} designs that should be obsolete when adding {2}, so obsoleting them.", DebugName, designsThatShouldBeObsolete.Count(), design.DebugName);
                    designsThatShouldBeObsolete.ForAll(d => ObsoleteDesign(d));
                }
            }
            else {
                SettlementCmdDesign existingTemplateDesign = _settlementCmdDesignLookupByName.Values.SingleOrDefault(d => d.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
                if (existingTemplateDesign != null) {
                    D.AssertEqual(existingTemplateDesign.RootDesignName, design.RootDesignName);
                    D.Log("{0} is replacing Template {1} with {2}.", DebugName, existingTemplateDesign.DebugName, design.DebugName);
                    _settlementCmdDesignLookupByName.Remove(existingTemplateDesign.DesignName);
                }
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
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(ShipDesign design) {
            if (!_shipDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _shipDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteFacilityDesign(string designName) {
            FacilityDesign design;
            if (!_facilityDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(FacilityDesign design) {
            if (!_facilityDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteStarbaseCmdDesign(string designName) {
            StarbaseCmdDesign design;
            if (!_starbaseCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdDesign).Name, designName, _starbaseCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(StarbaseCmdDesign design) {
            if (!_starbaseCmdDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _starbaseCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteSettlementCmdDesign(string designName) {
            SettlementCmdDesign design;
            if (!_settlementCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdDesign).Name, designName, _settlementCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(SettlementCmdDesign design) {
            if (!_settlementCmdDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _settlementCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteFleetCmdDesign(string designName) {
            FleetCmdDesign design;
            if (!_fleetCmdDesignLookupByName.TryGetValue(designName, out design)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FleetCmdDesign).Name, designName, _fleetCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        public void ObsoleteDesign(FleetCmdDesign design) {
            if (!_fleetCmdDesignLookupByName.Values.Contains(design)) {
                D.Error("{0}: {1} not present. DesignNames: {2}.", DebugName, design.DebugName, _fleetCmdDesignLookupByName.Keys.Concatenate());
            }
            D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete, design.Status);
            design.Status = AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete;
        }

        #endregion

        #region Design Presence

        public bool IsDesignNameInUse(string designName) {
            return _designNamesInUseLookup.Contains(designName);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Templates when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(ShipDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _shipDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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

        public bool IsUpgradeDesignPresent(ShipDesign designToUpgrade) {
            ShipDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Templates when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(FacilityDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _facilityDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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

        public bool IsUpgradeDesignPresent(FacilityDesign designToUpgrade) {
            FacilityDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Templates when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(FleetCmdDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _fleetCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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
            var templateDesign = _fleetCmdDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        public bool IsFleetCmdDefaultDesignPresent() {
            var templateDesign = _fleetCmdDesignLookupByName.Values.SingleOrDefault(design => design.RootDesignName == TempGameValues.FleetCmdDefaultRootDesignName);
            return templateDesign != null;
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(FleetCmdDesign designToUpgrade) {
            FleetCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Templates when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(StarbaseCmdDesign design, out string designName) {
            var designsPresent = _starbaseCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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
            var templateDesign = _starbaseCmdDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        ////[Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(StarbaseCmdDesign designToUpgrade) {
            StarbaseCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        /// <summary>
        /// Determines whether the content of the provided design is already present in current or obsolete designs.
        /// Does not consider SystemCreation_Templates when comparing designs.
        /// <remarks>Design content comparison does not pay attention to the design name or the status of the design.</remarks>
        /// </summary>
        /// <param name="design">The design.</param>
        /// <param name="designName">Name of the design that is present, if any.</param>
        /// <returns></returns>
        public bool IsDesignPresent(SettlementCmdDesign design, out string designName) {
            D.AssertEqual(_player, design.Player);
            var designsPresent = _settlementCmdDesignLookupByName.Values.Where(des => des.Status != AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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
            var templateDesign = _settlementCmdDesignLookupByName.Values.SingleOrDefault(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            return templateDesign != null;
        }

        ////[Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool IsUpgradeDesignPresent(SettlementCmdDesign designToUpgrade) {
            SettlementCmdDesign unusedUpgradeDesign;
            return TryGetUpgradeDesign(designToUpgrade, out unusedUpgradeDesign);
        }

        #endregion

        #region Get Design

        public IEnumerable<ShipDesign> GetAllShipDesigns(bool includeObsolete = false) {
            if (includeObsolete) {
                return _shipDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            return _shipDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        }

        public ShipDesign GetShipDesign(string designName) {
            if (!_shipDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(ShipDesign).Name, designName, _shipDesignLookupByName.Keys.Concatenate());
            }
            return _shipDesignLookupByName[designName];
        }

        public ShipDesign GetCurrentShipTemplateDesign(ShipHullCategory hullCat) {
            return _shipDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
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
                return _facilityDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            return _facilityDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        }

        public FacilityDesign GetFacilityDesign(string designName) {
            if (!_facilityDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(FacilityDesign).Name, designName, _facilityDesignLookupByName.Keys.Concatenate());
            }
            return _facilityDesignLookupByName[designName];
        }

        public FacilityDesign GetCurrentFacilityTemplateDesign(FacilityHullCategory hullCat) {
            return _facilityDesignsLookupByHull[hullCat].Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
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

            var candidateDesigns = hullDesigns.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
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
                return _starbaseCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            return _starbaseCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        }

        public StarbaseCmdDesign GetStarbaseCmdDesign(string designName) {
            if (!_starbaseCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(StarbaseCmdDesign).Name, designName, _starbaseCmdDesignLookupByName.Keys.Concatenate());
            }
            return _starbaseCmdDesignLookupByName[designName];
        }

        public StarbaseCmdDesign GetCurrentStarbaseTemplateDesign() {
            return _starbaseCmdDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        public bool TryGetDesign(string designName, out StarbaseCmdDesign design) {
            return _starbaseCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        ////[Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(StarbaseCmdDesign designToUpgrade, out StarbaseCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _starbaseCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
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
                return _fleetCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            return _fleetCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        }

        public FleetCmdDesign GetFleetCmdDesign(string designName) {
            if (!_fleetCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", GetType().Name, typeof(FleetCmdDesign).Name, designName, _fleetCmdDesignLookupByName.Keys.Concatenate());
            }
            return _fleetCmdDesignLookupByName[designName];
        }

        public FleetCmdDesign GetCurrentFleetTemplateDesign() {
            return _fleetCmdDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        public FleetCmdDesign GetCurrentDefaultFleetCmdDesign() {
            return _fleetCmdDesignLookupByName.Values.Single(design => design.RootDesignName == TempGameValues.FleetCmdDefaultRootDesignName);

        }

        public bool TryGetDesign(string designName, out FleetCmdDesign design) {
            return _fleetCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        [Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(FleetCmdDesign designToUpgrade, out FleetCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _fleetCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
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
                return _settlementCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
                || des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Obsolete);
            }
            return _settlementCmdDesignLookupByName.Values.Where(des => des.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        }

        ////[Obsolete]
        public SettlementCmdDesign GetSettlementCmdDesign(string designName) {
            if (!_settlementCmdDesignLookupByName.ContainsKey(designName)) {
                D.Error("{0}: {1} {2} not present. DesignNames: {3}.", DebugName, typeof(SettlementCmdDesign).Name, designName, _settlementCmdDesignLookupByName.Keys.Concatenate());
            }
            return _settlementCmdDesignLookupByName[designName];
        }

        public SettlementCmdDesign GetCurrentSettlementTemplateDesign() {
            return _settlementCmdDesignLookupByName.Values.Single(design => design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
        }

        public bool TryGetDesign(string designName, out SettlementCmdDesign design) {
            return _settlementCmdDesignLookupByName.TryGetValue(designName, out design);
        }

        ////[Obsolete("Upgrading CmdDesigns not yet supported")]
        public bool TryGetUpgradeDesign(SettlementCmdDesign designToUpgrade, out SettlementCmdDesign upgradeDesign) {
            D.AssertEqual(_player, designToUpgrade.Player);
            var candidateDesigns = _settlementCmdDesignLookupByName.Values.Where(d => d.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current
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

