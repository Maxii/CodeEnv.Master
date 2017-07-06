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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

    private const string DesignNameFormat = "{0}{1}";

    /// <summary>
    /// Static counter used to provide a unique name for each design name.
    /// </summary>
    private static int _rootDesignNameCounter = Constants.One;

    /// <summary>
    /// Gets a unique design name for an element.
    /// </summary>
    /// <param name="hullCategoryName">The hull category name.</param>
    /// <returns></returns>
    private static string GetUniqueElementRootDesignName(string hullCategoryName) {
        var designName = DesignNameFormat.Inject(hullCategoryName, _rootDesignNameCounter);
        _rootDesignNameCounter++;
        return designName;
    }

    private static string GetUniqueCmdRootDesignName() {
        var designName = DesignNameFormat.Inject("Cmd", _rootDesignNameCounter);
        _rootDesignNameCounter++;
        return designName;
    }

    public string DebugName { get { return GetType().Name; } }

    private bool ShowDebugLog { get { return _debugCntls.ShowDeploymentDebugLogs; } }

    private SensorStat _elementsReqdSRSensorStat;
    private SensorStat _cmdsReqdMRSensorStat;
    private FtlDampenerStat _cmdsReqdFtlDampener;

    private IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;
    private IList<ActiveCountermeasureStat> _availableActiveCountermeasureStats;
    private IList<ShieldGeneratorStat> _availableShieldGeneratorStats;

    private IList<SensorStat> _availableElementSensorStats;
    private IList<SensorStat> _availableCmdSensorStats;
    private IList<AWeaponStat> _availableBeamWeaponStats;
    private IList<AWeaponStat> _availableProjectileWeaponStats;
    private IList<AWeaponStat> _availableMissileWeaponStats;
    private IList<AWeaponStat> _availableAssaultWeaponStats;

    private IDictionary<ShipHullCategory, ShipHullStat> _shipHullStatLookup;
    private IDictionary<FacilityHullCategory, FacilityHullStat> _facilityHullStatLookup;

    private IDictionary<ShipHullCategory, EngineStat> _ftlEngineStatLookup;
    private IDictionary<ShipHullCategory, EngineStat> _stlEngineStatLookup;

    private GameManager _gameMgr;
    private DebugControls _debugCntls;

    public NewGameUnitConfigurator() {
        InitializeValuesAndReferences();
        CreateEquipmentStats();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _debugCntls = DebugControls.Instance;
    }

    /// <summary>
    /// Creates and registers any required designs including a design for the loneFleetCmd and designs
    /// for empty ships, facilities and cmds for use when creating new designs from scratch.
    /// </summary>
    public void CreateAndRegisterRequiredDesigns() {
        foreach (var player in _gameMgr.AllPlayers) {
            FleetCmdDesign loneFleetCmdDesign = MakeFleetCmdDesign(player, passiveCmQty: 0, sensorQty: 1, maxCmdEffectiveness: 0.50F);
            loneFleetCmdDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
            RegisterCmdDesign(loneFleetCmdDesign, optionalRootDesignName: TempGameValues.LoneFleetCmdDesignName);

            var emptyShipDesigns = MakeShipDesigns(player, _shipHullStatLookup.Values, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None, new ShipCombatStance[] { ShipCombatStance.BalancedBombard });
            foreach (var shipDesign in emptyShipDesigns) {
                shipDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
                RegisterElementDesign(shipDesign, optionalRootDesignName: shipDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            var emptyFacilityDesigns = MakeFacilityDesigns(player, _facilityHullStatLookup.Values, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None);
            foreach (var facilityDesign in emptyFacilityDesigns) {
                facilityDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
                RegisterElementDesign(facilityDesign, optionalRootDesignName: facilityDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            FleetCmdDesign emptyFleetCmdDesign = MakeFleetCmdDesign(player, passiveCmQty: 0, sensorQty: 1);
            emptyFleetCmdDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
            RegisterCmdDesign(emptyFleetCmdDesign, optionalRootDesignName: TempGameValues.EmptyFleetCmdTemplateDesignName);

            StarbaseCmdDesign emptyStarbaseCmdDesign = MakeStarbaseCmdDesign(player, passiveCmQty: 0, sensorQty: 1);
            emptyStarbaseCmdDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
            RegisterCmdDesign(emptyStarbaseCmdDesign, optionalRootDesignName: TempGameValues.EmptyStarbaseCmdTemplateDesignName);

            SettlementCmdDesign emptySettlementCmdDesign = MakeSettlementCmdDesign(player, passiveCmQty: 0, sensorQty: 1);
            emptySettlementCmdDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
            RegisterCmdDesign(emptySettlementCmdDesign, optionalRootDesignName: TempGameValues.EmptySettlementCmdTemplateDesignName);
        }
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

        __ValidateOwner(owner, editorSettings);

        int cmdPassiveCMQty = GetPassiveCMQty(editorSettings.CMsPerCommand, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        var hullStats = GetFacilityHullStats(editorSettings);

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        creator.transform.position = location;
        //D.Log(ShowDebugLog, "{0} has placed a {1} for {2}.", DebugName, typeof(DebugStarbaseCreator).Name, owner);
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

        __ValidateOwner(owner, editorSettings);

        int cmdPassiveCMQty = GetPassiveCMQty(editorSettings.CMsPerCommand, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.MaxCmdSensors);

        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        var hullStats = GetFacilityHullStats(editorSettings);

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        SystemFactory.Instance.InstallCelestialItemInOrbit(creator.gameObject, system.SettlementOrbitData);
        D.Log(ShowDebugLog, "{0} has installed a {1} for {2} in System {3}.", DebugName, typeof(DebugSettlementCreator).Name, owner,
            system.DebugName);
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

        __ValidateOwner(owner, editorSettings);

        int cmdPassiveCMQty = GetPassiveCMQty(editorSettings.CMsPerCommand, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        var hullStats = GetShipHullStats(editorSettings);   ////CreateShipHullStats(editorSettings);
        IEnumerable<ShipCombatStance> stances = SelectCombatStances(editorSettings.StanceExclusions);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement, stances);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        creator.Configuration = config;
        creator.transform.position = location;
        //D.Log(ShowDebugLog, "{0} has placed a {1} for {2}.", DebugName, typeof(DebugFleetCreator).Name, owner);
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

    #endregion

    #region Generate Preset AutoCreators

    /// <summary>
    /// Generates a preset fleet creator, places it at location and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public FleetCreator GeneratePresetAutoFleetCreator(Player owner, Vector3 location, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCMQty(_debugCntls.CMsPerCmd, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetShipHullStats(elementQty);
        var turretLoadout = _debugCntls.LosWeaponsPerElement;
        var launchedLoadout = _debugCntls.LaunchedWeaponsPerElement;
        var passiveCMLoadout = _debugCntls.PassiveCMsPerElement;
        var activeCMLoadout = _debugCntls.ActiveCMsPerElement;
        var srSensorLoadout = _debugCntls.SRSensorsPerElement;
        var shieldGenLoadout = _debugCntls.ShieldGeneratorsPerElement;
        var combatStances = Enums<ShipCombatStance>.GetValues(excludeDefault: true);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, combatStances);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a preset {1} for {2}.", DebugName, typeof(FleetCreator).Name, owner);
        return UnitFactory.Instance.MakeFleetCreatorInstance(location, config);
    }

    /// <summary>
    /// Generates a preset fleet creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public FleetCreator GeneratePresetAutoFleetCreator(Player owner, Vector3 location) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return GeneratePresetAutoFleetCreator(owner, location, deployDate);
    }

    /// <summary>
    /// Generates a preset starbase creator, places it at location and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public StarbaseCreator GeneratePresetAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCMQty(_debugCntls.CMsPerCmd, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(elementQty);
        var turretLoadout = _debugCntls.LosWeaponsPerElement;
        var launchedLoadout = _debugCntls.LaunchedWeaponsPerElement;
        var passiveCMLoadout = _debugCntls.PassiveCMsPerElement;
        var activeCMLoadout = _debugCntls.ActiveCMsPerElement;
        var srSensorLoadout = _debugCntls.SRSensorsPerElement;
        var shieldGenLoadout = _debugCntls.ShieldGeneratorsPerElement;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a preset {1} for {2}.", DebugName, typeof(StarbaseCreator).Name, owner);
        return UnitFactory.Instance.MakeStarbaseCreatorInstance(location, config);
    }

    /// <summary>
    /// Generates a preset starbase creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public StarbaseCreator GeneratePresetAutoStarbaseCreator(Player owner, Vector3 location) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0.1F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return GeneratePresetAutoStarbaseCreator(owner, location, deployDate);
    }

    /// <summary>
    /// Generates a preset settlement creator, places it in orbit around <c>system</c> and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public SettlementCreator GeneratePresetAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCMQty(_debugCntls.CMsPerCmd, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.MaxCmdSensors);
        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(elementQty);
        var turretLoadout = _debugCntls.LosWeaponsPerElement;
        var launchedLoadout = _debugCntls.LaunchedWeaponsPerElement;
        var passiveCMLoadout = _debugCntls.PassiveCMsPerElement;
        var activeCMLoadout = _debugCntls.ActiveCMsPerElement;
        var srSensorLoadout = _debugCntls.SRSensorsPerElement;
        var shieldGenLoadout = _debugCntls.ShieldGeneratorsPerElement;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        D.Log(ShowDebugLog, "{0} has placed a preset {1} for {2} in orbit in System {3}.", DebugName, typeof(SettlementCreator).Name, owner, system.DebugName);
        return UnitFactory.Instance.MakeSettlementCreatorInstance(config, system);
    }

    /// <summary>
    /// Generates a preset settlement creator, places it in orbit around <c>system</c> and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public SettlementCreator GeneratePresetAutoSettlementCreator(Player owner, SystemItem system) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(5F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return GeneratePresetAutoSettlementCreator(owner, system, deployDate);
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
        int cmdPassiveCMQty = GetPassiveCMQty(DebugPassiveCMLoadout.Random, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetShipHullStats(elementQty);
        var turretLoadout = DebugLosWeaponLoadout.Random;
        var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
        var passiveCMLoadout = DebugPassiveCMLoadout.Random;
        var activeCMLoadout = DebugActiveCMLoadout.Random;
        var srSensorLoadout = DebugSensorLoadout.Random;
        var shieldGenLoadout = DebugShieldGenLoadout.Random;
        var combatStances = Enums<ShipCombatStance>.GetValues(excludeDefault: true);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, combatStances);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a random {1} for {2}.", DebugName, typeof(FleetCreator).Name, owner);
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
        int cmdPassiveCMQty = GetPassiveCMQty(DebugPassiveCMLoadout.Random, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(elementQty);
        var turretLoadout = DebugLosWeaponLoadout.Random;
        var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
        var passiveCMLoadout = DebugPassiveCMLoadout.Random;
        var activeCMLoadout = DebugActiveCMLoadout.Random;
        var srSensorLoadout = DebugSensorLoadout.Random;
        var shieldGenLoadout = DebugShieldGenLoadout.Random;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a random {1} for {2}.", DebugName, typeof(StarbaseCreator).Name, owner);
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
        int cmdPassiveCMQty = GetPassiveCMQty(DebugPassiveCMLoadout.Random, TempGameValues.MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.MaxCmdSensors);
        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(elementQty);
        var turretLoadout = DebugLosWeaponLoadout.Random;
        var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
        var passiveCMLoadout = DebugPassiveCMLoadout.Random;
        var activeCMLoadout = DebugActiveCMLoadout.Random;
        var srSensorLoadout = DebugSensorLoadout.Random;
        var shieldGenLoadout = DebugShieldGenLoadout.Random;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        D.Log(ShowDebugLog, "{0} has placed a random {1} for {2} in orbit in System {3}.", DebugName, typeof(SettlementCreator).Name,
            owner, system.DebugName);
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
        _rootDesignNameCounter = Constants.One;
    }

    #region Create Equipment Stats

    private void CreateEquipmentStats() {
        _availableBeamWeaponStats = __CreateAvailableBeamWeaponStats(9);
        _availableProjectileWeaponStats = __CreateAvailableProjectileWeaponStats(9);
        _availableMissileWeaponStats = __CreateAvailableMissileWeaponStats(9);
        _availableAssaultWeaponStats = __CreateAvailableAssaultWeaponStats(9);
        _availablePassiveCountermeasureStats = __CreateAvailablePassiveCountermeasureStats(9);
        _availableActiveCountermeasureStats = __CreateAvailableActiveCountermeasureStats(9);

        _elementsReqdSRSensorStat = CreateReqdElementSRSensorStat();
        _availableElementSensorStats = __CreateAvailableElementSensorStats(5);

        _cmdsReqdMRSensorStat = CreateReqdCmdMRSensorStat();
        _availableCmdSensorStats = __CreateAvailableCmdSensorStats(9);

        _cmdsReqdFtlDampener = CreateReqdCmdFtlDampenerStat();

        _availableShieldGeneratorStats = __CreateAvailableShieldGeneratorStats(9);

        _shipHullStatLookup = CreateShipHullStats();
        _facilityHullStatLookup = CreateFacilityHullStats();

        _stlEngineStatLookup = CreateEngineStats(isFtlEngine: false);
        _ftlEngineStatLookup = CreateEngineStats(isFtlEngine: true);
    }

    private IList<AWeaponStat> __CreateAvailableMissileWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            WDVCategory deliveryVehicleCategory = WDVCategory.Missile;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);    // 0.04 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(15F, 18F);    // 10-15
            string name = "Torpedo Launcher";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(6F, 16F);  // 3-8
            float ordTurnRate = 700F;   // degrees per hour
            float ordCourseUpdateFreq = 0.4F; // course updates per hour    // 3.18.17 0.5 got turn not complete warnings
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(8F, 12F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 5F;
            float ordDrag = 0.02F;

            var weapStat = new MissileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass, ordDrag,
                ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy, isDamageable);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<AWeaponStat> __CreateAvailableAssaultWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            WDVCategory deliveryVehicleCategory = WDVCategory.AssaultVehicle;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 1F);    // 0.07 - 1 degrees
            float reloadPeriod = UnityEngine.Random.Range(25F, 28F);
            string name = "Assault Launcher";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(0.5F, 1F);
            float ordTurnRate = 270F;   // degrees per hour
            float ordCourseUpdateFreq = 0.4F; // course updates per hour
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(2F, 4F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 10F;
            float ordDrag = 0.03F;

            var weapStat = new AssaultWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass, ordDrag,
                ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy, isDamageable);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<AWeaponStat> __CreateAvailableProjectileWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            AWeaponStat weapStat;
            RangeCategory rangeCat = RangeCategory.Medium;
            float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.07 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(4F, 6F);  // 2-4
            string name = "KineticKill Projector";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(5F, 10F);   // 3-8
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVCategory deliveryVehicleCategory = WDVCategory.Projectile;
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(15F, 18F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 1F;
            float ordDrag = 0.01F;
            weapStat = new ProjectileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass, ordDrag, maxLaunchInaccuracy, isDamageable);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<AWeaponStat> __CreateAvailableBeamWeaponStats(int quantity) {
        IList<AWeaponStat> statsList = new List<AWeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            RangeCategory rangeCat = RangeCategory.Short;
            float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.04 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(6F, 10F); // 3-5
            float duration = UnityEngine.Random.Range(2F, 3F);  //1-2
            string name = "Phaser Projector";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(6F, 16F);   // 3-8
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVCategory deliveryVehicleCategory = WDVCategory.Beam;
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            AWeaponStat weapStat = new BeamWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, duration, maxLaunchInaccuracy, isDamageable);
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
            var countermeasureStat = new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
                0F, 0F, 0F, 0F, damageMitigation);
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
                        new WDVStrength(WDVCategory.Missile, 0.5F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 0.5F)
                    };
                    interceptAccuracy = 0.50F;
                    reloadPeriod = 0.2F;    //0.1
                    break;
                case RangeCategory.Medium:
                    name = "AvengerADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 3.0F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 3.0F)
                    };
                    interceptAccuracy = 0.80F;
                    reloadPeriod = 2.0F;
                    break;
                case RangeCategory.Long:
                    name = "PatriotADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 1.0F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 1.0F)
                    };
                    interceptAccuracy = 0.70F;
                    reloadPeriod = 3.0F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
            var countermeasureStat = new ActiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
                0F, 0F, 0F, 0F, rangeCat, interceptStrengths, interceptAccuracy, reloadPeriod, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private SensorStat CreateReqdElementSRSensorStat() {
        string name = "ReqdSRSensor";
        return new SensorStat(name, RangeCategory.Short, isDamageable: false);
    }

    private SensorStat CreateReqdCmdMRSensorStat() {
        string name = "ReqdMRSensor";
        return new SensorStat(name, RangeCategory.Medium, isDamageable: true);
    }

    private IList<SensorStat> __CreateAvailableCmdSensorStats(int quantity) {
        IList<SensorStat> statsList = new List<SensorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            bool isDamageable = true;
            RangeCategory rangeCat = Enums<RangeCategory>.GetRandomExcept(RangeCategory.None, RangeCategory.Short);
            //RangeCategory rangeCat = RangeCategory.Long;
            switch (rangeCat) {
                case RangeCategory.Medium:
                    name = "PulseSensor";
                    break;
                case RangeCategory.Long:
                    name = "DeepScanArray";
                    break;
                case RangeCategory.Short:
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            var sensorStat = new SensorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, isDamageable);
            statsList.Add(sensorStat);
        }
        return statsList;
    }

    private IList<SensorStat> __CreateAvailableElementSensorStats(int quantity) {
        IList<SensorStat> statsList = new List<SensorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = "ProximityDetector";
            bool isDamageable = true;
            var sensorStat = new SensorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                RangeCategory.Short, isDamageable);
            statsList.Add(sensorStat);
        }
        return statsList;
    }

    private FtlDampenerStat CreateReqdCmdFtlDampenerStat() { return new FtlDampenerStat("ReqdSRFtlDampener", RangeCategory.Short); }

    private IList<ShieldGeneratorStat> __CreateAvailableShieldGeneratorStats(int quantity) {
        IList<ShieldGeneratorStat> statsList = new List<ShieldGeneratorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            RangeCategory rangeCat = RangeCategory.Short;
            string name = "Deflector Generator";
            float maxCharge = 20F;
            float trickleChargeRate = 1F;
            float reloadPeriod = 20F;
            DamageStrength damageMitigation = default(DamageStrength);  // none for now
            var generatorStat = new ShieldGeneratorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
                0F, 1F, 0F, 0F, rangeCat, maxCharge, trickleChargeRate, reloadPeriod, damageMitigation);
            statsList.Add(generatorStat);
        }
        return statsList;
    }

    private IDictionary<ShipHullCategory, EngineStat> CreateEngineStats(bool isFtlEngine) {
        IDictionary<ShipHullCategory, EngineStat> engineStats = new Dictionary<ShipHullCategory, EngineStat>(TempGameValues.ShipHullCategoriesInUse.Length);

        float maxTurnRate = isFtlEngine ? UnityEngine.Random.Range(180F, 270F) : UnityEngine.Random.Range(TempGameValues.MinimumTurnRate, 180F);
        float engineSize = isFtlEngine ? 20F : 10F;
        float engineExpense = isFtlEngine ? 10F : 5F;
        string engineName = isFtlEngine ? "FtlEngine" : "StlEngine";
        bool isDamageable = isFtlEngine ? true : false;

        foreach (var hullCategory in TempGameValues.ShipHullCategoriesInUse) {
            float engineMass = __GetEngineMass(hullCategory);
            float fullPropulsionPower = GetFullStlPropulsionPower(hullCategory);   // FullFtlOpenSpaceSpeed ~ 30-40 units/hour, FullStlSystemSpeed ~ 1.2 - 1.6 units/hour
            if (isFtlEngine) {
                fullPropulsionPower *= TempGameValues.__StlToFtlPropulsionPowerFactor;
            }
            var engineStat = new EngineStat(engineName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", fullPropulsionPower, maxTurnRate,
                engineSize, engineMass, engineExpense, isDamageable, isFtlEngine);
            engineStats.Add(hullCategory, engineStat);
        }
        return engineStats;
    }

    private IDictionary<ShipHullCategory, ShipHullStat> CreateShipHullStats() {
        var hullStats = new Dictionary<ShipHullCategory, ShipHullStat>(TempGameValues.ShipHullCategoriesInUse.Length);
        foreach (var hullCat in TempGameValues.ShipHullCategoriesInUse) {
            var hullStat = CreateElementHullStat(hullCat);
            hullStats.Add(hullCat, hullStat);
        }
        return hullStats;
    }

    private IDictionary<FacilityHullCategory, FacilityHullStat> CreateFacilityHullStats() {
        var hullStats = new Dictionary<FacilityHullCategory, FacilityHullStat>(TempGameValues.FacilityHullCategoriesInUse.Length);
        foreach (var hullCat in TempGameValues.FacilityHullCategoriesInUse) {
            var hullStat = CreateElementHullStat(hullCat);
            hullStats.Add(hullCat, hullStat);
        }
        return hullStats;
    }

    private ShipHullStat CreateElementHullStat(ShipHullCategory hullCat) {
        float hullMass = hullCat.Mass();
        float drag = hullCat.Drag();
        float science = hullCat.Science();
        float culture = hullCat.Culture();
        float income = hullCat.Income();
        float expense = hullCat.Expense();
        Vector3 hullDimensions = hullCat.Dimensions();
        return new ShipHullStat(hullCat, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, drag, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    private FacilityHullStat CreateElementHullStat(FacilityHullCategory hullCat) {
        float science = hullCat.Science();
        float culture = hullCat.Culture();
        float income = hullCat.Income();
        float expense = hullCat.Expense();
        float hullMass = hullCat.Mass();
        Vector3 hullDimensions = hullCat.Dimensions();
        return new FacilityHullStat(hullCat, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    #endregion

    #region Element Designs

    private IList<ShipDesign> MakeShipDesigns(Player owner, IEnumerable<ShipHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout, IEnumerable<ShipCombatStance> stances) {

        IList<ShipDesign> designs = new List<ShipDesign>();
        foreach (var hullStat in hullStats) {
            ShipHullCategory hullCategory = hullStat.HullCategory;

            var weaponStats = GetWeaponStats(hullCategory, launchedLoadout, turretLoadout);
            int passiveCMQty = GetPassiveCMQty(passiveCMLoadout, hullCategory.__MaxPassiveCMs());
            var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCMQty);
            int activeCMQty = GetActiveCMQty(activeCMLoadout, hullCategory.__MaxActiveCMs());
            var activeCmStats = _availableActiveCountermeasureStats.Shuffle().Take(activeCMQty);

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCategory.__MaxSensors());
            if (srSensorQty > 1) {
                optionalSensorStats.AddRange(_availableElementSensorStats.Shuffle().Take(srSensorQty - 1));
            }

            int shieldGenQty = GetShieldGeneratorQty(shieldGenLoadout, hullCategory.__MaxShieldGenerators());
            var shieldGenStats = _availableShieldGeneratorStats.Shuffle().Take(shieldGenQty);
            Priority hqPriority = hullCategory.__HQPriority();    // TEMP, IMPROVE
            ShipCombatStance stance = RandomExtended.Choice(stances);

            var design = MakeElementDesign(owner, hullStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, hqPriority, stance);
            designs.Add(design);
        }
        return designs;
    }

    private IList<FacilityDesign> MakeFacilityDesigns(Player owner, IEnumerable<FacilityHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout) {

        IList<FacilityDesign> designs = new List<FacilityDesign>();
        foreach (var hullStat in hullStats) {
            FacilityHullCategory hullCategory = hullStat.HullCategory;

            var weaponStats = GetWeaponStats(hullCategory, launchedLoadout, turretLoadout);
            int passiveCMQty = GetPassiveCMQty(passiveCMLoadout, hullCategory.__MaxPassiveCMs());
            var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCMQty);
            int activeCMQty = GetActiveCMQty(activeCMLoadout, hullCategory.__MaxActiveCMs());
            var activeCmStats = _availableActiveCountermeasureStats.Shuffle().Take(activeCMQty);

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCategory.__MaxSensors());
            if (srSensorQty > 1) {
                optionalSensorStats.AddRange(_availableElementSensorStats.Shuffle().Take(srSensorQty - 1));
            }

            int shieldGenQty = GetShieldGeneratorQty(shieldGenLoadout, hullCategory.__MaxShieldGenerators());
            var shieldGenStats = _availableShieldGeneratorStats.Shuffle().Take(shieldGenQty);
            Priority hqPriority = hullCategory.__HQPriority();    // TEMP, IMPROVE

            var design = MakeElementDesign(owner, hullStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, hqPriority);
            designs.Add(design);
        }
        return designs;
    }

    private ShipDesign MakeElementDesign(Player owner, ShipHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority, ShipCombatStance stance) {
        ShipHullCategory hullCategory = hullStat.HullCategory;
        var stlEngineStat = GetEngineStatFor(hullCategory, isFtlEngine: false);
        var ftlEngineStat = GetEngineStatFor(hullCategory, isFtlEngine: true);
        var design = new ShipDesign(owner, hqPriority, _elementsReqdSRSensorStat, hullStat, stlEngineStat, ftlEngineStat, stance);
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            sensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        return design;
    }

    private FacilityDesign MakeElementDesign(Player owner, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority) {
        FacilityHullCategory hullCategory = hullStat.HullCategory;
        var design = new FacilityDesign(owner, hqPriority, _elementsReqdSRSensorStat, hullStat);
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            sensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        return design;
    }

    private string RegisterElementDesign(ShipDesign design, string optionalRootDesignName = null) {
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = _gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!_gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return rootDesignName;
        }

        ShipDesign existingDesign = _gameMgr.PlayersDesigns.GetShipDesign(design.Player, existingDesignName);
        existingDesign.Status = AUnitDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private string RegisterElementDesign(FacilityDesign design, string optionalRootDesignName = null) {
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = _gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!_gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return rootDesignName;
        }
        FacilityDesign existingDesign = _gameMgr.PlayersDesigns.GetFacilityDesign(design.Player, existingDesignName);
        existingDesign.Status = AUnitDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    #endregion

    #region Command Designs

    private SettlementCmdDesign MakeSettlementCmdDesign(Player owner, int passiveCmQty, int sensorQty) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.MaxCmdSensors);

        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCmQty);

        List<SensorStat> optionalCmdSensorStats = new List<SensorStat>();
        if (sensorQty > 1) {
            optionalCmdSensorStats.AddRange(_availableCmdSensorStats.Shuffle().Take(sensorQty - 1));
        }

        SettlementCmdStat cmdStat = MakeSettlementCmdStat();
        SettlementCmdDesign design = new SettlementCmdDesign(owner, _cmdsReqdFtlDampener, cmdStat, _cmdsReqdMRSensorStat);
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalCmdSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        return design;
    }

    private StarbaseCmdDesign MakeStarbaseCmdDesign(Player owner, int passiveCmQty, int sensorQty) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.MaxCmdSensors);

        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCmQty);

        List<SensorStat> optionalCmdSensorStats = new List<SensorStat>();
        if (sensorQty > 1) {
            optionalCmdSensorStats.AddRange(_availableCmdSensorStats.Shuffle().Take(sensorQty - 1));
        }

        UnitCmdStat cmdStat = MakeStarbaseCmdStat();
        StarbaseCmdDesign design = new StarbaseCmdDesign(owner, _cmdsReqdFtlDampener, cmdStat, _cmdsReqdMRSensorStat);
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalCmdSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        return design;
    }

    private FleetCmdDesign MakeFleetCmdDesign(Player owner, int passiveCmQty, int sensorQty, float maxCmdEffectiveness = Constants.OneHundredPercent) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.MaxCmdSensors);

        var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCmQty);

        List<SensorStat> optionalCmdSensorStats = new List<SensorStat>();
        if (sensorQty > 1) {
            optionalCmdSensorStats.AddRange(_availableCmdSensorStats.Shuffle().Take(sensorQty - 1));
        }

        UnitCmdStat cmdStat = MakeFleetCmdStat(maxCmdEffectiveness);
        FleetCmdDesign design = new FleetCmdDesign(owner, _cmdsReqdFtlDampener, cmdStat, _cmdsReqdMRSensorStat);
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalCmdSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        return design;
    }

    private string RegisterCmdDesign(StarbaseCmdDesign design, string optionalRootDesignName = null) {
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = _gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!_gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return rootDesignName;
        }
        StarbaseCmdDesign existingDesign = _gameMgr.PlayersDesigns.GetStarbaseCmdDesign(design.Player, existingDesignName);
        existingDesign.Status = AUnitDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private string RegisterCmdDesign(SettlementCmdDesign design, string optionalRootDesignName = null) {
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = _gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!_gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return rootDesignName;
        }
        SettlementCmdDesign existingDesign = _gameMgr.PlayersDesigns.GetSettlementCmdDesign(design.Player, existingDesignName);
        existingDesign.Status = AUnitDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private string RegisterCmdDesign(FleetCmdDesign design, string optionalRootDesignName = null) {
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = _gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!_gameMgr.PlayersDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            _gameMgr.PlayersDesigns.Add(design);
            return rootDesignName;
        }
        FleetCmdDesign existingDesign = _gameMgr.PlayersDesigns.GetFleetCmdDesign(design.Player, existingDesignName);
        existingDesign.Status = AUnitDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private UnitCmdStat MakeFleetCmdStat(float maxEffectiveness) {
        float maxHitPts = 10F;
        float maxCmdEffect = maxEffectiveness;
        return new UnitCmdStat(maxHitPts, maxCmdEffect);
    }

    private UnitCmdStat MakeStarbaseCmdStat() {
        float maxHitPts = 10F;
        float maxCmdEffect = Constants.OneHundredPercent;
        return new UnitCmdStat(maxHitPts, maxCmdEffect);
    }

    private SettlementCmdStat MakeSettlementCmdStat() {
        float maxHitPts = 10F;
        float maxCmdEffect = Constants.OneHundredPercent;
        int startingPopulation = 100;
        float startingApproval = Constants.OneHundredPercent;
        return new SettlementCmdStat(maxHitPts, maxCmdEffect, startingPopulation, startingApproval);
    }

    #endregion


    #region Support

    private EngineStat GetEngineStatFor(ShipHullCategory hullCat, bool isFtlEngine) {
        return isFtlEngine ? _ftlEngineStatLookup[hullCat] : _stlEngineStatLookup[hullCat];
    }

    private IEnumerable<FacilityHullStat> GetFacilityHullStats(BaseCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var hullStats = new List<FacilityHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                hullStats.Add(_facilityHullStatLookup[hullCat]);
            }
            return hullStats;
        }
        return GetFacilityHullStats(settings.NonPresetElementQty);
    }

    private IEnumerable<FacilityHullStat> GetFacilityHullStats(int qty) {
        return new List<FacilityHullStat>(RandomExtended.Choices<FacilityHullStat>(_facilityHullStatLookup.Values, qty));
    }

    private IEnumerable<ShipHullStat> GetShipHullStats(FleetCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var hullStats = new List<ShipHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                hullStats.Add(_shipHullStatLookup[hullCat]);
            }
            return hullStats;
        }
        return GetShipHullStats(settings.NonPresetElementQty);
    }

    private IEnumerable<ShipHullStat> GetShipHullStats(int qty) {
        return new List<ShipHullStat>(RandomExtended.Choices<ShipHullStat>(_shipHullStatLookup.Values, qty));
    }

    private IEnumerable<AWeaponStat> GetWeaponStats(ShipHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout, DebugLosWeaponLoadout turretLoadout) {
        int beamsPerElement;
        int projectilesPerElement;
        DetermineLosWeaponQtyAndMix(turretLoadout, hullCat.__MaxLOSWeapons(), out beamsPerElement, out projectilesPerElement);
        var weaponStats = _availableBeamWeaponStats.Shuffle().Take(beamsPerElement).ToList();
        weaponStats.AddRange(_availableProjectileWeaponStats.Shuffle().Take(projectilesPerElement));

        int missilesPerElement;
        int assaultVehiclesPerElement;
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullCat.__MaxLaunchedWeapons(), out missilesPerElement, out assaultVehiclesPerElement);
        weaponStats.AddRange(_availableMissileWeaponStats.Shuffle().Take(missilesPerElement));
        weaponStats.AddRange(_availableAssaultWeaponStats.Shuffle().Take(assaultVehiclesPerElement));
        return weaponStats;
    }

    private IEnumerable<AWeaponStat> GetWeaponStats(FacilityHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout, DebugLosWeaponLoadout turretLoadout) {
        int beamsPerElement;
        int projectilesPerElement;
        DetermineLosWeaponQtyAndMix(turretLoadout, hullCat.__MaxLOSWeapons(), out beamsPerElement, out projectilesPerElement);
        var weaponStats = _availableBeamWeaponStats.Shuffle().Take(beamsPerElement).ToList();
        weaponStats.AddRange(_availableProjectileWeaponStats.Shuffle().Take(projectilesPerElement));

        int missilesPerElement;
        int assaultVehiclesPerElement;
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullCat.__MaxLaunchedWeapons(), out missilesPerElement, out assaultVehiclesPerElement);
        weaponStats.AddRange(_availableMissileWeaponStats.Shuffle().Take(missilesPerElement));
        weaponStats.AddRange(_availableAssaultWeaponStats.Shuffle().Take(assaultVehiclesPerElement));
        return weaponStats;
    }

    private float __GetEngineMass(ShipHullCategory hullCat) {
        return hullCat.Mass() * 0.1F;
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

    private void DetermineLosWeaponQtyAndMix(DebugLosWeaponLoadout loadout, int maxAllowed, out int beams, out int projectiles) {
        switch (loadout) {
            case DebugLosWeaponLoadout.None:
                beams = Constants.Zero;
                projectiles = Constants.Zero;
                break;
            case DebugLosWeaponLoadout.OneBeam:
                beams = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                projectiles = Constants.Zero;
                break;
            case DebugLosWeaponLoadout.OneProjectile:
                beams = Constants.Zero;
                projectiles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                break;
            case DebugLosWeaponLoadout.OneEach:
                beams = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                projectiles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                break;
            case DebugLosWeaponLoadout.Random:
                beams = RandomExtended.Range(Constants.Zero, maxAllowed);
                projectiles = RandomExtended.Range(Constants.Zero, maxAllowed - beams);
                break;
            case DebugLosWeaponLoadout.MaxBeam:
                beams = maxAllowed;
                projectiles = Constants.Zero;
                break;
            case DebugLosWeaponLoadout.MaxProjectile:
                beams = Constants.Zero;
                projectiles = maxAllowed;
                break;
            case DebugLosWeaponLoadout.MaxMix:
                beams = Mathf.FloorToInt(maxAllowed / 2F);
                projectiles = maxAllowed - beams;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private void DetermineLaunchedWeaponQtyAndMix(DebugLaunchedWeaponLoadout loadout, int maxAllowed, out int missiles, out int assaultVehicles) {
        switch (loadout) {
            case DebugLaunchedWeaponLoadout.None:
                missiles = Constants.Zero;
                assaultVehicles = Constants.Zero;
                break;
            case DebugLaunchedWeaponLoadout.OneMissile:
                missiles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                assaultVehicles = Constants.Zero;
                break;
            case DebugLaunchedWeaponLoadout.OneAssaultVehicle:
                missiles = Constants.Zero;
                assaultVehicles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                break;
            case DebugLaunchedWeaponLoadout.OneEach:
                missiles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                assaultVehicles = maxAllowed > Constants.Zero ? Constants.One : Constants.Zero;
                break;
            case DebugLaunchedWeaponLoadout.Random:
                missiles = RandomExtended.Range(Constants.Zero, maxAllowed);
                assaultVehicles = RandomExtended.Range(Constants.Zero, maxAllowed - missiles);
                break;
            case DebugLaunchedWeaponLoadout.MaxMissiles:
                missiles = maxAllowed;
                assaultVehicles = Constants.Zero;
                break;
            case DebugLaunchedWeaponLoadout.MaxAssaultVehicles:
                missiles = Constants.Zero;
                assaultVehicles = maxAllowed;
                break;
            case DebugLaunchedWeaponLoadout.MaxMix:
                missiles = Mathf.FloorToInt(maxAllowed / 2F);
                assaultVehicles = maxAllowed - missiles;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private int GetPassiveCMQty(DebugPassiveCMLoadout loadout, int maxAllowed) {
        switch (loadout) {
            case DebugPassiveCMLoadout.None:
                return Constants.Zero;
            case DebugPassiveCMLoadout.One:
                return Constants.One;
            case DebugPassiveCMLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxAllowed);
            case DebugPassiveCMLoadout.Max:
                return maxAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private int GetActiveCMQty(DebugActiveCMLoadout loadout, int maxAllowed) {
        switch (loadout) {
            case DebugActiveCMLoadout.None:
                return Constants.Zero;
            case DebugActiveCMLoadout.One:
                return Constants.One;
            case DebugActiveCMLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxAllowed);
            case DebugActiveCMLoadout.Max:
                return maxAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private int GetSensorQty(DebugSensorLoadout loadout, int maxAllowed) {
        switch (loadout) {
            case DebugSensorLoadout.One:
                return Constants.One;
            case DebugSensorLoadout.Random:
                return RandomExtended.Range(Constants.One, maxAllowed);
            case DebugSensorLoadout.Max:
                return maxAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private int GetShieldGeneratorQty(DebugShieldGenLoadout loadout, int maxAllowed) {
        switch (loadout) {
            case DebugShieldGenLoadout.None:
                return Constants.Zero;
            case DebugShieldGenLoadout.One:
                return Constants.One;
            case DebugShieldGenLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxAllowed);
            case DebugShieldGenLoadout.Max:
                return maxAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(loadout));
        }
    }

    private IEnumerable<ShipCombatStance> SelectCombatStances(DebugShipCombatStanceExclusions stanceExclusions) {
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptBalancedBombard) {
            return new ShipCombatStance[] { ShipCombatStance.BalancedBombard };
        }
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptBalancedStrafe) {
            return new ShipCombatStance[] { ShipCombatStance.BalancedStrafe };
        }
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptPointBlank) {
            return new ShipCombatStance[] { ShipCombatStance.PointBlank };
        }
        if (stanceExclusions == DebugShipCombatStanceExclusions.AllExceptStandoff) {
            return new ShipCombatStance[] { ShipCombatStance.Standoff };
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
            return Enums<ShipCombatStance>.GetValuesExcept(excludedCombatStances.ToArray());
        }
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    public IEnumerable<AEquipmentStat> __GetAvailableUserElementEquipmentStats() {
        return _availableActiveCountermeasureStats.Cast<AEquipmentStat>().UnionBy(_availableElementSensorStats.Cast<AEquipmentStat>(),
        _availablePassiveCountermeasureStats.Cast<AEquipmentStat>(), _availableShieldGeneratorStats.Cast<AEquipmentStat>(),
        _availableAssaultWeaponStats.Cast<AEquipmentStat>(), _availableBeamWeaponStats.Cast<AEquipmentStat>(),
        _availableMissileWeaponStats.Cast<AEquipmentStat>(), _availableProjectileWeaponStats.Cast<AEquipmentStat>());
    }

    private void __ValidateOwner(Player owner, AUnitCreatorEditorSettings editorSettings) {
        if (owner.IsUser) {
            D.Assert(editorSettings.IsOwnerUser);
        }
        else {
            D.AssertEqual(owner.__InitialUserRelationship, editorSettings.DesiredRelationshipWithUser.Convert());
        }
    }

    #endregion

}

