﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameUnitConfigurator.cs
// Configures existing UnitCreators by generating and applying a UnitCreatorConfiguration 
// including owner, derived from the UnitCreator's EditorSettings.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Configures existing UnitCreators by generating and applying a UnitCreatorConfiguration 
/// including owner, derived from the UnitCreator's EditorSettings.
/// </summary>
public class NewGameUnitConfigurator {

    private const string UnitNameFormat = "{0} {1}";

    private const string DesignNameFormat = "{0}_{1}";

    private static FacilityHullCategory[] FacilityCategories {
        get {
            return new FacilityHullCategory[] { FacilityHullCategory.CentralHub, FacilityHullCategory.Factory, FacilityHullCategory.Defense, FacilityHullCategory.Economic,
                         FacilityHullCategory.Laboratory, FacilityHullCategory.Barracks, FacilityHullCategory.ColonyHab };
        }
    }

    private static FacilityHullCategory[] HQFacilityCategories { get { return new FacilityHullCategory[] { FacilityHullCategory.CentralHub }; } }


    private static ShipHullCategory[] ShipCategories {
        get {
            return new ShipHullCategory[] { ShipHullCategory.Frigate, ShipHullCategory.Destroyer, ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnought,
        ShipHullCategory.Colonizer, ShipHullCategory.Investigator, ShipHullCategory.Troop, ShipHullCategory.Support};
        }
    }

    private static ShipHullCategory[] HQShipCategories {
        get { return new ShipHullCategory[] { ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnought }; }
    }

    /// <summary>
    /// Static counter used to provide a unique name for each element.
    /// </summary>
    private static int _elementInstanceIDCounter = Constants.One;

    /// <summary>
    /// Static counter used to provide a unique name for each design name.
    /// </summary>
    private static int _designNameCounter = Constants.One;

    private static int _unitNameCounter = Constants.One;

    /// <summary>
    /// Gets a unique design name for an element.
    /// </summary>
    /// <param name="hullCategoryName">The hull category name.</param>
    /// <returns></returns>
    private static string GetUniqueElementDesignName(string hullCategoryName) {
        var designName = DesignNameFormat.Inject(hullCategoryName, _designNameCounter);
        _designNameCounter++;
        return designName;
    }

    private static string GetUniqueCmdDesignName() {
        var designName = DesignNameFormat.Inject("Cmd", _designNameCounter);
        _designNameCounter++;
        return designName;
    }

    private static string GetUniqueUnitName(string baseName) {
        var unitName = UnitNameFormat.Inject(baseName, _unitNameCounter);
        _unitNameCounter++;
        return unitName;
    }

    private IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;
    private IList<ActiveCountermeasureStat> _availableActiveCountermeasureStats;
    private IList<ShieldGeneratorStat> _availableShieldGeneratorStats;
    private IList<SensorStat> _availableSensorStats;
    private IList<AWeaponStat> _availableLosWeaponStats;
    private IList<AWeaponStat> _availableMissileWeaponStats;

    private IList<ADebugUnitCreator> _existingEditorCreators;
    private IDictionary<DiplomaticRelationship, IList<Player>> _aiPlayerInitialUserRelationsLookup;
    private GameManager _gameMgr;
    private UnitFactory _factory;

    public NewGameUnitConfigurator(IDictionary<DiplomaticRelationship, IList<Player>> aiPlayerInitialUserRelationsLookup, IList<ADebugUnitCreator> editorCreators) {
        _gameMgr = GameManager.Instance;
        _factory = UnitFactory.Instance;
        _aiPlayerInitialUserRelationsLookup = aiPlayerInitialUserRelationsLookup;
        _existingEditorCreators = editorCreators;
        CreateEquipmentStats();
    }

    #region Configure Existing Creators

    /// <summary>
    /// Configures the existing editor creators, returning the list of players that were not assigned a creator.
    /// </summary>
    /// <returns></returns>
    public IList<Player> ConfigureExistingEditorCreators() {
        IList<Player> creatorOwners = new List<Player>();
        foreach (var creator in _existingEditorCreators) {
            Player owner;
            if (creator is DebugStarbaseCreator) {
                owner = AssignConfigurationToExistingCreator(creator as DebugStarbaseCreator);
            }
            else if (creator is DebugSettlementCreator) {
                owner = AssignConfigurationToExistingCreator(creator as DebugSettlementCreator);
            }
            else {
                owner = AssignConfigurationToExistingCreator(creator as DebugFleetCreator);
            }
            if (owner != null) {
                creatorOwners.Add(owner);
            }
        }
        return _gameMgr.AllPlayers.Except(creatorOwners).ToList();
    }

