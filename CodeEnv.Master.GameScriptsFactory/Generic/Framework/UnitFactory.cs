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

    private MissileTube[] _missileTubePrefabs;
    private LOSTurret[] _losTurretPrefabs;

    private GameObject _fleetCmdPrefab;
    private GameObject _starbaseCmdPrefab;
    private GameObject _settlementCmdPrefab;

    private GameObject _countermeasureRangeMonitorPrefab;
    private GameObject _shieldPrefab;
    private GameObject _weaponRangeMonitorPrefab;
    private GameObject _sensorRangeMonitorPrefab;
    private GameObject _formationStationPrefab;

    private UnitFactory() {
        Initialize();
    }

    protected override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;

        _fleetCmdPrefab = reqdPrefabs.fleetCmd.gameObject;
        _starbaseCmdPrefab = reqdPrefabs.starbaseCmd.gameObject;
        _settlementCmdPrefab = reqdPrefabs.settlementCmd.gameObject;

        _countermeasureRangeMonitorPrefab = reqdPrefabs.countermeasureRangeMonitor.gameObject;
        _shieldPrefab = reqdPrefabs.shield.gameObject;
        _weaponRangeMonitorPrefab = reqdPrefabs.weaponRangeMonitor.gameObject;
        _sensorRangeMonitorPrefab = reqdPrefabs.sensorRangeMonitor.gameObject;
        _formationStationPrefab = reqdPrefabs.formationStation.gameObject;

        _shipItemPrefab = reqdPrefabs.shipItem;
        _shipHullPrefabs = reqdPrefabs.shipHulls;
        _facilityItemPrefab = reqdPrefabs.facilityItem;
        _facilityHullPrefabs = reqdPrefabs.facilityHulls;

        _missileTubePrefabs = reqdPrefabs.missileTubes;
        _losTurretPrefabs = reqdPrefabs.losTurrets;
    }

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat for this Cmd.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FleetCmdItem MakeInstance(FleetCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _fleetCmdPrefab);
        var cmd = cmdGo.GetSafeMonoBehaviour<FleetCmdItem>();
        MakeInstance(cmdStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided item instance with data from the stat object.  The Item  will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeInstance(FleetCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref FleetCmdItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new FleetCmdData(item.Transform, cmdStat, owner, passiveCMs);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship using basic default FleetCmdStats.
    /// The FleetCommand returned, along with the provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCommand and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="fleetName">Name of the fleet.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(string fleetName, ShipItem element, Action<FleetCmdItem> onCompletion) {
        FleetCmdStat cmdStat = new FleetCmdStat(fleetName, 10F, 100, Formation.Globe);
        var countermeasureStats = new PassiveCountermeasureStat[] { new PassiveCountermeasureStat() };
        MakeFleetInstance(cmdStat, countermeasureStats, element, onCompletion);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship. The FleetCmd returned, along with the
    /// provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="cmdStat">The stat for this fleetCmd.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(FleetCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> cmStats, ShipItem element, Action<FleetCmdItem> onCompletion) {
        FleetCmdItem cmd = MakeInstance(cmdStat, cmStats, element.Owner);
        GameObject unitGo = new GameObject(cmdStat.Name);
        UnityUtility.AttachChildToParent(unitGo, FleetsFolder.Instance.Folder.gameObject);
        UnityUtility.AttachChildToParent(cmd.gameObject, unitGo);

        if (!element.enabled) {
            D.Warn("{0}.{1} is not enabled. Enabling.", element.Data.Name, element.GetType().Name);
            element.enabled = true;
        }

        cmd.enabled = true;
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            // wait 1 frame to allow Cmd to initialize
            cmd.AddElement(element);  // resets the element's Command property and parents element to Cmd's parent GO
            cmd.HQElement = element;
            onCompletion(cmd);  // without this delegate, the method returns
        });
        // return cmd   // this non-delegate approach returned the cmd immediately after the Job started
    }

    public ShipItem MakeShipInstance(Player owner, string designName) {
        ShipDesign design = GameManager.Instance.PlayerDesigns.GetShipDesign(owner, designName);
        return MakeInstance(owner, design);
    }

    public ShipItem MakeInstance(Player owner, ShipDesign design) {
        ShipHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _shipHullPrefabs.Single(sHull => sHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(null, _shipItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.ShipCull;   // hull layer gets set to item layer by AddChild

        ShipItem element = elementGoClone.GetSafeMonoBehaviour<ShipItem>();
        PopulateInstance(owner, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, string designName, ref ShipItem element) {
        ShipDesign design = GameManager.Instance.PlayerDesigns.GetShipDesign(owner, designName);
        PopulateInstance(owner, design, ref element);
    }

    public void PopulateInstance(Player owner, ShipDesign design, ref ShipItem element) {
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        ShipHull hull = element.gameObject.GetSafeFirstMonoBehaviourInChildren<ShipHull>();
        var hullCategory = design.HullCategory;
        D.Assert(hullCategory == hull.HullCategory, "{0} should be same as {1}.".Inject(hullCategory.GetValueName(), hull.HullCategory.GetValueName()));
        ShipHullEquipment hullEquipment = new ShipHullEquipment(design.HullStat);
        hullEquipment.Hull = hull;

        element.SetSize(hullCategory.__HullDimensions());  // IMPROVE  size should eventually come with the hullStat

        // Make the weapons along with their already selected mounts and add the weapon to the hullEquipment
        var weapons = MakeWeapons(design.WeaponDesigns, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var activeCMs = MakeCountermeasures(design.ActiveCmStats, element);
        var sensors = MakeSensors(design.SensorStats);
        var shieldGenerators = MakeShieldGenerators(design.ShieldGeneratorStats, element);

        ShipData data = new ShipData(element.Transform, hullEquipment, design.EngineStat, design.CombatStance, owner, activeCMs, sensors, passiveCMs, shieldGenerators);
        element.Data = data;
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public StarbaseCmdItem MakeInstance(StarbaseCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _starbaseCmdPrefab);
        StarbaseCmdItem cmd = cmdGo.GetSafeMonoBehaviour<StarbaseCmdItem>();
        PopulateInstance(cmdStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(StarbaseCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref StarbaseCmdItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new StarbaseCmdData(item.Transform, cmdStat, owner, passiveCMs);
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public SettlementCmdItem MakeInstance(SettlementCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _settlementCmdPrefab);
        SettlementCmdItem cmd = cmdGo.GetSafeMonoBehaviour<SettlementCmdItem>();
        PopulateInstance(cmdStat, passiveCmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="passiveCmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(SettlementCmdStat cmdStat, IEnumerable<PassiveCountermeasureStat> passiveCmStats, Player owner, ref SettlementCmdItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        item.Data = new SettlementCmdData(item.Transform, cmdStat, owner, passiveCMs) {
            Approval = UnityEngine.Random.Range(.01F, 1.0F)
        };
    }

    public FacilityItem MakeFacilityInstance(Player owner, Topography topography, string designName) {
        FacilityDesign design = GameManager.Instance.PlayerDesigns.GetFacilityDesign(owner, designName);
        return MakeInstance(owner, topography, design);
    }

    public FacilityItem MakeInstance(Player owner, Topography topography, FacilityDesign design) {
        FacilityHullCategory hullCategory = design.HullCategory;

        GameObject hullPrefabGo = _facilityHullPrefabs.Single(fHull => fHull.HullCategory == hullCategory).gameObject;
        GameObject elementGoClone = UnityUtility.AddChild(null, _facilityItemPrefab.gameObject);
        GameObject hullGoClone = UnityUtility.AddChild(elementGoClone, hullPrefabGo);
        hullGoClone.layer = (int)Layers.FacilityCull;   // hull layer gets set to item layer by AddChild

        FacilityItem element = elementGoClone.GetSafeMonoBehaviour<FacilityItem>();
        PopulateInstance(owner, topography, design, ref element);
        return element;
    }

    public void PopulateInstance(Player owner, Topography topography, string designName, ref FacilityItem element) {
        FacilityDesign design = GameManager.Instance.PlayerDesigns.GetFacilityDesign(owner, designName);
        PopulateInstance(owner, topography, design, ref element);
    }

    public void PopulateInstance(Player owner, Topography topography, FacilityDesign design, ref FacilityItem element) {
        // Find Hull child of Item and attach it to newly made HullEquipment made from HullStat
        FacilityHull hull = element.gameObject.GetSafeFirstMonoBehaviourInChildren<FacilityHull>();
        var hullCategory = design.HullCategory;
        D.Assert(hullCategory == hull.HullCategory, "{0} should be same as {1}.".Inject(hullCategory.GetValueName(), hull.HullCategory.GetValueName()));
        FacilityHullEquipment hullEquipment = new FacilityHullEquipment(design.HullStat);
        hullEquipment.Hull = hull;

        element.SetSize(hullCategory.__HullDimensions());   // IMPROVE  size should eventually come with the hullStat

        // Make the weapons along with their already selected mounts and add the weapon to the hullEquipment
        var weapons = MakeWeapons(design.WeaponDesigns, element, hull);
        weapons.ForAll(weapon => {
            hullEquipment.AddWeapon(weapon);
        });

        var passiveCMs = MakeCountermeasures(design.PassiveCmStats);
        var activeCMs = MakeCountermeasures(design.ActiveCmStats, element);
        var sensors = MakeSensors(design.SensorStats);
        var shieldGenerators = MakeShieldGenerators(design.ShieldGeneratorStats, element);

        FacilityData data = new FacilityData(element.Transform, hullEquipment, topography, owner, activeCMs, sensors, passiveCMs, shieldGenerators);
        element.Data = data;
    }

    public FormationStationMonitor MakeFormationStationInstance(Vector3 stationOffset, FleetCmdItem fleetCmd) {
        // make a folder for neatness if one doesn't yet exist
        GameObject formationStationsFolder = null;
        var stations = fleetCmd.gameObject.GetComponentsInChildren<FormationStationMonitor>();
        if (stations.IsNullOrEmpty()) {
            formationStationsFolder = new GameObject("Formation Stations");
            UnityUtility.AttachChildToParent(formationStationsFolder, fleetCmd.gameObject);
            formationStationsFolder.layer = (int)Layers.IgnoreRaycast;
        }
        else {
            formationStationsFolder = stations.First().transform.parent.gameObject;
        }

        GameObject stationGo = UnityUtility.AddChild(formationStationsFolder, _formationStationPrefab);
        FormationStationMonitor station = stationGo.GetSafeMonoBehaviour<FormationStationMonitor>();
        station.ParentItem = fleetCmd;
        station.StationOffset = stationOffset;
        //D.Log("New FormationStation created at {0}, Offset = {1}, FleetCmd at {2}.", st.transform.position, stationOffset, fleetCmd.transform.position);
        return station;
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
        IDictionary<WDVCategory, int> nameCounter = new Dictionary<WDVCategory, int>() {
            {WDVCategory.Beam, Constants.One},
            {WDVCategory.Missile, Constants.One},
            {WDVCategory.Projectile, Constants.One}
        };

        var activeCMs = new List<ActiveCountermeasure>(activeCmStats.Count());
        activeCmStats.ForAll(stat => {
            var cmCategory = stat.InterceptStrength.Category;
            string cmName = stat.Name + nameCounter[cmCategory];
            nameCounter[cmCategory]++;

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
        IDictionary<WDVCategory, int> nameCounter = new Dictionary<WDVCategory, int>() {
            {WDVCategory.Beam, Constants.One},
            {WDVCategory.Missile, Constants.One},
            {WDVCategory.Projectile, Constants.One}
        };
        var weapons = new List<AWeapon>(weaponDesigns.Count());
        foreach (var design in weaponDesigns) {
            WeaponStat stat = design.WeaponStat;
            MountSlotID mountSlotID = design.MountSlotID;
            Facing mountFacing = design.MountFacing;
            WDVCategory weaponCategory = stat.DeliveryVehicleCategory;

            string weaponName = stat.Name + nameCounter[weaponCategory];
            nameCounter[weaponCategory]++;

            AWeapon weapon;
            switch (weaponCategory) {
                case WDVCategory.Beam:
                    weapon = new BeamProjector(stat, weaponName);
                    break;
                case WDVCategory.Projectile:
                    weapon = new ProjectileLauncher(stat, weaponName);
                    break;
                case WDVCategory.Missile:
                    weapon = new MissileLauncher(stat, weaponName);
                    break;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponCategory));
            }
            AttachMonitor(weapon, element);
            AttachMount(weapon, mountFacing, mountSlotID, hull);
            weapons.Add(weapon);
        }
        // destroy any remaining mount placeholders that didn't get weapons
        var remainingMountPlaceholders = hull.gameObject.GetComponentsInChildren<AMountPlaceholder>();
        if (remainingMountPlaceholders.Any()) {
            remainingMountPlaceholders.ForAll(mp => UnityUtility.Destroy(mp.gameObject));
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
                shield = shieldGo.GetSafeFirstMonoBehaviourInChildren<Shield>();
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
                monitor = monitorGo.GetSafeFirstMonoBehaviourInChildren<WeaponRangeMonitor>();
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
    private void AttachMount(AWeapon weapon, Facing mountFacing, MountSlotID mountSlotID, AHull hull) {
        var weaponDeliveryVehicle = weapon.DeliveryVehicleCategory;
        AMount mountPlaceholder;
        AWeaponMount weaponMountPrefab;
        bool isLOSWeapon = weaponDeliveryVehicle != WDVCategory.Missile;
        if (isLOSWeapon) {
            mountPlaceholder = hull.gameObject.GetSafeMonoBehavioursInChildren<LOSMountPlaceholder>().Single(placeholder => placeholder.slotID == mountSlotID);
            weaponMountPrefab = _losTurretPrefabs.Single(mountPrefab => mountPrefab.facing == mountFacing);
        }
        else {
            mountPlaceholder = hull.gameObject.GetSafeMonoBehavioursInChildren<MissileMountPlaceholder>().Single(placeholder => placeholder.slotID == mountSlotID);
            weaponMountPrefab = _missileTubePrefabs.Single(mountPrefab => mountPrefab.facing == mountFacing);
        }
        D.Assert(mountPlaceholder.facing == mountFacing);
        D.Assert(weaponMountPrefab.SlotID == MountSlotID.None); // mount prefabs won't yet have a slotID

        Quaternion prefabRotation = weaponMountPrefab.transform.rotation;
        GameObject mountGo = UnityUtility.AddChild(hull.gameObject, weaponMountPrefab.gameObject);
        // restore the mount's rotation from the prefab as AddChild sets it to Quaternion.Identity
        mountGo.transform.rotation = prefabRotation;
        mountGo.transform.position = mountPlaceholder.transform.position;

        // align the layer of the mount and its children to that of the HullMesh
        Layers hullMeshLayer = (Layers)hull.HullMesh.gameObject.layer;
        UnityUtility.SetLayerRecursively(mountGo.transform, hullMeshLayer);

        AWeaponMount weaponMount = mountGo.GetSafeMonoBehaviour<AWeaponMount>();
        weaponMount.SlotID = mountSlotID;
        if (isLOSWeapon) {
            var losMountPlaceholder = mountPlaceholder as LOSMountPlaceholder;
            var losWeaponMount = weaponMount as LOSTurret;
            losWeaponMount.InitializeBarrelElevationSettings(losMountPlaceholder.minimumBarrelElevation);
        }
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
                monitor = monitorGo.GetSafeFirstMonoBehaviourInChildren<ActiveCountermeasureRangeMonitor>();
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
    public IEnumerable<WeaponDesign> __MakeWeaponDesigns(ShipHullCategory hullCategory, IEnumerable<WeaponStat> weapStats) {
        IList<WeaponDesign> weapDesigns = new List<WeaponDesign>(weapStats.Count());
        ShipHull hullPrefab = _shipHullPrefabs.Single(h => h.HullCategory == hullCategory);
        // Make temp hull instance of the right category to get at its placeholders. Prefab references must be temporarily instantiated to use them
        GameObject tempHullGo = UnityUtility.AddChild(null, hullPrefab.gameObject);
        var missileMountPlaceholders = tempHullGo.GetSafeMonoBehavioursInChildren<MissileMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeMonoBehavioursInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        Facing placeholderFacing;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderFacing = placeholder.facing;
                placeholderSlotID = placeholder.slotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else {
                // LOSWeapon
                var placeholder = RandomExtended.Choice(losMountPlaceholders);
                placeholderFacing = placeholder.facing;
                placeholderSlotID = placeholder.slotID;
                losMountPlaceholders.Remove(placeholder);
            }
            var weaponDesign = new WeaponDesign(stat, placeholderSlotID, placeholderFacing);
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
    public IEnumerable<WeaponDesign> __MakeWeaponDesigns(FacilityHullCategory hullCategory, IEnumerable<WeaponStat> weapStats) {
        IList<WeaponDesign> weapDesigns = new List<WeaponDesign>(weapStats.Count());
        FacilityHull hullPrefab = _facilityHullPrefabs.Single(h => h.HullCategory == hullCategory);
        // Make temp hull instance of the right category to get at its placeholders. Prefab references must be temporarily instantiated to use them
        GameObject tempHullGo = UnityUtility.AddChild(null, hullPrefab.gameObject);
        var missileMountPlaceholders = tempHullGo.gameObject.GetSafeMonoBehavioursInChildren<MissileMountPlaceholder>().ToList();
        var losMountPlaceholders = tempHullGo.gameObject.GetSafeMonoBehavioursInChildren<LOSMountPlaceholder>().ToList();

        MountSlotID placeholderSlotID;
        Facing placeholderFacing;
        foreach (var stat in weapStats) {
            if (stat.DeliveryVehicleCategory == WDVCategory.Missile) {
                var placeholder = RandomExtended.Choice(missileMountPlaceholders);
                placeholderFacing = placeholder.facing;
                placeholderSlotID = placeholder.slotID;
                missileMountPlaceholders.Remove(placeholder);
            }
            else {
                // LOSWeapon
                var placeholder = RandomExtended.Choice(losMountPlaceholders);
                placeholderFacing = placeholder.facing;
                placeholderSlotID = placeholder.slotID;
                losMountPlaceholders.Remove(placeholder);
            }
            var weaponDesign = new WeaponDesign(stat, placeholderSlotID, placeholderFacing);
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
    public ISensorRangeMonitor MakeMonitorInstance(Sensor sensor, AUnitCmdItem command) {
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
                monitor = monitorGo.GetSafeFirstMonoBehaviourInChildren<SensorRangeMonitor>();
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

