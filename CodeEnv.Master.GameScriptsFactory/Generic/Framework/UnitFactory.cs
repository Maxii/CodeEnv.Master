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
/// Singleton factory that makes instances of Elements and Commands.
/// It also can make a standalone Fleet encompassing a single ship.
/// </summary>
public class UnitFactory : AGenericSingleton<UnitFactory> {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

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

    private UnitFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;

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

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat for this Cmd.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public FleetCmdItem MakeInstance(UnitCmdStat cmdStat, CameraFleetCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, GameObject unitContainer) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _fleetCmdPrefab);
        var cmd = cmdGo.GetSafeComponent<FleetCmdItem>();
        PopulateInstance(cmdStat, cameraStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided item instance with data from the stat object.  The Item  will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(UnitCmdStat cmdStat, CameraFleetCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref FleetCmdItem item) {
        D.Assert(!item.IsOperational, "{0} should not be operational.", item.FullName);
        D.Assert(item.transform.parent != null, "{0} should already have a parent.", item.FullName);
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new FleetCmdData(item.transform, owner, cameraStat, passiveCMs, cmdStat);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship using basic default FleetCmdStats.
    /// The FleetCommand returned, along with the provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCommand and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="fleetName">Name of the fleet.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    public FleetCmdItem MakeFleetInstance(string fleetName, ShipItem element) {
        UnitCmdStat cmdStat = new UnitCmdStat(fleetName, 10F, 100, Formation.Globe);
        float minViewDistance = TempGameValues.ShipMaxRadius + 1F;
        CameraFleetCmdStat cameraStat = new CameraFleetCmdStat(minViewDistance, optViewDistanceAdder: 1F, fov: 60F);
        var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
        return MakeFleetInstance(cmdStat, cameraStat, countermeasureStats, element);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship. The FleetCmd returned, along with the
    /// provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="cmdStat">The stat for this fleetCmd.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    public FleetCmdItem MakeFleetInstance(UnitCmdStat cmdStat, CameraFleetCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> cmStats, ShipItem element) {
        D.Assert(element.IsOperational, "{0} should already be operational to use this method.", element.Name);

        GameObject unitContainer = new GameObject(cmdStat.UnitName);
        UnityUtility.AttachChildToParent(unitContainer, FleetsFolder.Instance.Folder.gameObject);
        FleetCmdItem cmd = MakeInstance(cmdStat, cameraStat, cmStats, element.Owner, unitContainer);
        cmd.AddElement(element);  // resets the element's Command property and parents element to Cmd's parent GO
        cmd.HQElement = element;
        return cmd;
    }

    public ShipItem MakeShipInstance(Player owner, CameraFollowableStat cameraStat, string designName) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(owner, designName);
        return MakeInstance(owner, cameraStat, design);
    }

    public ShipItem MakeInstance(Player owner, CameraFollowableStat cameraStat, ShipDesign design) {
        ShipHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _shipHullPrefabs.Single(sHull => sHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(null, _shipItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.ShipCull;   // hull layer gets set to item layer by AddChild

        ShipItem element = elementGoClone.GetSafeComponent<ShipItem>();
        PopulateInstance(owner, cameraStat, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, CameraFollowableStat cameraStat, string designName, ref ShipItem element) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(owner, designName);
        PopulateInstance(owner, cameraStat, design, ref element);
    }

    public void PopulateInstance(Player owner, CameraFollowableStat cameraStat, ShipDesign design, ref ShipItem element) {
        D.Assert(!element.IsOperational, "{0} should not be operational.", element.FullName);
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        ShipHull hull = element.gameObject.GetSingleComponentInChildren<ShipHull>();
        var hullCategory = design.HullCategory;
        D.Assert(hullCategory == hull.HullCategory, "{0} should be same as {1}.".Inject(hullCategory.GetValueName(), hull.HullCategory.GetValueName()));
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

        Rigidbody elementRigidbody = element.GetComponent<Rigidbody>();
        ShipData data = new ShipData(element.transform, owner, cameraStat, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, elementRigidbody, design.EnginesStat, design.CombatStance);
        element.Data = data;
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeInstance(UnitCmdStat cmdStat, CameraUnitCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, GameObject unitContainer) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _starbaseCmdPrefab);
        StarbaseCmdItem cmd = cmdGo.GetSafeComponent<StarbaseCmdItem>();
        PopulateInstance(cmdStat, cameraStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(UnitCmdStat cmdStat, CameraUnitCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref StarbaseCmdItem item) {
        D.Assert(!item.IsOperational, "{0} should not be operational.", item.FullName);
        D.Assert(item.transform.parent != null, "{0} should already have a parent.", item.FullName);
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new StarbaseCmdData(item.transform, owner, cameraStat, passiveCMs, cmdStat);
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="unitContainer">The unit container.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeInstance(SettlementCmdStat cmdStat, CameraUnitCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, GameObject unitContainer) {
        GameObject cmdGo = UnityUtility.AddChild(unitContainer, _settlementCmdPrefab);
        SettlementCmdItem cmd = cmdGo.GetSafeComponent<SettlementCmdItem>();
        PopulateInstance(cmdStat, cameraStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cameraStat">The camera stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(SettlementCmdStat cmdStat, CameraUnitCmdStat cameraStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref SettlementCmdItem item) {
        D.Assert(!item.IsOperational, "{0} should not be operational.", item.FullName);
        D.Assert(item.transform.parent != null, "{0} should already have a parent.", item.FullName);
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new SettlementCmdData(item.transform, owner, cameraStat, passiveCMs, cmdStat) {
            Approval = UnityEngine.Random.Range(Constants.ZeroPercent, Constants.OneHundredPercent)
        };
    }

    public FacilityItem MakeFacilityInstance(Player owner, Topography topography, CameraFollowableStat cameraStat, string designName) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(owner, designName);
        return MakeInstance(owner, topography, cameraStat, design);
    }

    public FacilityItem MakeInstance(Player owner, Topography topography, CameraFollowableStat cameraStat, FacilityDesign design) {
        FacilityHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _facilityHullPrefabs.Single(fHull => fHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(null, _facilityItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.FacilityCull;   // hull layer gets set to item layer by AddChild

        FacilityItem element = elementGoClone.GetSafeComponent<FacilityItem>();
        PopulateInstance(owner, topography, cameraStat, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, Topography topography, CameraFollowableStat cameraStat, string designName, ref FacilityItem element) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(owner, designName);
        PopulateInstance(owner, topography, cameraStat, design, ref element);
    }

    public void PopulateInstance(Player owner, Topography topography, CameraFollowableStat cameraStat, FacilityDesign design, ref FacilityItem element) {
        D.Assert(!element.IsOperational, "{0} should not be operational.", element.FullName);
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        FacilityHull hull = element.gameObject.GetSingleComponentInChildren<FacilityHull>();
        var hullCategory = design.HullCategory;
        D.Assert(hullCategory == hull.HullCategory, "{0} should be same as {1}.".Inject(hullCategory.GetValueName(), hull.HullCategory.GetValueName()));
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

        Rigidbody elementRigidbody = element.GetComponent<Rigidbody>();
        FacilityData data = new FacilityData(element.transform, owner, cameraStat, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, elementRigidbody, topography);
        element.Data = data;
    }

    /// <summary>
    /// Makes and returns passive countermeasures made from the provided stats. PassiveCountermeasures donot use RangeMonitors.
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
        //D.Log("{0}: Making Weapons for {1}.", GetType().Name, element.FullName);
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
                UnityUtility.Destroy(mp.gameObject);
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
                shield = shieldGo.GetSafeComponent<Shield>();
            }
            shield.ParentItem = element;
            //D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(Shield).Name, generator.Name);
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
        D.Assert(weapon.RangeMonitor == null);
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
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _weaponRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeComponent<WeaponRangeMonitor>();
            }
            monitor.ParentItem = element;
            //D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
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
        D.Assert(weaponMountPrefab.SlotID == MountSlotID.None); // mount prefabs won't yet have a slotID

        // align the new mount's position and rotation with that of the placeholder it is replacing
        GameObject mountGo = UnityUtility.AddChild(hull.gameObject, weaponMountPrefab.gameObject);
        Transform mountTransform = mountGo.transform;
        mountTransform.rotation = mountPlaceholder.transform.rotation;
        mountTransform.position = mountPlaceholder.transform.position;

        // align the layer of the mount and its children to that of the HullMesh
        Layers hullMeshLayer = (Layers)hull.HullMesh.gameObject.layer;
        UnityUtility.SetLayerRecursively(mountTransform, hullMeshLayer);

        AWeaponMount weaponMount = mountGo.GetSafeComponent<AWeaponMount>();
        weaponMount.SlotID = mountSlotID;
        if (losWeapon != null) {
            // LOS weapon
            var losMountPlaceholder = mountPlaceholder as LOSMountPlaceholder;
            var losWeaponMount = weaponMount as LOSTurret;
            losWeaponMount.InitializeBarrelElevationSettings(losMountPlaceholder.MinimumBarrelElevation);
        }
        mountPlaceholder.transform.parent = null;   // detach placeholder from hull so it won't be found as Destroy takes a frame
        UnityUtility.Destroy(mountPlaceholder.gameObject);
        weapon.WeaponMount = weaponMount;
    }

    /// <summary>
    /// Makes or acquires an existing ActiveCountermeasureRangeMonitor and attaches it to this active countermeasure.
    /// Note: The monitor will be added and its events hooked up to the element when the element's data is attached.
    /// </summary>
    /// <param name="countermeasure">The countermeasure.</param>
    /// <param name="element">The element.</param>
    private void AttachMonitor(ActiveCountermeasure countermeasure, AUnitElementItem element) {
        D.Assert(countermeasure.RangeMonitor == null);
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
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _countermeasureRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeComponent<ActiveCountermeasureRangeMonitor>();
            }
            monitor.ParentItem = element;
            //D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(ActiveCountermeasureRangeMonitor).Name, countermeasure.Name);
        }
        monitor.Add(countermeasure);
    }

    /// <summary>
    ///Temporary method for making WeaponDesigns. Randomly picks a mountPlaceholder from the hull and creates a WeaponDesign using the stat and
    ///the mountPlaceholder's MountSlotID and Facing. In the future, this will be done in an ElementDesignScreen where the player picks the mountPlaceholder.
    /// </summary>
    /// <param name="hullCategory">The hull category.</param>
    /// <param name="weapStats">The weap stats.</param>
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
        UnityUtility.Destroy(tempHullGo);
        return weapDesigns;
    }

    /// <summary>
    ///Temporary method for making WeaponDesigns. Randomly picks a mountPlaceholder from the hull and creates a PlayerWeaponDesign using the stat and
    ///the mountPlaceholder's MountSlotID and Facing. In the future, this will be done in an ElementDesignScreen where the player picks the mountPlaceholder.
    /// </summary>
    /// <param name="hullCategory">The hull category.</param>
    /// <param name="weapStats">The weap stats.</param>
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
        UnityUtility.Destroy(tempHullGo);
        return weapDesigns;
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
                GameObject monitorGo = UnityUtility.AddChild(command.gameObject, _sensorRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeComponent<SensorRangeMonitor>();
            }
            monitor.ParentItem = command;
            //D.Log("{0} has had a {1} chosen for {2}.", command.FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
        }
        monitor.Add(sensor);
        return monitor;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

