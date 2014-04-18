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

    private ShipModel[] shipPrefabs;
    private FacilityModel[] facilityPrefabs;
    private WeaponRangeTracker weaponRangeTrackerPrefab;
    private FleetCmdModel fleetCmdPrefab;
    private StarbaseCmdModel starbaseCmdPrefab;
    private SettlementCmdModel settlementCmdPrefab;
    private FormationStation formationStationTrackerPrefab;

    private UnitFactory() {
        Initialize();
    }

    protected override void Initialize() {
        shipPrefabs = RequiredPrefabs.Instance.ships;
        facilityPrefabs = RequiredPrefabs.Instance.facilities;
        weaponRangeTrackerPrefab = RequiredPrefabs.Instance.weaponRangeTracker;
        fleetCmdPrefab = RequiredPrefabs.Instance.fleetCmd;
        starbaseCmdPrefab = RequiredPrefabs.Instance.starbaseCmd;
        settlementCmdPrefab = RequiredPrefabs.Instance.settlementCmd;
        formationStationTrackerPrefab = RequiredPrefabs.Instance.formationStationTracker;
    }

    /// <summary>
    /// Makes an unparented, unenabled FleetCommand instance. 
    /// </summary>
    /// <param name="unitName">The name of the Unit.</param>
    /// <param name="owner">The owner of the Unit.</param>
    /// <returns></returns>
    public FleetCmdModel MakeFleetCmdInstance(string unitName, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, fleetCmdPrefab.gameObject);
        FleetCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<FleetCmdModel>();
        PopulateCommand(unitName, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided instance of FleetCmdModel with data. As this
    /// <c>cmd</c> as yet has no elements, both the model and view are not yet enabled.
    /// </summary>
    /// <param name="unitName">Name of the unit.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="cmd">The fleet command.</param>
    public void PopulateCommand(string unitName, IPlayer owner, ref FleetCmdModel cmd) {
        cmd.Data = new FleetCmdData(unitName, 10F) {
            Strength = new CombatStrength(),
            Owner = owner,
            UnitFormation = Formation.Globe
        };
        cmd.gameObject.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(cmd.transform);
    }

    /// <summary>
    /// Makes a standalone fleet instance from a single ship. The FleetCmdModel returned, along with the
    /// provided ship is parented to an empty GameObject "the fleet" which itself is parented to
    /// the Scene's Fleets folder. The fleetCmd model, view and ship (if not already enabled) are all enabled when returned.
    /// </summary>
    /// <param name="unitName">The name of the unit.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <param name="element">The ship which becomes the HQ Element.</param>
    /// <returns></returns>
    public FleetCmdModel MakeFleetInstance(string unitName, IPlayer owner, ShipModel element) {
        FleetCmdModel cmd = MakeFleetCmdInstance(unitName, owner);
        GameObject unitGo = new GameObject(unitName);
        UnityUtility.AttachChildToParent(cmd.gameObject, unitGo);
        UnityUtility.AttachChildToParent(unitGo, Fleets.Instance.Folder.gameObject);

        cmd.AddElement(element);  // resets the element's Command property and parents element to Cmd's parent GO
        cmd.enabled = true;    // picks this element as the HQ Element. Cmd positions itself over element
        // enabling cmd model also enables the view and the ship
        // can't set PlayerIntelLevel here as View needs time to initialize Presenter after enabled
        return cmd;
    }

    /// <summary>
    /// Makes an instance of a ship based on the ShipStats provided. The shipModel and View will not be enabled.
    /// As the Ship is not yet attached to a Command, the GameObject will have no parent and will not yet have
    /// a formation position assigned.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <returns></returns>
    public ShipModel MakeInstance(ShipStats stat) {
        ShipData data = new ShipData(stat.Category, stat.Name, stat.MaxHitPoints, stat.Mass, stat.Drag) {
            FullThrust = stat.FullThrust,
            MaxTurnRate = stat.MaxTurnRate,
            Strength = stat.Strength,
            CurrentHitPoints = stat.CurrentHitPoints,
            CombatStance = stat.CombatStance
        };

        GameObject shipPrefabGo = shipPrefabs.Single(s => s.gameObject.name == stat.Category.GetName()).gameObject;
        GameObject shipGoClone = UnityUtility.AddChild(null, shipPrefabGo);

        ShipModel model = shipGoClone.GetSafeMonoBehaviourComponent<ShipModel>();
        model.Data = data;

        AttachWeapons(stat.Weapons, model);

        // this is not really necessary as Ship's prefab should already have Model as its Mesh's CameraLOSChangedRelay target
        shipGoClone.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(shipGoClone.transform);
        return model;
    }

    /// <summary>
    /// Populates the provided ShipModel instance with data from the stats object.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <param name="model">The model.</param>
    public void MakeInstance(ShipStats stat, ref ShipModel model) {
        GameObject shipGo = model.gameObject;
        ShipCategory categoryFromModel = Enums<ShipCategory>.Parse(shipGo.name);
        D.Assert(stat.Category == categoryFromModel, "{0} should be same as {1}.".Inject(stat.Category.GetName(), categoryFromModel.GetName()));
        ShipData data = new ShipData(stat.Category, stat.Name, stat.MaxHitPoints, stat.Mass, stat.Drag) {
            FullThrust = stat.FullThrust,
            MaxTurnRate = stat.MaxTurnRate,
            Strength = stat.Strength,
            CurrentHitPoints = stat.CurrentHitPoints,
            CombatStance = stat.CombatStance
        };
        model.Data = data;
        AttachWeapons(stat.Weapons, model);

        // this is not really necessary as ShipGo should already have Model as its Mesh's CameraLOSChangedRelay target
        shipGo.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(shipGo.transform);
    }

    /// <summary>
    /// Makes an unparented, unenabled StarbaseCmd instance.
    /// </summary>
    /// <param name="unitName">The name of the unit.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public StarbaseCmdModel MakeStarbaseCmdInstance(string unitName, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, starbaseCmdPrefab.gameObject);
        StarbaseCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<StarbaseCmdModel>();
        PopulateCommand(unitName, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided instance of StarbaseCmdModel with data. As this
    /// <c>cmd</c> as yet has no elements, both the model and view are not yet enabled.
    /// </summary>
    /// <param name="unitName">Name of the unit.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="cmd">The unit command.</param>
    public void PopulateCommand(string unitName, IPlayer owner, ref StarbaseCmdModel cmd) {
        cmd.Data = new StarbaseCmdData(unitName, 10F) {
            Strength = new CombatStrength(),
            Owner = owner,
            UnitFormation = Formation.Circle
        };
        cmd.gameObject.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(cmd.transform);
    }

    /// <summary>
    /// Makes an unparented, unenabled SettlementCmd instance.
    /// </summary>
    /// <param name="unitName">The name of the unit.</param>
    /// <param name="owner">The owner of the unit.</param>
    /// <returns></returns>
    public SettlementCmdModel MakeSettlementCmdInstance(string unitName, IPlayer owner) {
        GameObject cmdGo = UnityUtility.AddChild(null, settlementCmdPrefab.gameObject);
        SettlementCmdModel cmd = cmdGo.GetSafeMonoBehaviourComponent<SettlementCmdModel>();
        PopulateCommand(unitName, owner, ref cmd);
        return cmd;
    }

    /// <summary>
    /// Populates the provided instance of SettlementCmdModel with data. As this
    /// <c>cmd</c> as yet has no elements, both the model and view are not yet enabled.
    /// </summary>
    /// <param name="unitName">Name of the unit.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="cmd">The unit command.</param>
    public void PopulateCommand(string unitName, IPlayer owner, ref SettlementCmdModel cmd) {
        cmd.Data = new SettlementCmdData(unitName, 10F) {
            Strength = new CombatStrength(0F, 10F, 0F, 10F, 0F, 10F),  // no offense, strong defense
            Owner = owner,
            UnitFormation = Formation.Circle,
            Population = 100,
            CapacityUsed = 10,
            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F))
        };
        cmd.gameObject.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(cmd.transform);
    }

    /// <summary>
    /// Makes an instance of a facility based on the FacilityStats provided. The facilityModel and View will not be enabled.
    /// As the Facility is not yet attached to a Command, the GameObject will have no parent and will not yet have
    /// a formation position assigned.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <returns></returns>
    public FacilityModel MakeInstance(FacilityStats stat) {
        FacilityData data = new FacilityData(stat.Category, stat.Name, stat.MaxHitPoints, stat.Mass) {
            Strength = stat.Strength,
            CurrentHitPoints = stat.CurrentHitPoints
        };

        GameObject facilityPrefabGo = facilityPrefabs.Single(f => f.gameObject.name == stat.Category.GetName()).gameObject;
        GameObject facilityGoClone = UnityUtility.AddChild(null, facilityPrefabGo);

        FacilityModel model = facilityGoClone.GetSafeMonoBehaviourComponent<FacilityModel>();
        model.Data = data;
        AttachWeapons(stat.Weapons, model);

        // this is not really necessary as Facility's prefab should already have Model as its Mesh's CameraLOSChangedRelay target
        facilityGoClone.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(facilityGoClone.transform);
        return model;
    }

    public void MakeInstance(FacilityStats stat, ref FacilityModel model) {
        GameObject facilityGo = model.gameObject;
        FacilityCategory categoryFromModel = Enums<FacilityCategory>.Parse(facilityGo.name);
        D.Assert(stat.Category == categoryFromModel, "{0} should be same as {1}.".Inject(stat.Category.GetName(), categoryFromModel.GetName()));
        FacilityData data = new FacilityData(stat.Category, stat.Name, stat.MaxHitPoints, stat.Mass) {
            Strength = stat.Strength,
            CurrentHitPoints = stat.CurrentHitPoints
        };
        model.Data = data;
        AttachWeapons(stat.Weapons, model);

        // this is not really necessary as facilityGo should already have Model as its Mesh's CameraLOSChangedRelay target
        facilityGo.GetSafeInterfaceInChildren<ICameraLOSChangedRelay>().AddTarget(facilityGo.transform);
    }

    public FormationStation MakeFormationStationTrackerInstance(Vector3 stationOffset, FleetCmdModel fleetCmd) {
        // make a folder for neatness if one doesn't yet exist
        GameObject stationTrackerFolder = null;
        var trackers = fleetCmd.gameObject.GetComponentsInChildren<FormationStation>();
        if (trackers.IsNullOrEmpty()) {
            stationTrackerFolder = new GameObject("StationTrackers");
            UnityUtility.AttachChildToParent(stationTrackerFolder, fleetCmd.gameObject);
            stationTrackerFolder.layer = (int)Layers.IgnoreRaycast;
        }
        else {
            stationTrackerFolder = trackers.First().transform.parent.gameObject;
        }

        GameObject stGo = UnityUtility.AddChild(stationTrackerFolder, formationStationTrackerPrefab.gameObject);
        FormationStation st = stGo.GetSafeMonoBehaviourComponent<FormationStation>();
        st.StationOffset = stationOffset;
        //D.Log("New FormationStation created at {0}, Offset = {1}, FleetCmd at {2}.", st.transform.position, stationOffset, fleetCmd.transform.position);
        return st;
    }

    private void AttachWeapons(IEnumerable<Weapon> weapons, AUnitElementModel elementModel) {
        weapons.ForAll(w => AddWeapon(w, elementModel));
    }

    /// <summary>
    /// Primary method to use when adding a weapon to an Element. 
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="elementModel">The element model.</param>
    public void AddWeapon(Weapon weapon, AUnitElementModel elementModel) {
        var allWeaponTrackers = elementModel.gameObject.GetInterfacesInChildren<IWeaponRangeTracker>();
        var weaponTrackersInUse = allWeaponTrackers.Where(rt => rt.Range != Constants.ZeroF);
        var wRange = weapon.Range;

        // check trackers for range fit, if find it, assign ID, if not assign or create a tracker and assign its ID to the weapon
        var rTracker = weaponTrackersInUse.FirstOrDefault(rt => rt.RangeSpan.Contains(wRange));
        if (rTracker == null) {
            var unusedWeaponTrackers = allWeaponTrackers.Except(weaponTrackersInUse);
            if (!unusedWeaponTrackers.IsNullOrEmpty()) {
                rTracker = unusedWeaponTrackers.First();
            }
            else {
                GameObject rTrackerGo = UnityUtility.AddChild(elementModel.gameObject, weaponRangeTrackerPrefab.gameObject);
                rTrackerGo.layer = (int)Layers.IgnoreRaycast; // AddChild resets prefab layer to elementGo's layer
                rTracker = rTrackerGo.GetSafeInterfaceInChildren<IWeaponRangeTracker>();
            }
            //D.Log("{0}'s {1} with Range {2} assigned new Range {3}.", elementModel.FullName, typeof(IWeaponRangeTracker).Name, rTracker.Range, wRange);
            rTracker.Range = wRange;
        }
        elementModel.AddWeapon(weapon, rTracker);
        // IMPROVE how to keep track ranges from overlapping
    }

}

