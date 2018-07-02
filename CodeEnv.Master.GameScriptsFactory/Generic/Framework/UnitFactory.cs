// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitFactory.cs
// Singleton factory that makes instances of Elements and Commands.
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
/// Singleton factory that makes instances of Elements and Commands.
/// It also can make a standalone Fleet encompassing a single ship.
/// </summary>
public class UnitFactory : AGenericSingleton<UnitFactory> {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private AutoFleetCreator _fleetCreatorPrefab;
    private AutoStarbaseCreator _starbaseCreatorPrefab;
    private AutoSettlementCreator _settlementCreatorPrefab;

    private ShipItem _shipItemPrefab;
    private ShipHull[] _shipHullPrefabs;

    private FacilityItem _facilityItemPrefab;
    private FacilityHull[] _facilityHullPrefabs;

    private LaunchTube _launchTubePrefab;
    private LOSTurret _losTurretPrefab;

    private GameObject _fleetCmdPrefab;
    private GameObject _starbaseCmdPrefab;
    private GameObject _settlementCmdPrefab;

    private GameObject _countermeasureRangeMonitorPrefab;
    private GameObject _shieldPrefab;
    private GameObject _weaponRangeMonitorPrefab;
    private GameObject _ftlDampenerRangeMonitorPrefab;
    private GameObject _cmdSensorRangeMonitorPrefab;
    private GameObject _elementSensorRangeMonitorPrefab;

    private GameManager _gameMgr;

    #region Initialization

    private UnitFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;

        _fleetCreatorPrefab = reqdPrefabs.fleetCreator;
        _starbaseCreatorPrefab = reqdPrefabs.starbaseCreator;
        _settlementCreatorPrefab = reqdPrefabs.settlementCreator;

        _fleetCmdPrefab = reqdPrefabs.fleetCmd.gameObject;
        _starbaseCmdPrefab = reqdPrefabs.starbaseCmd.gameObject;
        _settlementCmdPrefab = reqdPrefabs.settlementCmd.gameObject;

        _countermeasureRangeMonitorPrefab = reqdPrefabs.countermeasureRangeMonitor.gameObject;
        _shieldPrefab = reqdPrefabs.shield.gameObject;
        _weaponRangeMonitorPrefab = reqdPrefabs.weaponRangeMonitor.gameObject;
        _cmdSensorRangeMonitorPrefab = reqdPrefabs.cmdSensorRangeMonitor.gameObject;
        _elementSensorRangeMonitorPrefab = reqdPrefabs.elementSensorRangeMonitor.gameObject;
        _ftlDampenerRangeMonitorPrefab = reqdPrefabs.ftlDampenerRangeMonitor.gameObject;

        _shipItemPrefab = reqdPrefabs.shipItem;
        _shipHullPrefabs = reqdPrefabs.shipHulls;
        _facilityItemPrefab = reqdPrefabs.facilityItem;
        _facilityHullPrefabs = reqdPrefabs.facilityHulls;

        _launchTubePrefab = reqdPrefabs.launchTube;
        _losTurretPrefab = reqdPrefabs.losTurret;

