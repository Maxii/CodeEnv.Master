// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameUnitConfigurator.cs
// Provides methods to generate and/or configure UnitCreators.
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
/// Provides methods to generate and/or configure UnitCreators. Existing DebugUnitCreators that are already present in the editor
/// just need to be configured by accessing the EditorSettings. AutoUnitCreators are both generated and then randomly configured.
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

    private string Name { get { return GetType().Name; } }

    private IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;
    private IList<ActiveCountermeasureStat> _availableActiveCountermeasureStats;
    private IList<ShieldGeneratorStat> _availableShieldGeneratorStats;
    private IList<SensorStat> _availableSensorStats;
    private IList<AWeaponStat> _availableLosWeaponStats;
    private IList<AWeaponStat> _availableMissileWeaponStats;

    private GameManager _gameMgr;
    private UnitFactory _factory;

    public NewGameUnitConfigurator() {
        InitializeValuesAndReferences();
        CreateEquipmentStats();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _factory = UnitFactory.Instance;
    }

    #region Configure Existing Creators

    /// <summary>
    /// Assigns a configuration to the provided existing DebugStarbaseCreator, using the DeployDate provided.
    /// <remarks>The DebugCreator's EditorSettings specifying the DeployDate will be ignored.</remarks>
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    public void AssignConfigurationToExistingCreator(DebugStarbaseCreator creator, Player owner, Vector3 location, GameDate deployDate) {
        var editorSettings = creator.EditorSettings as BaseCreatorEditorSettings;

        ValidateOwner(owner, editorSettings);

        string unitName = editorSettings.UnitName;
        string cmdDesignName = MakeAndRecordStarbaseCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateFacilityHullStats(editorSettings, isSettlement: false);
        IList<string> elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement);
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        creator.transform.position = location;
        //D.Log("{0} has placed a {1} for {2}.", Name, typeof(DebugStarbaseCreator).Name, owner);
    }

    /// <summary>
    /// Assigns a configuration to the provided existing DebugStarbaseCreator, using the DeployDate specified by the DebugCreator.
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    public void AssignConfigurationToExistingCreator(DebugStarbaseCreator creator, Player owner, Vector3 location) {
        GameDate deployDate = creator.EditorSettings.DateToDeploy;
        AssignConfigurationToExistingCreator(creator, owner, location, deployDate);
    }

    /// <summary>
    /// Assigns a configuration to the provided existing DebugSettlementCreator, using the DeployDate provided.
    /// <remarks>The DebugCreator's EditorSettings specifying the DeployDate will be ignored.</remarks>
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system to assign the settlement creator to.</param>
    /// <param name="deployDate">The deploy date.</param>
    public void AssignConfigurationToExistingCreator(DebugSettlementCreator creator, Player owner, SystemItem system, GameDate deployDate) {
        var editorSettings = creator.EditorSettings as BaseCreatorEditorSettings;

        ValidateOwner(owner, editorSettings);

        string unitName = editorSettings.UnitName;
        string cmdDesignName = MakeAndRecordSettlementCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateFacilityHullStats(editorSettings, isSettlement: true);
        IList<string> elementDesignNames = MakeAndRecordFacilityDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement);
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        SystemFactory.Instance.InstallCelestialItemInOrbit(creator.gameObject, system.SettlementOrbitData);
        D.Log("{0} has installed a {1} for {2} in System {3}.", Name, typeof(DebugSettlementCreator).Name, owner, system.FullName);
    }

    /// <summary>
    /// Assigns a configuration to the provided existing DebugSettlementCreator, using the DeployDate specified by the DebugCreator.
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system to assign the settlement creator to.</param>
    public void AssignConfigurationToExistingCreator(DebugSettlementCreator creator, Player owner, SystemItem system) {
        GameDate deployDate = creator.EditorSettings.DateToDeploy;
        AssignConfigurationToExistingCreator(creator, owner, system, deployDate);
    }

    /// <summary>
    /// Assigns a configuration to the provided existing DebugFleetCreator, using the DeployDate provided.
    /// <remarks>The DebugCreator's EditorSettings specifying the DeployDate will be ignored.</remarks>
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    public void AssignConfigurationToExistingCreator(DebugFleetCreator creator, Player owner, Vector3 location, GameDate deployDate) {
        var editorSettings = creator.EditorSettings as FleetCreatorEditorSettings;

        ValidateOwner(owner, editorSettings);

        string unitName = editorSettings.UnitName;
        string cmdDesignName = MakeAndRecordFleetCmdDesign(owner, editorSettings.UnitName, editorSettings.CMsPerCommand, editorSettings.Formation.Convert());
        var hullStats = CreateShipHullStats(editorSettings);
        ShipCombatStance stance = SelectCombatStance(editorSettings.StanceExclusions);
        IList<string> elementDesignNames = MakeAndRecordShipDesigns(owner, hullStats, editorSettings.LosTurretsPerElement,
            editorSettings.MissileLaunchersPerElement, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SensorsPerElement, editorSettings.ShieldGeneratorsPerElement, stance);
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        creator.transform.position = location;
        //D.Log("{0} has placed a {1} for {2}.", Name, typeof(DebugFleetCreator).Name, owner);
    }

    /// <summary>
    /// Assigns a configuration to the provided existing DebugFleetCreator, using the DeployDate specified by the DebugCreator.
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    public void AssignConfigurationToExistingCreator(DebugFleetCreator creator, Player owner, Vector3 location) {
        GameDate deployDate = creator.EditorSettings.DateToDeploy;
        AssignConfigurationToExistingCreator(creator, owner, location, deployDate);
    }

    private void ValidateOwner(Player owner, AUnitCreatorEditorSettings editorSettings) {
        if (owner.IsUser) {
            D.Assert(editorSettings.IsOwnerUser);
        }
        else {
            D.Assert(owner.__InitialUserRelationship == editorSettings.DesiredRelationshipWithUser.Convert());
        }
    }

    #endregion

    #region Generate Random AutoCreators

    /// <summary>
    /// Generates a random fleet creator, places it at location and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public FleetCreator GenerateRandomAutoFleetCreator(Player owner, Vector3 location, GameDate deployDate) {
        string unitName = GetUniqueUnitName("AutoFleet");
        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Formation.Diamond; // = Enums<Formation>.GetRandom(excludeDefault: true);
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

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log("{0} has generated/placed a random {1} for {2}.", Name, typeof(FleetCreator).Name, owner);
        return UnitFactory.Instance.MakeFleetCreatorInstance(location, config);
    }

    /// <summary>
    /// Generates a random fleet creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public FleetCreator GenerateRandomAutoFleetCreator(Player owner, Vector3 location) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return GenerateRandomAutoFleetCreator(owner, location, deployDate);
    }

    /// <summary>
    /// Generates a random starbase creator, places it at location and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public StarbaseCreator GenerateRandomAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate) {
        string unitName = GetUniqueUnitName("AutoStarbase");
        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Formation.Diamond;    // Enums<Formation>.GetRandomExcept(Formation.Wedge, default(Formation));
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

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log("{0} has generated/placed a random {1} for {2}.", Name, typeof(StarbaseCreator).Name, owner);
        return UnitFactory.Instance.MakeStarbaseCreatorInstance(location, config);
    }

    /// <summary>
    /// Generates a random starbase creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public StarbaseCreator GenerateRandomAutoStarbaseCreator(Player owner, Vector3 location) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0.1F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);

        return GenerateRandomAutoStarbaseCreator(owner, location, deployDate);
    }

    /// <summary>
    /// Generates a random settlement creator, places it in orbit around <c>system</c> and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public SettlementCreator GenerateRandomAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        string unitName = GetUniqueUnitName("AutoSettlement");
        int cmsPerCmd = RandomExtended.Range(0, 3);
        Formation formation = Formation.Diamond;    //Enums<Formation>.GetRandomExcept(Formation.Wedge, default(Formation));
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

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, owner, deployDate, cmdDesignName, elementDesignNames);
        D.Log("{0} has placed a random {1} for {2} in orbit in System {3}.", Name, typeof(SettlementCreator).Name, owner, system.FullName);
        return UnitFactory.Instance.MakeSettlementCreatorInstance(config, system);
    }

    /// <summary>
    /// Generates a random settlement creator, places it in orbit around <c>system</c> and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public SettlementCreator GenerateRandomAutoSettlementCreator(Player owner, SystemItem system) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(5F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);

        return GenerateRandomAutoSettlementCreator(owner, system, deployDate);
    }

    #endregion

    public void Reset() {
        _elementInstanceIDCounter = Constants.One;
        _designNameCounter = Constants.One;
        _unitNameCounter = Constants.One;
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

    private IList<string> MakeAndRecordFacilityDesigns(Player owner, IEnumerable<FacilityHullStat> hullStats, DebugWeaponLoadout turretLoadout,
        DebugWeaponLoadout missileLoadout, int passiveCMsPerElement, int activeCMsPerElement, int sensorsPerElement, int shieldGensPerElement) {

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

    #region Debug

    #endregion

}

