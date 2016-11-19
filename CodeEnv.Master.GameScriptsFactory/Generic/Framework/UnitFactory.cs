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

    private MissileTube _missileTubePrefab;
    private LOSTurret _losTurretPrefab;

    private GameObject _fleetCmdPrefab;
    private GameObject _starbaseCmdPrefab;
    private GameObject _settlementCmdPrefab;

    private GameObject _countermeasureRangeMonitorPrefab;
    private GameObject _shieldPrefab;
    private GameObject _weaponRangeMonitorPrefab;
    private GameObject _sensorRangeMonitorPrefab;

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
        _sensorRangeMonitorPrefab = reqdPrefabs.sensorRangeMonitor.gameObject;

        _shipItemPrefab = reqdPrefabs.shipItem;
        _shipHullPrefabs = reqdPrefabs.shipHulls;
        _facilityItemPrefab = reqdPrefabs.facilityItem;
        _facilityHullPrefabs = reqdPrefabs.facilityHulls;

        _missileTubePrefab = reqdPrefabs.missileTube;
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
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", Name, typeof(FleetCreator).Name);
        }
        creatorGo.transform.position = location;
        creatorGo.isStatic = true;
        var creator = creatorGo.GetComponent<FleetCreator>();
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
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, GameObject unitContainer) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        return MakeFleetCmdInstance(owner, cameraStat, design, unitContainer);
    }

    /// <summary>
    /// Makes an unenabled FleetCmd instance for the owner parented to unitContainer.
    /// </summary>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public FleetCmdItem MakeFleetCmdInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, GameObject unitContainer) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _fleetCmdPrefab);
        FleetCmdItem cmd = cmdGo.GetSafeComponent<FleetCmdItem>();
        PopulateInstance(owner, cameraStat, design, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="designName">Name of the design.</param>
    /// <param name="cmd">The item.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, string designName, ref FleetCmdItem cmd) {
        FleetCmdDesign design = GameManager.Instance.PlayersDesigns.GetFleetCmdDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref cmd);
    }

    /// <summary>
    /// Populates the provided FleetCmd instance with data from the design. The item will not be enabled.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="design">The design.</param>
    /// <param name="cmd">The item.</param>
    public void PopulateInstance(Player owner, FleetCmdCameraStat cameraStat, FleetCmdDesign design, ref FleetCmdItem cmd) {
        D.Assert(!cmd.IsOperational, cmd.FullName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.FullName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        cmd.Name = CommonTerms.Command;
        FleetCmdData data = new FleetCmdData(cmd, owner, passiveCMs, design.CmdStat);
        cmd.CameraStat = cameraStat;
        cmd.Data = data;
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship using a basic default FleetCmdDesign.
    /// The FleetCommand returned, along with the provided ship is parented to an empty GameObject "fleetName" which itself is parented to
    /// the Scene's Fleets folder. The fleetCommand and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="fleetName">Name of the fleet.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    public FleetCmdItem MakeFleetInstance(string fleetName, ShipItem element) {
        float minViewDistance = TempGameValues.ShipMaxRadius + 1F;  // HACK
        FleetCmdCameraStat cameraStat = new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder: 1F, fov: 60F);

        UnitCmdStat cmdStat = new UnitCmdStat(fleetName, 10F, 100, Formation.Globe);
        var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
        FleetCmdDesign design = new FleetCmdDesign(element.Owner, "FleetCmdDesignHack", countermeasureStats, cmdStat);

        GameObject unitContainer = new GameObject(fleetName);
        UnityUtility.AttachChildToParent(unitContainer, FleetsFolder.Instance.gameObject);

        FleetCmdItem cmd = MakeFleetCmdInstance(element.Owner, cameraStat, design, unitContainer);
        cmd.AddElement(element);    // resets the element's Command property and parents element to Cmd's parent, aka unitContainer
        cmd.HQElement = element;
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
        D.AssertNotNull(element.transform.parent, element.FullName);
        D.Assert(!element.IsOperational, element.FullName);
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
        var sensors = MakeSensors(design.SensorStats);
        var shieldGenerators = MakeShieldGenerators(design.ShieldGeneratorStats, element);
        Priority hqPriority = design.HQPriority;

        element.Name = GetUniqueElementName(hullCategory);
        ShipData data = new ShipData(element, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority, design.EnginesStat, design.CombatStance);
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
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", Name, typeof(StarbaseCreator).Name);
        }
        creatorGo.transform.position = location;
        creatorGo.isStatic = true;
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
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer) {
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
    /// <returns></returns>
    public StarbaseCmdItem MakeStarbaseCmdInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, GameObject unitContainer) {
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
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref StarbaseCmdItem cmd) {
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
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, StarbaseCmdDesign design, ref StarbaseCmdItem cmd) {
        D.Assert(!cmd.IsOperational, cmd.FullName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.FullName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        cmd.Name = CommonTerms.Command;
        StarbaseCmdData data = new StarbaseCmdData(cmd, owner, passiveCMs, design.CmdStat);
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
            D.Error("{0}: {1} should not start static as it has yet to be positioned.", Name, typeof(SettlementCreator).Name);
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
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, string designName, GameObject unitContainer) {
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
    /// <returns></returns>
    public SettlementCmdItem MakeSettlementCmdInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design, GameObject unitContainer) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _settlementCmdPrefab);
        //D.Log("{0}: {1}.localPosition = {2} after creation.", Name, design.CmdStat.UnitName, cmdGo.transform.localPosition);
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
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, string designName, ref SettlementCmdItem cmd) {
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
    public void PopulateInstance(Player owner, CmdCameraStat cameraStat, SettlementCmdDesign design, ref SettlementCmdItem cmd) {
        D.Assert(!cmd.IsOperational, cmd.FullName);
        if (cmd.transform.parent == null) {
            D.Error("{0} should already have a parent.", cmd.FullName);
        }
        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        cmd.Name = CommonTerms.Command;
        SettlementCmdData data = new SettlementCmdData(cmd, owner, passiveCMs, design.CmdStat);
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
        D.Assert(!element.IsOperational, element.FullName);
        if (element.transform.parent == null) {
            D.Error("{0} should already have a parent.", element.FullName);
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
        var sensors = MakeSensors(design.SensorStats);
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
        var missileMountPlaceholders = tempHullGo.GetSafeComponentsInChildren<MissileMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
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
        var missileMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<MissileMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeComponentsInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
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
    private IEnumerable<Sensor> MakeSensors(IEnumerable<SensorStat> sensorStats) {
        var sensors = new List<Sensor>(sensorStats.Count());
        sensorStats.ForAll(stat => sensors.Add(new Sensor(stat)));
        return sensors;
    }

    private IEnumerable<AWeapon> MakeWeapons(IEnumerable<WeaponDesign> weaponDesigns, AUnitElementItem element, AHull hull) {
        //D.Log("{0}: Making Weapons for {1}.", Name, element.FullName);
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
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", Name, element.FullName, typeof(Shield).Name, generator.Name);
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
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", Name, element.FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
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
            mountPlaceholder = hull.gameObject.GetSafeComponentsInChildren<MissileMountPlaceholder>().Single(placeholder => placeholder.SlotID == mountSlotID);
            weaponMountPrefab = _missileTubePrefab;
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
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", Name, element.FullName, typeof(ActiveCountermeasureRangeMonitor).Name, countermeasure.Name);
        }
        monitor.Add(countermeasure);
    }

    /// <summary>
    /// Makes or acquires an existing SensorRangeMonitor and pairs it with this sensor.
    /// <remarks>This method is public as it is used by the command when an element is attached to it.</remarks>
    /// </summary>
    /// <param name="sensor">The sensor from one of the command's elements.</param>
    /// <param name="command">The command that has sensor monitors as children.</param>
    /// <returns></returns>
    public ISensorRangeMonitor AttachSensorToCmdsMonitor(Sensor sensor, AUnitCmdItem command) {
        var allSensorMonitors = command.gameObject.GetComponentsInChildren<SensorRangeMonitor>();
        var sensorMonitorsInUse = allSensorMonitors.Where(m => m.RangeCategory != RangeCategory.None);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
        var monitor = sensorMonitorsInUse.FirstOrDefault(m => m.RangeCategory == sensor.RangeCategory);
        if (monitor == null) {
            var unusedSensorMonitors = allSensorMonitors.Except(sensorMonitorsInUse);
            if (unusedSensorMonitors.Any()) {
                monitor = unusedSensorMonitors.First();
            }
            else {
                D.AssertEqual(Layers.Collide_DefaultOnly, (Layers)_sensorRangeMonitorPrefab.layer);
                GameObject monitorGo = UnityUtility.AddChild(command.gameObject, _sensorRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.Collide_DefaultOnly;  // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetComponent<SensorRangeMonitor>();
            }
            monitor.ParentItem = command;
            //D.Log("{0}: {1} has had a {2} chosen for {3}.", Name, command.FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
        }
        monitor.Add(sensor);
        return monitor;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

