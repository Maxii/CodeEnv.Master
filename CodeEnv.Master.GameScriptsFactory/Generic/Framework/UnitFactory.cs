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
public class UnitFactory : AGenericSingleton<UnitFactory>, IUnitFactory {

    private GameObject[] _aiShipPrefabs;
    private GameObject[] _humanShipPrefabs;

    private FacilityModel[] _facilityPrefabs;
    private GameObject _aiFleetCmdPrefab;
    private GameObject _humanFleetCmdPrefab;

    private GameObject _aiStarbaseCmdPrefab;
    private GameObject _humanStarbaseCmdPrefab;

    private GameObject _aiSettlementCmdPrefab;
    private GameObject _humanSettlementCmdPrefab;

    private GameObject _weaponRangeMonitorPrefab;
    private GameObject _formationStationPrefab;

    private OrbiterForShips _orbiterForShipsPrefab;
    private MovingOrbiterForShips _movingOrbiterForShipsPrefab;

    private UnitFactory() {
        Initialize();
    }

    protected override void Initialize() {
        var reqdPrefabs = RequiredPrefabs.Instance;
        _aiShipPrefabs = reqdPrefabs.aiShips.Select<ShipView, GameObject>(v => v.gameObject).ToArray();
        _humanShipPrefabs = reqdPrefabs.humanShips.Select<ShipView_Player, GameObject>(v => v.gameObject).ToArray();

        _facilityPrefabs = reqdPrefabs.facilities;

        _aiFleetCmdPrefab = reqdPrefabs.aiFleetCmd.gameObject;
        _humanFleetCmdPrefab = reqdPrefabs.humanFleetCmd.gameObject;

        _aiStarbaseCmdPrefab = reqdPrefabs.aiStarbaseCmd.gameObject;
        _humanStarbaseCmdPrefab = reqdPrefabs.humanStarbaseCmd.gameObject;

        _aiSettlementCmdPrefab = reqdPrefabs.aiSettlementCmd.gameObject;
        _humanSettlementCmdPrefab = reqdPrefabs.humanSettlementCmd.gameObject;

        _weaponRangeMonitorPrefab = reqdPrefabs.weaponRangeMonitor.gameObject;
        _formationStationPrefab = reqdPrefabs.formationStation.gameObject;

        _orbiterForShipsPrefab = reqdPrefabs.orbiterForShips;
        _movingOrbiterForShipsPrefab = reqdPrefabs.movingOrbiterForShips;
    }

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance for the owner.
    /// </summary>
    /// <param name="cmdStat">Thestat for this Cmd.</param>
    /// <param name="owner">The owner of the Unit.</param>
    /// <returns></returns>
    public FleetCmdModel MakeFleetCmdInstance(FleetCmdStat cmdStat, IPlayer owner) {
        GameObject cmdPrefab = owner.IsHuman ? _humanFleetCmdPrefab : _aiFleetCmdPrefab;
        GameObject cmdGo = UnityUtility.AddChild(null, cmdPrefab);
        FleetCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<FleetCmdModel>();
        MakeFleetCmdInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided model instance with data from thestat object. If the provided model (view)
    /// is compatible with the designated owner the method returns true. If not, the model is replaced, assuming the same position
    /// and parent, and returns false. The Model and View will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="model">The model.</param>
    /// <returns><c>false</c> if the model was not compatible and had to be replaced.</returns>
    public bool MakeFleetCmdInstance(FleetCmdStat cmdStat, IPlayer owner, ref FleetCmdModel model) {
        if (owner.IsHuman == (model.gameObject.GetComponent<FleetCmdView_Player>() != null)) {
            // the owner and model view are compatible
            D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
            model.Data = new FleetCmdData(cmdStat) {
                Owner = owner
            };
            return true;
        }
        else {
            D.Warn("Provided Cmd {0} is not compatible with owner {1}. Replacing.", model.FullName, owner.LeaderName);
            Vector3 existingPosition = model.transform.position;
            GameObject existingParent = model.transform.parent.gameObject;
            model = MakeFleetCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(model.gameObject, existingParent);
            model.transform.position = existingPosition;
            return false;
        }
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship using basic default FleetCmdStats.
    /// The FleetCmdModel returned, along with the provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd model, view and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="fleetName">Name of the fleet.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="element">The element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(string fleetName, IPlayer owner, ShipModel element, Action<FleetCmdModel> onCompletion) {
        FleetCmdStat cmdStat = new FleetCmdStat(fleetName, 10F, 100, Formation.Globe, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F));
        MakeFleetInstance(cmdStat, owner, element, onCompletion);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship. The FleetCmdModel returned, along with the
    /// provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd model, view and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="cmdStat">Thestat for this fleetCmd.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="element">The ship which is designated the HQ Element.</param>
    /// <param name="onCompletion">Delegate that returns the Fleet on completion.</param>
    public void MakeFleetInstance(FleetCmdStat cmdStat, IPlayer owner, ShipModel element, Action<FleetCmdModel> onCompletion) {
        D.Assert(owner.IsHuman == (element.gameObject.GetComponent<ShipView_Player>() != null), "Owner {0} is not compatible with {1} view.".Inject(owner.LeaderName, element.FullName));
        FleetCmdModel cmd = MakeFleetCmdInstance(cmdStat, owner);
        GameObject unitGo = new GameObject(cmdStat.Name);
        UnityUtility.AttachChildToParent(unitGo, FleetsFolder.Instance.Folder.gameObject);
        UnityUtility.AttachChildToParent(cmd.gameObject, unitGo);

        if (!element.enabled) {
            D.Warn("{0}.{1} is not enabled. Enabling.", element.Data.Name, element.GetType().Name);
            element.enabled = true;
        }
        var elementView = element.gameObject.GetSafeMonoBehaviourComponent<ShipView>();
        if (!elementView.enabled) {
            D.Warn("{0}.{1} is not enabled. Enabling.", element.Data.Name, elementView.GetType().Name);
            elementView.enabled = true;
        }

        cmd.enabled = true;
        cmd.gameObject.GetSafeMonoBehaviourComponent<FleetCmdView>().enabled = true;
        UnityUtility.WaitOneToExecute(delegate {
            // wait 1 frame to allow Cmd to initialize
            cmd.AddElement(element);  // resets the element's Command property and parents element to Cmd's parent GO
            cmd.HQElement = element;
            onCompletion(cmd);  // without this delegate, the method returns
        });
        // return cmd   // this non-delegate approach returned the cmd immediately after the Job started
    }

    /// <summary>
    /// Makes an instance of an element based on the shipStat provided. The Model and View will not be enabled,
    /// nor will their gameObject have a parent. The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="shipStat">The ship stat.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public ShipModel MakeInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IPlayer owner) {
        GameObject[] shipPrefabs = owner.IsHuman ? _humanShipPrefabs : _aiShipPrefabs;
        GameObject shipPrefabGo = shipPrefabs.Single(s => s.name.Contains(shipStat.Category.GetName()));
        GameObject shipGoClone = UnityUtility.AddChild(null, shipPrefabGo);

        ShipModel model = shipGoClone.GetSafeMonoBehaviourComponent<ShipModel>();
        var wasCompatible = MakeInstance(shipStat, weapStats, owner, ref model);
        D.Assert(wasCompatible);    // by definition, the model submitted should be compatible
        return model;
    }

