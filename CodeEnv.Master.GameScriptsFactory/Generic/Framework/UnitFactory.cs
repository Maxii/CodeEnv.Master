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

    private ShipItem[] _shipPrefabs;

    private FacilityItem[] _facilityPrefabs;

    private GameObject _fleetCmdPrefab;

    private GameObject _starbaseCmdPrefab;

    private GameObject _settlementCmdPrefab;

    private GameObject _weaponRangeMonitorPrefab;
    private GameObject _formationStationPrefab;

    private OrbiterForShips _orbiterForShipsPrefab;
    private MovingOrbiterForShips _movingOrbiterForShipsPrefab;

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

        _formationStationPrefab = reqdPrefabs.formationStation.gameObject;

        _orbiterForShipsPrefab = reqdPrefabs.orbiterForShips;
        _movingOrbiterForShipsPrefab = reqdPrefabs.movingOrbiterForShips;
    }

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat for this Cmd.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FleetCommandItem MakeInstance(FleetCmdStat cmdStat, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _fleetCmdPrefab);
        var cmd = cmdGo.GetSafeMonoBehaviourComponent<FleetCommandItem>();
        MakeInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided item instance with data from the stat object.  The Item  will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeInstance(FleetCmdStat cmdStat, IPlayer owner, ref FleetCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new FleetCmdData(cmdStat) {
            Owner = owner
        };
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
        FleetCmdStat cmdStat = new FleetCmdStat(fleetName, 10F, 100, Formation.Globe, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F));
        MakeFleetInstance(cmdStat, element, onCompletion);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship. The FleetCmd returned, along with the
    /// provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="cmdStat">The stat for this fleetCmd.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(FleetCmdStat cmdStat, ShipItem element, Action<FleetCommandItem> onCompletion) {
        FleetCommandItem cmd = MakeInstance(cmdStat, element.Owner);
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
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public ShipItem MakeShipInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IPlayer owner) {
        GameObject shipPrefabGo = _shipPrefabs.Single(s => s.category == shipStat.Category).gameObject;
        GameObject shipGoClone = UnityUtility.AddChild(null, shipPrefabGo);

        ShipItem item = shipGoClone.GetSafeMonoBehaviourComponent<ShipItem>();
        MakeShipInstance(shipStat, weapStats, owner, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided item instance with data from the shipStat object. The item will not be enabled. 
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="shipStat">The ship stat.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeShipInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IPlayer owner, ref ShipItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        var categoryFromItem = item.category;
        D.Assert(shipStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(shipStat.Category.GetName(), categoryFromItem.GetName()));
        ShipData data = new ShipData(shipStat) {
            Owner = owner
        };
        item.Data = data;
        AttachWeapons(weapStats, item);
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public StarbaseCommandItem MakeInstance(StarbaseCmdStat cmdStat, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _starbaseCmdPrefab);
        StarbaseCommandItem cmd = cmdGo.GetSafeMonoBehaviourComponent<StarbaseCommandItem>();
        MakeInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided StarbaseCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeInstance(StarbaseCmdStat cmdStat, IPlayer owner, ref StarbaseCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new StarbaseCmdData(cmdStat) {
            Owner = owner
        };
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public SettlementCommandItem MakeInstance(SettlementCmdStat cmdStat, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, _settlementCmdPrefab);
        SettlementCommandItem cmd = cmdGo.GetSafeMonoBehaviourComponent<SettlementCommandItem>();
        MakeInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided SettlementCmd instance with data from the stat object. The item will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeInstance(SettlementCmdStat cmdStat, IPlayer owner, ref SettlementCommandItem item) {
        D.Assert(!item.enabled, "{0} should not be enabled.".Inject(item.FullName));
        item.Data = new SettlementCmdData(cmdStat) {
            Owner = owner
        };
    }

    /// <summary>
    /// Makes an instance of a facility based on the stats provided. The facility will not be enabled.
    /// As the Facility is not yet attached to a Command, the GameObject will have no parent and will not yet have
    /// a formation position assigned.
    /// </summary>
    /// <param name="facStat">The facility stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FacilityItem MakeFacilityInstance(FacilityStat facStat, Topography topography, IEnumerable<WeaponStat> weapStats, IPlayer owner) {
        GameObject facilityPrefabGo = _facilityPrefabs.Single(f => f.category == facStat.Category).gameObject;
        GameObject facilityGoClone = UnityUtility.AddChild(null, facilityPrefabGo);
        FacilityItem item = facilityGoClone.GetSafeMonoBehaviourComponent<FacilityItem>();
        MakeFacilityInstance(facStat, topography, weapStats, owner, ref item);
        return item;
    }

    /// <summary>
    /// Populates the provided facility instance with data from the stat objects. The facility will not be enabled. 
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="facStat">The fac stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="item">The item.</param>
    public void MakeFacilityInstance(FacilityStat facStat, Topography topography, IEnumerable<WeaponStat> weapStats, IPlayer owner, ref FacilityItem item) {
        var categoryFromItem = item.category;
        D.Assert(facStat.Category == categoryFromItem, "{0} should be same as {1}.".Inject(facStat.Category.GetName(), categoryFromItem.GetName()));
        FacilityData data = new FacilityData(facStat, topography) {
            Owner = owner
        };
        item.Data = data;
        AttachWeapons(weapStats, item);
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

    /// <summary>
    /// Makes a weapon from the provided weapon stats, finds or makes a WeaponRangeMonitor to work with it, returning
    /// both the Monitor and the Weapon.
    /// </summary>
    /// <param name="weapStat">The weapon stat specifying the weapon to make and attach.</param>
    /// <param name="element">The element acquiring the weapon.</param>
    /// <param name="weapon">The weapon created.</param>
    /// <returns></returns>
    public IWeaponRangeMonitor MakeWeaponInstance(WeaponStat weapStat, AUnitElementItem element, out Weapon weapon) {
        weapon = new Weapon(weapStat, element.Owner);
        var allWeaponMonitors = element.gameObject.GetComponentsInChildren<WeaponRangeMonitor>();
        var weaponMonitorsInUse = allWeaponMonitors.Where(m => m.Range > Constants.ZeroF);

        // check monitors for range fit, if find it, assign monitor, if not assign unused or create a new monitor and assign it to the weapon
        var wRange = weapon.Range;  // can't use out parameter inside lambda expression
        var monitor = weaponMonitorsInUse.FirstOrDefault(m => Mathfx.Approx(m.Range, wRange, .01F));
        if (monitor == null) {
            var unusedWeaponMonitors = allWeaponMonitors.Except(weaponMonitorsInUse);
            if (unusedWeaponMonitors.Any()) {
                monitor = unusedWeaponMonitors.First();
            }
            else {
                GameObject monitorGo = UnityUtility.AddChild(element.gameObject, _weaponRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeMonoBehaviourComponentInChildren<WeaponRangeMonitor>();
            }
            monitor.ParentElement = element;
            D.Log("{0} has had a {1} chosen for {2}.", element.FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
        }
        monitor.Add(weapon);
        return monitor;
    }

    /// <summary>
    /// Attaches the provided ship to the provided orbiter transform and enables the orbiter script.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="orbiterTransform">The orbiter transform.</param>
    public void AttachShipToOrbiter(ShipItem ship, ref Transform orbiterTransform) {
        D.Assert(orbiterTransform.parent != null, "OrbiterTransform being attached to {0} must already have a parent.".Inject(ship.FullName));
        var orbiter = orbiterTransform.GetSafeInterface<IOrbiterForShips>();
        D.Assert(orbiter != null, "The provided orbiter transform is not a {0}.".Inject(typeof(IOrbiterForShips).Name));
        ship.Transform.parent = orbiterTransform;    // ship retains existing position, rotation, scale and layer
        orbiter.enabled = true;
    }

    /// <summary>
    /// Attaches the provided ship to a newly instantiated IOrbiterForShips which is parented to the provided GameObject.
    /// </summary>
    /// <param name="parent">The parent GameObject for the new Orbiter.</param>
    /// <param name="ship">The ship.</param>
    /// <param name="orbitedObjectIsMobile">if set to <c>true</c> [orbited object is mobile].</param>
    public void AttachShipToOrbiter(GameObject parent, ShipItem ship, bool orbitedObjectIsMobile) {
        GameObject orbiterPrefab = orbitedObjectIsMobile ? _movingOrbiterForShipsPrefab.gameObject : _orbiterForShipsPrefab.gameObject;
        Transform orbiterTransform = UnityUtility.AddChild(parent, orbiterPrefab).transform;
        AttachShipToOrbiter(ship, ref orbiterTransform);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

