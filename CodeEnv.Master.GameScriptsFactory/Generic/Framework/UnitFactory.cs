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

    private FleetCreator _fleetCreatorPrefab;
    private StarbaseCreator _starbaseCreatorPrefab;
    private SettlementCreator _settlementCreatorPrefab;

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
    }

    #endregion

    #region Fleets

    /// <summary>
    /// Makes an unnamed FleetCreator instance at the provided location, parented to the FleetsFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    public FleetCreator MakeFleetCreatorInstance(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorPrefabGo = _fleetCreatorPrefab.gameObject;
        GameObject creatorGo = UnityUtility.AddChild(FleetsFolder.Instance.gameObject, creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(FleetCreator).Name);
        }
        creatorGo.transform.position = location;
        // UNCLEAR 1.17.17 what is the value of being static for this kind of object?
        ////creatorGo.isStatic = true;
        var creator = creatorGo.GetComponent<FleetCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes an unnamed LoneFleetCreator instance at the provided location, parented to the FleetsFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    private LoneFleetCreator MakeLoneFleetCreatorInstance(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorGo = new GameObject();
        UnityUtility.AttachChildToParent(creatorGo, FleetsFolder.Instance.gameObject);
        creatorGo.transform.position = location;
        var creator = creatorGo.AddComponent<LoneFleetCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, GameObject unitContainer,
        string unitName, Formation formation) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        return MakeFleetCmdInstance(owner, cameraStat, design, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, GameObject unitContainer,
        string unitName, Formation formation) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _fleetCmdPrefab);
        D.AssertEqual((Layers)_fleetCmdPrefab.layer, (Layers)cmdGo.layer);
        FleetCmdItem cmd = cmdGo.GetSafeComponent<FleetCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, ref FleetCmdItem cmd, string unitName,
        Formation formation) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, ref FleetCmdItem cmd, string unitName,
        Formation formation) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        D.AssertNotNull(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design);
        var sensors = MakeSensors(design, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = __GetUniqueFleetCmdName(design.DesignName);
        FleetCmdData data = new FleetCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.ReqdCmdStat, design.DesignName) {
            ParentName = unitName,
            UnitFormation = formation
        };
        cmd.CameraStat = cameraStat;
        cmd.Data = data;
    }

    /// <summary>
    /// Makes a standalone 'Lone' FleetCmd instance from a single element using a basic FleetCmdDesign.
    /// The Cmd returned, along with the provided element is parented to an empty GameObject "unitRootName" which itself is parented to
    /// the Scene's Fleets folder. The Cmd and element are or shortly will be operating when returned.
    /// </summary>
    /// <param name="element">The element which is designated the HQ Element.</param>
    /// <param name="optionalRootUnitName">Optional root name of the unit. 
    /// If not set, the root name of the unit will be set by the creator.</param>
    /// <returns></returns>
    public FleetCmdItem MakeLoneFleetInstance(ShipItem element, string optionalRootUnitName = null) {
        D.Assert(element.IsOperational);

        GameDate currentDate = GameTime.Instance.CurrentDate;
        string cmdDesignName = TempGameValues.LoneFleetCmdDesignName;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(element.Owner, currentDate, cmdDesignName, Enumerable.Empty<string>());
        var creator = MakeLoneFleetCreatorInstance(element.Position, config);
        if (optionalRootUnitName != null) {
            creator.RootUnitName = optionalRootUnitName;
        }
        creator.LoneElement = element;
        creator.BuildAndPositionUnit(); // 5.14.17 Makes LoneElement a child of UnitContainer (LoneFleetCreator.gameObject)
        creator.AuthorizeDeployment();
        FleetCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<FleetCmdItem>();
        return cmd;
    }

    #endregion

    #region Ships

    /// <summary>
    /// Makes an unenabled ship instance parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public ShipItem MakeShipInstance(Player owner, FollowableItemCameraStat cameraStat, string designName, GameObject unitContainer) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(owner, designName);
        return MakeShipInstance(owner, cameraStat, design, unitContainer);
    }

    /// <summary>
    /// Makes an unenabled ship instance parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public ShipItem MakeShipInstance(Player owner, FollowableItemCameraStat cameraStat, ShipDesign design, GameObject unitContainer) {
        ShipHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _shipHullPrefabs.Single(sHull => sHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(unitContainer, _shipItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.Cull_200;   // hull layer gets set to item layer by AddChild

        ShipItem element = elementGoClone.GetSafeComponent<ShipItem>();
        PopulateInstance(owner, cameraStat, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, FollowableItemCameraStat cameraStat, string designName, ref ShipItem element) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref element);
    }

    public void PopulateInstance(Player owner, FollowableItemCameraStat cameraStat, ShipDesign design, ref ShipItem element) {
        D.AssertNotNull(element.transform.parent, element.DebugName);
        D.Assert(!element.IsOperational, element.DebugName);
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
        Priority hqPriority = design.HQPriority;

        var stlEngine = MakeEngine(design.StlEngineStat, "StlEngine");
        FtlEngine ftlEngine = null;
        if (design.FtlEngineStat != null) {
            ftlEngine = MakeEngine(design.FtlEngineStat, "FtlEngine") as FtlEngine;
        }

        element.Name = __GetUniqueShipName(design.DesignName);
        ShipData data = new ShipData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority,
            stlEngine, ftlEngine, design.CombatStance, design.DesignName);
        element.GetComponent<Rigidbody>().mass = data.Mass; // 7.26.16 Set externally to keep the Rigidbody out of Data
        element.CameraStat = cameraStat;
        element.Data = data;
    }

    #endregion

    #region Starbases

    /// <summary>
    /// Makes an unnamed StarbaseCreator instance at the provided location, parented to the StarbasesFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    public StarbaseCreator MakeStarbaseCreatorInstance(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorPrefabGo = _starbaseCreatorPrefab.gameObject;
        GameObject creatorGo = UnityUtility.AddChild(StarbasesFolder.Instance.gameObject, creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(StarbaseCreator).Name);
        }
        creatorGo.transform.position = location;
        // UNCLEAR 1.17.17 what is the value of being static for this kind of object?
        ////creatorGo.isStatic = true;
        var creator = creatorGo.GetComponent<StarbaseCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer,
        string unitName, Formation formation) {
        StarbaseCmdDesign design = GameManager.Instance.PlayersDesigns.GetStarbaseCmdDesign(owner, designName);
        return MakeStarbaseCmdInstance(owner, cameraStat, design, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, GameObject unitContainer,
        string unitName, Formation formation) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _starbaseCmdPrefab);
        StarbaseCmdItem cmd = cmdGo.GetSafeComponent<StarbaseCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref StarbaseCmdItem cmd, string unitName,
        Formation formation) {
        StarbaseCmdDesign design = GameManager.Instance.PlayersDesigns.GetStarbaseCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, ref StarbaseCmdItem cmd, string unitName,
        Formation formation) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        D.AssertNotNull(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design);
        var sensors = MakeSensors(design, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = __GetUniqueStarbaseCmdName(design.DesignName);
        StarbaseCmdData data = new StarbaseCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.ReqdCmdStat, design.DesignName) {
            ParentName = unitName,
            UnitFormation = formation
        };
        cmd.CameraStat = cameraStat;
        cmd.Data = data;
    }

    #endregion

    #region Settlements

    /// <summary>
    /// Makes a settlement creator instance installed in orbit around system.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    public SettlementCreator MakeSettlementCreatorInstance(UnitCreatorConfiguration config, SystemItem system) {
        GameObject creatorPrefabGo = _settlementCreatorPrefab.gameObject;
        GameObject creatorGo = GameObject.Instantiate(creatorPrefabGo);
        if (creatorGo.isStatic) {
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", DebugName, typeof(SettlementCreator).Name);
        }
        SystemFactory.Instance.InstallCelestialItemInOrbit(creatorGo, system.SettlementOrbitData);
        var creator = creatorGo.GetComponent<SettlementCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer,
        string unitName, Formation formation) {
        SettlementCmdDesign design = GameManager.Instance.PlayersDesigns.GetSettlementCmdDesign(owner, designName);
        return MakeSettlementCmdInstance(owner, cameraStat, design, unitContainer, unitName, formation);
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design,
        GameObject unitContainer, string unitName, Formation formation) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _settlementCmdPrefab);
        //D.Log("{0}: {1}.localPosition = {2} after creation.", DebugName, design.CmdStat.UnitName, cmdGo.transform.localPosition);
        SettlementCmdItem cmd = cmdGo.GetSafeComponent<SettlementCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref SettlementCmdItem cmd, string unitName,
        Formation formation) {
        SettlementCmdDesign design = GameManager.Instance.PlayersDesigns.GetSettlementCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName, formation);
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Name of the overall Unit.</param>
    /// <param name="formation">The formation.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design, ref SettlementCmdItem cmd,
        string unitName, Formation formation) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        D.AssertNotNull(unitName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design);
        var sensors = MakeSensors(design, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = __GetUniqueSettlementCmdName(design.DesignName);
        SettlementCmdData data = new SettlementCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.ReqdCmdStat, design.DesignName) {
            ParentName = unitName,
            UnitFormation = formation
        };
        cmd.CameraStat = cameraStat;
        cmd.Data = data;
    }

    #endregion

    #region Facilities

    /// <summary>
    /// Makes a facility instance parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public FacilityItem MakeFacilityInstance(Player owner, Topography topography, FollowableItemCameraStat cameraStat, string designName, GameObject unitContainer) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(owner, designName);
        return MakeFacilityInstance(owner, topography, cameraStat, design, unitContainer);
    }

    /// <summary>
    /// Makes a facility instance parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public FacilityItem MakeFacilityInstance(Player owner, Topography topography, FollowableItemCameraStat cameraStat, FacilityDesign design, GameObject unitContainer) {
        FacilityHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _facilityHullPrefabs.Single(fHull => fHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(unitContainer, _facilityItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.Cull_400;   // hull layer gets set to item layer by AddChild

        FacilityItem element = elementGoClone.GetSafeComponent<FacilityItem>();
        PopulateInstance(owner, topography, cameraStat, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, Topography topography, FollowableItemCameraStat cameraStat, string designName, ref FacilityItem element) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(owner, designName);
        PopulateInstance(owner, topography, cameraStat, design, ref element);
    }

    public void PopulateInstance(Player owner, Topography topography, FollowableItemCameraStat cameraStat, FacilityDesign design,
        ref FacilityItem element) {
        D.Assert(!element.IsOperational, element.DebugName);
        if (element.transform.parent == null) {
            D.Error("{0} should already have a parent.", element.DebugName);
        }
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
        Priority hqPriority = design.HQPriority;

        element.Name = __GetUniqueFacilityName(design.DesignName);
        FacilityData data = new FacilityData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators,
            hqPriority, topography, design.DesignName);
        element.GetComponent<Rigidbody>().mass = data.Mass; // 7.26.16 Set externally to keep the Rigidbody out of Data
        element.CameraStat = cameraStat;
        element.Data = data;
    }

    #endregion

    #region Support Members

    /// <summary>
    /// Makes an STL or FTL Engine, depending on the indicator in engineStat.
    /// </summary>
    /// <param name="engineStat">The engine stat.</param>
    /// <param name="name">The optional engine name.</param>
    /// <returns></returns>
    private Engine MakeEngine(EngineStat engineStat, string name = null) {
        if (engineStat.IsFtlEngine) {
            return new FtlEngine(engineStat, name);
        }
        return new Engine(engineStat, name);
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
    private IEnumerable<PassiveCountermeasure> MakeCountermeasures(AUnitDesign unitDesign) {
        var passiveCMs = new List<PassiveCountermeasure>();
        AEquipmentStat eStat;
        while (unitDesign.GetNextEquipmentStat(EquipmentCategory.PassiveCountermeasure, out eStat)) {
            if (eStat != null) {
                passiveCMs.Add(new PassiveCountermeasure(eStat as PassiveCountermeasureStat));
            }
        }
        return passiveCMs;
    }

    private IEnumerable<ActiveCountermeasure> MakeCountermeasures(AElementDesign elementDesign, AUnitElementItem element) {
        int nameCounter = Constants.One;

        var activeCMs = new List<ActiveCountermeasure>();

        AEquipmentStat eStat;
        while (elementDesign.GetNextEquipmentStat(EquipmentCategory.ActiveCountermeasure, out eStat)) {
            if (eStat != null) {
                string cmName = eStat.Name + nameCounter;
                nameCounter++;

                var activeCM = new ActiveCountermeasure(eStat as ActiveCountermeasureStat, cmName);
                activeCMs.Add(activeCM);
                AttachMonitor(activeCM, element);
            }
        }
        return activeCMs;
    }

    private IEnumerable<ElementSensor> MakeSensors(AElementDesign elementDesign, AUnitElementItem element) {
        int nameCounter = Constants.One;

        var sensors = new List<ElementSensor>();

        SensorStat reqdSRSensorStat = elementDesign.ReqdSRSensorStat;
        string sName = reqdSRSensorStat.Name + nameCounter;
        nameCounter++;
        ElementSensor reqdSRSensor = new ElementSensor(reqdSRSensorStat, sName);
        sensors.Add(reqdSRSensor);
        AttachMonitor(reqdSRSensor, element);

        AEquipmentStat eStat;
        while (elementDesign.GetNextEquipmentStat(EquipmentCategory.ElementSensor, out eStat)) {
            if (eStat != null) {
                sName = eStat.Name + nameCounter;
                nameCounter++;

                var sensor = new ElementSensor(eStat as SensorStat, sName);
                sensors.Add(sensor);
                AttachMonitor(sensor, element);
            }
        }
        return sensors;
    }

    private IEnumerable<CmdSensor> MakeSensors(ACommandDesign cmdDesign, AUnitCmdItem cmd) {
        int nameCounter = Constants.One;

        var sensors = new List<CmdSensor>();

        SensorStat reqdMRSensorStat = cmdDesign.ReqdMRSensorStat;
        string sName = reqdMRSensorStat.Name + nameCounter;
        nameCounter++;
        CmdSensor reqdMRSensor = new CmdSensor(reqdMRSensorStat, sName);
        sensors.Add(reqdMRSensor);
        AttachMonitor(reqdMRSensor, cmd);

        AEquipmentStat eStat;
        while (cmdDesign.GetNextEquipmentStat(EquipmentCategory.CommandSensor, out eStat)) {
            if (eStat != null) {
                sName = eStat.Name + nameCounter;
                nameCounter++;

                var sensor = new CmdSensor(eStat as SensorStat, sName);
                sensors.Add(sensor);
                AttachMonitor(sensor, cmd);
            }
        }
        return sensors;
    }

    private IEnumerable<ShieldGenerator> MakeShieldGenerators(AElementDesign elementDesign, AUnitElementItem element) {
        var generators = new List<ShieldGenerator>();
        AEquipmentStat eStat;
        while (elementDesign.GetNextEquipmentStat(EquipmentCategory.ShieldGenerator, out eStat)) {
            if (eStat != null) {
                var generator = new ShieldGenerator(eStat as ShieldGeneratorStat);
                generators.Add(generator);
                AttachShield(generator, element);
            }
        }
        return generators;
    }

    private IEnumerable<AWeapon> MakeWeapons(AElementDesign elementDesign, AUnitElementItem element, AHull hull) {
        //D.Log("{0}: Making Weapons for {1}.", DebugName, element.DebugName);
        int nameCounter = Constants.One;

        EquipmentCategory[] equipCats = new EquipmentCategory[] { EquipmentCategory.LaunchedWeapon, EquipmentCategory.LosWeapon };
        var weapons = new List<AWeapon>();
        foreach (var eCat in equipCats) {
            EquipmentSlotID slotID;
            AEquipmentStat eStat;
            while (elementDesign.GetNextEquipmentStat(eCat, out slotID, out eStat)) {
                if (eStat != null) {
                    //D.Log("{0} elementDesign = {1}, slotID = {2}, eStat = {3}.", DebugName, elementDesign.DebugName, slotID.DebugName, eStat.DebugName);
                    AWeaponStat wStat = eStat as AWeaponStat;
                    WDVCategory weaponCategory = wStat.DeliveryVehicleCategory;
                    string weaponName = wStat.Name + nameCounter;
                    nameCounter++;

                    AWeapon weapon;
                    switch (weaponCategory) {
                        case WDVCategory.Beam:
                            weapon = new BeamProjector(wStat as BeamWeaponStat, weaponName);
                            break;
                        case WDVCategory.Projectile:
                            weapon = new ProjectileLauncher(wStat as ProjectileWeaponStat, weaponName);
                            break;
                        case WDVCategory.Missile:
                            weapon = new MissileLauncher(wStat as MissileWeaponStat, weaponName);
                            break;
                        case WDVCategory.AssaultVehicle:
                            weapon = new AssaultLauncher(wStat as AssaultWeaponStat, weaponName);
                            break;
                        case WDVCategory.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponCategory));
                    }
                    AttachMonitor(weapon, element);
                    AttachMount(weapon, slotID, hull);
                    weapons.Add(weapon);
                }
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

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
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
    private void AttachMount(AWeapon weapon, EquipmentSlotID slotID, AHull hull) {
        AMount mountPlaceholder;
        AWeaponMount weaponMountPrefab;
        if (slotID.Category == EquipmentCategory.LosWeapon) {
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().Single(placeholder => placeholder.SlotIDD == slotID);
            weaponMountPrefab = _losTurretPrefab;
        }
        else {
            D.AssertEqual(EquipmentCategory.LaunchedWeapon, slotID.Category);
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<LauncherMountPlaceholder>().Single(placeholder => placeholder.SlotIDD == slotID);
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
        if (slotID.Category == EquipmentCategory.LosWeapon) {
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

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
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

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
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
        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
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

    private const string UnitItemNameFormat = "{0}_{1}";    // aka DesignName_Count

    private IDictionary<string, int> __facilityNameCountLookup = new Dictionary<string, int>();
    private IDictionary<string, int> __shipNameCountLookup = new Dictionary<string, int>();
    private IDictionary<string, int> __fleetCmdNameCountLookup = new Dictionary<string, int>();
    private IDictionary<string, int> __starbaseCmdNameCountLookup = new Dictionary<string, int>();
    private IDictionary<string, int> __settlementCmdNameCountLookup = new Dictionary<string, int>();


    private string __GetUniqueFacilityName(string designName) {
        if (!__facilityNameCountLookup.ContainsKey(designName)) {
            __facilityNameCountLookup.Add(designName, 1);
        }
        int catNameCount = __facilityNameCountLookup[designName];
        string name = UnitItemNameFormat.Inject(designName, catNameCount);
        __facilityNameCountLookup[designName] = ++catNameCount;
        return name;
    }

    private string __GetUniqueShipName(string designName) {
        if (!__shipNameCountLookup.ContainsKey(designName)) {
            __shipNameCountLookup.Add(designName, 1);
        }
        int catNameCount = __shipNameCountLookup[designName];
        string name = UnitItemNameFormat.Inject(designName, catNameCount);
        __shipNameCountLookup[designName] = ++catNameCount;
        return name;
    }

    private string __GetUniqueFleetCmdName(string designName) {
        if (!__fleetCmdNameCountLookup.ContainsKey(designName)) {
            __fleetCmdNameCountLookup.Add(designName, 1);
        }
        int catNameCount = __fleetCmdNameCountLookup[designName];
        string name = UnitItemNameFormat.Inject(designName, catNameCount);
        __fleetCmdNameCountLookup[designName] = ++catNameCount;
        return name;
    }

    private string __GetUniqueStarbaseCmdName(string designName) {
        if (!__starbaseCmdNameCountLookup.ContainsKey(designName)) {
            __starbaseCmdNameCountLookup.Add(designName, 1);
        }
        int catNameCount = __starbaseCmdNameCountLookup[designName];
        string name = UnitItemNameFormat.Inject(designName, catNameCount);
        __starbaseCmdNameCountLookup[designName] = ++catNameCount;
        return name;
    }

    private string __GetUniqueSettlementCmdName(string designName) {
        if (!__settlementCmdNameCountLookup.ContainsKey(designName)) {
            __settlementCmdNameCountLookup.Add(designName, 1);
        }
        int catNameCount = __settlementCmdNameCountLookup[designName];
        string name = UnitItemNameFormat.Inject(designName, catNameCount);
        __settlementCmdNameCountLookup[designName] = ++catNameCount;
        return name;
    }



    #endregion

}