    /// <summary>
    /// Populates the provided model instance with data from the shipStat object. If the provided model (view)
    /// is compatible with the designated owner the method returns true. If not, the model is replaced, assuming the same position
    /// and parent, and returns false. The Model and View will not be enabled. The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="shipStat">The ship stat.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="model">The model.</param>
    /// <returns>
    ///   <c>false</c> if the model was not compatible and had to be replaced.
    /// </returns>
    public bool MakeInstance(ShipStat shipStat, IEnumerable<WeaponStat> weapStats, IPlayer owner, ref ShipModel model) {
        D.Assert(!model.enabled, "{0} should not be enabled.".Inject(model.FullName));
        if (owner.IsHuman == (model.gameObject.GetComponent<ShipView_Player>() != null)) {
            // owner and provided view are compatible
            GameObject shipGo = model.gameObject;
            ShipCategory categoryFromModel = GameUtility.DeriveEnumFromName<ShipCategory>(shipGo.name);
            D.Assert(shipStat.Category == categoryFromModel, "{0} should be same as {1}.".Inject(shipStat.Category.GetName(), categoryFromModel.GetName()));
            ShipData data = new ShipData(shipStat) {
                Owner = owner
            };
            model.Data = data;
            AttachWeapons(weapStats, model);
            return true;
        }
        else {
            D.Warn("Provided ship {0} is not compatible with owner {1}, replacing.", model.FullName, owner.LeaderName);
            Vector3 existingPosition = model.transform.position;
            GameObject existingParent = model.transform.parent.gameObject;
            model = MakeInstance(shipStat, weapStats, owner);
            UnityUtility.AttachChildToParent(model.gameObject, existingParent);
            model.transform.position = existingPosition;
            return false;
        }
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public StarbaseCmdModel MakeStarbaseCmdInstance(StarbaseCmdStat cmdStat, IPlayer owner) {
        GameObject cmdPrefab = owner.IsHuman ? _humanStarbaseCmdPrefab : _aiStarbaseCmdPrefab;
        GameObject cmdGo = UnityUtility.AddChild(null, cmdPrefab);
        StarbaseCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<StarbaseCmdModel>();
        MakeStarbaseCmdInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided model instance with data from thestat object. If the provided model (view)
    /// is compatible with the designated owner the method returns true. If not, the model is replaced, assuming the same position
    /// and parent, and returns false. The Model and View will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="model">The model.</param>
    /// <returns>
    ///   <c>false</c> if the model was not compatible and had to be replaced.
    /// </returns>
    public bool MakeStarbaseCmdInstance(StarbaseCmdStat cmdStat, IPlayer owner, ref StarbaseCmdModel model) {
        if (owner.IsHuman == (model.gameObject.GetComponent<StarbaseCmdView_Player>() != null)) {
            // owner and model view are compatible
            model.Data = new StarbaseCmdData(cmdStat) {
                Owner = owner
            };
            return true;
        }
        else {
            D.Warn("Provided Cmd {0} is not compatible with owner {1}, replacing.", model.FullName, owner.LeaderName);
            Vector3 existingPosition = model.transform.position;
            GameObject existingParent = model.transform.parent.gameObject;
            model = MakeStarbaseCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(model.gameObject, existingParent);
            model.transform.position = existingPosition;
            return false;
        }
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance for the owner.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public SettlementCmdModel MakeSettlementCmdInstance(SettlementCmdStat cmdStat, IPlayer owner) {
        GameObject cmdPrefab = owner.IsHuman ? _humanSettlementCmdPrefab : _aiSettlementCmdPrefab;
        GameObject cmdGo = UnityUtility.AddChild(null, cmdPrefab);
        SettlementCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<SettlementCmdModel>();
        MakeSettlementCmdInstance(cmdStat, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided model instance with data from the stat object. If the provided model (view)
    /// is compatible with the designated owner the method returns true. If not, the model is replaced, assuming the same position
    /// and parent, and returns false. The Model and View will not be enabled.
    /// </summary>
    /// <param name="cmdStat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="model">The model.</param>
    /// <returns><c>false</c> if the model was not compatible and had to be replaced.</returns>
    public bool MakeSettlementCmdInstance(SettlementCmdStat cmdStat, IPlayer owner, ref SettlementCmdModel model) {
        if (owner.IsHuman == (model.gameObject.GetComponent<SettlementCmdView_Player>() != null)) {
            // the owner and model view are compatible
            model.Data = new SettlementCmdData(cmdStat) {
                Owner = owner
            };
            return true;
        }
        else {
            D.Warn("Provided Cmd {0} is not compatible with owner {1}, replacing.", model.FullName, owner.LeaderName);
            Vector3 existingPosition = model.transform.position;
            GameObject existingParent = model.transform.parent.gameObject;
            model = MakeSettlementCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(model.gameObject, existingParent);
            model.transform.position = existingPosition;
            return false;
        }
    }

    /// <summary>
    /// Makes an instance of a facility based on the stats provided. The facilityModel and View will not be enabled.
    /// As the Facility is not yet attached to a Command, the GameObject will have no parent and will not yet have
    /// a formation position assigned.
    /// </summary>
    /// <param name="facStat">The facility stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="weapStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    public FacilityModel MakeInstance(FacilityStat facStat, SpaceTopography topography, IEnumerable<WeaponStat> weapStats, IPlayer owner) {
        GameObject facilityPrefabGo = _facilityPrefabs.Single(f => f.gameObject.name == facStat.Category.GetName()).gameObject;
        GameObject facilityGoClone = UnityUtility.AddChild(null, facilityPrefabGo);
        FacilityModel model = facilityGoClone.GetSafeMonoBehaviourComponent<FacilityModel>();
        MakeInstance(facStat, topography, weapStats, owner, ref model);
        return model;
    }

    /// <summary>
    /// Populates the provided model instance with data from the stat objects. The Model and View will not be enabled. 
    /// The element has yet to be assigned to a Command.
    /// </summary>
    /// <param name="facStat">The fac stat.</param>
    /// <param name="topography">The topography.</param>
    /// <param name="weapStats">The weap stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="model">The model.</param>
    public void MakeInstance(FacilityStat facStat, SpaceTopography topography, IEnumerable<WeaponStat> weapStats, IPlayer owner, ref FacilityModel model) {
        GameObject facilityGo = model.gameObject;
        FacilityCategory categoryFromModel = Enums<FacilityCategory>.Parse(facilityGo.name);
        D.Assert(facStat.Category == categoryFromModel, "{0} should be same as {1}.".Inject(facStat.Category.GetName(), categoryFromModel.GetName()));
        FacilityData data = new FacilityData(facStat, topography) {
            Owner = owner
        };
        model.Data = data;
        AttachWeapons(weapStats, model);
    }

    public FormationStation MakeFormationStationInstance(Vector3 stationOffset, FleetCmdModel fleetCmd) {
        // make a folder for neatness if one doesn't yet exist
        GameObject formationStationsFolder = null;
        var stations = fleetCmd.gameObject.GetComponentsInChildren<FormationStation>();
        if (stations.IsNullOrEmpty()) {
            formationStationsFolder = new GameObject("FormationStations");
            UnityUtility.AttachChildToParent(formationStationsFolder, fleetCmd.gameObject);
            formationStationsFolder.layer = (int)Layers.IgnoreRaycast;
        }
        else {
            formationStationsFolder = stations.First().transform.parent.gameObject;
        }

        GameObject stationGo = UnityUtility.AddChild(formationStationsFolder, _formationStationPrefab);
        FormationStation station = stationGo.GetSafeMonoBehaviourComponent<FormationStation>();
        station.StationOffset = stationOffset;
        //D.Log("New FormationStation created at {0}, Offset = {1}, FleetCmd at {2}.", st.transform.position, stationOffset, fleetCmd.transform.position);
        return station;
    }

    private void AttachWeapons(IEnumerable<WeaponStat> weapStats, AUnitElementModel elementModel) {
        weapStats.ForAll(weapStat => MakeAndAddWeapon(weapStat, elementModel)); // separate method as can't use a ref variable in a lambda expression
    }

    /// <summary>
    /// Primary method to use when making and adding a weapon to an Element. 
    /// </summary>
    /// <param name="weapStat">The weapon stat specifying the weapon to make.</param>
    /// <param name="elementModel">The element model to attach the weapon too.</param>
    private void MakeAndAddWeapon(WeaponStat weapStat, AUnitElementModel elementModel) {
        var weapon = new Weapon(weapStat);
        var allWeaponMonitors = elementModel.gameObject.GetInterfacesInChildren<IWeaponRangeMonitor>();
        var weaponMonitorsInUse = allWeaponMonitors.Where(m => m.Range != Constants.ZeroF);
        var wRange = weapon.Range;

        // check monitors for range fit, if find it, assign ID, if not assign or create a monitor and assign its ID to the weapon
        var monitor = weaponMonitorsInUse.FirstOrDefault(m => m.RangeSpan.ContainsValue(wRange));
        if (monitor == null) {
            var unusedWeaponMonitors = allWeaponMonitors.Except(weaponMonitorsInUse);
            if (!unusedWeaponMonitors.IsNullOrEmpty()) {
                monitor = unusedWeaponMonitors.First();
            }
            else {
                GameObject monitorGo = UnityUtility.AddChild(elementModel.gameObject, _weaponRangeMonitorPrefab);
                monitorGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                monitor = monitorGo.GetSafeInterfaceInChildren<IWeaponRangeMonitor>();
            }
            //D.Log("{0}'s {1} with Range {2} assigned new Range {3}.", elementModel.FullName, typeof(IWeaponRangeTracker).Name, rTracker.Range, wRange);
            monitor.Range = wRange;
        }
        elementModel.AddWeapon(weapon, monitor);
        // IMPROVE how to keep track ranges from overlapping
    }

    /// <summary>
    /// Attaches the provided ship to a newly instantiated IOrbiterForShips which is parented to the provided GameObject.
    /// </summary>
    /// <param name="parent">The parent GameObject for the new Orbiter.</param>
    /// <param name="ship">The ship.</param>
    /// <param name="orbitedObjectIsMobile">if set to <c>true</c> [orbited object is mobile].</param>
    public void AttachShipToOrbiter(GameObject parent, IShipModel ship, bool orbitedObjectIsMobile) {
        GameObject orbiterPrefab = orbitedObjectIsMobile ? _movingOrbiterForShipsPrefab.gameObject : _orbiterForShipsPrefab.gameObject;
        Transform orbiterTransform = UnityUtility.AddChild(parent, orbiterPrefab).transform;
        AttachShipToOrbiter(ship, ref orbiterTransform);
    }

    /// <summary>
    /// Attaches the provided ship to the provided orbiter transform and enables the orbiter script.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="orbiterTransform">The orbiter transform.</param>
    public void AttachShipToOrbiter(IShipModel ship, ref Transform orbiterTransform) {
        D.Assert(orbiterTransform.parent != null, "OrbiterTransform being attached to {0} must already have a parent.".Inject(ship.FullName));
        var orbiter = orbiterTransform.GetSafeInterface<IOrbiterForShips>();
        D.Assert(orbiter != null, "The provided orbiter transform is not a {0}.".Inject(typeof(IOrbiterForShips).Name));
        ship.Transform.parent = orbiterTransform;    // ship retains existing position, rotation, scale and layer
        orbiter.enabled = true;
    }

}

