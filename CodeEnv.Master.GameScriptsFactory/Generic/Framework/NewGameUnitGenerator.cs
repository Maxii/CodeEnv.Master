// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameUnitGenerator.cs
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
public class NewGameUnitGenerator {

    private const string RootDesignNameFormat = "{0}{1}";

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
        var designName = RootDesignNameFormat.Inject(hullCategoryName, _rootDesignNameCounter);
        _rootDesignNameCounter++;
        return designName;
    }

    private static string GetUniqueCmdRootDesignName() {
        var designName = RootDesignNameFormat.Inject("Cmd", _rootDesignNameCounter);
        _rootDesignNameCounter++;
        return designName;
    }

    public string DebugName { get { return GetType().Name; } }

    private bool ShowDebugLog { get { return __debugCntls.ShowDeploymentDebugLogs; } }

    private GameManager _gameMgr;
    private DebugControls __debugCntls;

    public NewGameUnitGenerator() {
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        __debugCntls = DebugControls.Instance;
    }

    /// <summary>
    /// Creates and registers any required designs including a design for the LoneFleetCmd and designs
    /// for empty ships, facilities and cmds for use when creating new designs from scratch.
    /// </summary>
    public void CreateAndRegisterRequiredDesigns() {
        foreach (var player in _gameMgr.AllPlayers) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;

            var allCurrentShipHullStats = playerDesigns.GetAllCurrentShipHullStats();
            var currentShipTemplateDesigns = MakeShipDesigns(player, allCurrentShipHullStats, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None, new ShipCombatStance[] { ShipCombatStance.BalancedBombard },
                AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, includeFtlEngine: false);
            foreach (var shipDesign in currentShipTemplateDesigns) {
                RegisterDesign(player, shipDesign, optionalRootDesignName: shipDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            var allCurrentFacilityHullStats = playerDesigns.GetAllCurrentFacilityHullStats();
            var currentFacilityTemplateDesigns = MakeFacilityDesigns(player, allCurrentFacilityHullStats, DebugLosWeaponLoadout.None,
                DebugLaunchedWeaponLoadout.None, DebugPassiveCMLoadout.None, DebugActiveCMLoadout.None, DebugSensorLoadout.One,
                DebugShieldGenLoadout.None, AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            foreach (var facilityDesign in currentFacilityTemplateDesigns) {
                RegisterDesign(player, facilityDesign, optionalRootDesignName: facilityDesign.HullCategory.GetEmptyTemplateDesignName());
            }

            FleetCmdModuleDesign currentFleetCmdTemplateDesign = MakeFleetCmdModDesign(player, passiveCmQty: 0, sensorQty: 1,
                status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            RegisterDesign(player, currentFleetCmdTemplateDesign, optionalRootDesignName: TempGameValues.FleetCmdModTemplateRootDesignName);

            SettlementCmdModuleDesign currentSettlementCmdTemplateDesign = MakeSettlementCmdModDesign(player, passiveCmQty: 0, sensorQty: 1,
                status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Template);
            RegisterDesign(player, currentSettlementCmdTemplateDesign, optionalRootDesignName: TempGameValues.SettlementCmdModTemplateRootDesignName);

            StarbaseCmdModuleDesign currentStarbaseCmdTemplateDesign;
            if (TryMakeStarbaseCmdModDesign(player, passiveCmQty: 0, sensorQty: 1, status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Template,
                design: out currentStarbaseCmdTemplateDesign)) {
                RegisterDesign(player, currentStarbaseCmdTemplateDesign, TempGameValues.StarbaseCmdModTemplateRootDesignName);
            }

            FleetCmdModuleDesign fleetCmdModDefaultDesign = MakeFleetCmdModDesign(player, passiveCmQty: 0, sensorQty: 1,
                status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            RegisterDesign(player, fleetCmdModDefaultDesign, optionalRootDesignName: TempGameValues.FleetCmdModDefaultRootDesignName);

            SettlementCmdModuleDesign settlementCmdModDefaultDesign = MakeSettlementCmdModDesign(player, passiveCmQty: 0, sensorQty: 1,
                status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            RegisterDesign(player, settlementCmdModDefaultDesign, optionalRootDesignName: TempGameValues.SettlementCmdModDefaultRootDesignName);

            StarbaseCmdModuleDesign starbaseCmdModDefaultDesign;
            if (TryMakeStarbaseCmdModDesign(player, passiveCmQty: 0, sensorQty: 1, status: AUnitMemberDesign.SourceAndStatus.SystemCreation_Default, design: out starbaseCmdModDefaultDesign)) {
                RegisterDesign(player, starbaseCmdModDefaultDesign, optionalRootDesignName: TempGameValues.StarbaseCmdModDefaultRootDesignName);
            }

        }
    }

    #region Configure Existing Creators

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
        FleetCmdModuleDesign cmdModDesign = MakeFleetCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        var hullStats = GetCurrentShipHullStats(owner, editorSettings);
        IEnumerable<ShipCombatStance> stances = SelectCombatStances(editorSettings.StanceExclusions);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement, stances, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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

        SettlementCmdModuleDesign cmdModDesign = MakeSettlementCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        var hullStats = GetCurrentFacilityHullStats(owner, editorSettings);

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
            editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
            editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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
    /// Returns <c>true</c> if a configuration is assigned to the provided existing DebugStarbaseCreator using the DeployDate provided, <c>false</c> otherwise.
    /// <remarks>The DebugCreator's EditorSettings specifying the DeployDate will be ignored.</remarks>
    /// <remarks>4.27.18 Currently, the only reason a configuration would not be assigned is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    public bool TryAssignConfigurationToExistingCreator(DebugStarbaseCreator creator, Player owner, Vector3 location, GameDate deployDate) {
        var editorSettings = creator.EditorSettings as BaseCreatorEditorSettings;

        __ValidateOwner(owner, editorSettings);

        int cmdPassiveCMQty = GetPassiveCmQty(editorSettings.CMsPerCommand, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(editorSettings.SensorsPerCommand, TempGameValues.__MaxCmdSensors);
        StarbaseCmdModuleDesign cmdModDesign;
        if (TryMakeStarbaseCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current, out cmdModDesign)) {
            string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

            var hullStats = GetCurrentFacilityHullStats(owner, editorSettings);

            IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, editorSettings.LosTurretLoadout,
                editorSettings.LauncherLoadout, editorSettings.PassiveCMsPerElement, editorSettings.ActiveCMsPerElement,
                editorSettings.SRSensorsPerElement, editorSettings.ShieldGeneratorsPerElement, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

            IList<string> elementDesignNames = new List<string>();
            foreach (var design in elementDesigns) {
                string registeredDesignName = RegisterDesign(owner, design);
                elementDesignNames.Add(registeredDesignName);
            }

            UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
            creator.Configuration = config;
            creator.transform.position = location;
            //D.Log(ShowDebugLog, "{0} has placed a {1} for {2}.", DebugName, typeof(DebugStarbaseCreator).Name, owner);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if a configuration is assigned to the provided existing DebugStarbaseCreator using the DeployDate specified by
    /// the DebugCreator, <c>false</c> otherwise.
    /// <remarks>4.27.18 Currently, the only reason a configuration would not be assigned is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    public bool TryAssignConfigurationToExistingCreator(DebugStarbaseCreator creator, Player owner, Vector3 location) {
        GameDate deployDate = creator.EditorSettings.DateToDeploy;
        return TryAssignConfigurationToExistingCreator(creator, owner, location, deployDate);
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
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, __debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(__debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(__debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        FleetCmdModuleDesign cmdModDesign = MakeFleetCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetCurrentShipHullStats(owner, elementQty);
        var turretLoadout = __debugCntls.LosWeaponsPerElement;
        var launchedLoadout = __debugCntls.LaunchedWeaponsPerElement;
        var passiveCMLoadout = __debugCntls.PassiveCMsPerElement;
        var activeCMLoadout = __debugCntls.ActiveCMsPerElement;
        var srSensorLoadout = __debugCntls.SRSensorsPerElement;
        var shieldGenLoadout = __debugCntls.ShieldGeneratorsPerElement;
        var combatStances = Enums<ShipCombatStance>.GetValues(excludeDefault: true);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, combatStances, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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
    /// Generates a preset settlement creator, places it in orbit around <c>system</c> and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public AutoSettlementCreator GeneratePresetAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, __debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(__debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(__debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        SettlementCmdModuleDesign cmdModDesign = MakeSettlementCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetCurrentFacilityHullStats(owner, elementQty);
        var turretLoadout = __debugCntls.LosWeaponsPerElement;
        var launchedLoadout = __debugCntls.LaunchedWeaponsPerElement;
        var passiveCMLoadout = __debugCntls.PassiveCMsPerElement;
        var activeCMLoadout = __debugCntls.ActiveCMsPerElement;
        var srSensorLoadout = __debugCntls.SRSensorsPerElement;
        var shieldGenLoadout = __debugCntls.ShieldGeneratorsPerElement;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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

    /// <summary>
    /// Returns <c>true</c> if a preset starbase creator is generated, <c>false</c> otherwise.
    /// The creator is placed at location and deploys it on the provided date.
    /// <remarks>4.27.18 Currently, the only reason a Creator would not be generated is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <param name="creator">The resulting creator.</param>
    /// <returns></returns>
    public bool TryGeneratePresetAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate, out AutoStarbaseCreator creator) {
        D.AssertEqual(DebugControls.EquipmentLoadout.Preset, __debugCntls.EquipmentPlan);
        int cmdPassiveCMQty = GetPassiveCmQty(__debugCntls.CMsPerCmd, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(__debugCntls.SensorsPerCmd, TempGameValues.__MaxCmdSensors);
        StarbaseCmdModuleDesign cmdModDesign;
        if (TryMakeStarbaseCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current, out cmdModDesign)) {
            string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

            int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
            var hullStats = GetCurrentFacilityHullStats(owner, elementQty);
            var turretLoadout = __debugCntls.LosWeaponsPerElement;
            var launchedLoadout = __debugCntls.LaunchedWeaponsPerElement;
            var passiveCMLoadout = __debugCntls.PassiveCMsPerElement;
            var activeCMLoadout = __debugCntls.ActiveCMsPerElement;
            var srSensorLoadout = __debugCntls.SRSensorsPerElement;
            var shieldGenLoadout = __debugCntls.ShieldGeneratorsPerElement;

            IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
                activeCMLoadout, srSensorLoadout, shieldGenLoadout, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

            IList<string> elementDesignNames = new List<string>();
            foreach (var design in elementDesigns) {
                string registeredDesignName = RegisterDesign(owner, design);
                elementDesignNames.Add(registeredDesignName);
            }

            UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
            //D.Log(ShowDebugLog, "{0} has generated/placed a preset {1} for {2}.", DebugName, typeof(AutoStarbaseCreator).Name, owner);
            creator = UnitFactory.Instance.MakeStarbaseCreator(location, config);
            return true;
        }
        creator = null;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if a preset starbase creator is generated, <c>false</c> otherwise.
    /// The creator is placed at location and deploys it on a random date.
    /// <remarks>4.27.18 Currently, the only reason a Creator would not be generated is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="creator">The resulting creator.</param>
    /// <returns></returns>
    public bool TryGeneratePresetAutoStarbaseCreator(Player owner, Vector3 location, out AutoStarbaseCreator creator) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0.1F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return TryGeneratePresetAutoStarbaseCreator(owner, location, deployDate, out creator);
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
        FleetCmdModuleDesign cmdModDesign = MakeFleetCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxShipsPerFleet);
        var hullStats = GetCurrentShipHullStats(owner, elementQty);
        var turretLoadout = DebugLosWeaponLoadout.Random;
        var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
        var passiveCMLoadout = DebugPassiveCMLoadout.Random;
        var activeCMLoadout = DebugActiveCMLoadout.Random;
        var srSensorLoadout = DebugSensorLoadout.Random;
        var shieldGenLoadout = DebugShieldGenLoadout.Random;
        var combatStances = Enums<ShipCombatStance>.GetValues(excludeDefault: true);

        IList<ShipDesign> elementDesigns = MakeShipDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, combatStances, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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
    /// Generates a random settlement creator, places it in orbit around <c>system</c> and deploys it on the provided date.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="system">The system.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <returns></returns>
    public AutoSettlementCreator GenerateRandomAutoSettlementCreator(Player owner, SystemItem system, GameDate deployDate) {
        int cmdPassiveCMQty = GetPassiveCmQty(DebugPassiveCMLoadout.Random, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.__MaxCmdSensors);
        SettlementCmdModuleDesign cmdModDesign = MakeSettlementCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);
        string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

        int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
        var hullStats = GetCurrentFacilityHullStats(owner, elementQty);
        var turretLoadout = DebugLosWeaponLoadout.Random;
        var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
        var passiveCMLoadout = DebugPassiveCMLoadout.Random;
        var activeCMLoadout = DebugActiveCMLoadout.Random;
        var srSensorLoadout = DebugSensorLoadout.Random;
        var shieldGenLoadout = DebugShieldGenLoadout.Random;

        IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
            activeCMLoadout, srSensorLoadout, shieldGenLoadout, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

        IList<string> elementDesignNames = new List<string>();
        foreach (var design in elementDesigns) {
            string registeredDesignName = RegisterDesign(owner, design);
            elementDesignNames.Add(registeredDesignName);
        }

        UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
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

    /// <summary>
    /// Returns <c>true</c> if a random starbase creator is generated, <c>false</c> otherwise.
    /// The creator is placed at location and deploys it on the provided date.
    /// <remarks>4.27.18 Currently, the only reason a Creator would not be generated is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="deployDate">The deploy date.</param>
    /// <param name="creator">The resulting creator.</param>
    /// <returns></returns>
    public bool TryGenerateRandomAutoStarbaseCreator(Player owner, Vector3 location, GameDate deployDate, out AutoStarbaseCreator creator) {
        int cmdPassiveCMQty = GetPassiveCmQty(DebugPassiveCMLoadout.Random, TempGameValues.__MaxCmdPassiveCMs);
        int cmdSensorQty = GetSensorQty(DebugSensorLoadout.Random, TempGameValues.__MaxCmdSensors);
        StarbaseCmdModuleDesign cmdModDesign;
        if (TryMakeStarbaseCmdModDesign(owner, cmdPassiveCMQty, cmdSensorQty, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current, out cmdModDesign)) {
            string cmdModDesignName = RegisterDesign(owner, cmdModDesign);

            int elementQty = RandomExtended.Range(1, TempGameValues.MaxFacilitiesPerBase);
            var hullStats = GetCurrentFacilityHullStats(owner, elementQty);
            var turretLoadout = DebugLosWeaponLoadout.Random;
            var launchedLoadout = DebugLaunchedWeaponLoadout.Random;
            var passiveCMLoadout = DebugPassiveCMLoadout.Random;
            var activeCMLoadout = DebugActiveCMLoadout.Random;
            var srSensorLoadout = DebugSensorLoadout.Random;
            var shieldGenLoadout = DebugShieldGenLoadout.Random;

            IList<FacilityDesign> elementDesigns = MakeFacilityDesigns(owner, hullStats, turretLoadout, launchedLoadout, passiveCMLoadout,
                activeCMLoadout, srSensorLoadout, shieldGenLoadout, AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current);

            IList<string> elementDesignNames = new List<string>();
            foreach (var design in elementDesigns) {
                string registeredDesignName = RegisterDesign(owner, design);
                elementDesignNames.Add(registeredDesignName);
            }

            UnitCreatorConfiguration config = new UnitCreatorConfiguration(owner, deployDate, cmdModDesignName, elementDesignNames);
            //D.Log(ShowDebugLog, "{0} has generated/placed a random {1} for {2}.", DebugName, typeof(AutoStarbaseCreator).Name, owner);
            creator = UnitFactory.Instance.MakeStarbaseCreator(location, config);
            return true;
        }
        creator = null;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if a random starbase creator is generated, <c>false</c> otherwise.
    /// The creator is placed at location and deploys it on a random date.
    /// <remarks>4.27.18 Currently, the only reason a Creator would not be generated is there is no current StarbaseCmdModuleStat.</remarks>
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="location">The location.</param>
    /// <param name="creator">The resulting creator.</param>
    /// <returns></returns>
    public bool TryGenerateRandomAutoStarbaseCreator(Player owner, Vector3 location, out AutoStarbaseCreator creator) {
        GameTimeDuration deployDateDelay = new GameTimeDuration(UnityEngine.Random.Range(Constants.ZeroF, 3F));
        //GameTimeDuration deployDateDelay = new GameTimeDuration(0.1F);
        GameDate deployDate = GameTime.Instance.GenerateRandomFutureDate(deployDateDelay);
        return TryGenerateRandomAutoStarbaseCreator(owner, location, deployDate, out creator);
    }

    #endregion

    public void ResetForReuse() {
        _rootDesignNameCounter = Constants.One;
    }

    #region Element Designs

    private IList<ShipDesign> MakeShipDesigns(Player owner, IEnumerable<ShipHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout, IEnumerable<ShipCombatStance> stances,
        AUnitMemberDesign.SourceAndStatus status, bool includeFtlEngine = true) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        IList<ShipDesign> designs = new List<ShipDesign>();
        foreach (var hullStat in hullStats) {
            ShipHullCategory hullCat = hullStat.HullCategory;

            var weaponStats = GetCurrentWeaponStats(owner, hullCat, launchedLoadout, turretLoadout);
            var passiveCmStats = GetCurrentPassiveCmStats(owner, passiveCMLoadout, hullCat.__MaxPassiveCMs());
            var activeCmStats = GetCurrentActiveCmStats(owner, activeCMLoadout, hullCat.__MaxActiveCMs());

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCat.__MaxSensors());
            if (srSensorQty > 1) {
                var srSensorStat = ownerDesigns.GetCurrentSRSensorStat();
                optionalSensorStats.AddRange(Enumerable.Repeat<SensorStat>(srSensorStat, srSensorQty - 1));
            }

            var shieldGenStats = GetCurrentShieldGenStats(owner, shieldGenLoadout, hullCat.__MaxShieldGenerators());
            ShipCombatStance stance = RandomExtended.Choice(stances);

            EngineStat ftlEngineStat = null;
            if (includeFtlEngine) {
                ftlEngineStat = GetCurrentEngineStat(owner, EquipmentCategory.FtlPropulsion);
                if (ftlEngineStat == null) {
                    D.Warn("{0}: Cannot install FtlEngineStat in ship as it is not yet available.", DebugName);
                }
            }

            var design = MakeElementDesign(owner, hullStat, ftlEngineStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, stance, status);
            designs.Add(design);
        }
        return designs;
    }

    private IList<FacilityDesign> MakeFacilityDesigns(Player owner, IEnumerable<FacilityHullStat> hullStats, DebugLosWeaponLoadout turretLoadout,
        DebugLaunchedWeaponLoadout launchedLoadout, DebugPassiveCMLoadout passiveCMLoadout, DebugActiveCMLoadout activeCMLoadout,
        DebugSensorLoadout srSensorLoadout, DebugShieldGenLoadout shieldGenLoadout, AUnitMemberDesign.SourceAndStatus status) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        IList<FacilityDesign> designs = new List<FacilityDesign>();
        foreach (var hullStat in hullStats) {
            FacilityHullCategory hullCat = hullStat.HullCategory;

            var weaponStats = GetCurrentWeaponStats(owner, hullCat, launchedLoadout, turretLoadout);
            var passiveCmStats = GetCurrentPassiveCmStats(owner, passiveCMLoadout, hullCat.__MaxPassiveCMs());
            var activeCmStats = GetCurrentActiveCmStats(owner, activeCMLoadout, hullCat.__MaxActiveCMs());

            List<SensorStat> optionalSensorStats = new List<SensorStat>();
            int srSensorQty = GetSensorQty(srSensorLoadout, hullCat.__MaxSensors());
            if (srSensorQty > 1) {
                var srSensorStat = ownerDesigns.GetCurrentSRSensorStat();
                optionalSensorStats.AddRange(Enumerable.Repeat<SensorStat>(srSensorStat, srSensorQty - 1));
            }

            var shieldGenStats = GetCurrentShieldGenStats(owner, shieldGenLoadout, hullCat.__MaxShieldGenerators());

            var design = MakeElementDesign(owner, hullStat, weaponStats, passiveCmStats, activeCmStats, optionalSensorStats,
                shieldGenStats, status);
            designs.Add(design);
        }
        return designs;
    }

    private ShipDesign MakeElementDesign(Player owner, ShipHullStat hullStat, EngineStat ftlEngineStat, IEnumerable<AWeaponStat> weaponStats,
    IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
    IEnumerable<SensorStat> optionalSensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, ShipCombatStance stance,
    AUnitMemberDesign.SourceAndStatus status) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        var stlEngineStat = GetCurrentEngineStat(owner, EquipmentCategory.StlPropulsion);

        var elementsReqdSRSensorStat = ownerDesigns.GetCurrentSRSensorStat();
        var design = new ShipDesign(owner, elementsReqdSRSensorStat, hullStat, stlEngineStat, stance) {
            Status = status
        };

        IList<AEquipmentStat> allOptEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            optionalSensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToList();
        if (ftlEngineStat != null) {
            allOptEquipStats.Add(ftlEngineStat);
        }

        foreach (var stat in allOptEquipStats) {
            OptionalEquipSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private FacilityDesign MakeElementDesign(Player owner, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> optionalSensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, AUnitMemberDesign.SourceAndStatus status) {

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        var elementsReqdSRSensorStat = ownerDesigns.GetCurrentSRSensorStat();
        var design = new FacilityDesign(owner, elementsReqdSRSensorStat, hullStat) {
            Status = status
        };
        AEquipmentStat[] allOptEquipStats = passiveCmStats.Cast<AEquipmentStat>().UnionBy(activeCmStats.Cast<AEquipmentStat>(),
            optionalSensorStats.Cast<AEquipmentStat>(), shieldGenStats.Cast<AEquipmentStat>(), weaponStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allOptEquipStats) {
            OptionalEquipSlotID availCatSlotID;
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
    private string RegisterDesign(Player player, ShipDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
        }
        else if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            ShipDesign existingDesign = playerDesigns.__GetShipDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            D.AssertNotDefault((int)existingDesign.Status);
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
    private string RegisterDesign(Player player, FacilityDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
        }
        else if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueElementRootDesignName(design.HullCategory.GetValueName());
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            FacilityDesign existingDesign = playerDesigns.__GetFacilityDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            D.AssertNotDefault((int)existingDesign.Status);
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2} with name {3}.",
                DebugName, design.DebugName, existingDesign.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    #endregion

    #region Command Designs

    private FleetCmdModuleDesign MakeFleetCmdModDesign(Player owner, int passiveCmQty, int sensorQty, AUnitMemberDesign.SourceAndStatus status) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;

        var passiveCmStats = GetCurrentPassiveCmStats(owner, passiveCmQty);

        SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
        var optionalSensorStats = GetCurrentOptionalCmdSensorStats(owner, sensorQty - 1);

        FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();
        FleetCmdModuleStat cmdStat = ownerDesigns.GetCurrentFleetCmdModuleStat();

        FleetCmdModuleDesign design = new FleetCmdModuleDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            OptionalEquipSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private SettlementCmdModuleDesign MakeSettlementCmdModDesign(Player owner, int passiveCmQty, int sensorQty, AUnitMemberDesign.SourceAndStatus status) {
        Utility.ValidateForRange(passiveCmQty, 0, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, 1, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;

        var passiveCmStats = GetCurrentPassiveCmStats(owner, passiveCmQty);

        SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
        var optionalSensorStats = GetCurrentOptionalCmdSensorStats(owner, sensorQty - 1);

        FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();
        SettlementCmdModuleStat cmdStat = ownerDesigns.GetCurrentSettlementCmdModuleStat();

        SettlementCmdModuleDesign design = new SettlementCmdModuleDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
            Status = status
        };
        AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
        foreach (var stat in allEquipStats) {
            OptionalEquipSlotID availCatSlotID;
            bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
            D.Assert(isSlotAvailable);
            design.Add(availCatSlotID, stat);
        }
        design.AssignPropertyValues();
        return design;
    }

    private bool TryMakeStarbaseCmdModDesign(Player owner, int passiveCmQty, int sensorQty, AUnitMemberDesign.SourceAndStatus status,
        out StarbaseCmdModuleDesign design) {
        Utility.ValidateForRange(passiveCmQty, Constants.Zero, TempGameValues.__MaxCmdPassiveCMs);
        Utility.ValidateForRange(sensorQty, Constants.One, TempGameValues.__MaxCmdSensors);

        var ownerDesigns = _gameMgr.GetAIManagerFor(owner).Designs;
        StarbaseCmdModuleStat cmdStat;
        if (ownerDesigns.TryGetCurrentStarbaseCmdModuleStat(out cmdStat)) {
            var passiveCmStats = GetCurrentPassiveCmStats(owner, passiveCmQty);

            SensorStat reqdMrCmdSensorStat = ownerDesigns.GetCurrentMRCmdSensorStat();
            var optionalSensorStats = GetCurrentOptionalCmdSensorStats(owner, sensorQty - 1);

            FtlDampenerStat cmdsReqdFtlDampenerStat = ownerDesigns.GetCurrentFtlDampenerStat();

            design = new StarbaseCmdModuleDesign(owner, cmdsReqdFtlDampenerStat, cmdStat, reqdMrCmdSensorStat) {
                Status = status
            };
            AEquipmentStat[] allEquipStats = passiveCmStats.Cast<AEquipmentStat>().Union(optionalSensorStats.Cast<AEquipmentStat>()).ToArray();
            foreach (var stat in allEquipStats) {
                OptionalEquipSlotID availCatSlotID;
                bool isSlotAvailable = design.TryGetEmptySlotIDFor(stat.Category, out availCatSlotID);
                D.Assert(isSlotAvailable);
                design.Add(availCatSlotID, stat);
            }
            design.AssignPropertyValues();
            return true;
        }
        design = null;
        return false;
    }

    private string RegisterDesign(Player player, FleetCmdModuleDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
        }
        else if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            FleetCmdModuleDesign existingDesign = playerDesigns.__GetFleetCmdModDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            D.AssertNotDefault((int)existingDesign.Status);
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    private string RegisterDesign(Player player, SettlementCmdModuleDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
        }
        else if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            SettlementCmdModuleDesign existingDesign = playerDesigns.__GetSettlementCmdModDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            D.AssertNotDefault((int)existingDesign.Status);
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    private string RegisterDesign(Player player, StarbaseCmdModuleDesign design, string optionalRootDesignName = null) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        string registeredDesignName;
        string existingDesignName;
        if (optionalRootDesignName != null) {
            bool isDesignAlreadyRegistered = playerDesigns.IsDesignPresent(design, out existingDesignName);
            D.Assert(!isDesignAlreadyRegistered);
            design.RootDesignName = optionalRootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
        }
        else if (!playerDesigns.IsDesignPresent(design, out existingDesignName)) {
            string rootDesignName = GetUniqueCmdRootDesignName();
            design.RootDesignName = rootDesignName;
            playerDesigns.Add(design);
            registeredDesignName = design.DesignName;
            __CreateAndRegisterRandomUpgradedDesign(player, design);
        }
        else {
            D.AssertNotNull(existingDesignName);
            StarbaseCmdModuleDesign existingDesign = playerDesigns.__GetStarbaseCmdModDesign(existingDesignName);
            if (existingDesign.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Template) {
                D.Warn("{0}: {1} and TemplateDesign {2} are equivalent?", DebugName, design.DebugName, existingDesign.DebugName);
            }
            D.AssertNotDefault((int)existingDesign.Status);
            D.Log(ShowDebugLog, "{0} found Design {1} has equivalent already registered so using {2}.", DebugName, design.DebugName, existingDesignName);
            registeredDesignName = existingDesignName;
        }
        return registeredDesignName;
    }

    #endregion

    #region Support

    private IEnumerable<FacilityHullStat> GetCurrentFacilityHullStats(Player player, BaseCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            var hullStats = new List<FacilityHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                FacilityHullStat hullStat;
                if (playerDesigns.TryGetCurrentHullStat(hullCat, out hullStat)) {
                    hullStats.Add(hullStat);
                }
                else {
                    D.Log(ShowDebugLog, "{0}: There is no current {1} available for {2}.{3}.", DebugName, typeof(FacilityHullStat).Name,
                        typeof(FacilityHullCategory).Name, hullCat.GetValueName());
                }
            }
            return hullStats;
        }
        return GetCurrentFacilityHullStats(player, settings.NonPresetElementQty);
    }

    private IEnumerable<FacilityHullStat> GetCurrentFacilityHullStats(Player player, int qty) {
        var allCurrentFacilityHullStats = _gameMgr.GetAIManagerFor(player).Designs.GetAllCurrentFacilityHullStats();
        return RandomExtended.Choices<FacilityHullStat>(allCurrentFacilityHullStats, qty);
    }

    private IEnumerable<ShipHullStat> GetCurrentShipHullStats(Player player, FleetCreatorEditorSettings settings) {
        if (settings.IsCompositionPreset) {
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            var hullStats = new List<ShipHullStat>();
            foreach (var hullCat in settings.PresetElementHullCategories) {
                ShipHullStat hullStat;
                if (playerDesigns.TryGetCurrentHullStat(hullCat, out hullStat)) {
                    hullStats.Add(hullStat);
                }
                else {
                    D.Log(ShowDebugLog, "{0}: There is no current {1} available for {2}.{3}.", DebugName, typeof(ShipHullStat).Name,
                        typeof(ShipHullCategory).Name, hullCat.GetValueName());
                }
            }
            return hullStats;
        }
        return GetCurrentShipHullStats(player, settings.NonPresetElementQty);
    }

    private IEnumerable<ShipHullStat> GetCurrentShipHullStats(Player player, int qty) {
        var allCurrentShipHullStats = _gameMgr.GetAIManagerFor(player).Designs.GetAllCurrentShipHullStats();
        return RandomExtended.Choices<ShipHullStat>(allCurrentShipHullStats, qty);
    }

    private IEnumerable<AWeaponStat> GetCurrentWeaponStats(Player player, ShipHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout,
    DebugLosWeaponLoadout turretLoadout) {

        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;

        int beamsPerElement;
        int projectilesPerElement;
        int hullMaxTurretWeapons = hullCat.MaxTurretMounts();
        DetermineLosWeaponQtyAndMix(turretLoadout, hullMaxTurretWeapons, out beamsPerElement, out projectilesPerElement);

        List<AWeaponStat> weaponStats = new List<AWeaponStat>();
        AWeaponStat beamWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.BeamWeapon, out beamWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(beamWeaponStat, beamsPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(BeamWeaponStat).Name);
        }
        AWeaponStat projectileWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.ProjectileWeapon, out projectileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(ProjectileWeaponStat).Name);
        }

        int missilesPerElement;
        int assaultVehiclesPerElement;
        int hullMaxSiloWeapons = hullCat.MaxSiloMounts();
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullMaxSiloWeapons, out missilesPerElement, out assaultVehiclesPerElement);

        AWeaponStat missileWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.MissileWeapon, out missileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(MissileWeaponStat).Name);
        }

        AWeaponStat assaultWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.AssaultWeapon, out assaultWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(AssaultWeaponStat).Name);
        }

        return weaponStats;
    }

    private IEnumerable<AWeaponStat> GetCurrentWeaponStats(Player player, FacilityHullCategory hullCat, DebugLaunchedWeaponLoadout launchedLoadout,
        DebugLosWeaponLoadout turretLoadout) {

        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;

        int beamsPerElement;
        int projectilesPerElement;
        int hullMaxTurretWeapons = hullCat.MaxTurretMounts();
        DetermineLosWeaponQtyAndMix(turretLoadout, hullMaxTurretWeapons, out beamsPerElement, out projectilesPerElement);

        List<AWeaponStat> weaponStats = new List<AWeaponStat>();
        AWeaponStat beamWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.BeamWeapon, out beamWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(beamWeaponStat, beamsPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(BeamWeaponStat).Name);
        }

        AWeaponStat projectileWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.ProjectileWeapon, out projectileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(projectileWeaponStat, projectilesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(ProjectileWeaponStat).Name);
        }

        int missilesPerElement;
        int assaultVehiclesPerElement;
        int hullMaxSiloWeapons = hullCat.MaxSiloMounts();
        DetermineLaunchedWeaponQtyAndMix(launchedLoadout, hullMaxSiloWeapons, out missilesPerElement, out assaultVehiclesPerElement);


        AWeaponStat missileWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.MissileWeapon, out missileWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(missileWeaponStat, missilesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(MissileWeaponStat).Name);
        }

        AWeaponStat assaultWeaponStat;
        if (playerDesigns.TryGetCurrentWeaponStat(EquipmentCategory.AssaultWeapon, out assaultWeaponStat)) {
            weaponStats.AddRange(Enumerable.Repeat(assaultWeaponStat, assaultVehiclesPerElement));
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(AssaultWeaponStat).Name);
        }

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

    private IEnumerable<PassiveCountermeasureStat> GetCurrentPassiveCmStats(Player player, DebugPassiveCMLoadout loadout, int maxQty) {
        int qty = GetPassiveCmQty(loadout, maxQty);
        return GetCurrentPassiveCmStats(player, qty);
    }

    private IEnumerable<PassiveCountermeasureStat> GetCurrentPassiveCmStats(Player player, int qty) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        PassiveCountermeasureStat pStat;
        if (playerDesigns.TryGetCurrentPassiveCmStat(out pStat)) {
            return Enumerable.Repeat(pStat, qty);
        }
        D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(PassiveCountermeasureStat).Name);
        return Enumerable.Empty<PassiveCountermeasureStat>();
    }

    private IEnumerable<ActiveCountermeasureStat> GetCurrentActiveCmStats(Player player, DebugActiveCMLoadout loadout, int maxQty) {
        int qty = GetActiveCmQty(loadout, maxQty);
        return GetCurrentActiveCmStats(player, qty);
    }

    private IEnumerable<ActiveCountermeasureStat> GetCurrentActiveCmStats(Player player, int qty) {
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
            D.Log(ShowDebugLog, "{0}: There is no current SR {1} available.", DebugName, typeof(ActiveCountermeasureStat).Name);
        }
        if (mrAStat != null) {
            stats.Enqueue(mrAStat);
        }
        else {
            D.Log(ShowDebugLog, "{0}: There is no current MR {1} available.", DebugName, typeof(ActiveCountermeasureStat).Name);
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

    private IEnumerable<ShieldGeneratorStat> GetCurrentShieldGenStats(Player player, DebugShieldGenLoadout loadout, int maxQty) {
        int qty = GetShieldGeneratorQty(loadout, maxQty);
        return GetCurrentShieldGenStats(player, qty);
    }

    private IEnumerable<ShieldGeneratorStat> GetCurrentShieldGenStats(Player player, int qty) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        ShieldGeneratorStat sgStat;
        if (playerDesigns.TryGetCurrentShieldGeneratorStat(out sgStat)) {
            return Enumerable.Repeat(sgStat, qty);
        }
        D.Log(ShowDebugLog, "{0}: There is no current {1} available.", DebugName, typeof(ShieldGeneratorStat).Name);
        return Enumerable.Empty<ShieldGeneratorStat>();
    }

    private EngineStat GetCurrentEngineStat(Player player, EquipmentCategory engineCat) {
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        if (engineCat == EquipmentCategory.StlPropulsion) {
            return playerDesigns.GetCurrentStlEngineStat();
        }

        D.AssertEqual(EquipmentCategory.FtlPropulsion, engineCat);
        EngineStat ftlEngineStat;
        playerDesigns.TryGetCurrentFtlEngineStat(out ftlEngineStat);
        if (ftlEngineStat == null) {
            D.Log(ShowDebugLog, "{0}: There is no current FTL EngineStat available.", DebugName);
        }
        return ftlEngineStat;   // can be null
    }

    private IEnumerable<SensorStat> GetCurrentOptionalCmdSensorStats(Player player, int optionalQty) {
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
                D.Log(ShowDebugLog, "{0}: There is no current LR SensorStat available.", DebugName);
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
    /// <remark>Purpose is to have upgraded designs available so that some unit members can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, ShipDesign designToConsiderUpgrading) {
        D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToConsiderUpgrading.Status);
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            ShipDesign upgradedDesign = new ShipDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();   // DesignName will be [HullName][Unique#]_[IncrementedDesignLevel]
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteDesign(designToObsolete);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some unit members can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, FacilityDesign designToConsiderUpgrading) {
        D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToConsiderUpgrading.Status);
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            FacilityDesign upgradedDesign = new FacilityDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();   // DesignName will be [HullName][Unique#]_[IncrementedDesignLevel]
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteDesign(designToObsolete);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some unit members can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, SettlementCmdModuleDesign designToConsiderUpgrading) {
        D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToConsiderUpgrading.Status);
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            SettlementCmdModuleDesign upgradedDesign = new SettlementCmdModuleDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();   // DesignName will be [Cmd][Unique#]_[IncrementedDesignLevel]
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteDesign(designToObsolete);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some unit members can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, StarbaseCmdModuleDesign designToConsiderUpgrading) {
        D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToConsiderUpgrading.Status);
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            StarbaseCmdModuleDesign upgradedDesign = new StarbaseCmdModuleDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();   // DesignName will be [Cmd][Unique#]_[IncrementedDesignLevel]
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteDesign(designToObsolete);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Randomly selects designs to upgrade and if selected, creates an upgraded version of that design.
    /// <remark>Purpose is to have upgraded designs available so that some unit members can be refit.</remark>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="designToConsiderUpgrading">The design to consider upgrading.</param>
    private void __CreateAndRegisterRandomUpgradedDesign(Player player, FleetCmdModuleDesign designToConsiderUpgrading) {
        D.AssertNotEqual(AUnitMemberDesign.SourceAndStatus.SystemCreation_Template, designToConsiderUpgrading.Status);
        bool toUpgrade = RandomExtended.SplitChance();
        if (toUpgrade) {
            FleetCmdModuleDesign upgradedDesign = new FleetCmdModuleDesign(designToConsiderUpgrading);
            upgradedDesign.IncrementDesignLevelAndName();   // DesignName will be [Cmd][Unique#]_[IncrementedDesignLevel]
            var designToObsolete = designToConsiderUpgrading;
            var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
            playerDesigns.ObsoleteDesign(designToObsolete);
            playerDesigns.Add(upgradedDesign);
            D.Log(ShowDebugLog, "{0} has upgraded {1} to {2}.", DebugName, designToConsiderUpgrading.DebugName, upgradedDesign.DebugName);
        }
    }

    /// <summary>
    /// Determines whether [is starbase command module stat available] [the specified player].
    /// <remarks>Used as a way of avoiding work by UniverseCreator when a StarbaseCmdModuleStat has not yet been researched at startup.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public bool __IsStarbaseCmdModuleStatAvailable(Player player) {
        StarbaseCmdModuleStat unusedCmdStat;
        var playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        return playerDesigns.TryGetCurrentStarbaseCmdModuleStat(out unusedCmdStat);
    }

    #endregion

}

