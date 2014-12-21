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

        _weaponRangeMonitorPrefab = reqdPrefabs.weaponRangeMonitor.gameObject;
        _sensorRangeMonitorPrefab = reqdPrefabs.sensorRangeMonitor.gameObject;
        _formationStationPrefab = reqdPrefabs.formationStation.gameObject;
    }

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat for this Cmd.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FleetCommandItem MakeInstance(FleetCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _fleetCmdPrefab);
        var cmd = cmdGo.GetSafeMonoBehaviourComponent<FleetCommandItem>();
        MakeInstance(cmdStat, cmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided item instance with data from the stat object.  The Item  will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeInstance(FleetCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner, ref FleetCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new FleetCmdData(cmdStat) {
            Owner = owner
        };
        AttachCountermeasures(cmStats, item);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship using basic default FleetCmdStats.
    /// The FleetCommand returned, along with the provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCommand and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="fleetName">Name of the fleet.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(string fleetName, ShipItem element, Action<FleetCommandItem> onCompletion) {
        FleetCmdStat cmdStat = new FleetCmdStat(fleetName, 10F, 100, Formation.Globe);
        var countermeasureStats = new CountermeasureStat[] { new CountermeasureStat(new CombatStrength(), 0F, 0F) };
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
    public void MakeFleetInstance(FleetCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, ShipItem element, Action<FleetCommandItem> onCompletion) {
        FleetCommandItem cmd = MakeInstance(cmdStat, cmStats, element.Owner);
        GameObject unitGo = new GameObject(cmdStat.Name);
        UnityUtility.AttachChildToParent(unitGo, FleetsFolder.Instance.Folder.gameObject);
        UnityUtility.AttachChildToParent(cmd.gameObject, unitGo);

        if (!element.enabled) {
            D.Warn("{0}.{1} is not enabled. Enabling.", element.Data.Name, element.GetType().Name);
            element.enabled = true;
        }

        cmd.enabled = true;
        UnityUtility.WaitOneToExecute(delegate {
            // wait 1 frame to allow Cmd to initialize
            cmd.AddElement(element);  // resets the element's Command property and parents element to Cmd's parent GO
            cmd.HQElement = element;
            onCompletion(cmd);  // without this delegate, the method returns
        });
        // return cmd   // this non-delegate approach returned the cmd immediately after the Job started
    }

    /// <summary>
    /// Makes an instance of an element based on the shipStat provided. The Item  will not be enabled,
    /// nor will their gameObject have a parent. The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="shipStat">The ship stat.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public ShipItem MakeInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, Player owner) {
        GameObject shipPrefabGo = _shipPrefabs.Single(s => s.category == shipStat.Category).gameObject;
        GameObject shipGoClone = UnityUtility.AddChild(null, shipPrefabGo);

        ShipItem item = shipGoClone.GetSafeMonoBehaviourComponent<ShipItem>();
        PopulateInstance(shipStat, weapStats, cmStats, sensorStats, owner, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided item instance with data from the shipStat object. The item will not be enabled.
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="shipStat">The ship stat.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, Player owner, ref ShipItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var categoryFromItem = item.category;
        D.Assert(shipStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(shipStat.Category.GetName(), categoryFromItem.GetName()));
        ShipData data = new ShipData(shipStat) {
            Owner = owner
        };
        item.Data = data;
        AttachCountermeasures(cmStats, item);
        AttachWeapons(weapStats, item);
        AttachSensors(sensorStats, item);
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public StarbaseCommandItem MakeInstance(StarbaseCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _starbaseCmdPrefab);
        StarbaseCommandItem cmd = cmdGo.GetSafeMonoBehaviourComponent<StarbaseCommandItem>();
        PopulateInstance(cmdStat, cmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(StarbaseCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner, ref StarbaseCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new StarbaseCmdData(cmdStat) {
            Owner = owner
        };
        AttachCountermeasures(cmStats, item);
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public SettlementCommandItem MakeInstance(SettlementCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _settlementCmdPrefab);
        SettlementCommandItem cmd = cmdGo.GetSafeMonoBehaviourComponent<SettlementCommandItem>();
        PopulateInstance(cmdStat, cmStats, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(SettlementCmdStat cmdStat, IEnumerable<CountermeasureStat> cmStats, Player owner, ref SettlementCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new SettlementCmdData(cmdStat) {
            Owner = owner
        };
        AttachCountermeasures(cmStats, item);
    }

    /// <summary>
    /// Makes an instance of a facility based on the stats provided. The facility will not be enabled.
    /// As the Facility is not yet attached to a Command, the GameObject will have no parent and will not yet have
    /// a formation position assigned.
    /// </summary>
    /// <param name="facStat">The facility stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="wStats">The weapon stats.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FacilityItem MakeInstance(FacilityStat facStat, Topography topography, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, Player owner) {
        GameObject facilityPrefabGo = _facilityPrefabs.Single(f => f.category == facStat.Category).gameObject;
        GameObject facilityGoClone = UnityUtility.AddChild(null, facilityPrefabGo);
        FacilityItem item = facilityGoClone.GetSafeMonoBehaviourComponent<FacilityItem>();
        PopulateInstance(facStat, topography, wStats, cmStats, sensorStats, owner, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided facility instance with data from the stat objects. The facility will not be enabled.
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="facStat">The fac stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="wStats">The weapon stats.</param>
    /// <param name="cmStats">The countermeasure stats.</param>
    /// <param name="sensorStats">The sensor stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void PopulateInstance(FacilityStat facStat, Topography topography, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, Player owner, ref FacilityItem item) {
        var categoryFromItem = item.category;
        D.Assert(facStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(facStat.Category.GetName(), categoryFromItem.GetName()));
        FacilityData data = new FacilityData(facStat, topography) {
            Owner = owner
        };
        item.Data = data;
        AttachCountermeasures(cmStats, item);
        AttachWeapons(wStats, item);
        AttachSensors(sensorStats, item);
    }

    public FormationStationMonitor MakeFormationStationInstance(Vector3 stationOffset, FleetCommandItem fleetCmd) {
        // make a folder for neatness if one doesn't yet exist
        GameObject formationStationsFolder = null;
        var stations = fleetCmd.gameObject.GetComponentsInChildren<FormationStationMonitor>();
        if (stations.IsNullOrEmpty()) {
            formationStationsFolder = new GameObject("FormationStations");
            UnityUtility.AttachChildToParent(formationStationsFolder, fleetCmd.gameObject);
            formationStationsFolder.layer = (int)Layers.IgnoreRaycast;
        }
        else {
            formationStationsFolder = stations.First().transform.parent.gameObject;
        }

        GameObject stationGo = UnityUtility.AddChild(formationStationsFolder, _formationStationPrefab);
        FormationStationMonitor station = stationGo.GetSafeMonoBehaviourComponent<FormationStationMonitor>();
        station.StationOffset = stationOffset;
        //D.Log("New FormationStation created at {0}, Offset = {1}, FleetCmd at {2}.", st.transform.position, stationOffset, fleetCmd.transform.position);
        return station;
    }

    private void AttachWeapons(IEnumerable<WeaponStat> weapStats, AUnitElementItem elementItem) {
        weapStats.ForAll(wStat => elementItem.AddWeapon(wStat)); // separate method as can't use a ref variable in a lambda expression
    }

    private void AttachCountermeasures(IEnumerable<CountermeasureStat> cmStats, AMortalItem mortalItem) {
        cmStats.ForAll(cmStat => mortalItem.AddCountermeasure(cmStat));
    }

    private void AttachSensors(IEnumerable<SensorStat> sensorStats, AUnitElementItem elementItem) {
        sensorStats.ForAll(sensorStat => elementItem.AddSensor(sensorStat));
    }

    /// <summary>
    /// Makes or acquires an existing WeaponRangeMonitor and pairs it with this weapon.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="element">The element that owns the weapon and has weapon monitors as children.</param>
    /// <returns></returns>
    public IWeaponRangeMonitor MakeMonitorInstance(Weapon weapon, AUnitElementItem element) {
        var allMonitors = element.gameObject.GetComponentsInChildren<WeaponRangeMonitor>();
        var monitorsInUse = allMonitors.Where(m => m.Range != DistanceRange.None);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
        var monitor = monitorsInUse.FirstOrDefault(m => m.Range == weapon.Range);
        if (monitor == null) {
            var unusedMonitors = allMonitors.Except(monitorsInUse);
            if (unusedMonitors.Any()) {
                monitor = unusedMonitors.First();
            }
            else {
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _weaponRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeMonoBehaviourComponentInChildren<WeaponRangeMonitor>();
            }
            monitor.ParentElement = element;
            //D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
        }
        monitor.Add(weapon);
        return monitor;
    }

    /// <summary>
    /// Makes or acquires an existing SensorRangeMonitor and pairs it with this sensor.
    /// </summary>
    /// <param name="sensor">The sensor from one of the command's elements.</param>
    /// <param name="command">The command that has sensor monitors as children.</param>
    /// <returns></returns>
    public ISensorRangeMonitor MakeMonitorInstance(Sensor sensor, AUnitCommandItem command) {
        var allSensorMonitors = command.gameObject.GetComponentsInChildren<SensorRangeMonitor>();
        var sensorMonitorsInUse = allSensorMonitors.Where(m => m.Range != DistanceRange.None);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
        var monitor = sensorMonitorsInUse.FirstOrDefault(m => m.Range == sensor.Range);
        if (monitor == null) {
            var unusedSensorMonitors = allSensorMonitors.Except(sensorMonitorsInUse);
            if (unusedSensorMonitors.Any()) {
                monitor = unusedSensorMonitors.First();
            }
            else {
                GameObject monitorGo = UnityUtility.AddChild(command.gameObject, _sensorRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeMonoBehaviourComponentInChildren<SensorRangeMonitor>();
            }
            monitor.ParentCommand = command;
            //D.Log("{0} has had a {1} chosen for {2}.", command.FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
        }
        monitor.Add(sensor);
        return monitor;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