    /// <summary>
    /// Assigns a configuration including an owner to the provided unit creator.
    /// Returns the Player that is now the new owner of the creator.
    /// </summary>
    /// <param name="unitCreator">The unit creator.</param>
    /// <returns></returns>
    private Player AssignConfigurationToExistingCreator(DebugStarbaseCreator unitCreator) {
        var editorSettings = unitCreator.EditorSettings as BaseCreatorEditorSettings;

        string unitName = editorSettings.UnitName;
        Player owner;
        if (!TryDetermineOwner(editorSettings, out owner)) {
            return null;
        }
        GameDate deployDate = editorSettings.DateToDeploy;
        string cmdDesignName = MakeAndRecordStarbaseCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateFacilityHullStats(editorSettings, isSettlement: false);
        IList<string> elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement);
        bool enableTrackingLabel = editorSettings.EnableTrackingLabel;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames,
            enableTrackingLabel);
        unitCreator.Configuration = config;
        return owner;
    }

    private Player AssignConfigurationToExistingCreator(DebugSettlementCreator unitCreator) {
        var editorSettings = unitCreator.EditorSettings as BaseCreatorEditorSettings;

        string unitName = editorSettings.UnitName;
        Player owner;
        if (!TryDetermineOwner(editorSettings, out owner)) {
            return null;
        }
        GameDate deployDate = editorSettings.DateToDeploy;
        string cmdDesignName = MakeAndRecordSettlementCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateFacilityHullStats(editorSettings, isSettlement: true);
        IList<string> elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement);
        bool enableTrackingLabel = editorSettings.EnableTrackingLabel;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames,
            enableTrackingLabel);
        unitCreator.Configuration = config;
        return owner;
    }

    private Player AssignConfigurationToExistingCreator(DebugFleetCreator unitCreator) {
        var editorSettings = unitCreator.EditorSettings as FleetCreatorEditorSettings;

        string unitName = editorSettings.UnitName;
        Player owner;
        if (!TryDetermineOwner(editorSettings, out owner)) {
            return null;
        }
        GameDate deployDate = editorSettings.DateToDeploy;
        string cmdDesignName = MakeAndRecordFleetCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateShipHullStats(editorSettings);
        ShipCombatStance stance = SelectCombatStance(editorSettings.StanceExclusions);
        IList<string> elementDesignNames = MakeAndRecordShipDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement, stance);
        bool enableTrackingLabel = editorSettings.EnableTrackingLabel;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames,
             enableTrackingLabel);
        unitCreator.Configuration = config;
        return owner;
    }

    /// <summary>
    /// Returns true if owner is valid and should be assigned to the creator, false otherwise.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    private bool TryDetermineOwner(AUnitCreatorEditorSettings settings, out Player owner) {
        if (settings.IsOwnerUser) {
            owner = _gameMgr.UserPlayer;
            return true;
        }
        else {
            var desiredUserRelationship = settings.DesiredRelationshipWithUser.Convert();
            IList<Player> aiOwnerCandidates;
            if (_aiPlayerInitialUserRelationsLookup.TryGetValue(desiredUserRelationship, out aiOwnerCandidates)) {
                owner = RandomExtended.Choice(aiOwnerCandidates);
                return true;
            }
        }
        owner = null;
        return false;
    }

    #endregion

    #region Generate Random AutoCreators

    public void GenerateRandomAutoFleetCreator(Player owner) {
        string unitName = GetUniqueUnitName("AutoFleet");
        GameDate deployDate = GameTime.GameStartDate;

        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Enums<Formation>.GetRandom(excludeDefault: true);
        string cmdDesignName = MakeAndRecordFleetCmdDesign(owner, unitName, cmsPerCmd, formation);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = CreateShipHullStats(elementQty);
        var turretLoadout = DebugWeaponLoadout.Random;
        var missileLoadout = DebugWeaponLoadout.Random;
        int elementPassiveCMs = RandomExtended.Range(0, 3);
        int elementActiveCMs = RandomExtended.Range(0, 3);
        int elementSensors = RandomExtended.Range(1, 5);
        int elementShieldGens = RandomExtended.Range(0, 3);
        var combatStance = Enums<ShipCombatStance>.GetRandom(excludeDefault: true);
        var elementDesignNames = MakeAndRecordShipDesigns(owner, hullStats, turretLoadout, missileLoadout, elementPassiveCMs, elementActiveCMs, elementSensors, elementShieldGens, combatStance);

        bool isTrackingLabelEnabled = DebugControls.Instance.AreAutoUnitCreatorTrackingLabelsEnabled;

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames, isTrackingLabelEnabled);

        Sector randomSector = SectorGrid.Instance.RandomSector;
        Vector3 randomPointInSector = randomSector.GetClearRandomPointInsideSector();
        UnitFactory.Instance.MakeFleetCreatorInstance(randomPointInSector, config);
    }

    public void GenerateRandomAutoStarbaseCreator(Player owner) {
        string unitName = GetUniqueUnitName("AutoStarbase");
        GameDate deployDate = GameTime.GameStartDate;

        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Enums<Formation>.GetRandomExcept(Formation.Wedge, default(Formation));
        string cmdDesignName = MakeAndRecordStarbaseCmdDesign(owner, unitName, cmsPerCmd, formation);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = CreateFacilityHullStats(elementQty, isSettlement: false);
        var turretLoadout = DebugWeaponLoadout.Random;
        var missileLoadout = DebugWeaponLoadout.Random;
        int elementPassiveCMs = RandomExtended.Range(0, 3);
        int elementActiveCMs = RandomExtended.Range(0, 3);
        int elementSensors = RandomExtended.Range(1, 5);
        int elementShieldGens = RandomExtended.Range(0, 3);
        var elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, turretLoadout, missileLoadout, elementPassiveCMs, elementActiveCMs, elementSensors, elementShieldGens);

        bool isTrackingLabelEnabled = DebugControls.Instance.AreAutoUnitCreatorTrackingLabelsEnabled;

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames, isTrackingLabelEnabled);

        Sector randomSector = SectorGrid.Instance.RandomSector;
        Vector3 randomPointInSector = randomSector.GetClearRandomPointInsideSector();
        UnitFactory.Instance.MakeStarbaseCreatorInstance(randomPointInSector, config);
    }

    public void GenerateRandomAutoSettlementCreator(Player owner) {
        string unitName = GetUniqueUnitName("AutoSettlement");
        GameDate deployDate = GameTime.GameStartDate;

        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Enums<Formation>.GetRandomExcept(Formation.Wedge, default(Formation));
        string cmdDesignName = MakeAndRecordSettlementCmdDesign(owner, unitName, cmsPerCmd, formation);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = CreateFacilityHullStats(elementQty, isSettlement: true);
        var turretLoadout = DebugWeaponLoadout.Random;
        var missileLoadout = DebugWeaponLoadout.Random;
        int elementPassiveCMs = RandomExtended.Range(0, 3);
        int elementActiveCMs = RandomExtended.Range(0, 3);
        int elementSensors = RandomExtended.Range(1, 5);
        int elementShieldGens = RandomExtended.Range(0, 3);
        var elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, turretLoadout, missileLoadout, elementPassiveCMs, elementActiveCMs, elementSensors, elementShieldGens);

        bool isTrackingLabelEnabled = DebugControls.Instance.AreAutoUnitCreatorTrackingLabelsEnabled;

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames, isTrackingLabelEnabled);

        UnitFactory.Instance.MakeSettlementCreatorInstance(config);
    }

    #endregion

    /// <summary>
    /// Gets the AiPlayers that either currently have the specified user relationship or will have it when they meet.
    /// </summary>
    /// <param name="userRelationship">The specified user relationship.</param>
    /// <returns></returns>
    [Obsolete]
    public IEnumerable<Player> GetAiPlayersWithCurrentOrInitialUserRelationsOf(DiplomaticRelationship userRelationship) {
        Player userPlayer = _gameMgr.UserPlayer;
        var aiPlayersWithSpecifiedCurrentUserRelations = _gameMgr.AIPlayers.Where(aiPlayer => aiPlayer.IsRelationshipWith(userPlayer, userRelationship));
        IList<Player> aiPlayersWithSpecifiedInitialUserRelations;
        if (_aiPlayerInitialUserRelationsLookup.TryGetValue(userRelationship, out aiPlayersWithSpecifiedInitialUserRelations)) {
            return aiPlayersWithSpecifiedInitialUserRelations.Union(aiPlayersWithSpecifiedCurrentUserRelations);
        }
        return aiPlayersWithSpecifiedCurrentUserRelations;
    }

    /// <summary>
    /// Gets the AIPlayers that the User has not yet met, that have been assigned the initialUserRelationship to begin with when they do meet.
    /// </summary>
    /// <param name="initialUserRelationship">The initial user relationship.</param>
    /// <returns></returns>
    public IEnumerable<Player> GetUnmetAiPlayersWithInitialUserRelationsOf(DiplomaticRelationship initialUserRelationship) {
        Player userPlayer = _gameMgr.UserPlayer;
        IList<Player> aiPlayersWithSpecifiedInitialUserRelations;
        if (_aiPlayerInitialUserRelationsLookup.TryGetValue(initialUserRelationship, out aiPlayersWithSpecifiedInitialUserRelations)) {
            return aiPlayersWithSpecifiedInitialUserRelations.Except(userPlayer.OtherKnownPlayers);
        }
        return Enumerable.Empty<Player>();
    }

    #region Create Equipment Stats

    private void CreateEquipmentStats() {
        _availableLosWeaponStats = __CreateAvailableLosWeaponStats(TempGameValues.MaxLosWeaponsForAnyElement);
        _availableMissileWeaponStats = __CreateAvailableMissileWeaponStats(TempGameValues.MaxMissileWeaponsForAnyElement);
        _availablePassiveCountermeasureStats = __CreateAvailablePassiveCountermeasureStats(9);
        _availableActiveCountermeasureStats = __CreateAvailableActiveCountermeasureStats(9);
        _availableSensorStats = __CreateAvailableSensorStats(9);
        _availableShieldGeneratorStats = __CreateAvailableShieldGeneratorStats(9);
    }

    private IList<AWeaponStat> __CreateAvailableMissileWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            WDVCategory deliveryVehicleCategory = WDVCategory.Missile;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);    // 0.04 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(10F, 12F);
            string name = "Torpedo Launcher";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(3F, 8F);
            float ordMaxSpeed = UnityEngine.Random.Range(4F, 6F);
            float ordMass = 5F;
            float ordDrag = 0.01F;
            float ordTurnRate = 700F;   // degrees per hour
            float ordCourseUpdateFreq = 0.5F; // course updates per hour
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);

            var weapStat = new MissileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass, ordDrag,
                ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<AWeaponStat> __CreateAvailableLosWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            AWeaponStat weapStat;
            RangeCategory rangeCat;
            float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.04 - 3 degrees
            float reloadPeriod;
            string name;
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(3F, 8F);
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVCategory deliveryVehicleCategory = Enums<WDVCategory>.GetRandomExcept(default(WDVCategory), WDVCategory.Missile);
            //WDVCategory deliveryVehicleCategory = WDVCategory.Beam;
            //WDVCategory deliveryVehicleCategory = WDVCategory.Projectile;
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);

            switch (deliveryVehicleCategory) {
                case WDVCategory.Beam:
                    rangeCat = RangeCategory.Short;
                    reloadPeriod = UnityEngine.Random.Range(3F, 5F);
                    name = "Phaser Projector";
                    float duration = UnityEngine.Random.Range(1F, 2F);
                    weapStat = new BeamWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, rangeCat,
                        deliveryVehicleStrength, reloadPeriod, damagePotential, duration, maxLaunchInaccuracy);
                    break;
                case WDVCategory.Projectile:
                    rangeCat = RangeCategory.Medium;
                    reloadPeriod = UnityEngine.Random.Range(2F, 4F);
                    name = "KineticKill Projector";
                    float ordMaxSpeed = UnityEngine.Random.Range(6F, 8F);
                    float ordMass = 1F;
                    float ordDrag = 0.02F;
                    weapStat = new ProjectileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, rangeCat,
                        deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass, ordDrag, maxLaunchInaccuracy);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(deliveryVehicleCategory));
            }
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<PassiveCountermeasureStat> __CreateAvailablePassiveCountermeasureStats(int quantity) {
        IList<PassiveCountermeasureStat> statsList = new List<PassiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            DamageStrength damageMitigation;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
            float damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
            switch (damageMitigationCategory) {
                case DamageCategory.Thermal:
                    name = "ThermalArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Atomic:
                    name = "AtomicArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Kinetic:
                    name = "KineticArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.None:
                    name = "GeneralArmor";
                    damageMitigation = new DamageStrength(2F, 2F, 2F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
            }
            var countermeasureStat = new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private IList<ActiveCountermeasureStat> __CreateAvailableActiveCountermeasureStats(int quantity) {
        IList<ActiveCountermeasureStat> statsList = new List<ActiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            RangeCategory rangeCat = Enums<RangeCategory>.GetRandom(excludeDefault: true);
            WDVStrength[] interceptStrengths;
            float interceptAccuracy;
            float reloadPeriod;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageMitigationValue = UnityEngine.Random.Range(1F, 2F);
            switch (rangeCat) {
                case RangeCategory.Short:
                    name = "CIWS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Projectile, 0.2F),
                        new WDVStrength(WDVCategory.Missile, 0.5F)
                    };
                    interceptAccuracy = 0.50F;
                    reloadPeriod = 0.1F;
                    break;
                case RangeCategory.Medium:
                    name = "AvengerADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 3.0F)
                    };
                    interceptAccuracy = 0.80F;
                    reloadPeriod = 2.0F;
                    break;
                case RangeCategory.Long:
                    name = "PatriotADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 1.0F)
                    };
                    interceptAccuracy = 0.70F;
                    reloadPeriod = 3.0F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
            var countermeasureStat = new ActiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, interceptStrengths, interceptAccuracy, reloadPeriod, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private IList<SensorStat> __CreateAvailableSensorStats(int quantity) {
        IList<SensorStat> statsList = new List<SensorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            RangeCategory rangeCat = Enums<RangeCategory>.GetRandom(excludeDefault: true);
            //RangeCategory rangeCat = RangeCategory.Long;
            switch (rangeCat) {
                case RangeCategory.Short:
                    name = "ProximityDetector";
                    break;
                case RangeCategory.Medium:
                    name = "PulseSensor";
                    break;
                case RangeCategory.Long:
                    name = "DeepScanArray";
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            var sensorStat = new SensorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat);
            statsList.Add(sensorStat);
        }
        return statsList;
    }

    private IList<ShieldGeneratorStat> __CreateAvailableShieldGeneratorStats(int quantity) {
        IList<ShieldGeneratorStat> statsList = new List<ShieldGeneratorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            RangeCategory rangeCat = RangeCategory.Short;
            string name = "Deflector Generator";
            float maxCharge = 20F;
            float trickleChargeRate = 1F;
            float reloadPeriod = 20F;
            DamageStrength damageMitigation = default(DamageStrength);  // none for now
            var generatorStat = new ShieldGeneratorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 1F, 0F, 0F,
                rangeCat, maxCharge, trickleChargeRate, reloadPeriod, damageMitigation);
            statsList.Add(generatorStat);
        }
        return statsList;
    }

    #endregion

    #region Hull Stats

    private IEnumerable<FacilityHullStat> CreateFacilityHullStats(BaseCreatorEditorSettings settings, bool isSettlement) {
        if (settings.IsCompositionPreset) {
            return CreateFacilityHullStats(settings.PresetElementHullCategories, isSettlement);
        }
        return CreateFacilityHullStats(settings.NonPresetElementQty, isSettlement);
    }


    private IEnumerable<FacilityHullStat> CreateFacilityHullStats(int elementCount, bool isSettlement) {
        var elementHullStats = new List<FacilityHullStat>(elementCount);
        for (int i = 0; i < elementCount; i++) {
            FacilityHullCategory category = (i == 0) ? RandomExtended.Choice(HQFacilityCategories) : RandomExtended.Choice(FacilityCategories);
            int elementInstanceID = _elementInstanceIDCounter;
            _elementInstanceIDCounter++;
            string uniqueElementName = category.GetValueName() + Constants.Underscore + elementInstanceID;
            elementHullStats.Add(CreateElementHullStat(category, uniqueElementName, isSettlement));
        }
        return elementHullStats;
    }

    private IEnumerable<FacilityHullStat> CreateFacilityHullStats(IList<FacilityHullCategory> hullCats, bool isSettlement) {
        var elementHullStats = new List<FacilityHullStat>(hullCats.Count);
        foreach (var hullCat in hullCats) {
            int elementInstanceID = _elementInstanceIDCounter;
            _elementInstanceIDCounter++;
            string uniqueElementName = hullCat.GetValueName() + Constants.Underscore + elementInstanceID;
            elementHullStats.Add(CreateElementHullStat(hullCat, uniqueElementName, isSettlement));
        }
        return elementHullStats;
    }

    private IEnumerable<ShipHullStat> CreateShipHullStats(FleetCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            return CreateShipHullStats(settings.PresetElementHullCategories);
        }
        return CreateShipHullStats(settings.NonPresetElementQty);
    }

    private IEnumerable<ShipHullStat> CreateShipHullStats(int elementCount) {
        var elementHullStats = new List<ShipHullStat>(elementCount);
        for (int i = 0; i < elementCount; i++) {
            ShipHullCategory category = (i == 0) ? RandomExtended.Choice(HQShipCategories) : RandomExtended.Choice(ShipCategories);
            int elementInstanceID = _elementInstanceIDCounter;
            _elementInstanceIDCounter++;
            string uniqueElementName = category.GetValueName() + Constants.Underscore + elementInstanceID;
            elementHullStats.Add(CreateElementHullStat(category, uniqueElementName));
        }
        return elementHullStats;
    }

    private IEnumerable<ShipHullStat> CreateShipHullStats(IList<ShipHullCategory> hullCats) {
        var elementHullStats = new List<ShipHullStat>(hullCats.Count);
        foreach (var hullCat in hullCats) {
            int elementInstanceID = _elementInstanceIDCounter;
            _elementInstanceIDCounter++;
            string uniqueElementName = hullCat.GetValueName() + Constants.Underscore + elementInstanceID;
            elementHullStats.Add(CreateElementHullStat(hullCat, uniqueElementName));
        }
        return elementHullStats;
    }

    private IEnumerable<FacilityHullStat> CreateShipHullStats(BaseCreatorEditorSettings settings, bool isSettlement) {
        if (settings.IsCompositionPreset) {
            return CreateFacilityHullStats(settings.PresetElementHullCategories, isSettlement);
        }
        return CreateFacilityHullStats(settings.NonPresetElementQty, isSettlement);
    }

    private ShipHullStat CreateElementHullStat(ShipHullCategory hullCat, string elementName) {
        float hullMass = hullCat.Mass();
        float drag = hullCat.Drag();
        float science = hullCat.Science();
        float culture = hullCat.Culture();
        float income = hullCat.Income();
        float expense = hullCat.Expense();
        Vector3 hullDimensions = hullCat.Dimensions();
        return new ShipHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, drag, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    private FacilityHullStat CreateElementHullStat(FacilityHullCategory hullCat, string elementName, bool isSettlement) {
        float science = hullCat.Science(isSettlement);
        float culture = hullCat.Culture(isSettlement);
        float income = hullCat.Income(isSettlement);
        float expense = hullCat.Expense(isSettlement);
        float hullMass = hullCat.Mass();
        Vector3 hullDimensions = hullCat.Dimensions();
        return new FacilityHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    #endregion

    #region Element Designs

    private IList<string> MakeAndRecordFacilityDesigns(Player owner, IEnumerable<FacilityHullStat> hullStats, DebugWeaponLoadout turretLoadout, DebugWeaponLoadout missileLoadout,
        int passiveCMsPerElement, int activeCMsPerElement, int sensorsPerElement, int shieldGensPerElement) {

        IList<string> designNames = new List<string>();
        foreach (var hullStat in hullStats) {
            FacilityHullCategory hullCategory = hullStat.HullCategory;
            int losWeaponsPerElement = GetWeaponQtyToInstall(turretLoadout, hullCategory.__MaxLOSWeapons());
            var weaponStats = _availableLosWeaponStats.Shuffle().Take(losWeaponsPerElement).ToList();
            int missileWeaponsPerElement = GetWeaponQtyToInstall(missileLoadout, hullCategory.__MaxMissileWeapons());
            weaponStats.AddRange(_availableMissileWeaponStats.Shuffle().Take(missileWeaponsPerElement));

            var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCMsPerElement);
            var activeCmStats = _availableActiveCountermeasureStats.Shuffle().Take(activeCMsPerElement);
            var sensorStats = _availableSensorStats.Shuffle().Take(sensorsPerElement);
            var shieldGenStats = _availableShieldGeneratorStats.Shuffle().Take(shieldGensPerElement);
            Priority hqPriority = hullCategory.__HQPriority();    // TEMP, IMPROVE

            string designName = GetUniqueElementDesignName(hullCategory.GetValueName());
            designNames.Add(designName);
            MakeAndRecordElementDesign(designName, owner, hullStat, weaponStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, hqPriority);
        }
        return designNames;
    }

    private IList<string> MakeAndRecordShipDesigns(Player owner, IEnumerable<ShipHullStat> hullStats, DebugWeaponLoadout turretLoadout, DebugWeaponLoadout missileLoadout,
        int passiveCMsPerElement, int activeCMsPerElement, int sensorsPerElement, int shieldGensPerElement, ShipCombatStance stance) {

        IList<string> designNames = new List<string>();
        foreach (var hullStat in hullStats) {
            ShipHullCategory hullCategory = hullStat.HullCategory;
            int losWeaponsPerElement = GetWeaponQtyToInstall(turretLoadout, hullCategory.__MaxLOSWeapons());
            var weaponStats = _availableLosWeaponStats.Shuffle().Take(losWeaponsPerElement).ToList();
            int missileWeaponsPerElement = GetWeaponQtyToInstall(missileLoadout, hullCategory.__MaxMissileWeapons());
            weaponStats.AddRange(_availableMissileWeaponStats.Shuffle().Take(missileWeaponsPerElement));

            var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCMsPerElement);
            var activeCmStats = _availableActiveCountermeasureStats.Shuffle().Take(activeCMsPerElement);
            var sensorStats = _availableSensorStats.Shuffle().Take(sensorsPerElement);
            var shieldGenStats = _availableShieldGeneratorStats.Shuffle().Take(shieldGensPerElement);
            Priority hqPriority = hullCategory.__HQPriority();    // TEMP, IMPROVE

            string designName = GetUniqueElementDesignName(hullCategory.GetValueName());
            designNames.Add(designName);
            MakeAndRecordElementDesign(designName, owner, hullStat, weaponStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, hqPriority, stance);
        }
        return designNames;
    }

    private void MakeAndRecordElementDesign(string designName, Player owner, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority) {
        FacilityHullCategory hullCategory = hullStat.HullCategory;
        var weaponDesigns = _factory.__MakeWeaponDesigns(hullCategory, weaponStats);
        var design = new FacilityDesign(owner, designName, hullStat, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, hqPriority);
        _gameMgr.PlayersDesigns.Add(design);
    }

    private void MakeAndRecordElementDesign(string designName, Player owner, ShipHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority, ShipCombatStance stance) {
        ShipHullCategory hullCategory = hullStat.HullCategory;
        var engineStat = MakeEnginesStat(hullCategory);
        var weaponDesigns = _factory.__MakeWeaponDesigns(hullCategory, weaponStats);
        var design = new ShipDesign(owner, designName, hullStat, engineStat, stance, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, hqPriority);
        _gameMgr.PlayersDesigns.Add(design);
    }

    #endregion

    #region Command Designs

    private string MakeAndRecordFleetCmdDesign(Player owner, string unitName, int cmsPerCmd, Formation formation) {
        string designName = GetUniqueCmdDesignName();
        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(cmsPerCmd);
        UnitCmdStat cmdStat = MakeFleetCmdStat(unitName, formation);
        FleetCmdDesign design = new FleetCmdDesign(owner, designName, passiveCmStats, cmdStat);
        _gameMgr.PlayersDesigns.Add(design);
        return designName;
    }

    private string MakeAndRecordStarbaseCmdDesign(Player owner, string unitName, int cmsPerCmd, Formation formation) {
        string designName = GetUniqueCmdDesignName();
        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(cmsPerCmd);
        UnitCmdStat cmdStat = MakeStarbaseCmdStat(unitName, formation);
        StarbaseCmdDesign design = new StarbaseCmdDesign(owner, designName, passiveCmStats, cmdStat);
        _gameMgr.PlayersDesigns.Add(design);
        return designName;
    }

    private string MakeAndRecordSettlementCmdDesign(Player owner, string unitName, int cmsPerCmd, Formation formation) {
        string designName = GetUniqueCmdDesignName();
        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(cmsPerCmd);
        SettlementCmdStat cmdStat = MakeSettlementCmdStat(unitName, formation);
        SettlementCmdDesign design = new SettlementCmdDesign(owner, designName, passiveCmStats, cmdStat);
        _gameMgr.PlayersDesigns.Add(design);
        return designName;
    }

    private UnitCmdStat MakeFleetCmdStat(string unitName, Formation formation) {
        float maxHitPts = 10F;
        float maxCmdEffect = 1.0F;
        return new UnitCmdStat(unitName, maxHitPts, maxCmdEffect, formation);
    }

    private UnitCmdStat MakeStarbaseCmdStat(string unitName, Formation formation) {
        float maxHitPts = 10F;
        float maxCmdEffect = 1.0F;
        return new UnitCmdStat(unitName, maxHitPts, maxCmdEffect, formation);
    }

    private SettlementCmdStat MakeSettlementCmdStat(string unitName, Formation formation) {
        float maxHitPts = 10F;
        float maxCmdEffect = 1.0F;
        int population = 100;
        return new SettlementCmdStat(unitName, maxHitPts, maxCmdEffect, formation, population);
    }

    #endregion

    private EnginesStat MakeEnginesStat(ShipHullCategory hullCategory) {
        float maxTurnRate = UnityEngine.Random.Range(90F, 270F);
        float singleEngineSize = 10F;
        float singleEngineMass = GetEngineMass(hullCategory);
        float singleEngineExpense = 5F;

        float fullStlPropulsionPower = GetFullStlPropulsionPower(hullCategory);   // FullFtlOpenSpaceSpeed ~ 30-40 units/hour, FullStlSystemSpeed ~ 1.2 - 1.6 units/hour
        return new EnginesStat("EngineName", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", fullStlPropulsionPower, maxTurnRate, singleEngineSize, singleEngineMass, singleEngineExpense, TempGameValues.__StlToFtlPropulsionPowerFactor, engineQty: 1);
    }

    private float GetEngineMass(ShipHullCategory hullCat) {
        return hullCat.Mass();
    }

    /// <summary>
    /// Gets the power generated by the STL engines when operating at Full capability.
    /// </summary>
    /// <param name="hull">The ship hull.</param>
    /// <returns></returns>
    private float GetFullStlPropulsionPower(ShipHullCategory hullCat) {
        float fastestFullFtlSpeedTgt = TempGameValues.__TargetFtlOpenSpaceFullSpeed; // 40F
        float slowestFullFtlSpeedTgt = fastestFullFtlSpeedTgt * 0.75F;   // this way, the slowest Speed.OneThird speed >= Speed.Slow
        float fullFtlSpeedTgt = UnityEngine.Random.Range(slowestFullFtlSpeedTgt, fastestFullFtlSpeedTgt);
        float hullMass = hullCat.Mass(); // most but not all of the mass of the ship
        float hullOpenSpaceDrag = hullCat.Drag();

        float reqdFullFtlPower = GameUtility.CalculateReqdPropulsionPower(fullFtlSpeedTgt, hullMass, hullOpenSpaceDrag);
        return reqdFullFtlPower / TempGameValues.__StlToFtlPropulsionPowerFactor;
    }

    private int GetWeaponQtyToInstall(DebugWeaponLoadout loadout, int maxAllowed) {
        switch (loadout) {
            case DebugWeaponLoadout.None:
                return Constants.Zero;
            case DebugWeaponLoadout.One:
                return maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
            case DebugWeaponLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxAllowed);
            case DebugWeaponLoadout.Max:
                return maxAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private ShipCombatStance SelectCombatStance(DebugShipCombatStanceExclusions stanceExclusions) {
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptBalanced) {
            return ShipCombatStance.Balanced;
        }
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptPointBlank) {
            return ShipCombatStance.PointBlank;
        }
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptStandoff) {
            return ShipCombatStance.Standoff;
        }
        else {
            IList<ShipCombatStance> excludedCombatStances = new List<ShipCombatStance>() { default(ShipCombatStance) };
            if (stanceExclusions == DebugShipCombatStanceExclusions.Disengage) {
                excludedCombatStances.Add(ShipCombatStance.Disengage);
            }
            else if (stanceExclusions == DebugShipCombatStanceExclusions.DefensiveAndDisengage) {
                excludedCombatStances.Add(ShipCombatStance.Disengage);
                excludedCombatStances.Add(ShipCombatStance.Defensive);
            }
            return Enums<ShipCombatStance>.GetRandomExcept(excludedCombatStances.ToArray());
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

