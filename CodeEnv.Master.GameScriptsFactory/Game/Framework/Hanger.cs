// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Hanger.cs
// A hanger attached to a Settlement or Starbase Cmd that holds ships.
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
/// A hanger attached to a Settlement or Starbase Cmd that holds ships.
/// </summary>
public class Hanger : AMonoBase, IFormationMgrClient, IHanger/*, IHanger_Ltd*/ {

    private const string DebugNameFormat = "{0}.{1}";

    public string DebugName { get { return DebugNameFormat.Inject(ParentBaseCmd.DebugName, GetType().Name); } }

    public bool IsJoinable { get { return IsJoinableBy(Constants.One); } }

    public BaseConstructionManager ConstructionMgr { get { return ParentBaseCmd.ConstructionMgr; } }

    private IList<ShipItem> _allShips;
    public IList<ShipItem> AllShips { get { return new List<ShipItem>(_allShips); } }

    public int ShipCount { get { return _allShips.Count; } }

    public AUnitBaseCmdItem ParentBaseCmd { get; private set; }

    public Player Owner { get { return ParentBaseCmd.Owner; } }

    private HangerFormationManager _formationMgr;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        ParentBaseCmd = gameObject.GetSingleComponentInParents<AUnitBaseCmdItem>();
        _allShips = new List<ShipItem>();
    }

    public bool IsJoinableBy(int shipsToJoinCount) {
        return _allShips.Count + shipsToJoinCount <= Formation.Hanger.MaxFormationSlots();
    }

    /// <summary>
    /// Adds the ship to this Hanger.
    /// <remarks>Will throw an error if ship has not already detached itself from its Command.</remarks>
    /// </summary>
    /// <param name="ship">The ship.</param>
    public void AddShip(ShipItem ship) {
        D.Assert(IsJoinable);
        D.Assert(!ship.IsHQ);
        D.Assert(!ship.__HasCommand);
        D.Assert(!_allShips.Contains(ship));


        bool isFirstShip = false;
        if (_formationMgr == null) {
            _formationMgr = new HangerFormationManager(this);
            isFirstShip = true;
        }

        _allShips.Add(ship);
        UnityUtility.AttachChildToParent(ship.gameObject, gameObject);
        ship.subordinateDeathOneShot += HangerShipDeathEventHandler;
        ship.PrepareForHangerArrival();

        if (isFirstShip) {
            _formationMgr.RepositionAllElementsInFormation(_allShips.Cast<IUnitElement>());
        }
        else {
            _formationMgr.AddAndPositionNonHQElement(ship);
        }
        D.Log("{0} has added {1}. Total ships count = {2}.", DebugName, ship.DebugName, ShipCount);
    }

    public void RemoveUncompletedShip(ShipItem ship) {
        Remove(ship);
        // no need to detach from HangerGo as ship will be destroyed
    }

    /// <summary>
    /// Returns <c>true</c> if this hanger contains the provided ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    public bool Contains(IShip_Ltd ship) {
        return _allShips.Contains(ship as ShipItem);
    }

    /// <summary>
    /// Forms a fleet from the provided ships from this hanger using the provided cmdModDesign and orders them to AssumeFormation
    /// at the closest Base LocalAssyStation.
    /// </summary>
    /// <param name="fleetRootname">The fleet root name.</param>
    /// <param name="cmdModDesign">The command mod design.</param>
    /// <param name="formation">The formation.</param>
    /// <param name="ships">The ships.</param>
    /// <returns></returns>
    public FleetCmdItem FormFleetFrom(string fleetRootname, FleetCmdModuleDesign cmdModDesign, Formation formation, IEnumerable<ShipItem> ships) {
        Utility.ValidateNotNullOrEmpty<ShipItem>(ships);

        ships.ForAll(ship => {
            // canceling any existing ship orders is handled below
            Remove(ship);
        });
        Utility.ValidateNotNullOrEmpty<ShipItem>(ships);    // will fail if ships was ref to _allShips

        Vector3 fleetCreatorLocation = DetermineFormFleetCreatorLocation();
        var fleet = UnitFactory.Instance.MakeFleetInstance(fleetCreatorLocation, cmdModDesign, ships, formation, fleetRootname);
        D.Log(ShowDebugLog, "{0}: Location of formed fleet {1} is {2}, creator is {3}, {4:0.} units apart.",
            DebugName, fleet.DebugName, fleet.Position, fleetCreatorLocation, Vector3.Distance(fleet.Position, fleetCreatorLocation));

        ships.ForAll(ship => {
            D.Assert(ship.__HasCommand);
            D.Assert(!ship.IsCollisionAvoidanceOperational);
            ship.PrepareForHangerDeparture();
            // 1.6.18 Fleet will clear orders of ships during CommenceOperations
        });

        var closestLocalAssyStation = GameUtility.GetClosest(fleet.Position, ParentBaseCmd.LocalAssemblyStations);
        var assumeFormationAtAssyStationOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
        fleet.CurrentOrder = assumeFormationAtAssyStationOrder;

        return fleet;
    }

    /// <summary>
    /// Replaces shipToReplace with refittedShip in this Hanger.
    /// <remarks>Only called after a hanger ship refit has successfully completed.</remarks>
    /// <remarks>Handles adding, removing, rotation, position and formation assignment. 
    /// Client must create the refittedShip, complete its initialization, commence operations and destroy shipToReplace.</remarks>
    /// </summary>
    /// <param name="shipToReplace">The ship that needs to be replaced with refittedShip.</param>
    /// <param name="refittedShip">The refitted replacement for shipToReplace.</param>
    public void ReplaceRefittedShip(ShipItem shipToReplace, ShipItem refittedShip) {
        // AddElement without dealing with Cmd death, HQ or FormationManager
        _allShips.Add(refittedShip);
        UnityUtility.AttachChildToParent(refittedShip.gameObject, gameObject);
        refittedShip.subordinateDeathOneShot += HangerShipDeathEventHandler;
        D.Assert(!refittedShip.IsCollisionAvoidanceOperational);    // CommenceOps does not activate CA if located in hanger
        D.Assert(!refittedShip.SRSensorMonitor.IsOperational);  // CommenceOps does not activate Sensors if located in hanger

        // Remove(ship) without dealing with Cmd death, HQ or FormationManager
        bool isRemoved = _allShips.Remove(shipToReplace);
        D.Assert(isRemoved);
        shipToReplace.subordinateDeathOneShot -= HangerShipDeathEventHandler;

        // no need to worry about IsJoinable as there shouldn't be any checks when using this method
        _formationMgr.ReplaceElement(shipToReplace, refittedShip);
        isRemoved = RemoveAndRecycleFormationStation(shipToReplace);
        D.Assert(isRemoved);
    }

    #region Event and Property Change Handlers

    private void HangerShipDeathEventHandler(object sender, EventArgs e) {
        ShipItem deadHangerShip = sender as ShipItem;
        HandleHangerShipDeath(deadHangerShip);
    }

    #endregion

    private void HandleHangerShipDeath(ShipItem deadHangerShip) {
        D.Assert(deadHangerShip.IsDead);
        D.Log("{0} is removing dead {1}.", DebugName, deadHangerShip.DebugName);
        Remove(deadHangerShip);
        // no need to detach from HangerGo as ship will be destroyed
        if (ConstructionMgr.IsConstructionQueuedFor(deadHangerShip)) {
            var deadShipConstruction = ConstructionMgr.GetConstructionFor(deadHangerShip);
            ConstructionMgr.RemoveFromQueue(deadShipConstruction);
        }
    }

    public void HandleAlertStatusChange(AlertStatus alertStatus) {
        _allShips.ForAll(ship => ship.AlertStatus = alertStatus);
    }

    public void HandleDeath() {
        // Make a fleet from all remaining ships. Base has already removed all construction from ConstructionMgr 
        // so there won't be any ships that have not yet completed initial construction
        if (_allShips.Any()) {
            _allShips.ForAll(ship => D.Assert(!ConstructionMgr.IsConstructionQueuedFor(ship)));
            DispatchLostHangerFleet();
        }
    }

    public void HandleLosingOwnership() {
        // Make a fleet from all remaining ships. Base has already removed all construction from ConstructionMgr 
        // so there won't be any ships that have not yet completed initial construction
        if (_allShips.Any()) {
            _allShips.ForAll(ship => D.Assert(!ConstructionMgr.IsConstructionQueuedFor(ship)));
            DispatchLostHangerFleet();
        }
    }

    #region Losing Hanger Fleet Generation

    private void DispatchLostHangerFleet() {
        if (Owner.IsUser && !DebugControls.Instance.AiChoosesUserCmdModInitialDesigns) {
            string dialogText = "Pick the CmdModDesign you wish to use to create this HangerEmergencyEscape Fleet. \nCancel to use the default design.";
            EventDelegate cancelDelegate = new EventDelegate(() => {
                var cmdModDefaultDesign = _gameMgr.UserAIManager.Designs.GetFleetCmdModDefaultDesign();
                FormFleetFrom(cmdModDefaultDesign); // canceling dialog results in use of CmdModuleDefaultDesign
                DialogWindow.Instance.Hide();
            });
            DialogWindow.Instance.HaveUserPickCmdModDesign(FormID.SelectFleetCmdModDesignDialog, dialogText, cancelDelegate,
                (chosenFleetCmdModDesign) => FormFleetFrom(chosenFleetCmdModDesign), useUserActionButton: true);
        }
        else {
            var chosenDesign = _gameMgr.GetAIManagerFor(Owner).ChooseFleetCmdModDesign();
            FormFleetFrom(chosenDesign);
        }
    }

    private void FormFleetFrom(AUnitCmdModuleDesign chosenDesign) {
        var fleetShipsCopy = _allShips.ToList();
        D.Assert(fleetShipsCopy.Any());
        FormFleetFrom("LostHangerFleet", chosenDesign as FleetCmdModuleDesign, Formation.Globe, fleetShipsCopy);
    }

    #endregion

    /// <summary>
    /// Returns a safe location for the RuntimeFleetCreator that will be used to deploy the formed fleet.
    /// <remarks>This is not the location where the new FleetCmd will start. That location depends on where the
    /// chosen HQElement is located, in this case on their hanger berth.</remarks>
    /// </summary>
    /// <returns></returns>
    private Vector3 DetermineFormFleetCreatorLocation() {
        var universeSize = _gameMgr.GameSettings.UniverseSize;
        float baseOffsetDistance = ParentBaseCmd.HQElement.Radius + Constants.OneF;
        float randomOffsetDistance = UnityEngine.Random.Range(baseOffsetDistance, baseOffsetDistance * 1.1F);
        Vector3 offset = Vector3.one * randomOffsetDistance;

        Vector3 hangerPosition = transform.position;
        Vector3 locationForCreator = hangerPosition + offset;
        if (!GameUtility.IsLocationContainedInUniverse(locationForCreator, universeSize)) {
            locationForCreator = hangerPosition - offset;
        }
        D.Assert(GameUtility.IsLocationContainedInUniverse(locationForCreator, universeSize));
        return locationForCreator;
    }

    private void Remove(ShipItem ship) {
        bool isRemoved = _allShips.Remove(ship);
        D.Assert(isRemoved);
        ship.subordinateDeathOneShot -= HangerShipDeathEventHandler;
        _formationMgr.RestoreSlotToAvailable(ship);
    }

    /// <summary>
    /// Removes and recycles the provided ship's FormationStation if it is
    /// present, returning <c>true</c> if it was removed, <c>false</c> if it 
    /// did not need to be removed since it wasn't present.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    private bool RemoveAndRecycleFormationStation(ShipItem ship) {
        var station = ship.FormationStation;
        if (station != null) {
            ship.FormationStation = null;
            station.AssignedShip = null;
            GamePoolManager.Instance.DespawnFormationStation(station.transform);
            return true;
        }
        return false;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    public bool ShowDebugLog { get { return ParentBaseCmd.ShowDebugLog; } }

    #endregion

    #region IFormationMgrClient Members

    Formation IFormationMgrClient.Formation { get { return Formation.Hanger; } }

    void IFormationMgrClient.HandleMaxFormationRadiusDetermined(float maxFormationRadius) {
        // Nothing to do as max formation radius is not used in Hanger
    }

    /// <summary>
    /// Positions the element in formation. This Hanger version assigns a FleetFormationStation to the ship (element) after
    /// removing the existing station, if any. The ship will be placed at the station's location, pointing at the hanger.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="stationSlotInfo">The station slot information.</param>
    void IFormationMgrClient.PositionElementInFormation(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        ShipItem ship = element as ShipItem;

        ship.transform.localPosition = stationSlotInfo.LocalOffset;

        Vector3 hangerPosition = transform.position;
        Vector3 faceHangerDirection = (hangerPosition - ship.Position).normalized;
        Quaternion faceHangerRotation = Quaternion.LookRotation(faceHangerDirection);
        ship.transform.localRotation = faceHangerRotation;

        if (RemoveAndRecycleFormationStation(ship)) {
            D.Warn("{0} had to remove and despawn {1}'s old {2}.", DebugName, ship.DebugName, typeof(FleetFormationStation).Name);
        }

        //D.Log(ShowDebugLog, "{0} is adding a new {1} with SlotID {2}.", DebugName, typeof(FleetFormationStation).Name, stationSlotInfo.SlotID.GetValueName());
        FleetFormationStation station = GamePoolManager.Instance.SpawnFormationStation(hangerPosition, faceHangerRotation, transform);
        station.StationInfo = stationSlotInfo;  // modifies position by localOffset
        station.AssignedShip = ship;
        ship.FormationStation = station;
    }

    #endregion
}

