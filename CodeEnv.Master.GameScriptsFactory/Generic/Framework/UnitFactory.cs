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

    private ShipItem[] _shipPrefabs;
    private FacilityItem[] _facilityPrefabs;

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

        _shipPrefabs = reqdPrefabs.ships;
        _facilityPrefabs = reqdPrefabs.facilities;
        _fleetCmdPrefab = reqdPrefabs.fleetCmd.gameObject;
        _starbaseCmdPrefab = reqdPrefabs.starbaseCmd.gameObject;
        _settlementCmdPrefab = reqdPrefabs.settlementCmd.gameObject;

        _countermeasureRangeMonitorPrefab = reqdPrefabs.countermeasureRangeMonitor.gameObject;
        _shieldPrefab = reqdPrefabs.shield.gameObject;
        _weaponRangeMonitorPrefab = reqdPrefabs.weaponRangeMonitor.gameObject;
        _sensorRangeMonitorPrefab = reqdPrefabs.sensorRangeMonitor.gameObject;
        _formationStationPrefab = reqdPrefabs.formationStation.gameObject;
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

    /// <summary>
    /// Makes an instance of an element based on the ShipHullStat provided. The Item  will not be enabled,
    /// nor will their gameObject have a parent. The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="hullStat">The hull stat.</param>
    /// <param name="engineStat">The engine stat.</param>
    /// <param name="combatStance">The combat stance.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="passiveCmStats">The passive cm stats.</param>
    /// <param name="activeCmStats">The active cm stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="shieldGenStats">The shield generator stats.</param>
    /// <returns></returns>
    public ShipItem MakeInstance(ShipHullStat hullStat, EngineStat engineStat, ShipCombatStance combatStance, Player owner,
        IEnumerable<WeaponStat> weapStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        GameObject shipPrefabGo = _shipPrefabs.Single(s => s.category == hullStat.Category).gameObject;
        GameObject shipGoClone = UnityUtility.AddChild(null, shipPrefabGo);

        ShipItem item = shipGoClone.GetSafeMonoBehaviour<ShipItem>();
        PopulateInstance(hullStat, engineStat, combatStance, owner, weapStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided item instance with data from the ShipHullStat object. The item will not be enabled.
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="hullStat">The hull stat.</param>
    /// <param name="engineStat">The engine stat.</param>
    /// <param name="combatStance">The combat stance.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="passiveCmStats">The passive cm stats.</param>
    /// <param name="activeCmStats">The active cm stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="shieldGenStats">The shield generator stats.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(ShipHullStat hullStat, EngineStat engineStat, ShipCombatStance combatStance, Player owner,
        IEnumerable<WeaponStat> weapStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, ref ShipItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var categoryFromItem = item.category;
        D.Assert(hullStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(hullStat.Category.GetValueName(), categoryFromItem.GetValueName()));

        var weapons = MakeWeapons(weapStats, item);
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        var activeCMs = MakeCountermeasures(activeCmStats, item);
        var sensors = MakeSensors(sensorStats);
        var shieldGenerators = MakeShieldGenerators(shieldGenStats, item);

        ShipData data = new ShipData(item.Transform, hullStat, engineStat, combatStance, owner, weapons, activeCMs, sensors, passiveCMs, shieldGenerators);
        item.Data = data;
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

    /// <summary>
    /// Makes an instance of a facility based on the stats provided. The facility will not be enabled.
    /// As the Facility is not yet attached to a Command, the GameObject will have no parent and will not yet have a formation position assigned.
    /// </summary>
    /// <param name="hullStat">The hull stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="passiveCmStats">The passive cm stats.</param>
    /// <param name="activeCmStats">The active cm stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="shieldGenStats">The shield generator stats.</param>
    /// <returns></returns>
    public FacilityItem MakeInstance(FacilityHullStat hullStat, Topography topography, Player owner, IEnumerable<WeaponStat> weapStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        GameObject facilityPrefabGo = _facilityPrefabs.Single(f => f.category == hullStat.Category).gameObject;
        GameObject facilityGoClone = UnityUtility.AddChild(null, facilityPrefabGo);
        FacilityItem item = facilityGoClone.GetSafeMonoBehaviour<FacilityItem>();
        PopulateInstance(hullStat, topography, owner, weapStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided facility instance with data from the stat objects. The facility will not be enabled.
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="hullStat">The hull stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="passiveCmStats">The passive cm stats.</param>
    /// <param name="activeCmStats">The active cm stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="shieldGenStats">The shield generator stats.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(FacilityHullStat hullStat, Topography topography, Player owner, IEnumerable<WeaponStat> weapStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, ref FacilityItem item) {
        var categoryFromItem = item.category;
        D.Assert(hullStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(hullStat.Category.GetValueName(), categoryFromItem.GetValueName()));

        var weapons = MakeWeapons(weapStats, item);
        var passiveCMs = MakeCountermeasures(passiveCmStats);
        var activeCMs = MakeCountermeasures(activeCmStats, item);
        var sensors = MakeSensors(sensorStats);
        var shieldGenerators = MakeShieldGenerators(shieldGenStats, item);

        FacilityData data = new FacilityData(item.Transform, hullStat, topography, owner, weapons, activeCMs, sensors, passiveCMs, shieldGenerators);
        item.Data = data;
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
        var activeCMs = new List<ActiveCountermeasure>(activeCmStats.Count());
        activeCmStats.ForAll(stat => {
            var activeCM = new ActiveCountermeasure(stat);
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

    /// <summary>
    /// Makes and returns weapons made from the provided stats including attaching them to a RangeMonitor of the element.
    /// </summary>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    private IEnumerable<AWeapon> MakeWeapons(IEnumerable<WeaponStat> weapStats, AUnitElementItem element) {
        var weapons = new List<AWeapon>(weapStats.Count());
        weapStats.ForAll(stat => {
            AWeapon weapon;
            switch (stat.DeliveryVehicleCategory) {
                case WDVCategory.Beam:
                    weapon = new BeamProjector(stat);
                    break;
                case WDVCategory.Projectile:
                    weapon = new ProjectileLauncher(stat);
                    break;
                case WDVCategory.Missile:
                    weapon = new MissileLauncher(stat);
                    break;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(stat.DeliveryVehicleCategory));
            }
            weapons.Add(weapon);
            AttachMonitor(weapon, element);
        });
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
            D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(Shield).Name, generator.Name);
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

