﻿// --------------------------------------------------------------------------------------------------------------------
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

    private GameManager _gameMgr;
    private DebugControls _debugCntls;
    ////[Obsolete]
    ////private EquipmentStatFactory _eStatFactory;

    public NewGameUnitConfigurator() {
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _debugCntls = DebugControls.Instance;
        ////_eStatFactory = EquipmentStatFactory.Instance;
    }

    /// <summary>
    /// Creates and registers any required designs including a design for the LoneFleetCmd and designs
    /// for empty ships, facilities and cmds for use when creating new designs from scratch.
    /// </summary>
    public void CreateAndRegisterRequiredDesigns() {
        foreach (var player in _gameMgr.AllPlayers) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            FleetCmdDesign basicFleetCmdDesign = MakeFleetCmdDesign(player, passiveCmQty: 0, sensorQty: 1);
            basicFleetCmdDesign.Status = AUnitMemberDesign.SourceAndStatus.System_CreationTemplate;
            RegisterCmdDesign(player, basicFleetCmdDesign, optionalRootDesignName: TempGameValues.__FleetCmdDesignName_Basic);

            var allShipHullStats = playerDesigns.GetAllCurrentShipHullStats();////_eStatFactory.GetAllShipHullStats(player, Level.One);
            var emptyShipDesigns = MakeShipDesigns(player, allShipHullStats, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None, new ShipCombatStance[] { ShipCombatStance.BalancedBombard },
                AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var shipDesign in emptyShipDesigns) {
                RegisterElementDesign(player, shipDesign, optionalRootDesignName: shipDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            var allFacilityHullStats = playerDesigns.GetAllCurrentFacilityHullStats();////_eStatFactory.GetAllFacilityHullStats(player, Level.One);
            var emptyFacilityDesigns = MakeFacilityDesigns(player, allFacilityHullStats, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None, AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            foreach (var facilityDesign in emptyFacilityDesigns) {
                RegisterElementDesign(player, facilityDesign, optionalRootDesignName: facilityDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            FleetCmdDesign emptyFleetCmdDesign = MakeFleetCmdDesign(player, passiveCmQty: 0, sensorQty: 1, status: AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            RegisterCmdDesign(player, emptyFleetCmdDesign, optionalRootDesignName: TempGameValues.EmptyFleetCmdTemplateDesignName);

            StarbaseCmdDesign emptyStarbaseCmdDesign = MakeStarbaseCmdDesign(player, passiveCmQty: 0, sensorQty: 1, status: AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            RegisterCmdDesign(player, emptyStarbaseCmdDesign, optionalRootDesignName: TempGameValues.EmptyStarbaseCmdTemplateDesignName);

            SettlementCmdDesign emptySettlementCmdDesign = MakeSettlementCmdDesign(player, passiveCmQty: 0, sensorQty: 1, status: AUnitMemberDesign.SourceAndStatus.System_CreationTemplate);
            RegisterCmdDesign(player, emptySettlementCmdDesign, optionalRootDesignName: TempGameValues.EmptySettlementCmdTemplateDesignName);
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

        int cmdPassiveCMQty = GetPassiveCmQty(editorSettings.CMsPerCommand, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.__MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        var hullStats = GetFacilityHullStats(owner, editorSettings);

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(owner, design);
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

        int cmdPassiveCMQty = GetPassiveCmQty(editorSettings.CMsPerCommand, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.__MaxCmdSensors);

        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        var hullStats = GetFacilityHullStats(owner, editorSettings);

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(owner, design);
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

        int cmdPassiveCMQty = GetPassiveCmQty(editorSettings.CMsPerCommand, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.__MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        var hullStats = GetShipHullStats(owner, editorSettings);
        IEnumerable<ShipCombatStance> stances = SelectCombatStances(editorSettings.StanceExclusions);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement, stances);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterElementDesign(owner, design);
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
    public AutoFleetCreator GeneratePresetAutoFleetCreator(Player owner, Vector3 location, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(_debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetShipHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a preset {1} for {2}.", DebugName, typeof(AutoFleetCreator).Name, owner);
        return UnitFactory.Instance.MakeFleetCreator(location, config);
    }

    /// <summary>
    /// Generates a preset fleet creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public AutoFleetCreator GeneratePresetAutoFleetCreator(Player owner, Vector3 location) {
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
    public AutoStarbaseCreator GeneratePresetAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(_debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a preset {1} for {2}.", DebugName, typeof(AutoStarbaseCreator).Name, owner);
        return UnitFactory.Instance.MakeStarbaseCreator(location, config);
    }

    /// <summary>
    /// Generates a preset starbase creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public AutoStarbaseCreator GeneratePresetAutoStarbaseCreator(Player owner, Vector3 location) {
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
    public AutoSettlementCreator GeneratePresetAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, _debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(_debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(_debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        D.Log(ShowDebugLog, "{0} has placed a preset {1} for {2} in orbit in System {3}.", DebugName, typeof(AutoSettlementCreator).Name, owner, system.DebugName);
        return UnitFactory.Instance.MakeSettlementCreator(config, system);
    }

    /// <summary>
    /// Generates a preset settlement creator, places it in orbit around <c>system</c> and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public AutoSettlementCreator GeneratePresetAutoSettlementCreator(Player owner, SystemItem system) {
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
    public AutoFleetCreator GenerateRandomAutoFleetCreator(Player owner, Vector3 location, GameDate deployDate) {
        int cmdPassiveCMQty = GetPassiveCmQty(DebugPassiveCMLoadout.Random, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.__MaxCmdSensors);
        FleetCmdDesign cmdDesign = MakeFleetCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetShipHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a random {1} for {2}.", DebugName, typeof(AutoFleetCreator).Name, owner);
        return UnitFactory.Instance.MakeFleetCreator(location, config);
    }

    /// <summary>
    /// Generates a random fleet creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public AutoFleetCreator GenerateRandomAutoFleetCreator(Player owner, Vector3 location) {
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
    public AutoStarbaseCreator GenerateRandomAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate) {
        int cmdPassiveCMQty = GetPassiveCmQty(DebugPassiveCMLoadout.Random, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.__MaxCmdSensors);
        StarbaseCmdDesign cmdDesign = MakeStarbaseCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        //D.Log(ShowDebugLog, "{0} has generated/placed a random {1} for {2}.", DebugName, typeof(AutoStarbaseCreator).Name, owner);
        return UnitFactory.Instance.MakeStarbaseCreator(location, config);
    }

    /// <summary>
    /// Generates a random starbase creator, places it at location and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public AutoStarbaseCreator GenerateRandomAutoStarbaseCreator(Player owner, Vector3 location) {
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
    public AutoSettlementCreator GenerateRandomAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        int cmdPassiveCMQty = GetPassiveCmQty(DebugPassiveCMLoadout.Random, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.__MaxCmdSensors);
        SettlementCmdDesign cmdDesign = MakeSettlementCmdDesign(owner, cmdPassiveCMQty, cmdSensorQty);
        string cmdDesignName = RegisterCmdDesign(owner, cmdDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetFacilityHullStats(owner, elementQty);
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
            string registeredDesignName = RegisterElementDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdDesignName, elementDesignNames);
        D.Log(ShowDebugLog, "{0} has placed a random {1} for {2} in orbit in System {3}.", DebugName, typeof(AutoSettlementCreator).Name,
            owner, system.DebugName);
        return UnitFactory.Instance.MakeSettlementCreator(config, system);
    }

    /// <summary>
    /// Generates a random settlement creator, places it in orbit around <c>system</c> and deploys it on a random date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public AutoSettlementCreator GenerateRandomAutoSettlementCreator(Player owner, SystemItem system) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(5F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return GenerateRandomAutoSettlementCreator(owner, system, deployDate);
    }

    #endregion

    public void ResetForReuse() {
        _rootDesignNameCounter = Constants.One;
    }

    #region Element Designs

    private IList<ShipDesign> MakeShipDesigns(Player owner, IEnumerable<ShipHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout, IEnumerable<ShipCombatStance> stances,
        AUnitMemberDesign.SourceAndStatus status = AUnitMemberDesign.SourceAndStatus.Player_Current) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        IList<ShipDesign> designs = new List<ShipDesign>();
        foreach (var hullStat in hullStats) {
            ShipHullCategory hullCat = hullStat.HullCategory;

            var weaponStats = GetWeaponStats(owner, hullCat, launchedLoadout, turretLoadout);
            ////int passiveCMQty = GetPassiveCMQty(passiveCMLoadout, hullCat.__MaxPassiveCMs());
            ////var initialPassiveCmStat = _eStatFactory.MakeNonHullInstance(owner, EquipmentCategory.PassiveCountermeasure, Level.One) as PassiveCountermeasureStat;
            ////var passiveCmStats = Enumerable.Repeat<PassiveCountermeasureStat>(initialPassiveCmStat, passiveCMQty);
            var passiveCmStats = GetPassiveCmStats(owner, passiveCMLoadout, hullCat.__MaxPassiveCMs());
            ////int activeCMQty = GetActiveCMQty(activeCMLoadout, hullCat.__MaxActiveCMs());
            ////List<ActiveCountermeasureStat> activeCmStats = new List<ActiveCountermeasureStat>(activeCMQty);
            ////if (activeCMQty > 0) {
            ////    var srActiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRActiveCountermeasure, Level.One) as ActiveCountermeasureStat;
            ////    activeCmStats.Add(srActiveCmStat);
            ////    if (activeCMQty > 1) {
            ////        var mrActiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.MRActiveCountermeasure, Level.One) as ActiveCountermeasureStat;
            ////        activeCmStats.AddRange(Enumerable.Repeat<ActiveCountermeasureStat>(mrActiveCmStat, activeCMQty - 1));
            ////    }
            ////}
            var activeCmStats = GetActiveCmStats(owner, activeCMLoadout, hullCat.__MaxActiveCMs());

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCat.__MaxSensors());
            if (srSensorQty > 1) {
                ////var srSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRSensor, Level.One) as SensorStat;
                var srSensorStat = ownerDesigns.GetCurrentSRSensorStat();
                optionalSensorStats.AddRange(Enumerable.Repeat<SensorStat>(srSensorStat, srSensorQty - 1));
            }

            ////int shieldGenQty = GetShieldGeneratorQty(shieldGenLoadout, hullCat.__MaxShieldGenerators());
            ////var shieldGenStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.ShieldGenerator, Level.One) as ShieldGeneratorStat;
            ////var shieldGenStats = Enumerable.Repeat<ShieldGeneratorStat>(shieldGenStat, shieldGenQty);
            var shieldGenStats = GetShieldGenStats(owner, shieldGenLoadout, hullCat.__MaxShieldGenerators());
            Priority hqPriority = hullCat.__HQPriority();    // TEMP, IMPROVE
            ShipCombatStance stance = RandomExtended.Choice(stances);

            var design = MakeElementDesign(owner, hullStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, hqPriority, stance, status);
            designs.Add(design);
        }
        return designs;
    }

    private IList<FacilityDesign> MakeFacilityDesigns(Player owner, IEnumerable<FacilityHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout,
        AUnitMemberDesign.SourceAndStatus status = AUnitMemberDesign.SourceAndStatus.Player_Current) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        IList<FacilityDesign> designs = new List<FacilityDesign>();
        foreach (var hullStat in hullStats) {
            FacilityHullCategory hullCat = hullStat.HullCategory;

            var weaponStats = GetWeaponStats(owner, hullCat, launchedLoadout, turretLoadout);
            ////int passiveCMQty = GetPassiveCMQty(passiveCMLoadout, hullCat.__MaxPassiveCMs());
            ////var initialPassiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.PassiveCountermeasure, Level.One) as PassiveCountermeasureStat;
            ////var passiveCmStats = Enumerable.Repeat<PassiveCountermeasureStat>(initialPassiveCmStat, passiveCMQty);
            var passiveCmStats = GetPassiveCmStats(owner, passiveCMLoadout, hullCat.__MaxPassiveCMs());
            ////int activeCMQty = GetActiveCMQty(activeCMLoadout, hullCat.__MaxActiveCMs());
            ////List<ActiveCountermeasureStat> activeCmStats = new List<ActiveCountermeasureStat>(activeCMQty);
            ////if (activeCMQty > 0) {
            ////    var srActiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRActiveCountermeasure, Level.One) as ActiveCountermeasureStat;
            ////    activeCmStats.Add(srActiveCmStat);
            ////    if (activeCMQty > 1) {
            ////        var mrActiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.MRActiveCountermeasure, Level.One) as ActiveCountermeasureStat;
            ////        activeCmStats.AddRange(Enumerable.Repeat<ActiveCountermeasureStat>(mrActiveCmStat, activeCMQty - 1));
            ////    }
            ////}
            var activeCmStats = GetActiveCmStats(owner, activeCMLoadout, hullCat.__MaxActiveCMs());

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCat.__MaxSensors());
            if (srSensorQty > 1) {
                ////var srSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRSensor, Level.One) as SensorStat;
                var srSensorStat = ownerDesigns.GetCurrentSRSensorStat();
                optionalSensorStats.AddRange(Enumerable.Repeat<SensorStat>(srSensorStat, srSensorQty - 1));
            }

            ////int shieldGenQty = GetShieldGeneratorQty(shieldGenLoadout, hullCat.__MaxShieldGenerators());
            ////var initialShieldGenStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.ShieldGenerator, Level.One) as ShieldGeneratorStat;
            ////var shieldGenStats = Enumerable.Repeat<ShieldGeneratorStat>(initialShieldGenStat, shieldGenQty);
            var shieldGenStats = GetShieldGenStats(owner, shieldGenLoadout, hullCat.__MaxShieldGenerators());
            Priority hqPriority = hullCat.__HQPriority();    // TEMP, IMPROVE

            var design = MakeElementDesign(owner, hullStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, hqPriority, status);
            designs.Add(design);
        }
        return designs;
    }

    private ShipDesign MakeElementDesign(Player owner, ShipHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> optionalSensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority, ShipCombatStance stance,
        AUnitMemberDesign.SourceAndStatus status) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        ShipHullCategory hullCat = hullStat.HullCategory;
        ////Level engineLevel = DebugControls.Instance.AreShipsFast ? Level.Five : Level.One;
        ////var stlEngineStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.StlPropulsion, engineLevel) as EngineStat;
        ////var ftlEngineStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.FtlPropulsion, engineLevel) as EngineStat;
        var stlEngineStat = GetEngineStat(owner, EquipmentCategory.StlPropulsion);
        var ftlEngineStat = GetEngineStat(owner, EquipmentCategory.FtlPropulsion);  // can be null

        ////var elementsReqdSRSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRSensor, Level.One) as SensorStat;
        var elementsReqdSRSensorStat = ownerDesigns.GetCurrentSRSensorStat();
        var design = new ShipDesign(owner, hqPriority, elementsReqdSRSensorStat, hullStat, stlEngineStat, ftlEngineStat, stance) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            optionalSensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private FacilityDesign MakeElementDesign(Player owner, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> optionalSensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority,
        AUnitMemberDesign.SourceAndStatus status) {


        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        FacilityHullCategory hullCat = hullStat.HullCategory;
        ////var elementsReqdSRSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SRSensor, Level.One) as SensorStat;
        var elementsReqdSRSensorStat = ownerDesigns.GetCurrentSRSensorStat();
        var design = new FacilityDesign(owner, hqPriority, elementsReqdSRSensorStat, hullStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            optionalSensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        D.Log(ShowDebugLog, "{0} has created {1} with {2}, {3}, {4}, {5}, {6} EquipmentStats.",
            DebugName, design.DebugName, weaponStats.Count(), passiveCmStats.Count(), activeCmStats.Count(), optionalSensorStats.Count(),
            shieldGenStats.Count());
        return design;
    }

    /// <summary>
    /// Registers the element design in PlayerDesigns, returning the name the design is registered under.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="design">The design.</param>
    /// <param name="optionalRootDesignName">Name of the optional root design.</param>
    /// <returns></returns>
    private string RegisterElementDesign(Player player, ShipDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = optionalRootDesignName;
            return registeredDesignName;
        }

        if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = rootDesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            ShipDesign existingDesign = playerDesigns.GetShipDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.System_CreationTemplate) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            existingDesign.Status = AUnitMemberDesign.SourceAndStatus.Player_Current;
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2} with name {3}.",
                DebugName, design.DebugName, existingDesign.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    /// <summary>
    /// Registers the element design in PlayerDesigns, returning the name the design is registered under.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="design">The design.</param>
    /// <param name="optionalRootDesignName">Name of the optional root design.</param>
    /// <returns></returns>
    private string RegisterElementDesign(Player player, FacilityDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = optionalRootDesignName;
            return registeredDesignName;
        }

        if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = rootDesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            FacilityDesign existingDesign = playerDesigns.GetFacilityDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.System_CreationTemplate) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            existingDesign.Status = AUnitMemberDesign.SourceAndStatus.Player_Current;
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2} with name {3}.",
                DebugName, design.DebugName, existingDesign.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    #endregion

    #region Command Designs

    private SettlementCmdDesign MakeSettlementCmdDesign(Player owner, int passiveCmQty, int sensorQty,
        AUnitMemberDesign.SourceAndStatus status = AUnitMemberDesign.SourceAndStatus.Player_Current) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;


        ////var initialPassiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.PassiveCountermeasure, Level.One) as PassiveCountermeasureStat;
        ////var passiveCmStats = Enumerable.Repeat<PassiveCountermeasureStat>(initialPassiveCmStat, passiveCmQty);
        var passiveCmStats = GetPassiveCmStats(owner, passiveCmQty);

        SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
        ////SensorStat reqdMrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.MRSensor, Level.One) as SensorStat;
        ////List<SensorStat> optionalSensorStats = new List<SensorStat>();
        ////if (sensorQty > 1) {
        ////    var optionalLrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.LRSensor, Level.One) as SensorStat;
        ////    optionalSensorStats.Add(optionalLrCmdSensorStat);
        ////    if (sensorQty > 2) {
        ////        var availableCmdSensorStats = new List<SensorStat>() { reqdMrCmdSensorStat, optionalLrCmdSensorStat };
        ////        for (int i = 2; i < sensorQty; i++) {
        ////            var randomOptionalCmdSensorStat = RandomExtended.Choice(availableCmdSensorStats) as SensorStat;
        ////            optionalSensorStats.Add(randomOptionalCmdSensorStat);
        ////        }
        ////    }
        ////}
        var optionalSensorStats = GetCmdSensorStats(owner, sensorQty - 1);

        ////FtlDampenerStat cmdsReqdFtlDampener = _eStatFactory.MakeInstance(owner, EquipmentCategory.FtlDampener, Level.One) as FtlDampenerStat;
        ////SettlementCmdModuleStat cmdStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.SettlementCmdModule, Level.One) as SettlementCmdModuleStat;
        FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();
        SettlementCmdModuleStat cmdStat = ownerDesigns.GetCurrentSettlementCmdModuleStat();

        SettlementCmdDesign design = new SettlementCmdDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private StarbaseCmdDesign MakeStarbaseCmdDesign(Player owner, int passiveCmQty, int sensorQty,
        AUnitMemberDesign.SourceAndStatus status = AUnitMemberDesign.SourceAndStatus.Player_Current) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;


        ////var initialPassiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.PassiveCountermeasure, Level.One) as PassiveCountermeasureStat;
        ////var passiveCmStats = Enumerable.Repeat<PassiveCountermeasureStat>(initialPassiveCmStat, passiveCmQty);
        var passiveCmStats = GetPassiveCmStats(owner, passiveCmQty);

        SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
        ////SensorStat reqdMrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.MRSensor, Level.One) as SensorStat;
        ////List<SensorStat> optionalSensorStats = new List<SensorStat>();
        ////if (sensorQty > 1) {
        ////    var optionalLrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.LRSensor, Level.One) as SensorStat;
        ////    optionalSensorStats.Add(optionalLrCmdSensorStat);
        ////    if (sensorQty > 2) {
        ////        var availableCmdSensorStats = new List<SensorStat>() { reqdMrCmdSensorStat, optionalLrCmdSensorStat };
        ////        for (int i = 2; i < sensorQty; i++) {
        ////            var randomOptionalCmdSensorStat = RandomExtended.Choice(availableCmdSensorStats) as SensorStat;
        ////            optionalSensorStats.Add(randomOptionalCmdSensorStat);
        ////        }
        ////    }
        ////}
        var optionalSensorStats = GetCmdSensorStats(owner, sensorQty - 1);

        FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();
        StarbaseCmdModuleStat cmdStat = ownerDesigns.GetCurrentStarbaseCmdModuleStat();
        ////FtlDampenerStat cmdsReqdFtlDampener = _eStatFactory.MakeInstance(owner, EquipmentCategory.FtlDampener, Level.One) as FtlDampenerStat;
        ////StarbaseCmdModuleStat cmdStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.StarbaseCmdModule, Level.One) as StarbaseCmdModuleStat;
        StarbaseCmdDesign design = new StarbaseCmdDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private FleetCmdDesign MakeFleetCmdDesign(Player owner, int passiveCmQty, int sensorQty,
        AUnitMemberDesign.SourceAndStatus status = AUnitMemberDesign.SourceAndStatus.Player_Current) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;


        ////var initialPassiveCmStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.PassiveCountermeasure, Level.One) as PassiveCountermeasureStat;
        ////var passiveCmStats = Enumerable.Repeat<PassiveCountermeasureStat>(initialPassiveCmStat, passiveCmQty);
        var passiveCmStats = GetPassiveCmStats(owner, passiveCmQty);

        SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
        ////SensorStat reqdMrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.MRSensor, Level.One) as SensorStat;
        ////List<SensorStat> optionalSensorStats = new List<SensorStat>();
        ////if (sensorQty > 1) {
        ////    var optionalLrCmdSensorStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.LRSensor, Level.One) as SensorStat;
        ////    optionalSensorStats.Add(optionalLrCmdSensorStat);
        ////    if (sensorQty > 2) {
        ////        var availableCmdSensorStats = new List<SensorStat>() { reqdMrCmdSensorStat, optionalLrCmdSensorStat };
        ////        for (int i = 2; i < sensorQty; i++) {
        ////            var randomOptionalCmdSensorStat = RandomExtended.Choice(availableCmdSensorStats) as SensorStat;
        ////            optionalSensorStats.Add(randomOptionalCmdSensorStat);
        ////        }
        ////    }
        ////}
        var optionalSensorStats = GetCmdSensorStats(owner, sensorQty - 1);

        FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();
        FleetCmdModuleStat cmdStat = ownerDesigns.GetCurrentFleetCmdModuleStat();

        ////FtlDampenerStat cmdsReqdFtlDampener = _eStatFactory.MakeInstance(owner, EquipmentCategory.FtlDampener, Level.One) as FtlDampenerStat;
        ////FleetCmdModuleStat cmdStat = _eStatFactory.MakeInstance(owner, EquipmentCategory.FleetCmdModule, Level.One) as FleetCmdModuleStat;

        FleetCmdDesign design = new FleetCmdDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            EquipmentSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private string RegisterCmdDesign(Player player, StarbaseCmdDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            return rootDesignName;
        }
        StarbaseCmdDesign existingDesign = playerDesigns.GetStarbaseCmdDesign(existingDesignName);
        existingDesign.Status = AUnitMemberDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private string RegisterCmdDesign(Player player, SettlementCmdDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            return rootDesignName;
        }
        SettlementCmdDesign existingDesign = playerDesigns.GetSettlementCmdDesign(existingDesignName);
        existingDesign.Status = AUnitMemberDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    private string RegisterCmdDesign(Player player, FleetCmdDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            return optionalRootDesignName;
        }

        if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            return rootDesignName;
        }
        FleetCmdDesign existingDesign = playerDesigns.GetFleetCmdDesign(existingDesignName);
        existingDesign.Status = AUnitMemberDesign.SourceAndStatus.Player_Current;
        D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
        return existingDesignName;
    }

    #endregion

    #region Support

    private IEnumerable<FacilityHullStat> GetFacilityHullStats(Player player, BaseCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            var hullStats = new List<FacilityHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                FacilityHullStat hullStat; //// = _eStatFactory.MakeInstance(player, hullCat, Level.One);
                if (playerDesigns.TryGetCurrentHullStat(hullCat, out hullStat)) {
                    hullStats.Add(hullStat);
                }
                else {
                    D.Warn("{0}: There is no current {1} available for {2}.{3}.", DebugName, typeof(FacilityHullStat).Name,
                        typeof(FacilityHullCategory).Name, hullCat.GetValueName());
                }
            }
            return hullStats;
        }
        return GetFacilityHullStats(player, settings.NonPresetElementQty);
    }

    private IEnumerable<FacilityHullStat> GetFacilityHullStats(Player player, int qty) {
        ////var allFacilityHullStats = _eStatFactory.GetAllFacilityHullStats(player, Level.One);
        var allCurrentFacilityHullStats = _gameMgr.GetAIManagerFor(player).Designs.GetAllCurrentFacilityHullStats();
        return RandomExtended.Choices<FacilityHullStat>(allCurrentFacilityHullStats, qty);
    }

    private IEnumerable<ShipHullStat> GetShipHullStats(Player player, FleetCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            var hullStats = new List<ShipHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                ShipHullStat hullStat; //// = _eStatFactory.MakeInstance(player, hullCat, Level.One);
                if (playerDesigns.TryGetCurrentHullStat(hullCat, out hullStat)) {
                    hullStats.Add(hullStat);
                }
                else {
                    D.Warn("{0}: There is no current {1} available for {2}.{3}.", DebugName, typeof(ShipHullStat).Name,
                        typeof(ShipHullCategory).Name, hullCat.GetValueName());
                }
            }
            return hullStats;
        }
        return GetShipHullStats(player, settings.NonPresetElementQty);
    }

    private IEnumerable<ShipHullStat> GetShipHullStats(Player player, int qty) {
        ////var allShipHullStats = _eStatFactory.GetAllShipHullStats(player, Level.One);
        var allCurrentShipHullStats = _gameMgr.GetAIManagerFor(player).Designs.GetAllCurrentShipHullStats();
        return RandomExtended.Choices<ShipHullStat>(allCurrentShipHullStats, qty);
    }

    private IEnumerable<AWeaponStat> GetWeaponStats(Player player, ShipHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout,
    DebugLosWeaponLoadout turretLoadout) {

        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;

        int beamsPerElement;
        int projectilesPerElement;
        int hullMaxTurretWeapons = hullCat.MaxTurretMounts();
        DetermineLosWeaponQtyAndMix(turretLoadout, hullMaxTurretWeapons, out beamsPerElement, out projectilesPerElement);

        List<AWeaponStat> weaponStats = new List<AWeaponStat>();
        AWeaponStat beamWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.BeamWeapon, Level.One);

        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.BeamWeapon, out beamWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(beamWeaponStat, beamsPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(BeamWeaponStat).Name);
        }
        AWeaponStat projectileWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.ProjectileWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.ProjectileWeapon, out projectileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(ProjectileWeaponStat).Name);
        }

        ////var weaponStats = Enumerable.Repeat(beamWeaponStat, beamsPerElement).ToList();
        ////weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));

        int missilesPerElement;
        int assaultVehiclesPerElement;
        int hullMaxSiloWeapons = hullCat.MaxSiloMounts();
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullMaxSiloWeapons, out missilesPerElement, out assaultVehiclesPerElement);


        AWeaponStat missileWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.MissileWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.MissileWeapon, out missileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(MissileWeaponStat).Name);
        }

        AWeaponStat assaultWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.AssaultWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.AssaultWeapon, out assaultWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(AssaultWeaponStat).Name);
        }

        ////weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        ////weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        ////return weaponStats.Cast<AWeaponStat>();
        return weaponStats;
    }

    private IEnumerable<AWeaponStat> GetWeaponStats(Player player, FacilityHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout, DebugLosWeaponLoadout turretLoadout) {

        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;

        int beamsPerElement;
        int projectilesPerElement;
        int hullMaxTurretWeapons = hullCat.MaxTurretMounts();
        DetermineLosWeaponQtyAndMix(turretLoadout, hullMaxTurretWeapons, out beamsPerElement, out projectilesPerElement);

        List<AWeaponStat> weaponStats = new List<AWeaponStat>();
        AWeaponStat beamWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.BeamWeapon, Level.One);

        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.BeamWeapon, out beamWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(beamWeaponStat, beamsPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(BeamWeaponStat).Name);
        }

        AWeaponStat projectileWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.ProjectileWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.ProjectileWeapon, out projectileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(ProjectileWeaponStat).Name);
        }

        ////var weaponStats = Enumerable.Repeat(beamWeaponStat, beamsPerElement).ToList();
        ////weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));

        int missilesPerElement;
        int assaultVehiclesPerElement;
        int hullMaxSiloWeapons = hullCat.MaxSiloMounts();
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullMaxSiloWeapons, out missilesPerElement, out assaultVehiclesPerElement);


        AWeaponStat missileWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.MissileWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.MissileWeapon, out missileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(MissileWeaponStat).Name);
        }

        AWeaponStat assaultWeaponStat; //// = _eStatFactory.MakeInstance(player, EquipmentCategory.AssaultWeapon, Level.One);
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.AssaultWeapon, out assaultWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        }
        else {
            D.Warn("{0}: There is no current {1} available.", DebugName, typeof(AssaultWeaponStat).Name);
        }

        ////weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        ////weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        ////return weaponStats.Cast<AWeaponStat>();
        return weaponStats;
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

    private IEnumerable<PassiveCountermeasureStat> GetPassiveCmStats(Player player, DebugPassiveCMLoadout loadout, int maxQty) {
        int qty = GetPassiveCmQty(loadout, maxQty);
        return GetPassiveCmStats(player, qty);
    }

    private IEnumerable<PassiveCountermeasureStat> GetPassiveCmStats(Player player, int qty) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        PassiveCountermeasureStat pStat;
        if (playerDesigns.TryGetCurrentPassiveCmStat(out pStat)) {
            return Enumerable.Repeat(pStat, qty);
        }
        D.Warn("{0}: There is no current {1} available.", DebugName, typeof(PassiveCountermeasureStat).Name);
        return Enumerable.Empty<PassiveCountermeasureStat>();
    }

    private IEnumerable<ActiveCountermeasureStat> GetActiveCmStats(Player player, DebugActiveCMLoadout loadout, int maxQty) {
        int qty = GetActiveCmQty(loadout, maxQty);
        return GetActiveCmStats(player, qty);
    }

    private IEnumerable<ActiveCountermeasureStat> GetActiveCmStats(Player player, int qty) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        ActiveCountermeasureStat srAStat;
        playerDesigns.TryGetCurrentActiveCmStat(EquipmentCategory.SRActiveCountermeasure, out srAStat);
        ActiveCountermeasureStat mrAStat;
        playerDesigns.TryGetCurrentActiveCmStat(EquipmentCategory.MRActiveCountermeasure, out mrAStat);

        Queue<ActiveCountermeasureStat> stats = new Queue<ActiveCountermeasureStat>();
        if (srAStat != null) {
            stats.Enqueue(srAStat);
        }
        else {
            D.Warn("{0}: There is no current SR {1} available.", DebugName, typeof(ActiveCountermeasureStat).Name);
        }
        if (mrAStat != null) {
            stats.Enqueue(mrAStat);
        }
        else {
            D.Warn("{0}: There is no current MR {1} available.", DebugName, typeof(ActiveCountermeasureStat).Name);
        }

        if (stats.Any()) {
            IList<ActiveCountermeasureStat> result = new List<ActiveCountermeasureStat>();
            for (int i = 0; i < qty; i++) {
                var stat = stats.Dequeue();
                result.Add(stat);
                stats.Enqueue(stat);
            }
            return result;
        }
        return Enumerable.Empty<ActiveCountermeasureStat>();
    }

    private IEnumerable<ShieldGeneratorStat> GetShieldGenStats(Player player, DebugShieldGenLoadout loadout, int maxQty) {
        int qty = GetShieldGeneratorQty(loadout, maxQty);
        return GetShieldGenStats(player, qty);
    }

    private IEnumerable<ShieldGeneratorStat> GetShieldGenStats(Player player, int qty) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        ShieldGeneratorStat sgStat;
        if (playerDesigns.TryGetCurrentShieldGeneratorStat(out sgStat)) {
            return Enumerable.Repeat(sgStat, qty);
        }
        D.Warn("{0}: There is no current {1} available.", DebugName, typeof(ShieldGeneratorStat).Name);
        return Enumerable.Empty<ShieldGeneratorStat>();
    }

    private EngineStat GetEngineStat(Player player, EquipmentCategory engineCat) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        if (engineCat == EquipmentCategory.StlPropulsion) {
            return playerDesigns.GetCurrentStlEngineStat();
        }

        D.AssertEqual(EquipmentCategory.FtlPropulsion, engineCat);
        EngineStat ftlEngineStat;
        playerDesigns.TryGetCurrentFtlEngineStat(out ftlEngineStat);
        if (ftlEngineStat == null) {
            D.Warn("{0}: There is no current FTL EngineStat available.", DebugName);
        }
        return ftlEngineStat;   // can be null
    }

    private IEnumerable<SensorStat> GetCmdSensorStats(Player player, int optionalQty) {
        if (optionalQty > Constants.Zero) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            SensorStat mrStat = playerDesigns.GetCurrentMRCmdSensorStat();
            SensorStat lrStat;
            playerDesigns.TryGetCurrentLRCmdSensorStat(out lrStat);

            if (optionalQty == Constants.One) {
                SensorStat singleStatToReturn = lrStat != null ? lrStat : mrStat;
                return new List<SensorStat>() { singleStatToReturn };
            }

            Queue<SensorStat> stats = new Queue<SensorStat>();
            if (lrStat != null) {
                stats.Enqueue(lrStat);
            }
            else {
                D.Warn("{0}: There is no current LR SensorStat available.", DebugName);
            }
            stats.Enqueue(mrStat);

            IList<SensorStat> result = new List<SensorStat>();
            for (int i = 0; i < optionalQty; i++) {
                var stat = stats.Dequeue();
                result.Add(stat);
                stats.Enqueue(stat);
            }
            return result;
        }
        return Enumerable.Empty<SensorStat>();
    }


    private int GetPassiveCmQty(DebugPassiveCMLoadout loadout, int maxAllowed) {
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

    private int GetActiveCmQty(DebugActiveCMLoadout loadout, int maxAllowed) {
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

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateOwner(Player owner, AUnitCreatorEditorSettings editorSettings) {
        if (owner.IsUser) {
            D.Assert(editorSettings.IsOwnerUser);
        }
        else {
            D.AssertEqual(owner.__InitialUserRelationship, editorSettings.DesiredRelationshipWithUser.Convert());
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some elements can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, ShipDesign designToConsiderUpgrading) {
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            ShipDesign upgradedDesign = new ShipDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteShipDesign(designToObsolete.DesignName);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some elements can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, FacilityDesign designToConsiderUpgrading) {
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            FacilityDesign upgradedDesign = new FacilityDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteFacilityDesign(designToObsolete.DesignName);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    #endregion

}

