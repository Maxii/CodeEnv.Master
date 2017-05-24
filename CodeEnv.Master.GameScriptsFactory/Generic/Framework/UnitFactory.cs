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
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, GameObject unitContainer, string unitName = null) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        return MakeFleetCmdInstance(owner, cameraStat, design, unitContainer, unitName);
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, GameObject unitContainer, string unitName = null) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _fleetCmdPrefab);
        D.AssertEqual((Layers)_fleetCmdPrefab.layer, (Layers)cmdGo.layer);
        FleetCmdItem cmd = cmdGo.GetSafeComponent<FleetCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName);
        return cmd;
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, ref FleetCmdItem cmd, string unitName = null) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd, unitName);
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, ref FleetCmdItem cmd, string unitName = null) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var sensors = MakeSensors(design.SensorStats, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = CommonTerms.Command;
        FleetCmdData data = new FleetCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.CmdStat);
        if (unitName != null) {
            if (data.ParentName != unitName) {   // avoids property equals warning
                data.ParentName = unitName;
            }
        }
        cmd.CameraStat = cameraStat;
        cmd.Data = data;
    }

    /// <summary>
    /// Makes a standalone 'ferry' FleetCmd instance from a single ship using a basic FleetCmdDesign.
    /// The Cmd returned, along with the provided element is parented to an empty GameObject "unitName" which itself is parented to
    /// the Scene's Fleets folder. The Cmd and element (if not already enabled) are all enabled when returned.
    /// <remarks>Be sure to CommenceOperations before issuing an order.</remarks>
    /// </summary>
    /// <param name="unitName">Name of the Unit.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    [Obsolete]
    public FleetCmdItem MakeFerryFleetCmdInstance(string unitName, ShipItem element) {
        D.Assert(element.IsOperational);

        string ferryFleetDesignName = "FerryFleetCmdDesign";
        FleetCmdDesign ferryCmdDesign;
        var playersDesigns = GameManager.Instance.PlayersDesigns;
        if (!playersDesigns.TryGetFleetCmdDesign(element.Owner, ferryFleetDesignName, out ferryCmdDesign)) {
            D.Log("{0} is using a newly designed {1} to make FerryFleetCmd {2} for {3}.", DebugName, ferryFleetDesignName, unitName, element.Owner);
            var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
            var sensorStats = new SensorStat[] { new SensorStat(RangeCategory.Medium), new SensorStat(RangeCategory.Long) };
            var ftlDampenerStat = new FtlDampenerStat();
            UnitCmdStat cmdStat = new UnitCmdStat(unitName, 10F, 100, Formation.Globe);
            ferryCmdDesign = new FleetCmdDesign(element.Owner, ferryFleetDesignName, countermeasureStats, sensorStats, ftlDampenerStat, cmdStat);
            playersDesigns.Add(ferryCmdDesign);
        }

        GameObject unitContainer = new GameObject(unitName);
        UnitDebugControl unitDebugCntl = unitContainer.AddMissingComponent<UnitDebugControl>();
        UnityUtility.AttachChildToParent(unitContainer, FleetsFolder.Instance.gameObject);
        unitContainer.transform.position = element.Position;    // UnitContainer at Vector3.zero when attached to FleetsFolder

        float minViewDistance = TempGameValues.ShipMaxRadius + 1F;  // HACK
        FleetCmdCameraStat cameraStat = new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder: 1F, fov: 60F);
        FleetCmdItem cmd = MakeFleetCmdInstance(element.Owner, cameraStat, ferryCmdDesign, unitContainer, unitName);
        cmd.transform.rotation = element.transform.rotation;
        cmd.IsLoneCmd = true;

        cmd.AddElement(element);    // resets the element's Command property and parents element to Cmd's parent, aka unitContainer
        cmd.HQElement = element;
        cmd.FinalInitialize();
        GameManager.Instance.GameKnowledge.AddUnit(cmd, Enumerable.Empty<IUnitElement>()); // element already present
        unitDebugCntl.Initialize();
        return cmd;
    }

    /// <summary>
    /// Makes a standalone 'Lone' FleetCmd instance from a single element using a basic FleetCmdDesign.
    /// The Cmd returned, along with the provided element is parented to an empty GameObject "unitRootName" which itself is parented to
    /// the Scene's Fleets folder. The Cmd and element are or shortly will be operating when returned.
    /// </summary>
    /// <param name="unitRootName">The root name of the Unit.</param>
    /// <param name="element">The element which is designated the HQ Element.</param>
    public FleetCmdItem MakeLoneFleetInstance(string unitRootName, ShipItem element) {
        D.Assert(element.IsOperational);

        string cmdDesignName = "LoneFleetCmdDesign";
        FleetCmdDesign cmdDesign;
        var playersDesigns = GameManager.Instance.PlayersDesigns;
        if (!playersDesigns.TryGetFleetCmdDesign(element.Owner, cmdDesignName, out cmdDesign)) {
            //D.Log("{0} is using a new design to make LoneFleetCmd {1} for {2}'s {3}.", DebugName, unitRootName, element.Owner, element.DebugName);
            var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
            var sensorStats = new SensorStat[] { new SensorStat(RangeCategory.Medium), new SensorStat(RangeCategory.Long) };
            var ftlDampenerStat = new FtlDampenerStat();
            UnitCmdStat cmdStat = new UnitCmdStat(unitRootName, 10F, 100, Formation.Globe);
            cmdDesign = new FleetCmdDesign(element.Owner, cmdDesignName, countermeasureStats, sensorStats, ftlDampenerStat, cmdStat);
            playersDesigns.Add(cmdDesign);
        }

        string uniqueUnitName = NewGameUnitConfigurator.GetUniqueUnitName(unitRootName);
        GameDate currentDate = GameTime.Instance.CurrentDate;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(uniqueUnitName, element.Owner, currentDate, cmdDesignName, Enumerable.Empty<string>());
        var creator = MakeLoneFleetCreatorInstance(element.Position, config);
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
        var weapons = MakeWeapons(design.WeaponDesigns, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var activeCMs = MakeCountermeasures(design.ActiveCmStats, element);
        var sensors = MakeSensors(design.SensorStats, element);
        var shieldGenerators = MakeShieldGenerators(design.ShieldGeneratorStats, element);
        Priority hqPriority = design.HQPriority;

        var stlEngine = MakeEngine(design.StlEngineStat, "StlEngine");
        FtlEngine ftlEngine = null;
        if (design.FtlEngineStat != null) {
            ftlEngine = MakeEngine(design.FtlEngineStat, "FtlEngine") as FtlEngine;
        }

        element.Name = GetUniqueElementName(hullCategory);
        ShipData data = new ShipData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority,
            stlEngine, ftlEngine, design.CombatStance);
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
    /// Makes an unnamed LoneStarbaseCreator instance at the provided location, parented to the StarbasesFolder.
    /// </summary>
    /// <param name="location">The world space location.</param>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    [Obsolete]
    private LoneStarbaseCreator MakeLoneStarbaseCreatorInstance(Vector3 location, UnitCreatorConfiguration config) {
        GameObject creatorGo = new GameObject();
        UnityUtility.AttachChildToParent(creatorGo, StarbasesFolder.Instance.gameObject);
        creatorGo.transform.position = location;
        var creator = creatorGo.AddComponent<LoneStarbaseCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes a standalone 'Lone' StarbaseCmd instance from a single facility using a basic StarbaseCmdDesign.
    /// The Cmd returned, along with the provided element is parented to an empty GameObject "unitName" which itself is parented to
    /// the Scene's Starbases folder. The Cmd and element are all operating when returned.
    /// </summary>
    /// <param name="unitName">Name of the Unit.</param>
    /// <param name="element">The element which is designated the HQ Element.</param>
    [Obsolete]
    public StarbaseCmdItem MakeLoneStarbaseInstance(string unitName, FacilityItem element) {
        D.Assert(element.IsOperational);

        string cmdDesignName = "LoneStarbaseCmdDesign";
        StarbaseCmdDesign cmdDesign;
        var playersDesigns = GameManager.Instance.PlayersDesigns;
        if (!playersDesigns.TryGetStarbaseCmdDesign(element.Owner, cmdDesignName, out cmdDesign)) {
            D.Log("{0} is using a newly designed {1} to make LoneStarbaseCmd {2} for {3}.", DebugName, cmdDesignName, unitName, element.Owner);
            var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
            var sensorStats = new SensorStat[] { new SensorStat(RangeCategory.Medium), new SensorStat(RangeCategory.Long) };
            var ftlDampenerStat = new FtlDampenerStat();
            UnitCmdStat cmdStat = new UnitCmdStat(unitName, 10F, 100, Formation.Globe);
            cmdDesign = new StarbaseCmdDesign(element.Owner, cmdDesignName, countermeasureStats, sensorStats, ftlDampenerStat, cmdStat);
            playersDesigns.Add(cmdDesign);
        }

        GameDate currentDate = GameTime.Instance.CurrentDate;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, element.Owner, currentDate, cmdDesignName, Enumerable.Empty<string>());
        var creator = MakeLoneStarbaseCreatorInstance(element.Position, config);
        creator.LoneElement = element;
        creator.BuildAndPositionUnit();
        creator.AuthorizeDeployment();
        StarbaseCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<StarbaseCmdItem>();
        return cmd;
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer, string unitName = null) {
        StarbaseCmdDesign design = GameManager.Instance.PlayersDesigns.GetStarbaseCmdDesign(owner, designName);
        return MakeStarbaseCmdInstance(owner, cameraStat, design, unitContainer);
    }

    /// <summary>
    /// Makes an unenabled StarbaseCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, GameObject unitContainer, string unitName = null) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _starbaseCmdPrefab);
        StarbaseCmdItem cmd = cmdGo.GetSafeComponent<StarbaseCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref StarbaseCmdItem cmd, string unitName = null) {
        StarbaseCmdDesign design = GameManager.Instance.PlayersDesigns.GetStarbaseCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd);
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, ref StarbaseCmdItem cmd, string unitName = null) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var sensors = MakeSensors(design.SensorStats, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = CommonTerms.Command;
        StarbaseCmdData data = new StarbaseCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.CmdStat);
        if (unitName != null) {
            if (data.ParentName != unitName) {   // avoids property equals warning
                data.ParentName = unitName;
            }
        }
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
    /// Makes an unnamed, unpositioned LoneSettlementCreator.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <returns></returns>
    [Obsolete]
    private LoneSettlementCreator MakeLoneSettlementCreatorInstance(UnitCreatorConfiguration config) {
        GameObject creatorGo = new GameObject();
        var creator = creatorGo.AddComponent<LoneSettlementCreator>();
        creator.Configuration = config;
        return creator;
    }

    /// <summary>
    /// Makes a standalone 'Lone' StarbaseCmd instance from a single facility using a basic StarbaseCmdDesign.
    /// The Cmd returned, along with the provided element is parented to an empty GameObject "unitName" which itself is parented to
    /// the Scene's Starbases folder. The Cmd and element are all operating when returned.
    /// </summary>
    /// <param name="unitName">Name of the Unit.</param>
    /// <param name="element">The element which is designated the HQ Element.</param>
    /// <param name="settlementOrbitSlot">The settlement orbit slot data.</param>
    /// <param name="localPosition">The local position of the element relative to the OrbitSimulator.</param>
    /// <returns></returns>
    [Obsolete]
    public SettlementCmdItem MakeLoneSettlementInstance(string unitName, FacilityItem element, OrbitData settlementOrbitSlot, Vector3 localPosition) {
        D.Assert(element.IsOperational);

        string cmdDesignName = "LoneSettlementCmdDesign";
        SettlementCmdDesign cmdDesign;
        var playersDesigns = GameManager.Instance.PlayersDesigns;
        if (!playersDesigns.TryGetSettlementCmdDesign(element.Owner, cmdDesignName, out cmdDesign)) {
            D.Log("{0} is using a newly designed {1} to make LoneSettlementCmd {2} for {3}.", DebugName, cmdDesignName, unitName, element.Owner);
            var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
            var sensorStats = new SensorStat[] { new SensorStat(RangeCategory.Medium), new SensorStat(RangeCategory.Long) };
            var ftlDampenerStat = new FtlDampenerStat();
            SettlementCmdStat cmdStat = new SettlementCmdStat(unitName, 10F, 100, Formation.Globe, 10, Constants.OneHundredPercent);
            cmdDesign = new SettlementCmdDesign(element.Owner, cmdDesignName, countermeasureStats, sensorStats, ftlDampenerStat, cmdStat);
            playersDesigns.Add(cmdDesign);
        }

        GameDate currentDate = GameTime.Instance.CurrentDate;
        UnitCreatorConfiguration config = new UnitCreatorConfiguration(unitName, element.Owner, currentDate, cmdDesignName, Enumerable.Empty<string>());
        var creator = MakeLoneSettlementCreatorInstance(config);

        SystemFactory.Instance.InstallCelestialItemInOrbit(creator.gameObject, settlementOrbitSlot, localPosition);

        creator.LoneElement = element;
        creator.BuildAndPositionUnit();
        creator.AuthorizeDeployment();
        SettlementCmdItem cmd = creator.gameObject.GetSingleComponentInImmediateChildren<SettlementCmdItem>();
        return cmd;
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer, string unitName = null) {
        SettlementCmdDesign design = GameManager.Instance.PlayersDesigns.GetSettlementCmdDesign(owner, designName);
        return MakeSettlementCmdInstance(owner, cameraStat, design, unitContainer);
    }

    /// <summary>
    /// Makes an unenabled SettlementCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design, GameObject unitContainer, string unitName = null) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _settlementCmdPrefab);
        //D.Log("{0}: {1}.localPosition = {2} after creation.", DebugName, design.CmdStat.UnitName, cmdGo.transform.localPosition);
        SettlementCmdItem cmd = cmdGo.GetSafeComponent<SettlementCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref SettlementCmdItem cmd, string unitName = null) {
        SettlementCmdDesign design = GameManager.Instance.PlayersDesigns.GetSettlementCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd);
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    /// <param name="unitName">Optional unitName if you wish to override the unitName embedded in the design.</param>
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design, ref SettlementCmdItem cmd, string unitName = null) {
        D.Assert(!cmd.IsOperational, cmd.DebugName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.DebugName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var sensors = MakeSensors(design.SensorStats, cmd);
        var ftlDampener = MakeFtlDampener(design.FtlDampenerStat, cmd);
        cmd.Name = CommonTerms.Command;
        SettlementCmdData data = new SettlementCmdData(cmd, owner, passiveCMs, sensors, ftlDampener, design.CmdStat);
        if (unitName != null) {
            if (data.ParentName != unitName) {   // avoids property equals warning
                data.ParentName = unitName;
            }
        }
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

    public void PopulateInstance(Player owner, Topography topography, FollowableItemCameraStat cameraStat, FacilityDesign design, ref FacilityItem element) {
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
        var weapons = MakeWeapons(design.WeaponDesigns, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var activeCMs = MakeCountermeasures(design.ActiveCmStats, element);
        var sensors = MakeSensors(design.SensorStats, element);
        var shieldGenerators = MakeShieldGenerators(design.ShieldGeneratorStats, element);
        Priority hqPriority = design.HQPriority;

        element.Name = GetUniqueElementName(hullCategory);
        FacilityData data = new FacilityData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority, topography);
        element.GetComponent<Rigidbody>().mass = data.Mass; // 7.26.16 Set externally to keep the Rigidbody out of Data
        element.CameraStat = cameraStat;
        element.Data = data;
    }

    #endregion

    #region Weapons

    /// <summary>
    ///Temporary method for making WeaponDesigns. Randomly picks a mountPlaceholder from the hull and creates a WeaponDesign using the stat and
    ///the mountPlaceholder's MountSlotID and Facing. In the future, this will be done in an ElementDesignScreen where the player picks the mountPlaceholder.
    /// </summary>
    /// <param name="hullCategory">The hull category.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <returns></returns>
    public IEnumerable<WeaponDesign> __MakeWeaponDesigns(ShipHullCategory hullCategory, IEnumerable<AWeaponStat> weapStats) {
        IList<WeaponDesign> weapDesigns = new List<WeaponDesign>(weapStats.Count());
        ShipHull hullPrefab = _shipHullPrefabs.Single(h => h.HullCategory == hullCategory);
        // Make temp hull instance of the right category to get at its placeholders. Prefab references must be temporarily instantiated to use them
        GameObject tempHullGo = UnityUtility.AddChild(null, hullPrefab.gameObject);
        var missileMountPlaceholders = tempHullGo.GetSafeComponentsInChildren<LauncherMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else if (stat.DeliveryVehicleCategory == WDVCategory.AssaultVehicle) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else {
                // LOSWeapon
                var placeholder = RandomExtended.Choice(losMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                losMountPlaceholders.Remove(placeholder);
            }
            var weaponDesign = new WeaponDesign(stat, placeholderSlotID);
            weapDesigns.Add(weaponDesign);
        }
        GameUtility.Destroy(tempHullGo);
        return weapDesigns;
    }

    /// <summary>
    ///Temporary method for making WeaponDesigns. Randomly picks a mountPlaceholder from the hull and creates a PlayerWeaponDesign using the stat and
    ///the mountPlaceholder's MountSlotID and Facing. In the future, this will be done in an ElementDesignScreen where the player picks the mountPlaceholder.
    /// </summary>
    /// <param name="hullCategory">The hull category.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <returns></returns>
    public IEnumerable<WeaponDesign> __MakeWeaponDesigns(FacilityHullCategory hullCategory, IEnumerable<AWeaponStat> weapStats) {
        IList<WeaponDesign> weapDesigns = new List<WeaponDesign>(weapStats.Count());
        FacilityHull hullPrefab = _facilityHullPrefabs.Single(h => h.HullCategory == hullCategory);
        // Make temp hull instance of the right category to get at its placeholders. Prefab references must be temporarily instantiated to use them
        GameObject tempHullGo = UnityUtility.AddChild(null, hullPrefab.gameObject);
        var missileMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<LauncherMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else if (stat.DeliveryVehicleCategory == WDVCategory.AssaultVehicle) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else {
                // LOSWeapon
                var placeholder = RandomExtended.Choice(losMountPlaceholders);
                placeholderSlotID = placeholder.SlotID;
                losMountPlaceholders.Remove(placeholder);
            }
            var weaponDesign = new WeaponDesign(stat, placeholderSlotID);
            weapDesigns.Add(weaponDesign);
        }
        GameUtility.Destroy(tempHullGo);
        return weapDesigns;
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
    /// Makes and returns passive countermeasures made from the provided stats. PassiveCountermeasures do not use RangeMonitors.
    /// </summary>
    /// <param name="passiveCmStats">The cm stats.</param>
    /// <returns></returns>
    private IEnumerable<PassiveCountermeasure> MakeCountermeasures(IEnumerable<PassiveCountermeasureStat> passiveCmStats) {
        var passiveCMs = new List<PassiveCountermeasure>(passiveCmStats.Count());
        passiveCmStats.ForAll(stat => passiveCMs.Add(new PassiveCountermeasure(stat)));
        return passiveCMs;
    }

    /// <summary>
    /// Makes and returns active countermeasures made from the provided stats including attaching them to a RangeMonitor of the element.
    /// </summary>
    /// <param name="activeCmStats">The cm stats.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    private IEnumerable<ActiveCountermeasure> MakeCountermeasures(IEnumerable<ActiveCountermeasureStat> activeCmStats, AUnitElementItem element) {
        int nameCounter = Constants.One;

        var activeCMs = new List<ActiveCountermeasure>(activeCmStats.Count());
        activeCmStats.ForAll(stat => {
            string cmName = stat.Name + nameCounter;
            nameCounter++;

            var activeCM = new ActiveCountermeasure(stat, cmName);
            activeCMs.Add(activeCM);
            AttachMonitor(activeCM, element);
        });
        return activeCMs;
    }

    /// <summary>
    /// Makes and returns sensors made from the provided stats including attaching them to a RangeMonitor of the element.
    /// </summary>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <returns></returns>
    private IEnumerable<ElementSensor> MakeSensors(IEnumerable<SensorStat> sensorStats, AUnitElementItem element) {
        int nameCounter = Constants.One;

        var sensors = new List<ElementSensor>(sensorStats.Count());
        sensorStats.ForAll(stat => {
            string sName = stat.Name + nameCounter;
            nameCounter++;

            var sensor = new ElementSensor(stat, sName);
            sensors.Add(sensor);
            AttachMonitor(sensor, element);
        });
        return sensors;
    }

    private IEnumerable<CmdSensor> MakeSensors(IEnumerable<SensorStat> sensorStats, AUnitCmdItem cmd) {
        int nameCounter = Constants.One;

        var sensors = new List<CmdSensor>(sensorStats.Count());
        sensorStats.ForAll(stat => {
            string sName = stat.Name + nameCounter;
            nameCounter++;

            var sensor = new CmdSensor(stat, sName);
            sensors.Add(sensor);
            AttachMonitor(sensor, cmd);
        });
        return sensors;
    }

    private IEnumerable<AWeapon> MakeWeapons(IEnumerable<WeaponDesign> weaponDesigns, AUnitElementItem element, AHull hull) {
        //D.Log("{0}: Making Weapons for {1}.", DebugName, element.DebugName);
        int nameCounter = Constants.One;
        var weapons = new List<AWeapon>(weaponDesigns.Count());
        foreach (var design in weaponDesigns) {
            AWeaponStat stat = design.WeaponStat;
            MountSlotID mountSlotID = design.MountSlotID;
            WDVCategory weaponCategory = stat.DeliveryVehicleCategory;

            string weaponName = stat.Name + nameCounter;
            nameCounter++;

            AWeapon weapon;
            switch (weaponCategory) {
                case WDVCategory.Beam:
                    weapon = new BeamProjector(stat as BeamWeaponStat, weaponName);
                    break;
                case WDVCategory.Projectile:
                    weapon = new ProjectileLauncher(stat as ProjectileWeaponStat, weaponName);
                    break;
                case WDVCategory.Missile:
                    weapon = new MissileLauncher(stat as MissileWeaponStat, weaponName);
                    break;
                case WDVCategory.AssaultVehicle:
                    weapon = new AssaultLauncher(stat as AssaultWeaponStat, weaponName);
                    break;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponCategory));
            }
            AttachMonitor(weapon, element);
            AttachMount(weapon, mountSlotID, hull);
            weapons.Add(weapon);
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

    private IEnumerable<ShieldGenerator> MakeShieldGenerators(IEnumerable<ShieldGeneratorStat> genStats, AUnitElementItem element) {
        var generators = new List<ShieldGenerator>(genStats.Count());
        genStats.ForAll(gStat => {
            var generator = new ShieldGenerator(gStat);
            generators.Add(generator);
            AttachShield(generator, element);
        });
        return generators;
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
    /// Attaches a newly instantiated AWeaponMount of the proper type and facing to the weapon and the provided slot in the hull.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="mountFacing">The mount facing.</param>
    /// <param name="mountSlotID">The mount slot identifier.</param>
    /// <param name="hull">The hull.</param>
    private void AttachMount(AWeapon weapon, MountSlotID mountSlotID, AHull hull) {
        AMount mountPlaceholder;
        AWeaponMount weaponMountPrefab;
        var losWeapon = weapon as ALOSWeapon;
        if (losWeapon != null) {
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().Single(placeholder => placeholder.SlotID == mountSlotID);
            weaponMountPrefab = _losTurretPrefab;
        }
        else {
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<LauncherMountPlaceholder>().Single(placeholder => placeholder.SlotID == mountSlotID);
            weaponMountPrefab = _launchTubePrefab;
        }
        D.AssertDefault((int)weaponMountPrefab.SlotID); // mount prefabs won't yet have a slotID

        // align the new mount's position and rotation with that of the placeholder it is replacing
        GameObject mountGo = UnityUtility.AddChild(hull.gameObject, weaponMountPrefab.gameObject);
        Transform mountTransform = mountGo.transform;
        mountTransform.rotation = mountPlaceholder.transform.rotation;
        mountTransform.position = mountPlaceholder.transform.position;

        // align the layer of the mount and its children to that of the HullMesh
        Layers hullMeshLayer = (Layers)hull.HullMesh.gameObject.layer;
        UnityUtility.SetLayerRecursively(mountTransform, hullMeshLayer);

        AWeaponMount weaponMount = mountGo.GetComponent<AWeaponMount>();
        weaponMount.SlotID = mountSlotID;
        if (losWeapon != null) {
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

    private const string ElementNameFormat = "{0}{1}";

    private IDictionary<FacilityHullCategory, int> _facilityNameCountLookup = new Dictionary<FacilityHullCategory, int>(FacilityHullCategoryEqualityComparer.Default);

    private IDictionary<ShipHullCategory, int> _shipNameCountLookup = new Dictionary<ShipHullCategory, int>(ShipHullCategoryEqualityComparer.Default);

    private string GetUniqueElementName(FacilityHullCategory cat) {
        if (!_facilityNameCountLookup.ContainsKey(cat)) {
            _facilityNameCountLookup.Add(cat, 1);
        }
        int nameCount = _facilityNameCountLookup[cat];
        string name = ElementNameFormat.Inject(cat.GetValueName(), nameCount);
        _facilityNameCountLookup[cat] = ++nameCount;
        return name;
    }

    private string GetUniqueElementName(ShipHullCategory cat) {
        if (!_shipNameCountLookup.ContainsKey(cat)) {
            _shipNameCountLookup.Add(cat, 1);
        }
        int nameCount = _shipNameCountLookup[cat];
        string name = ElementNameFormat.Inject(cat.GetValueName(), nameCount);
        _shipNameCountLookup[cat] = ++nameCount;
        return name;
    }

    #endregion

}