        _gameMgr = GameManager.Instance;
    }

    #endregion

    #region Fleets

    /// <summary>
    /// Makes an unnamed AutoFleetCreator instance at the provided location, parented to the FleetsFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    public AutoFleetCreator MakeFleetCreator(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorPrefabGo = _fleetCreatorPrefab.gameObject;
        GameObject creatorGo = UnityUtility.AddChild(FleetsFolder.Instance.gameObject, creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(AutoFleetCreator).Name);
        }
        creatorGo.transform.position = location;
        // UNCLEAR 1.17.17 what is the value of being static for this kind of object?
        var creator = creatorGo.GetComponent<AutoFleetCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Immediately makes and commences operations of a fleet instance parented to a new Creator from the provided ships.
    /// The fleet will use the default FleetCmdModuleDesign.
    /// </summary>
    /// <param name="creatorLocation">The creator location.</param>
    /// <param name="ships">The ships.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="optionalRootUnitName">Optional RootName of the Unit.</param>
    /// <returns></returns>
    [Obsolete("Not currently used")]
    public FleetCmdItem MakeFleetInstance(Vector3 creatorLocation, IEnumerable<ShipItem> ships, Formation formation, string optionalRootUnitName = null) {
        Player player = ships.First().Owner;
        PlayerDesigns playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        FleetCmdModuleDesign defaultCmdModDesign = playerDesigns.GetFleetCmdModDefaultDesign();
        return MakeFleetInstance(creatorLocation, defaultCmdModDesign, ships, formation, optionalRootUnitName);
    }

    /// <summary>
    /// Immediately makes and commences operations of a fleet instance parented to a new Creator from the provided ships using the
    /// provided CmdModule design.
    /// </summary>
    /// <param name="creatorLocation">The creator location.</param>
    /// <param name="cmdModDesign">The command module design.</param>
    /// <param name="ships">The ships.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="optionalRootUnitName">Name of the optional root unit.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetInstance(Vector3 creatorLocation, FleetCmdModuleDesign cmdModDesign, IEnumerable<ShipItem> ships,
        Formation formation, string optionalRootUnitName = null) {
        var creator = MakeFleetCreator(creatorLocation, ships, cmdModDesign);
        if (optionalRootUnitName != null) {
            creator.RootUnitName = optionalRootUnitName;
        }
        creator.PrepareUnitForDeployment();
        creator.AuthorizeDeployment();
        FleetCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<FleetCmdItem>();
        cmd.Data.Formation = formation;
        return cmd;
    }

    /// <summary>
    /// Immediately makes and commences operations of a fleet instance parented to a new Creator from the provided ships using a Cmd 
    /// built from a CmdModule design acquired using the cmdModDesignName.
    /// </summary>
    /// <param name="creatorLocation">The creator location.</param>
    /// <param name="cmdModDesignName">Name of the cmd module design.</param>
    /// <param name="ships">The ships.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="optionalRootUnitName">Name of the optional root unit.</param>
    /// <returns></returns>
    [Obsolete("Use version with the actual cmdModDesign")]
    public FleetCmdItem MakeFleetInstance(Vector3 creatorLocation, string cmdModDesignName, IEnumerable<ShipItem> ships, Formation formation,
        string optionalRootUnitName = null) {

        Player player = ships.First().Owner;
        PlayerDesigns playerDesigns = _gameMgr.GetAIManagerFor(player).Designs;
        FleetCmdModuleDesign cmdModDesign = playerDesigns.__GetFleetCmdModDesign(cmdModDesignName);
        return MakeFleetInstance(creatorLocation, cmdModDesign, ships, formation, optionalRootUnitName);
    }

    private RuntimeFleetCreator MakeFleetCreator(Vector3 creatorLocation, IEnumerable<ShipItem> ships, FleetCmdModuleDesign cmdModDesign) {
        GameObject creatorGo = new GameObject();
        UnityUtility.AttachChildToParent(creatorGo, FleetsFolder.Instance.gameObject);
        creatorGo.transform.position = creatorLocation;
        var creator = creatorGo.AddComponent<RuntimeFleetCreator>();
        creator.Elements = ships;
        creator.CmdModDesign = cmdModDesign;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance from the design acquired using the designName, parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, string cmdModDesignName, GameObject unitContainer, string unitName, Formation formation = Formation.Globe) {
        FleetCmdModuleDesign design = _gameMgr.GetAIManagerFor(owner).Designs.__GetFleetCmdModDesign(cmdModDesignName);
        return MakeFleetCmdInstance(owner, design, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance from the provided design,  parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="design">The CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdModuleDesign design, GameObject unitContainer, string unitName, Formation formation = Formation.Globe) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _fleetCmdPrefab);
        D.AssertEqual((Layers)_fleetCmdPrefab.layer, (Layers)cmdGo.layer);
        FleetCmdItem cmd = cmdGo.GetSafeComponent<FleetCmdItem>();
        PopulateInstance(owner, design, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, string cmdModDesignName, ref FleetCmdItem cmd, string unitName, Formation formation = Formation.Globe) {
        FleetCmdModuleDesign design = _gameMgr.GetAIManagerFor(owner).Designs.__GetFleetCmdModDesign(cmdModDesignName);
        PopulateInstance(owner, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesign">The CmdModule design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, FleetCmdModuleDesign cmdModDesign, ref FleetCmdItem cmd, string unitName,
        Formation formation = Formation.Globe) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        Utility.ValidateNotNullOrEmpty(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(cmdModDesign);
        var sensors = MakeSensors(cmdModDesign, cmd);
        var ftlDampener = MakeFtlDampener(cmdModDesign.FtlDampenerStat, cmd);
        FleetCmdData data = new FleetCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, cmdModDesign) {
            // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
            Formation = formation
        };
        cmd.CameraStat = MakeFleetCmdCameraStat(TempGameValues.MaxShipRadius);
        cmd.Data = data;
        cmd.Data.UnitName = unitName;
    }

    #endregion

    #region Ships

    /// <summary>
    /// Makes and returns a ship instance from the provided design parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="design">The design.</param>
    /// <param name="name">The ship name.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public ShipItem MakeShipInstance(Player owner, ShipDesign design, string name, GameObject unitContainer) {
        ShipHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _shipHullPrefabs.Single(sHull => sHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(unitContainer, _shipItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.Cull_200;   // hull layer gets set to item layer by AddChild

        ShipItem element = elementGoClone.GetSafeComponent<ShipItem>();
        PopulateInstance(owner, design, name, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, ShipDesign design, string name, ref ShipItem element) {
        D.AssertNotNull(element.transform.parent, element.DebugName);
        D.Assert(!element.IsOperational, element.DebugName);
        Utility.ValidateNotNullOrEmpty(name);
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        ShipHull hull = element.gameObject.GetSingleComponentInChildren<ShipHull>();
        var hullCategory = design.HullCategory;
        if (hullCategory != hull.HullCategory) {
            D.Error("{0} should be same as {1}.", hullCategory.GetValueName(), hull.HullCategory.GetValueName());
        }
        ShipHullEquipment hullEquipment = new ShipHullEquipment(design.HullStat);
        hullEquipment.Hull = hull;

        // Make the weapons along with their already selected mounts and add the weapon to the hullEquipment
        var weapons = MakeWeapons(design, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design);
        var activeCMs = MakeCountermeasures(design, element);
        var sensors = MakeSensors(design, element);
        var shieldGenerators = MakeShieldGenerators(design, element);

        Engine stlEngine;
        FtlEngine ftlEngine;
        MakeEngines(design, out stlEngine, out ftlEngine);

        ShipData data = new ShipData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, stlEngine, ftlEngine,
            design) {
            // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
        };

        element.GetComponent<Rigidbody>().mass = data.Mass; // 7.26.16 Set externally to keep the Rigidbody out of Data
        element.CameraStat = MakeElementCameraStat(design.HullStat);
        element.Data = data;
        element.Name = name;
    }

    #endregion

    #region Starbases

    /// <summary>
    /// Makes an unnamed AutoStarbaseCreator instance at the provided location, parented to the StarbasesFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    public AutoStarbaseCreator MakeStarbaseCreator(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorPrefabGo = _starbaseCreatorPrefab.gameObject;
        GameObject creatorGo = UnityUtility.AddChild(StarbasesFolder.Instance.gameObject, creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(AutoStarbaseCreator).Name);
        }
        creatorGo.transform.position = location;
        // UNCLEAR 1.17.17 what is the value of being static for this kind of object?
        var creator = creatorGo.GetComponent<AutoStarbaseCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes a StarbaseCmdItem instance at the location of a Sector's vacantStation using a 
    /// RuntimeStarbaseCreator parented to the StarbasesFolder.
    /// </summary>
    /// <param name="cmdModDesign">The command mod design.</param>
    /// <param name="centralHubDesign">The central hub design.</param>
    /// <param name="colonyShip">The colony ship.</param>
    /// <param name="vacantStation">A vacant station in a Sector.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="optionalRootUnitName">Name of the optional root unit.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseInstance(StarbaseCmdModuleDesign cmdModDesign, FacilityDesign centralHubDesign, ShipItem colonyShip,
        StationaryLocation vacantStation, Formation formation = Formation.Globe, string optionalRootUnitName = null) {
        var creator = MakeStarbaseCreator(cmdModDesign, centralHubDesign, colonyShip, vacantStation);
        if (optionalRootUnitName != null) {
            creator.RootUnitName = optionalRootUnitName;
        }
        creator.PrepareUnitForDeployment();
        creator.AuthorizeDeployment();
        StarbaseCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<StarbaseCmdItem>();
        cmd.Data.Formation = formation;
        return cmd;
    }

    private RuntimeStarbaseCreator MakeStarbaseCreator(StarbaseCmdModuleDesign cmdModDesign, FacilityDesign centralHubDesign,
        ShipItem colonyShip, StationaryLocation vacantStation) {
        GameObject creatorGo = new GameObject();
        UnityUtility.AttachChildToParent(creatorGo, StarbasesFolder.Instance.gameObject);
        creatorGo.transform.position = vacantStation.Position;

        var creator = creatorGo.AddComponent<RuntimeStarbaseCreator>();

        creator.CmdModDesign = cmdModDesign;
        creator.CentralHubDesign = centralHubDesign;
        creator.ColonyShip = colonyShip;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, string cmdModDesignName, GameObject unitContainer, string unitName,
        Formation formation = Formation.Globe) {
        StarbaseCmdModuleDesign cmdModDesign = _gameMgr.GetAIManagerFor(owner).Designs.__GetStarbaseCmdModDesign(cmdModDesignName);
        return MakeStarbaseCmdInstance(owner, cmdModDesign, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cmdModDesign">The CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, StarbaseCmdModuleDesign cmdModDesign, GameObject unitContainer, string unitName,
        Formation formation = Formation.Globe) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _starbaseCmdPrefab);
        StarbaseCmdItem cmd = cmdGo.GetSafeComponent<StarbaseCmdItem>();
        PopulateInstance(owner, cmdModDesign, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="cmd">The Cmd instance to populate.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, string cmdModDesignName, ref StarbaseCmdItem cmd, string unitName, Formation formation = Formation.Globe) {
        StarbaseCmdModuleDesign design = _gameMgr.GetAIManagerFor(owner).Designs.__GetStarbaseCmdModDesign(cmdModDesignName);
        PopulateInstance(owner, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesign">The CmdModule design.</param>
    /// <param name="cmd">The Cmd instance to populate.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, StarbaseCmdModuleDesign cmdModDesign, ref StarbaseCmdItem cmd, string unitName, Formation formation = Formation.Globe) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        Utility.ValidateNotNullOrEmpty(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(cmdModDesign);
        var sensors = MakeSensors(cmdModDesign, cmd);
        var ftlDampener = MakeFtlDampener(cmdModDesign.FtlDampenerStat, cmd);
        StarbaseCmdData data = new StarbaseCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, cmdModDesign) {
            // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
            Formation = formation
        };
        cmd.CameraStat = MakeBaseCmdCameraStat(TempGameValues.MaxFacilityRadius);
        cmd.Data = data;
        cmd.Data.UnitName = unitName;
    }

    #endregion

    #region Settlements

    /// <summary>
    /// Makes a AutoSettlementCreator instance installed in orbit around system.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public AutoSettlementCreator MakeSettlementCreator(UnitCreatorConfiguration config, SystemItem system) {
        GameObject creatorPrefabGo = _settlementCreatorPrefab.gameObject;
        GameObject creatorGo = GameObject.Instantiate(creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(AutoSettlementCreator).Name);
        }
        SystemFactory.Instance.InstallCelestialItemInOrbit(creatorGo, system.SettlementOrbitData);
        // UNCLEAR 1.17.17 what is the value of being static for this kind of object?
        var creator = creatorGo.GetComponent<AutoSettlementCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes a settlement instance at the location of the SettlementStation using a 
    /// RuntimeSettlementCreator parented to the designated system.
    /// </summary>
    /// <param name="cmdModDesign">The command mod design.</param>
    /// <param name="centralHubDesign">The central hub design.</param>
    /// <param name="colonyShip">The colony ship.</param>
    /// <param name="system">The system.</param>
    /// <param name="settlementStation">The settlement station.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="optionalRootUnitName">Name of the optional root unit.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementInstance(SettlementCmdModuleDesign cmdModDesign, FacilityDesign centralHubDesign, ShipItem colonyShip,
        SystemItem system, StationaryLocation settlementStation, Formation formation = Formation.Globe, string optionalRootUnitName = null) {
        var creator = MakeSettlementCreator(cmdModDesign, centralHubDesign, colonyShip, system, settlementStation.Position);
        if (optionalRootUnitName != null) {
            creator.RootUnitName = optionalRootUnitName;
        }
        creator.PrepareUnitForDeployment();
        creator.AuthorizeDeployment();
        SettlementCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<SettlementCmdItem>();
        cmd.Data.Formation = formation;
        return cmd;
    }

    private RuntimeSettlementCreator MakeSettlementCreator(SettlementCmdModuleDesign cmdModDesign, FacilityDesign centralHubDesign,
        ShipItem colonyShip, SystemItem system, Vector3 creatorWorldLocation) {
        GameObject creatorGo = new GameObject();
        Vector3 creatorLocalPosition = creatorWorldLocation - system.Position;
        SystemFactory.Instance.InstallCelestialItemInOrbit(creatorGo, system.SettlementOrbitData, creatorLocalPosition);
        var creator = creatorGo.AddComponent<RuntimeSettlementCreator>();
        creator.CmdModDesign = cmdModDesign;
        creator.CentralHubDesign = centralHubDesign;
        creator.ColonyShip = colonyShip;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, string cmdModDesignName, GameObject unitContainer, string unitName,
    Formation formation = Formation.Globe) {
        SettlementCmdModuleDesign design = _gameMgr.GetAIManagerFor(owner).Designs.__GetSettlementCmdModDesign(cmdModDesignName);
        return MakeSettlementCmdInstance(owner, design, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cmdModDesign">The CmdModule design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, SettlementCmdModuleDesign cmdModDesign, GameObject unitContainer,
        string unitName, Formation formation = Formation.Globe) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _settlementCmdPrefab);
        //D.Log("{0}: {1}.localPosition = {2} after creation.", DebugName, design.CmdStat.UnitName, cmdGo.transform.localPosition);
        SettlementCmdItem cmd = cmdGo.GetSafeComponent<SettlementCmdItem>();
        PopulateInstance(owner, cmdModDesign, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesignName">Name of the CmdModule design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, string cmdModDesignName, ref SettlementCmdItem cmd, string unitName, Formation formation = Formation.Globe) {
        SettlementCmdModuleDesign design = _gameMgr.GetAIManagerFor(owner).Designs.__GetSettlementCmdModDesign(cmdModDesignName);
        PopulateInstance(owner, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cmdModDesign">The CmdModule design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, SettlementCmdModuleDesign cmdModDesign, ref SettlementCmdItem cmd, string unitName, Formation formation = Formation.Globe) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        Utility.ValidateNotNullOrEmpty(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(cmdModDesign);
        var sensors = MakeSensors(cmdModDesign, cmd);
        var ftlDampener = MakeFtlDampener(cmdModDesign.FtlDampenerStat, cmd);
        SettlementCmdData data = new SettlementCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, cmdModDesign) {
            // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
            Formation = formation
        };
        cmd.CameraStat = MakeBaseCmdCameraStat(TempGameValues.MaxFacilityRadius);
        cmd.Data = data;
        cmd.Data.UnitName = unitName;
    }

    #endregion

    #region Facilities

    /// <summary>
    /// Makes and returns a facility instance from design parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="design">The design.</param>
    /// <param name="name">The facility name.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public FacilityItem MakeFacilityInstance(Player owner, Topography topography, FacilityDesign design, string name, GameObject unitContainer) {
        FacilityHullCategory hullCategory = design.HullCategory;
        GameObject hullPrefabGo = _facilityHullPrefabs.Single(fHull => fHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(unitContainer, _facilityItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.Cull_400;   // hull layer gets set to item layer by AddChild

        FacilityItem element = elementGoClone.GetSafeComponent<FacilityItem>();
        PopulateInstance(owner, topography, design, name, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, Topography topography, FacilityDesign design, string name, ref FacilityItem element) {
        D.Assert(!element.IsOperational, element.DebugName);
        if (element.transform.parent == null) {
            D.Error("{0} should already have a parent.", element.DebugName);
        }
        Utility.ValidateNotNullOrEmpty(name);
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        FacilityHull hull = element.gameObject.GetSingleComponentInChildren<FacilityHull>();
        var hullCategory = design.HullCategory;

        if (hullCategory != hull.HullCategory) {
            D.Error("{0} should be same as {1}.", hullCategory.GetValueName(), hull.HullCategory.GetValueName());
        }
        FacilityHullEquipment hullEquipment = new FacilityHullEquipment(design.HullStat);
        hullEquipment.Hull = hull;

        // Make the weapons along with their already selected mounts and add the weapon to the hullEquipment
        var weapons = MakeWeapons(design, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design);
        var activeCMs = MakeCountermeasures(design, element);
        var sensors = MakeSensors(design, element);
        var shieldGenerators = MakeShieldGenerators(design, element);

        FacilityData data = new FacilityData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, topography,
            design) {
            // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
        };
        element.GetComponent<Rigidbody>().mass = data.Mass; // 7.26.16 Set externally to keep the Rigidbody out of Data
        element.CameraStat = MakeElementCameraStat(design.HullStat);
        element.Data = data;
        element.Name = name;
    }

    #endregion

    #region Support Members

    /// <summary>
    /// Refits the provided instance of <c>cmd</c> to be consistent with the specifications in the provided <c>cmdModDesign</c>.
    //// <remarks>Will throw an error if the Level of the new design is not greater than the level of the existing design.</remarks>
    /// </summary>
    /// <param name="cmdModDesign">The command module design.</param>
    /// <param name="cmd">The command instance to be refit.</param>
    public void RefitCmdInstance(AUnitCmdModuleDesign cmdModDesign, AUnitCmdItem cmd) {
        // cmdModDesign.DesignLevel not necessarily > existing design's as RootDesignName can be different
        ReplaceCmdModuleWith(cmdModDesign, cmd);
    }

    /// <summary>
    /// Replaces the CmdModule of <c>cmd</c> with the provided <c>cmdModDesign</c>.
    /// <remarks>Replacement will occur without regard to the Level of the two designs.</remarks>
    /// </summary>
    /// <param name="cmdModDesign">The command module design.</param>
    /// <param name="cmd">The command instance to be modified.</param>
    public void ReplaceCmdModuleWith(AUnitCmdModuleDesign cmdModDesign, AUnitCmdItem cmd) {
        D.AssertNotEqual(cmd.Data.CmdModuleDesign, cmdModDesign);
        //D.Log("{0}.ReplaceCmdModuleWith() called for {1} using {2}.", DebugName, cmd.DebugName, cmdModDesign.DebugName);
        // Deactivate, decouple and remove all CmdSensors and the FtlDampener from their Monitors
        cmd.SensorMonitors.ForAll(mon => mon.ResetForReuse());
        cmd.FtlDampenerMonitor.ResetForReuse();

        // Generate the replacement equipment
        var passiveCmReplacements = MakeCountermeasures(cmdModDesign);
        var sensorReplacements = MakeSensors(cmdModDesign, cmd); // makes new sensors and attaches monitor(s) from Cmd
        var ftlDampenerReplacement = MakeFtlDampener(cmdModDesign.FtlDampenerStat, cmd); // makes new FtlDampener and attaches monitor from Cmd

        // Apply the design and new equipment to the Cmd
        cmd.ReplaceCmdModuleWith(cmdModDesign, passiveCmReplacements, sensorReplacements, ftlDampenerReplacement);
    }

    private FollowableItemCameraStat MakeElementCameraStat(FacilityHullStat hullStat) {
        FacilityHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                fov = 70F;
                break;
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Barracks:
                fov = 60F;
                break;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullStat.HullDimensions.magnitude / 2F;
        //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
    }

    private FollowableItemCameraStat MakeElementCameraStat(ShipHullStat hullStat) {
        ShipHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Troop:
                fov = 70F;
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Investigator:
                fov = 65F;
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                fov = 60F;
                break;
            case ShipHullCategory.Frigate:
                fov = 55F;
                break;
            //case ShipHullCategory.Fighter:
            //case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullCat.Dimensions().magnitude / 2F; ////hullStat.HullDimensions.magnitude / 2F;
        //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        float distanceDampener = 3F;    // default
        float rotationDampener = 10F;   // ships can change direction pretty fast
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov, distanceDampener, rotationDampener);
    }

    private FleetCmdCameraStat MakeFleetCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F;
        float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
        // there is no optViewDistance value for a FleetCmd CameraStat
        return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private CmdCameraStat MakeBaseCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    /// <summary>
    /// Makes an STL or FTL Engine, depending on the indicator in engineStat.
    /// </summary>
    /// <param name="engineStat">The engine stat.</param>
    /// <param name="name">The optional engine name.</param>
    /// <returns></returns>
    [Obsolete]
    private Engine MakeEngine(EngineStat engineStat, string name = null) {
        if (engineStat.Category == EquipmentCategory.FtlPropulsion) {
            return new FtlEngine(engineStat, name);
        }
        return new Engine(engineStat, name);
    }

    private void MakeEngines(ShipDesign shipDesign, out Engine stlEngine, out FtlEngine ftlEngine) {
        EngineStat stlEngineStat = shipDesign.StlEngineStat;
        stlEngine = new Engine(shipDesign.StlEngineStat);

        var stats = shipDesign.GetOptEquipStatsFor(EquipmentCategory.FtlPropulsion);
        ftlEngine = stats.Any() ? new FtlEngine(stats.First() as EngineStat) : null;
    }

    /// <summary>
    /// Makes an FTL dampener.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    private FtlDampener MakeFtlDampener(FtlDampenerStat stat, AUnitCmdItem cmd, string name = null) {
        var dampener = new FtlDampener(stat, name);
        AttachMonitor(dampener, cmd);
        return dampener;
    }

    /// <summary>
    /// Makes and returns passive countermeasures made from the provided design. PassiveCountermeasures do not use RangeMonitors.
    /// </summary>
    /// <param name="unitDesign">The unit design.</param>
    /// <returns></returns>
    private IEnumerable<PassiveCountermeasure> MakeCountermeasures(AUnitMemberDesign unitDesign) {
        var passiveCMs = new List<PassiveCountermeasure>();

        var stats = unitDesign.GetOptEquipStatsFor(EquipmentCategory.PassiveCountermeasure);
        foreach (var stat in stats) {
            passiveCMs.Add(new PassiveCountermeasure(stat as PassiveCountermeasureStat));
        }
        return passiveCMs;
    }

    private IEnumerable<ActiveCountermeasure> MakeCountermeasures(AUnitElementDesign elementDesign, AUnitElementItem element) {
        int nameCounter = Constants.One;
        var activeCMs = new List<ActiveCountermeasure>();

        var stats = elementDesign.GetOptEquipStatsFor(EquipmentCategory.SRActiveCountermeasure).ToList();
        stats.AddRange(elementDesign.GetOptEquipStatsFor(EquipmentCategory.MRActiveCountermeasure));
        foreach (var stat in stats) {
            string cmName = stat.Name + nameCounter;
            nameCounter++;

            var activeCM = new ActiveCountermeasure(stat as ActiveCountermeasureStat, cmName);
            activeCMs.Add(activeCM);
            AttachMonitor(activeCM, element);
        }
        return activeCMs;
    }

    private IEnumerable<ElementSensor> MakeSensors(AUnitElementDesign elementDesign, AUnitElementItem element) {
        int nameCounter = Constants.One;
        var sensors = new List<ElementSensor>();

        SensorStat reqdSRSensorStat = elementDesign.ReqdSRSensorStat;
        string sName = reqdSRSensorStat.Name + nameCounter;
        nameCounter++;
        ElementSensor reqdSRSensor = new ElementSensor(reqdSRSensorStat, sName);
        sensors.Add(reqdSRSensor);
        AttachMonitor(reqdSRSensor, element);

        var optionalSensorStats = elementDesign.GetOptEquipStatsFor(EquipmentCategory.SRSensor);
        foreach (var stat in optionalSensorStats) {
            sName = stat.Name + nameCounter;
            nameCounter++;
            var sensor = new ElementSensor(stat as SensorStat, sName);
            sensors.Add(sensor);
            AttachMonitor(sensor, element);
        }
        return sensors;
    }

    private IEnumerable<CmdSensor> MakeSensors(AUnitCmdModuleDesign cmdModDesign, AUnitCmdItem cmd) {
        int nameCounter = Constants.One;
        var sensors = new List<CmdSensor>();

        SensorStat reqdMRSensorStat = cmdModDesign.ReqdMRSensorStat;
        string sName = reqdMRSensorStat.Name + nameCounter;
        nameCounter++;
        CmdSensor reqdMRSensor = new CmdSensor(reqdMRSensorStat, sName);
        sensors.Add(reqdMRSensor);
        AttachMonitor(reqdMRSensor, cmd);

        List<AEquipmentStat> optionalSensorStats = cmdModDesign.GetOptEquipStatsFor(EquipmentCategory.MRSensor).ToList();
        optionalSensorStats.AddRange(cmdModDesign.GetOptEquipStatsFor(EquipmentCategory.LRSensor));
        foreach (var stat in optionalSensorStats) {
            sName = stat.Name + nameCounter;
            nameCounter++;
            var sensor = new CmdSensor(stat as SensorStat, sName);
            sensors.Add(sensor);
            AttachMonitor(sensor, cmd);
        }
        return sensors;
    }

    private IEnumerable<ShieldGenerator> MakeShieldGenerators(AUnitElementDesign elementDesign, AUnitElementItem element) {
        var generators = new List<ShieldGenerator>();
        var generatorStats = elementDesign.GetOptEquipStatsFor(EquipmentCategory.ShieldGenerator);
        foreach (var gStat in generatorStats) {
            var generator = new ShieldGenerator(gStat as ShieldGeneratorStat);
            generators.Add(generator);
            AttachShield(generator, element);
        }
        return generators;
    }

    private IEnumerable<AWeapon> MakeWeapons(AUnitElementDesign elementDesign, AUnitElementItem element, AHull hull) {
        //D.Log("{0}: Making Weapons for {1} using {2}.", DebugName, element.DebugName, hull.DebugName);
        int nameCounter = Constants.One;

        EquipmentCategory[] weaponEquipCats = new EquipmentCategory[] { EquipmentCategory.MissileWeapon, EquipmentCategory.AssaultWeapon,
            EquipmentCategory.BeamWeapon, EquipmentCategory.ProjectileWeapon };
        var weapons = new List<AWeapon>();
        foreach (var weapCat in weaponEquipCats) {
            var weapStatsLookupBySlot = elementDesign.GetEquipmentLookupFor(weapCat);
            foreach (var slotID in weapStatsLookupBySlot.Keys) {
                AWeaponStat wStat = weapStatsLookupBySlot[slotID] as AWeaponStat;
                string weaponName = wStat.Name + nameCounter;
                nameCounter++;

                AWeapon weapon;
                switch (weapCat) {
                    case EquipmentCategory.BeamWeapon:
                        weapon = new BeamProjector(wStat as BeamWeaponStat, weaponName);
                        break;
                    case EquipmentCategory.ProjectileWeapon:
                        weapon = new ProjectileLauncher(wStat as ProjectileWeaponStat, weaponName);
                        break;
                    case EquipmentCategory.MissileWeapon:
                        weapon = new MissileLauncher(wStat as MissileWeaponStat, weaponName);
                        break;
                    case EquipmentCategory.AssaultWeapon:
                        weapon = new AssaultLauncher(wStat as AssaultWeaponStat, weaponName);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weapCat));
                }
                AttachMonitor(weapon, element);
                AttachMount(weapon, slotID, hull);
                weapons.Add(weapon);
            }
        }
        // remove and destroy any remaining mount placeholders that didn't get weapons
        var remainingMountPlaceholders = hull.GetComponentsInChildren<AMountPlaceholder>();
        if (remainingMountPlaceholders.Any()) {
            remainingMountPlaceholders.ForAll(mp => {
                mp.transform.parent = null; // detach placeholder from hull so it won't be found as Destroy takes a frame
                GameUtility.Destroy(mp.gameObject);
            });
        }
        return weapons;
    }

    /// <summary>
    /// Makes or acquires an existing Shield and attaches it to this generator.
    /// Note: The monitor will be added and its events hooked up to the element when the element's data is attached.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="element">The element.</param>
    private void AttachShield(ShieldGenerator generator, AUnitElementItem element) {
        var allShields = element.gameObject.GetComponentsInChildren<Shield>();
        var shieldsInUse = allShields.Where(s => s.RangeCategory != RangeCategory.None);

        // check shields for range fit, if find it, assign shield, if not assign unused or create a new shield and assign it
        var shield = shieldsInUse.FirstOrDefault(s => s.RangeCategory == generator.RangeCategory);
        if (shield == null) {
            var unusedShields = allShields.Except(shieldsInUse);
            if (unusedShields.Any()) {
                shield = unusedShields.First();
            }
            else {
                GameObject shieldGo = UnityUtility.AddChild(element.gameObject, _shieldPrefab);
                shieldGo.layer = (int)Layers.Shields;  // AddChild resets prefab layer to elementGo's layer
                shield = shieldGo.GetComponent<Shield>();
            }
            shield.ParentItem = element;
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", DebugName, element.DebugName, typeof(Shield).Name, generator.Name);
        }
        shield.Add(generator);
    }

    /// <summary>
    /// Makes or acquires an existing WeaponRangeMonitor and attaches it to this weapon.
    /// Note: The monitor will be added and its events hooked up to the element when the element's data is attached.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="element">The element.</param>
    private void AttachMonitor(AWeapon weapon, AUnitElementItem element) {
        D.AssertNull(weapon.RangeMonitor);
        var allMonitors = element.gameObject.GetComponentsInChildren<WeaponRangeMonitor>();
        var monitorsInUse = allMonitors.Where(m => m.RangeCategory != RangeCategory.None);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the equipment
        var monitor = monitorsInUse.FirstOrDefault(m => m.RangeCategory == weapon.RangeCategory);
        if (monitor == null) {
            var unusedMonitors = allMonitors.Except(monitorsInUse);
            if (unusedMonitors.Any()) {
                monitor = unusedMonitors.First();
            }
            else {
                D.AssertEqual(Layers.Collide_DefaultOnly, (Layers)_weaponRangeMonitorPrefab.layer);
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _weaponRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.Collide_DefaultOnly;  // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetComponent<WeaponRangeMonitor>();
            }
            monitor.ParentItem = element;
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", DebugName, element.DebugName, typeof(WeaponRangeMonitor).Name, weapon.Name);
        }
        monitor.Add(weapon);
    }

    /// <summary>
    /// Attaches a newly instantiated AWeaponMount of the proper type to the weapon, replacing the mountPlaceholder
    /// at the indicated slot on the hull.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="slotID">The mount slot identifier.</param>
    /// <param name="hull">The hull.</param>
    private void AttachMount(AWeapon weapon, OptionalEquipSlotID slotID, AHull hull) {
        AMount mountPlaceholder;
        AWeaponMount weaponMountPrefab;
        if (slotID.SupportedMount == OptionalEquipMountCategory.Turret) {
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>()
                                    .Single(placeholder => placeholder.SlotID == slotID);
            weaponMountPrefab = _losTurretPrefab;
        }
        else {
            D.AssertEqual(OptionalEquipMountCategory.Silo, slotID.SupportedMount);
            var mountPlaceholders = hull.gameObject.GetSafeComponentsInChildren<LauncherMountPlaceholder>();
            //D.Log("{0}: MountPlaceholders found = {1}.", DebugName, mountPlaceholders.Select(mp => mp.DebugName).Concatenate());
            mountPlaceholder = mountPlaceholders.Single(placeholder => placeholder.SlotID == slotID);
            weaponMountPrefab = _launchTubePrefab;
        }

        // align the new mount's position and rotation with that of the placeholder it is replacing
        GameObject mountGo = UnityUtility.AddChild(hull.gameObject, weaponMountPrefab.gameObject);
        Transform mountTransform = mountGo.transform;
        mountTransform.rotation = mountPlaceholder.transform.rotation;
        mountTransform.position = mountPlaceholder.transform.position;

        // align the layer of the mount and its children to that of the HullMesh
        Layers hullMeshLayer = (Layers)hull.HullMesh.gameObject.layer;
        UnityUtility.SetLayerRecursively(mountTransform, hullMeshLayer);

        AWeaponMount weaponMount = mountGo.GetComponent<AWeaponMount>();
        if (slotID.SupportedMount == OptionalEquipMountCategory.Turret) {
            // LOS weapon
            var losMountPlaceholder = mountPlaceholder as LOSMountPlaceholder;
            var losWeaponMount = weaponMount as LOSTurret;
            losWeaponMount.InitializeBarrelElevationSettings(losMountPlaceholder.MinimumBarrelElevation);
        }
        mountPlaceholder.transform.parent = null;   // detach placeholder from hull so it won't be found as Destroy takes a frame
        GameUtility.Destroy(mountPlaceholder.gameObject);
        weapon.WeaponMount = weaponMount;
    }

    /// <summary>
    /// Makes or acquires an existing ActiveCountermeasureRangeMonitor and attaches it to this active countermeasure.
    /// Note: The monitor will be added and its events hooked up to the element when the element's data is attached.
    /// </summary>
    /// <param name="countermeasure">The countermeasure.</param>
    /// <param name="element">The element.</param>
    private void AttachMonitor(ActiveCountermeasure countermeasure, AUnitElementItem element) {
        D.AssertNull(countermeasure.RangeMonitor);
        var allMonitors = element.gameObject.GetComponentsInChildren<ActiveCountermeasureRangeMonitor>();
        var monitorsInUse = allMonitors.Where(m => m.RangeCategory != RangeCategory.None);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the equipment
        var monitor = monitorsInUse.FirstOrDefault(m => m.RangeCategory == countermeasure.RangeCategory);
        if (monitor == null) {
            var unusedMonitors = allMonitors.Except(monitorsInUse);
            if (unusedMonitors.Any()) {
                monitor = unusedMonitors.First();
            }
            else {
                D.AssertEqual(Layers.Collide_ProjectileOnly, (Layers)_countermeasureRangeMonitorPrefab.layer);
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _countermeasureRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.Collide_ProjectileOnly;   // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetComponent<ActiveCountermeasureRangeMonitor>();
            }
            monitor.ParentItem = element;
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", DebugName, element.DebugName, typeof(ActiveCountermeasureRangeMonitor).Name, countermeasure.Name);
        }
        monitor.Add(countermeasure);
    }

    /// <summary>
    /// Makes or acquires an existing SensorRangeMonitor and attaches it to this sensor.
    /// Note: The monitor will be added and its events hooked up to the element when the element's data is attached.
    /// </summary>
    /// <param name="sensor">The countermeasure.</param>
    /// <param name="element">The element.</param>
    private void AttachMonitor(ElementSensor sensor, AUnitElementItem element) {
        D.AssertNull(sensor.RangeMonitor);
        D.AssertEqual(RangeCategory.Short, sensor.RangeCategory);
        var srMonitor = element.gameObject.GetComponentInChildren<ElementSensorRangeMonitor>();

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the equipment
        if (srMonitor == null) {
            D.AssertEqual(Layers.Collide_DefaultOnly, (Layers)_elementSensorRangeMonitorPrefab.layer);
            GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _elementSensorRangeMonitorPrefab);
            monitorGo.layer = (int)Layers.Collide_DefaultOnly;   // AddChild resets prefab layer to elementGo's layer
            srMonitor = monitorGo.GetComponent<ElementSensorRangeMonitor>();
            srMonitor.ParentItem = element;
            //D.Log("{0}: {1} has had a {2} made for {3}.", DebugName, element.DebugName, typeof(ElementSensorRangeMonitor).Name, sensor.Name);
        }
        srMonitor.Add(sensor);
    }

    /// <summary>
    /// Makes or acquires an existing CmdSensorRangeMonitor and pairs it with this CmdSensor.
    /// </summary>
    /// <param name="sensor">The sensor from the command.</param>
    /// <param name="command">The command that has sensor monitors as children.</param>
    private void AttachMonitor(CmdSensor sensor, AUnitCmdItem command) {
        D.AssertNotEqual(RangeCategory.Short, sensor.RangeCategory);
        var allSensorMonitors = command.gameObject.GetComponentsInChildren<CmdSensorRangeMonitor>();
        allSensorMonitors.ForAll(srm => D.AssertNotEqual(RangeCategory.Short, srm.RangeCategory));        // OPTIMIZE

        var sensorMonitorsInUse = allSensorMonitors.Where(m => m.RangeCategory != RangeCategory.None);
        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the equipment
        var monitor = sensorMonitorsInUse.FirstOrDefault(m => m.RangeCategory == sensor.RangeCategory);
        if (monitor == null) {
            var unusedSensorMonitors = allSensorMonitors.Except(sensorMonitorsInUse);
            if (unusedSensorMonitors.Any()) {
                monitor = unusedSensorMonitors.First();
            }
            else {
                D.AssertEqual(Layers.Collide_DefaultOnly, (Layers)_cmdSensorRangeMonitorPrefab.layer);
                GameObject monitorGo = UnityUtility.AddChild(command.gameObject, _cmdSensorRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.Collide_DefaultOnly;  // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetComponent<CmdSensorRangeMonitor>();
                //D.Log("{0}: {1} has had a {2} made for {3}.", DebugName, command.DebugName, typeof(CmdSensorRangeMonitor).Name, sensor.Name);
            }
            monitor.ParentItem = command;
        }
        monitor.Add(sensor);
    }

    /// <summary>
    /// Makes or acquires an existing FtlDampenerRangeMonitor and pairs it with this ftlDampener.
    /// </summary>
    /// <param name="ftlDampener">The command's FtlDampener.</param>
    /// <param name="command">The command that has an FtlDampener monitor as a child.</param>
    private void AttachMonitor(FtlDampener ftlDampener, AUnitCmdItem command) {
        var monitor = command.gameObject.GetComponentInChildren<FtlDampenerRangeMonitor>();
        if (monitor == null) {
            D.AssertEqual(Layers.Collide_DefaultOnly, (Layers)_ftlDampenerRangeMonitorPrefab.layer);
            GameObject monitorGo = UnityUtility.AddChild(command.gameObject, _ftlDampenerRangeMonitorPrefab);
            monitorGo.layer = (int)Layers.Collide_DefaultOnly;  // AddChild resets prefab layer to elementGo's layer
            monitor = monitorGo.GetComponent<FtlDampenerRangeMonitor>();
        }
        D.AssertDefault((int)monitor.RangeCategory);
        monitor.ParentItem = command;
        monitor.Add(ftlDampener);
    }

    #endregion

    #region Debug

    private const string __UnitElementUniqueNameFormat = "{0}#{1}";    // aka DesignName#1, distinguished from  DesignName1

    private IDictionary<string, int> __facilityNameCountLookup = new Dictionary<string, int>();
    private IDictionary<string, int> __shipNameCountLookup = new Dictionary<string, int>();

    public string __GetUniqueFacilityName(string designName) {
        if (!__facilityNameCountLookup.ContainsKey(designName)) {
            __facilityNameCountLookup.Add(designName, 1);
        }
        int nameCount = __facilityNameCountLookup[designName];
        string name = __UnitElementUniqueNameFormat.Inject(designName, nameCount);
        __facilityNameCountLookup[designName] = ++nameCount;
        return name;
    }

    public string __GetUniqueShipName(string designName) {
        if (!__shipNameCountLookup.ContainsKey(designName)) {
            __shipNameCountLookup.Add(designName, 1);
        }
        int nameCount = __shipNameCountLookup[designName];
        string name = __UnitElementUniqueNameFormat.Inject(designName, nameCount);
        __shipNameCountLookup[designName] = ++nameCount;
        return name;
    }

    #endregion

}

