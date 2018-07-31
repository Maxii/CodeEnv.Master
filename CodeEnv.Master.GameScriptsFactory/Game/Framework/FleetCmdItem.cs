// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCommandItem.cs
// AUnitCmdItems that are Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using MoreLinq;
using Pathfinding;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// AUnitCmdItems that are Fleets.
/// </summary>
public class FleetCmdItem : AUnitCmdItem, IFleetCmd, IFleetCmd_Ltd, ICameraFollowable, ISectorViewHighlightable {

    /// <summary>
    /// Occurs when the fleet ability to travel at FTL speeds has changed.
    /// <remarks>Can be caused by 1) the addition or removal of a non-FtlCapable ship, 2) the addition or removal of an FtlCapable ship, 
    /// 3) one or more FtlCapable ships changing their FtlEngine damped state, 4) one or more FtlCapable ships changing their FtlEngine
    /// damaged state, and 5) when a ship's FtlEngines are first activated.</remarks>
    /// </summary>
    public event EventHandler isFtlOperationalChanged;

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public override float ClearanceRadius { get { return Data.UnitMaxFormationRadius * 2F; } }

    public new ShipItem HQElement {
        get { return base.HQElement as ShipItem; }
        set { base.HQElement = value; }
    }

    private FleetOrder _currentOrder;
    public FleetOrder CurrentOrder {
        get { return _currentOrder; }
        set {
            if (_currentOrder != value) {
                CurrentOrderPropChangingHandler(value);
                _currentOrder = value;
                CurrentOrderPropChangedHandler();
            }
        }
    }

    public FleetCmdReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    /// <summary>
    /// Indicates whether ALL the fleet's ships have FTL engines. If <c>false</c> the fleet is not capable of traveling at FTL speeds.
    /// <remarks>Returning <c>true</c> says nothing about the operational state of the engines.</remarks>
    /// </summary>
    public bool IsFtlCapable { get { return Data.IsFtlCapable; } }

    /// <summary>
    /// Indicates whether ANY of the fleet's ship's FTL engines, if any, are damaged. If <c>true</c> the fleet is not 
    /// currently capable of traveling at FTL speeds.
    /// </summary>
    public bool IsFtlDamaged { get { return Data.IsFtlDamaged; } }

    /// <summary>
    /// Indicates whether ANY of the fleet's ship's FTL engines, if any, are damped by an FTL Damping Field. 
    /// If <c>true</c> the fleet is not currently capable of traveling at FTL speeds.
    /// </summary>
    public bool IsFtlDampedByField { get { return Data.IsFtlDampedByField; } }

    /// <summary>
    /// Indicates whether ALL the fleet's ships have FTL engines that are operational, aka activated, undamaged and not damped 
    /// by an FTL damping field. If <c>false</c> the fleet is not currently capable of traveling at FTL speeds.
    /// <remarks>To subscribe to changes, use isFtlOperationalChanged.</remarks>
    /// </summary>
    public bool IsFtlOperational { get { return Data.IsFtlOperational; } }

    public float UnitFullSpeedValue { get { return Data.UnitFullSpeedValue; } }

    public new FleetCmdCameraStat CameraStat {
        protected get { return base.CameraStat as FleetCmdCameraStat; }
        set { base.CameraStat = value; }
    }

    protected new FleetFormationManager FormationMgr { get { return base.FormationMgr as FleetFormationManager; } }

    private FleetMoveHelper _moveHelper;

    #region Initialization

    protected override bool __InitializeDebugLog() {
        return __debugCntls.ShowFleetCmdDebugLogs;
    }

    protected override AFormationManager InitializeFormationMgr() {
        return new FleetFormationManager(this);
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeMoveHelper();
    }

    private void InitializeMoveHelper() {
        _moveHelper = new FleetMoveHelper(this, Data, gameObject.GetSafeComponent<Seeker>());
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<FleetCmdData, float>(d => d.UnitFullSpeedValue, FullSpeedPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<FleetCmdData, bool>(d => d.IsFtlOperational, IsFtlOperationalPropChangedHandler));
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        __InitializeShowVelocityRay();
        __InitializeShowCoursePlot();
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        D.AssertNotEqual(TempGameValues.NoPlayer, owner);
        return owner.IsUser ? new FleetCtxControl_User(this) as ICtxControl : new FleetCtxControl_AI(this);
    }

    protected override SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        return new SectorViewHighlightManager(this, UnitMaxFormationRadius * 10F);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        CurrentState = FleetState.FinalInitialize;
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = FleetState.Idling;
        ClearOrderAndStateForAllElements();  // 1.6.18 Added to handle case where FleetCmd formed from supplied ships
        RegisterForOrders();
        Data.ActivateShipSensors();
        Data.ActivateCmdSensors();
        AssessAlertStatus();
        SubscribeToSensorEvents();
        __IsActivelyOperating = true;
    }

    // 11.10.17 Removed 'flee and regroup' order when Cmd first becomes operational as should be done as result of taking damage

    public override void RemoveElement(AUnitElementItem element) {
        base.RemoveElement(element);

        var removedShip = element as ShipItem;
        RemoveFromStateMachine(removedShip);

        if (!IsDead) {
            if (removedShip == HQElement) {
                // 12.13.17 The ship that was just removed is the HQ, so select another. As HQElement changes to a new ship, the
                // removedHQShip will have its FormationStation slot restored to available in the FormationManager followed 
                // by the removal/recycling of the removedHQShip's FormationStation. It can't be done here now as the FormationManager
                // needs to know it is the HQ to restore the right slot.
                HQElement = SelectHQElement();
                D.Log(ShowDebugLog, "{0} selected {1} as Flagship after removal of {2}.", DebugName, HQElement.DebugName, removedShip.DebugName);
                return;
            }
        }
        // Whether Cmd is alive or dead, we still need to remove and recycle the FormationStation from the removed non-HQ ship
        bool isFormationStationRemoved = RemoveAndRecycleFormationStation(removedShip);
        D.Assert(isFormationStationRemoved);
    }

    /// <summary>
    /// Replaces elementToReplace with refittedElement in this Cmd.
    /// <remarks>Only called after a element refit has successfully completed.</remarks>
    /// <remarks>Handles adding, removing, cmd assignment, unifiedSRSensorMonitor, rotation, position, 
    /// Topography, HQ state and formation assignment. Client must create the refittedElement, 
    /// complete initialization, commence operations and destroy elementToReplace.</remarks>
    /// </summary>
    /// <param name="elementToReplace">The element to replace.</param>
    /// <param name="refittedElement">The refitted element replacing elementToReplace.</param>
    public void ReplaceRefittedElement(ShipItem elementToReplace, ShipItem refittedElement) {
        D.Assert(elementToReplace.IsCurrentOrderDirectiveAnyOf(ShipDirective.Refit));
        // AddElement without dealing with Cmd death, HQ or FormationManager
        Elements.Add(refittedElement);
        Data.AddElement(refittedElement.Data);
        refittedElement.Command = this;
        refittedElement.AttachAsChildOf(UnitContainer);
        UnifiedSRSensorMonitor.Add(refittedElement.SRSensorMonitor);

        refittedElement.subordinateDeathOneShot += SubordinateDeathEventHandler;
        refittedElement.subordinateDamageIncurred += SubordinateDamageIncurredEventHandler;
        refittedElement.isAvailableChanged += SubordinateIsAvailableChangedEventHandler;
        refittedElement.subordinateOrderOutcome += SubordinateOrderOutcomeEventHandler;

        // RemoveElement without dealing with Cmd death, HQ or FormationManager
        bool isRemoved = Elements.Remove(elementToReplace);
        D.Assert(isRemoved);
        Data.RemoveElement(elementToReplace.Data);

        UnifiedSRSensorMonitor.Remove(elementToReplace.SRSensorMonitor);

        elementToReplace.subordinateDeathOneShot -= SubordinateDeathEventHandler;
        elementToReplace.subordinateDamageIncurred -= SubordinateDamageIncurredEventHandler;
        elementToReplace.isAvailableChanged -= SubordinateIsAvailableChangedEventHandler;
        elementToReplace.subordinateOrderOutcome -= SubordinateOrderOutcomeEventHandler;
        // no need to null Command as elementToReplace will be destroyed

        // no need to AssessIcon as replacingElement only has enhanced performance
        // no need to worry about IsJoinable as there shouldn't be any checks when using this method
        refittedElement.transform.rotation = elementToReplace.transform.rotation;
        refittedElement.Data.Topography = elementToReplace.Topography;

        if (elementToReplace.IsHQ) {
            // handle all HQ change here without firing HQ change handlers
            _hqElement = refittedElement;
            refittedElement.IsHQ = true;
            Data.HQElementData = refittedElement.Data;
            AttachCmdToHQElement(); // needs to occur before formation changed
        }
        FormationMgr.ReplaceElement(elementToReplace, refittedElement);

        // 11.26.17 Don't communicate removal/addition to FSM as this method is only called after a Element refit has been successful.
        // ReplacingElement has already been refit so doesn't need to try it again, and elementToReplace has already 
        // called back with success so Cmd is not waiting for it.
    }

    /// <summary>
    /// Causes this fleet to 'join' the provided fleet by transferring its ships and hero (if any)
    /// assuming fleetToJoin does not already have a hero.
    /// <remarks>1.2.18 Currently the transfer of ships is instant, aka there is no ship travel time reqd to transfer.</remarks>
    /// <remarks>IMPROVE return any unassigned Heros to 'HeroManagement' for assignment.</remarks>
    /// </summary>
    /// <param name="fleetToJoin">The fleet to join.</param>
    public void Join(FleetCmdItem fleetToJoin) {
        D.AssertEqual(Owner, fleetToJoin.Owner);
        D.Assert(fleetToJoin.IsJoinableBy(ElementCount));
        if (!fleetToJoin.IsHeroPresent && IsHeroPresent) {
            fleetToJoin.Data.Hero = Data.Hero;
        }
        var elementsCopy = new List<AUnitElementItem>(Elements);
        foreach (var element in elementsCopy) {
            RemoveElement(element);
            fleetToJoin.AddElement(element);
            // 12.8.17 don't clear ship orders as non-HQ ships are likely to still be moving
            fleetToJoin.UponSubordinateJoined(element as ShipItem);
        }
    }

    public FleetCmdReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    public ShipReport[] GetElementReports(Player player) {
        return Elements.Cast<ShipItem>().Select(s => s.GetReport(player)).ToArray();
    }

    public bool IsLocatedIn(IntVector3 sectorID) {
        IntVector3 currentSectorID;
        if (Data.TryGetSectorID(out currentSectorID)) {
            return currentSectorID == sectorID;
        }
        return false;
    }

    public bool TryGetSectorID(out IntVector3 sectorID) {
        return Data.TryGetSectorID(out sectorID);
    }


    public override bool IsAttacking(IUnitCmd_Ltd unitCmd) {
        return IsCurrentStateAnyOf(FleetState.ExecuteAttackOrder) && _fsmTgt == unitCmd;
    }

    public bool Contains(ShipItem ship) { return Elements.Contains(ship); }

    public override bool IsJoinableBy(int additionalElementCount) {
        bool isJoinable = Utility.IsInRange(ElementCount + additionalElementCount, Constants.One, TempGameValues.MaxShipsPerFleet);
        if (isJoinable) {
            D.Assert(FormationMgr.HasRoomFor(additionalElementCount));
        }
        return isJoinable;
    }

    /// <summary>
    /// Returns <c>true</c> if there are ships slowing the fleet.
    /// The ships returned will either be 1) not FTL capable, 2) FTL capable but with a damaged and/or damped FTL engine.
    /// <remarks>Also returns all other ships present that don't meet the slowing criteria so the client
    /// can determine whether the fleet could move faster if the slowedShips were separated from the fleet.</remarks>
    /// </summary>
    /// <param name="slowedShips">The slowed ships.</param>
    /// <param name="remainingShips">The remaining ships.</param>
    /// <returns></returns>
    public bool TryGetShipsSlowingFleet(out IEnumerable<ShipItem> slowedShips, out IEnumerable<ShipItem> remainingShips) {
        D.Assert(!IsDead);
        var ships = Elements.Cast<ShipItem>();
        slowedShips = ships.Where(s => !s.Data.IsFtlOperational);
        remainingShips = ships.Except(slowedShips);
        return slowedShips.Any();
    }

    /// <summary>
    /// Forms a fleet from the provided ship from this Fleet using the default FleetCmdModuleDesign.
    /// Throws an error if the provided ship is not currently part of this fleet.
    /// <remarks>Primary use is when making a temporary throw away fleet for a single ship 
    /// which needs to detach to execute its orders.</remarks>
    /// <remarks>7.26.18 In all cases, the ships forming the fleet have their existing orders cleared.</remarks>
    /// </summary>
    /// <param name="fleetRootname">The fleet root name.</param>
    /// <param name="ship">The ship from this fleet.</param>
    /// <returns></returns>
    internal FleetCmdItem FormFleetFrom(string fleetRootname, ShipItem ship) {
        FleetCmdModuleDesign defaultCmdModDesign = OwnerAiMgr.Designs.GetFleetCmdModDefaultDesign();
        return FormFleetFrom(fleetRootname, defaultCmdModDesign, new ShipItem[] { ship }, toValidateSameOwners: true);
    }

    /// <summary>
    /// Forms a Fleet from the provided ships from this Fleet using the provided FleetCmdModuleDesign.
    /// Throws an error if one or more of the provided ships are not currently part of this fleet.
    /// <remarks>Primary purpose is to allow PlayerAIMgr and AFleetUnitHudForm to split the fleet.</remarks>
    /// <remarks>7.26.18 In all cases, the ships forming the fleet have their existing orders cleared.</remarks>
    /// </summary>
    /// <param name="fleetRootname">The fleet root name.</param>
    /// <param name="cmdModDesign">The command mod design.</param>
    /// <param name="ships">The ships from this fleet.</param>
    /// <returns></returns>
    public FleetCmdItem FormFleetFrom(string fleetRootname, FleetCmdModuleDesign cmdModDesign, IEnumerable<ShipItem> ships) {
        return FormFleetFrom(fleetRootname, cmdModDesign, ships, toValidateSameOwners: true);
    }

    /// <summary>
    /// Forms a fleet from the provided ships from this Fleet using the provided FleetCmdModuleDesign.
    /// Throws an error if one or more of the provided ships are not currently part of this fleet.
    /// <remarks>7.26.18 If the provided ships are the only ships present in the current fleet, the
    /// current FleetCmd is used, but its CmdModule is replaced using the provided cmdModDesign since 
    /// the player has presumably already paid for the new CmdModule.</remarks>
    /// <remarks>7.26.18 In all cases, the ships forming the fleet have their existing orders cleared.
    /// If a new fleet is formed, the orders are cleared when the RuntimeCreator calls Cmd.CommenceOperations.
    /// If the existing fleet is used, this method clears all ship orders before replacing the CmdModule.</remarks>
    /// </summary>
    /// <param name="fleetRootname">The fleet root name.</param>
    /// <param name="cmdModDesign">The command mod design.</param>
    /// <param name="ships">The ships from this fleet.</param>
    /// <param name="toValidateSameOwners">if set to <c>true</c> validate that ships and fleet owner are the same.</param>
    /// <returns></returns>
    private FleetCmdItem FormFleetFrom(string fleetRootname, FleetCmdModuleDesign cmdModDesign, IEnumerable<ShipItem> ships,
        bool toValidateSameOwners) {
        Utility.ValidateNotNullOrEmpty<ShipItem>(ships);
        ships.ForAll(ship => {
            D.Assert(ship.IsOperational);
            D.Assert(Contains(ship));
            if (toValidateSameOwners) {
                D.AssertEqual(Owner, ship.Owner);
            }
        });

        if (ships.Count() == ElementCount) {
            string previousDebugName = DebugName;
            ClearOrderAndStateForAllElements();
            if (cmdModDesign != Data.CmdModuleDesign) {
                UnitFactory.Instance.ReplaceCmdModuleWith(cmdModDesign, this);
            }
            UnitName = AUnitCreator.__GetUniqueUnitName(fleetRootname);
            D.Log("{0} did not need to form a new fleet as it already is the desired fleet. Name changed to {1}.", previousDebugName, DebugName);
            return this;
        }

        ships.ForAll(ship => RemoveElement(ship));

        // Clearing existing ship orders is handled by Fleet.CommenceOperations
        Vector3 fleetCreatorLocation = DetermineFormFleetCreatorLocation();
        var fleet = UnitFactory.Instance.MakeFleetInstance(fleetCreatorLocation, cmdModDesign, ships, Formation, fleetRootname);
        return fleet;
    }

    /// <summary>
    /// Returns <c>true</c> if this fleet is in orbit, <c>false</c> otherwise. If in high orbit,
    /// highOrbitedItem will be valid. If in close orbit, closeOrbitedItem will be valid.
    /// </summary>
    /// <param name="highOrbitedItem">The high orbited item.</param>
    /// <param name="closeOrbitedItem">The close orbited item.</param>
    /// <returns></returns>
    public bool TryAssessOrbit(out IShipOrbitable highOrbitedItem, out IShipCloseOrbitable closeOrbitedItem) {
        highOrbitedItem = null;
        closeOrbitedItem = null;
        if (HQElement.IsInOrbit) {
            if (HQElement.IsInCloseOrbit) {
                closeOrbitedItem = HQElement.ItemBeingOrbited as IShipCloseOrbitable;
                D.AssertNotNull(closeOrbitedItem);
            }
            else {
                D.Assert(HQElement.IsInHighOrbit);
                highOrbitedItem = HQElement.ItemBeingOrbited;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Selects and returns a new HQElement.
    /// <remarks>TEMP public to allow creator use.</remarks>
    /// </summary>
    /// <returns></returns>
    internal ShipItem SelectHQElement() {
        AUnitElementItem bestElement = null;
        float bestElementScore = Constants.ZeroF;
        var descendingHQPriority = Enums<Priority>.GetValues(excludeDefault: true).OrderByDescending(p => p);
        IEnumerable<AUnitElementItem> hqCandidates;
        foreach (var priority in descendingHQPriority) {
            AUnitElementItem bestCandidate;
            float bestCandidateScore;
            if (TryGetHQCandidatesOf(priority, out hqCandidates)) {
                bestCandidate = hqCandidates.MaxBy(e => e.Data.Health);
                float distanceSqrd = Vector3.SqrMagnitude(bestCandidate.Position - Position);
                int bestCandidateDistanceScore = distanceSqrd < 100 ? 3 : distanceSqrd < 900 ? 2 : 1;   // < 10 = 3, < 30 = 2, otherwise 1
                bestCandidateScore = (int)priority * bestCandidate.Data.Health * bestCandidateDistanceScore;
                if (bestCandidateScore > bestElementScore) {
                    bestElement = bestCandidate;
                    bestElementScore = bestCandidateScore;
                }
            }
        }
        D.AssertNotNull(bestElement);
        // IMPROVE bestScore algorithm. Include large defense and small offense criteria as will be located in HQ formation slot (usually in center)
        ShipItem ship = bestElement as ShipItem;
        // CombatStance assignment handled by Ship.IsHQChangedEventHandler
        return ship;
    }

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        if (IsPaused) {
            _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        }
    }

    protected override void PrepareForDeadState() {
        base.PrepareForDeadState();
        CurrentOrder = null;
    }

    protected override void AssignDeadState() {
        CurrentState = FleetState.Dead;
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered
    /// to Scuttle (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    /// <param name="orderSource">The order source.</param>
    private void ScuttleUnit(OrderSource orderSource) {
        var elementScuttleOrder = new ShipOrder(ShipDirective.Scuttle, orderSource);
        // Scuttle HQElement last to avoid multiple selections of new HQElement
        NonHQElements.ForAll(e => (e as ShipItem).CurrentOrder = elementScuttleOrder);
        (HQElement as ShipItem).CurrentOrder = elementScuttleOrder;
    }

    protected override void ShowSelectedItemHud() {
        // 8.7.17 UnitHudWindow's FleetForm will auto show InteractibleHudWindow's FleetForm
        if (Owner.IsUser) {
            UnitHudWindow.Instance.Show(FormID.UserFleet, this);
        }
        else {
            UnitHudWindow.Instance.Show(FormID.AiFleet, this);
        }
    }

    protected override TrackingIconInfo MakeIconInfo() {
        return FleetIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    public bool __RequestPermissionToWithdraw(ShipItem ship) {
        return true;    // TODO false if fight to the death...
    }

    /// <summary>
    /// Requests a change in the ship's formation station assignment based on the stationSelectionCriteria provided.
    /// Returns <c>true</c> if the ship's formation station assignment was changed, <c>false</c> otherwise.
    /// <remarks>If return <c>true</c> the ship's assigned FormationStation has been changed and its former station recycled.</remarks>
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="stationSelectionCriteria">The station selection criteria.</param>
    /// <returns></returns>
    public bool RequestFormationStationChange(ShipItem ship, AFormationManager.FormationStationSelectionCriteria stationSelectionCriteria) {
        if (FormationMgr.IsSlotAvailable(stationSelectionCriteria)) {
            D.Log(ShowDebugLog, "{0} request for formation station change has been approved.", ship.DebugName);
            FormationMgr.AddAndPositionNonHQElement(ship, stationSelectionCriteria);
            return true;
        }
        return false;
    }

    [Obsolete("Fleets no longer refit en-masse inside a hanger")]
    public int GetRefittableShipCount() {
        int count = Constants.Zero;
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.Refit)) {
                count++;
            }
        }
        return count;
    }

    #region Event and Property Change Handlers

    private void OnIsFtlOperationalChanged() {
        if (isFtlOperationalChanged != null) {
            isFtlOperationalChanged(this, EventArgs.Empty);
        }
    }

    private void IsFtlOperationalPropChangedHandler() {
        HandleIsFtlOperationalChanged();
    }

    private void FullSpeedPropChangedHandler() {
        HandleFullSpeedChanged();
    }

    private void CurrentOrderPropChangingHandler(FleetOrder incomingOrder) {
        HandleCurrentOrderPropChanging(incomingOrder);
    }

    private void CurrentOrderPropChangedHandler() {
        HandleCurrentOrderPropChanged();
    }

    private void CurrentOrderChangedWhilePausedUponResumeEventHandler(object sender, EventArgs e) {
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
        HandleCurrentOrderChangedWhilePausedUponResume();
    }

    protected override void SubordinateOrderOutcomeEventHandler(object sender, AUnitElementItem.OrderOutcomeEventArgs e) {
        HandleSubordinateOrderOutcome(sender as ShipItem, e.Target, e.Outcome, e.CmdOrderID);
    }

    #endregion

    private void HandleFullSpeedChanged() {
        Elements.ForAll(e => (e as ShipItem).HandleFleetFullSpeedChanged());
    }

    private void HandleIsFtlOperationalChanged() {
        if (!IsDead) {
            OnIsFtlOperationalChanged();
        }
    }

    protected override void HandleFormationChanged() {
        base.HandleFormationChanged();
        if (IsCurrentStateAnyOf(FleetState.Idling, FleetState.Guarding, FleetState.AssumingFormation)) {
            D.Log(ShowDebugLog, "{0} is issuing an order to assume new formation {1}.", DebugName, Formation.GetValueName());
            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
        }
    }

    protected override void HandleHQElementChanging(AUnitElementItem newHQElement) {
        base.HandleHQElementChanging(newHQElement);

        if (IsOperational) {
            // runtime assignment of Flagship
            var previousFlagship = HQElement;
            D.AssertNotNull(previousFlagship);
            if (!Elements.Contains(previousFlagship)) {
                // previousFlagship has been removed from the fleet
                bool isFormationStationRemoved = RemoveAndRecycleFormationStation(previousFlagship);
                D.Assert(isFormationStationRemoved);
            }
        }
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        __AssessShowVelocityRay();
        // 7.19.18 Purposefully NOT assessing CoursePlot as I want plot to show even if Fleet is off screen
    }

    protected override void ImplementUiChangesFollowingOwnerChange() {
        base.ImplementUiChangesFollowingOwnerChange();
        __AssessShowCoursePlot();
    }

    /// <summary>
    /// Returns a safe location for the RuntimeFleetCreator that will be used to deploy the formed fleet.
    /// <remarks>This is not the location where the new FleetCmd will start. That location depends on where the
    /// chosen HQElement is located.</remarks>
    /// </summary>
    /// <returns></returns>
    private Vector3 DetermineFormFleetCreatorLocation() {
        float randomOffsetDistance = UnityEngine.Random.Range(TempGameValues.FleetFormationStationRadius, TempGameValues.FleetFormationStationRadius * 1.1F);
        Vector3 offset = Vector3.one * randomOffsetDistance;
        Vector3 locationForFleetCreator = Position + offset;
        if (!GameUtility.IsLocationContainedInNavigableUniverse(locationForFleetCreator)) {
            locationForFleetCreator = Position - offset;
        }
        GameUtility.__ValidateLocationContainedInNavigableUniverse(locationForFleetCreator);
        return locationForFleetCreator;
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

    #region Orders

    #region Orders Received While Paused System

    /// <summary>
    /// The sequence of orders received while paused. If any are present, the bottom of the stack will
    /// contain the order that was current (including null) when the first order was received while paused.
    /// </summary>
    private Stack<FleetOrder> _ordersReceivedWhilePaused = new Stack<FleetOrder>();

    private void HandleCurrentOrderPropChanging(FleetOrder incomingOrder) {
        __ValidateIncomingOrder(incomingOrder);
        if (IsPaused) {
            if (!_ordersReceivedWhilePaused.Any()) {
                // incomingOrder is the first order received while paused so record the CurrentOrder (including null) before recording it
                _ordersReceivedWhilePaused.Push(CurrentOrder);
            }
        }
    }

    private void HandleCurrentOrderPropChanged() {
        if (IsPaused) {
            // previous CurrentOrder already recorded in _ordersReceivedWhilePaused including null
            if (CurrentOrder != null) {
                if (CurrentOrder.Directive == FleetDirective.Scuttle) {
                    // allow a Scuttle order to proceed while paused
                    ResetOrdersReceivedWhilePausedSystem();
                    HandleNewOrder();
                    return;
                }
                _ordersReceivedWhilePaused.Push(CurrentOrder);
                // deal with multiple changes all while paused
                _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
                _gameMgr.isPausedChanged += CurrentOrderChangedWhilePausedUponResumeEventHandler;
                return;
            }
            // CurrentOrder is internally nulled for a number of reasons, including while paused
            _ordersReceivedWhilePaused.Clear(); // PropChanging will have recorded any previous order
        }
        HandleNewOrder();
    }

    private void HandleCurrentOrderChangedWhilePausedUponResume() {
        D.Assert(!IsPaused);
        D.AssertNotNull(CurrentOrder);
        D.AssertNotEqual(Constants.Zero, _ordersReceivedWhilePaused.Count);
        // If the last order received was Cancel, then the order that was current when the first order was issued during this pause
        // should be reinstated, aka all the orders received while paused are not valid and the original order should continue.
        FleetOrder order;
        var lastOrderReceivedWhilePaused = _ordersReceivedWhilePaused.Pop();
        if (lastOrderReceivedWhilePaused.Directive == FleetDirective.Cancel) {
            // if Cancel, then order that was canceled and original order (including null) at minimum must still be present
            D.Assert(_ordersReceivedWhilePaused.Count >= 2);
            //D.Log(ShowDebugLog, "{0} received the following order sequence from User during pause prior to Cancel: {1}.", DebugName,
            //_ordersReceivedWhilePaused.Where(o => o != null).Select(o => o.DebugName).Concatenate());
            order = _ordersReceivedWhilePaused.First(); // restore original order which can be null
        }
        else {
            order = lastOrderReceivedWhilePaused;
        }
        _ordersReceivedWhilePaused.Clear();
        if (order != null) {
            D.AssertNotEqual(FleetDirective.Cancel, order.Directive);
        }
        string orderMsg = order != null ? order.DebugName : "None";
        D.Log("{0} is changing or re-instating order to {1} after resuming from pause.", DebugName, orderMsg);

        if (CurrentOrder != order) {
            CurrentOrder = order;
        }
        else {
            HandleNewOrder();
        }
    }

    protected override void ResetOrdersReceivedWhilePausedSystem() {
        _ordersReceivedWhilePaused.Clear();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    #endregion

    /// <summary>
    /// Returns <c>true</c> if the directive of the CurrentOrder or if paused, a pending order 
    /// about to become the CurrentOrder matches any of the provided directive(s).
    /// </summary>
    /// <param name="directiveA">The directive a.</param>
    /// <returns></returns>
    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FleetOrder newOrder = _ordersReceivedWhilePaused.Peek();
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA;
        }
        return CurrentOrder != null && CurrentOrder.Directive == directiveA;
    }

    /// <summary>
    /// Returns <c>true</c> if the directive of the CurrentOrder or if paused, a pending order 
    /// about to become the CurrentOrder matches any of the provided directive(s).
    /// </summary>
    /// <param name="directiveA">The directive a.</param>
    /// <param name="directiveB">The directive b.</param>
    /// <returns></returns>
    public bool IsCurrentOrderDirectiveAnyOf(FleetDirective directiveA, FleetDirective directiveB) {
        if (IsPaused && _ordersReceivedWhilePaused.Any()) {
            // paused with a pending order replacement
            FleetOrder newOrder = _ordersReceivedWhilePaused.Peek();
            // newOrder will immediately replace CurrentOrder as soon as unpaused
            return newOrder.Directive == directiveA || newOrder.Directive == directiveB;
        }
        return CurrentOrder != null && (CurrentOrder.Directive == directiveA || CurrentOrder.Directive == directiveB);
    }

    /// <summary>
    /// The CmdStaff uses this method to override orders already issued.
    /// </summary>
    /// <param name="overrideOrder">The CmdStaff's override order.</param>
    /// <param name="retainSuperiorsOrder">if set to <c>true</c> [retain superiors order].</param>
    [Obsolete]
    private void OverrideCurrentOrder(FleetOrder overrideOrder, bool retainSuperiorsOrder) {
        D.AssertEqual(OrderSource.CmdStaff, overrideOrder.Source);
        D.AssertNull(overrideOrder.StandingOrder);

        FleetOrder standingOrder = null;
        if (retainSuperiorsOrder && CurrentOrder != null) {
            if (CurrentOrder.Source > OrderSource.CmdStaff) {
                D.AssertNull(CurrentOrder.FollowonOrder, CurrentOrder.DebugName);
                // the current order is from the CmdStaff's superior so retain it
                standingOrder = CurrentOrder;
            }
            else {
                // the current order is from CmdStaff, so its StandingOrder or its FollowonOrder's StandingOrder, if any, should be retained
                standingOrder = CurrentOrder.FollowonOrder != null ? CurrentOrder.FollowonOrder.StandingOrder : CurrentOrder.StandingOrder;
            }
        }
        // assign the standingOrder, if any, to the last order to be executed in the overrideOrder
        if (overrideOrder.FollowonOrder != null) {
            overrideOrder.FollowonOrder.StandingOrder = standingOrder;
        }
        else {
            overrideOrder.StandingOrder = standingOrder;
        }
        CurrentOrder = overrideOrder;
    }

    /// <summary>
    /// Returns <c>true</c> if the provided directive is authorized for use in a new order about to be issued.
    /// <remarks>Does not take into account whether consecutive order directives of the same value are allowed.
    /// If this criteria should be included, the client will need to include it manually.</remarks>
    /// <remarks>Warning: Do not use to Assert once CurrentOrder has changed and unpaused as order directives that
    /// result in Availability.Unavailable will fail the assert.</remarks>
    /// </summary>
    /// <param name="orderDirective">The order directive.</param>
    /// <returns></returns>
    public bool IsAuthorizedForNewOrder(FleetDirective orderDirective) {
        string unusedFailCause;
        return __TryAuthorizeNewOrder(orderDirective, out unusedFailCause);
    }

    private void HandleNewOrder() {
        // 4.13.17 Must get out of Call()ed states even if new order is null as only a non-Call()ed state's 
        // ExitState method properly resets all the conditions for entering another state, aka Idling.
        ReturnFromCalledStates();

        if (CurrentOrder != null) {
            __ValidateKnowledgeOfOrderTarget(CurrentOrder);

            UponNewOrderReceived();

            D.Log(ShowDebugLog, "{0} received new {1}. CurrentState = {2}, Frame = {3}.", DebugName, CurrentOrder, CurrentState.GetValueName(), Time.frameCount);
            if (Data.Target != CurrentOrder.Target) {    // OPTIMIZE avoids same value warning
                Data.Target = CurrentOrder.Target;  // can be null
            }

            FleetDirective directive = CurrentOrder.Directive;
            switch (directive) {
                case FleetDirective.Move:
                case FleetDirective.FullSpeedMove:
                    CurrentState = FleetState.ExecuteMoveOrder;
                    break;
                case FleetDirective.Attack:
                    CurrentState = FleetState.ExecuteAttackOrder;
                    break;
                case FleetDirective.Guard:
                    CurrentState = FleetState.ExecuteGuardOrder;
                    break;
                case FleetDirective.Patrol:
                    CurrentState = FleetState.ExecutePatrolOrder;
                    break;
                case FleetDirective.Explore:
                    CurrentState = FleetState.ExecuteExploreOrder;
                    break;
                case FleetDirective.JoinFleet:
                    CurrentState = FleetState.ExecuteJoinFleetOrder;
                    break;
                case FleetDirective.JoinHanger:
                    CurrentState = FleetState.ExecuteJoinHangerOrder;
                    break;
                case FleetDirective.AssumeFormation:
                    CurrentState = FleetState.ExecuteAssumeFormationOrder;
                    // OPTIMIZE could also be CurrentState = FleetState.AssumingFormation; as long as AssumingFormation does Return(Idling)
                    break;
                case FleetDirective.Repair:
                    CurrentState = FleetState.ExecuteRepairOrder;
                    break;
                case FleetDirective.Refit:
                    CurrentState = FleetState.ExecuteRefitOrder;
                    break;
                case FleetDirective.Disband:
                    CurrentState = FleetState.ExecuteDisbandOrder;
                    break;
                case FleetDirective.ChangeHQ:
                    HQElement = CurrentOrder.Target as ShipItem;
                    break;
                case FleetDirective.FoundSettlement:
                    CurrentState = FleetState.ExecuteFoundSettlementOrder;
                    break;
                case FleetDirective.FoundStarbase:
                    CurrentState = FleetState.ExecuteFoundStarbaseOrder;
                    break;
                case FleetDirective.Retreat:
                case FleetDirective.Withdraw:
                    D.Warn("{0}.{1} is not currently implemented.", typeof(FleetDirective).Name, directive.GetValueName());
                    break;
                //case FleetDirective.Regroup:    // 3.20.17 No ContextMenu order as direction or destinations need to be available
                //    CurrentState = FleetState.ExecuteRegroupOrder;
                //    break;
                case FleetDirective.Scuttle:
                    ScuttleUnit(CurrentOrder.Source);
                    return; // CurrentOrder will be set to null as a result of death
                case FleetDirective.Cancel:
                // 9.13.17 Cancel should never be processed here as it is only issued by User while paused and is 
                // handled by HandleCurrentOrderChangedWhilePausedUponResume(). 
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }
    }

    protected override void ClearCurrentOrderAndIdle() {
        D.Assert(!IsDead);
        _currentOrder = null;   // avoid order changed while paused system
        CurrentState = FleetState.Idling;   // 4.20.17 Will unsubscribe from any FsmEvents when exiting the Current non-Call()ed state
        D.AssertDefault(_executingOrderID); // 1.22.18 _executingOrderID now set to default(Guid) in _ExitState
    }

    #endregion

    #region StateMachine

    // 7.6.16 RelationsChange event methods added. Accommodates a change in relations when you know the current owner.
    // TODO Doesn't account for ownership changes that occur to the targets which may swap an enemy for a friend or vise versa

    protected new FleetState CurrentState {
        get { return (FleetState)base.CurrentState; }   // NRE means base.CurrentState is null -> not yet set
        set { base.CurrentState = value; }
    }

    protected new FleetState LastState {
        get { return base.LastState != null ? (FleetState)base.LastState : default(FleetState); }
    }

    protected override bool IsCurrentStateCalled { get { return IsCallableState(CurrentState); } }

    private bool IsCallableState(FleetState state) {
        return state == FleetState.Patrolling || state == FleetState.AssumingFormation
            || state == FleetState.Guarding || state == FleetState.Repairing || state == FleetState.Refitting
            || state == FleetState.Disbanding || state == FleetState.JoiningHanger || state == FleetState.Attacking
            || state == FleetState.Exploring || state == FleetState.FoundingSettlement || state == FleetState.FoundingStarbase;
    }

    private bool IsCurrentStateAnyOf(FleetState state) {
        return CurrentState == state;
    }

    private bool IsCurrentStateAnyOf(FleetState stateA, FleetState stateB) {
        return CurrentState == stateA || CurrentState == stateB;
    }

    private bool IsCurrentStateAnyOf(FleetState stateA, FleetState stateB, FleetState stateC) {
        return CurrentState == stateA || CurrentState == stateB || CurrentState == stateC;
    }

    /// <summary>
    /// Restarts execution of the CurrentState. If the CurrentState is a Call()ed state, Return()s first, then restarts
    /// execution of the state Return()ed too, aka the new CurrentState.
    /// </summary>
    private void RestartState() {
        if (IsDead) {
            D.Warn("{0}.RestartState() called when dead.", DebugName);
            return;
        }
        var stateWhenCalled = CurrentState;
        ReturnFromCalledStates();
        D.Log(/*ShowDebugLog, */"{0}.RestartState called from {1}.{2}. RestartedState = {3}.",
            DebugName, typeof(FleetState).Name, stateWhenCalled.GetValueName(), CurrentState.GetValueName());
        CurrentState = CurrentState;
    }

    #region FinalInitialize

    void FinalInitialize_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonNonCallableEnterStateValues();
    }

    void FinalInitialize_EnterState() {
        LogEvent();
    }

    void FinalInitialize_UponNewOrderReceived() {
        D.Error("{0} received FinalInitialize_UponNewOrderReceived().", DebugName);
    }

    [Obsolete("Error Detection")]
    void FinalInitialize_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void FinalInitialize_UponRelationsChangedWith(Player player) {
        LogEvent();
        // 5.19.17 Creators have elements CommenceOperations before Cmds do. Reversing won't work as
        // Cmds have Sensors too. Its the sensors that come up and detect things before all Cmd is ready
        // 5.30.17 IMPROVE No real harm as will change to Idling immediately afterward
        D.Warn("{0} received FinalInitialize_UponRelationsChangedWith({1}).", DebugName, player);
    }

    void FinalInitialize_ExitState() {
        LogEvent();
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Idling

    void Idling_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonNonCallableEnterStateValues();
        Data.Target = null;
        // 12.4.17 Can't ChangeAvailabilityTo(Available) here as can atomically cause a new order to be received 
        // which would violate FSM rule: no state change in void EnterStates
    }

    IEnumerator Idling_EnterState() {
        LogEvent();

        if (CurrentOrder != null) {
            if (CurrentOrder.FollowonOrder != null) {
                D.Log(ShowDebugLog, "{0} is executing follow-on order {1}.", DebugName, CurrentOrder.FollowonOrder);
                if (Availability == NewOrderAvailability.Unavailable) {
                    // Resulting state may throw an error if it doesn't expect Unavailable
                    D.Warn("FYI. {0} is about to execute FollowonOrder {1} while still {2}. Fixing.", DebugName, CurrentOrder.FollowonOrder.DebugName, NewOrderAvailability.Unavailable.GetValueName());
                    ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
                }

                OrderSource followonOrderSource = CurrentOrder.FollowonOrder.Source;
                D.AssertEqual(OrderSource.CmdStaff, followonOrderSource, CurrentOrder.ToString());

                CurrentOrder = CurrentOrder.FollowonOrder;
                yield return null;
                D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
            }
            //D.Log(ShowDebugLog, "{0} has completed {1} with no follow-on order queued.", DebugName, CurrentOrder);
            CurrentOrder = null;
        }

        if (AssessNeedForRepair()) {
            if (Availability == NewOrderAvailability.Unavailable) {
                // ExecuteRepairOrder state may throw an error if it doesn't expect Unavailable
                D.Warn("FYI. {0} is about to execute a Repair Order while still {1}. Fixing.", DebugName, NewOrderAvailability.Unavailable.GetValueName());
                ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
            }

            IssueCmdStaffsRepairOrder();
            yield return null;
            D.Error("{0} should never get here as CurrentOrder was changed to {1}.", DebugName, CurrentOrder);
        }

        __CheckForIdleWarnings();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // Set after repair check so if going to repair, repair assesses availability
        ChangeAvailabilityTo(NewOrderAvailability.Available);   // Can atomically cause a new order to be received
    }

    void Idling_UponNewOrderReceived() {
        LogEvent();
        // do nothing
    }

    [Obsolete("Error Detection")]
    void Idling_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Idling_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    void Idling_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    void Idling_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    void Idling_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Idling_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships are repositioned in formation
        IssueCmdStaffsAssumeFormationOrder();
    }

    void Idling_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    void Idling_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void Idling_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
            IssueCmdStaffsRepairOrder();
        }
    }

    [Obsolete("Not currently used")]
    void Idling_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // TODO
    }

    void Idling_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void Idling_UponResetOrderAndState() {
        LogEvent();
    }

    void Idling_UponDeath() {
        LogEvent();
    }

    void Idling_ExitState() {
        LogEvent();
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteMoveOrder

    #region ExecuteMoveOrder Support Members

    /// <summary>
    /// Assesses whether to order the fleet that has completed its move to assume formation.
    /// </summary>
    /// <param name="moveTgt">The move target.</param>
    /// <returns></returns>
    private bool AssessWhetherToAssumeFormationAfterMoveTo(IFleetNavigableDestination moveTgt) {
        if (moveTgt is ISystem || moveTgt is ISector || moveTgt is StationaryLocation || moveTgt is IFleetCmd) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines the AutoPilot move target from the provided moveOrder.
    /// Can be a StationaryLocation as the AutoPilot move target of a sector or a system
    /// from within the same system is the closest LocalAssemblyStation.
    /// </summary>
    /// <param name="moveOrder">The move order.</param>
    /// <returns></returns>
    private IFleetNavigableDestination DetermineApMoveTarget(FleetOrder moveOrder) {
        D.Assert(moveOrder.Directive == FleetDirective.Move || moveOrder.Directive == FleetDirective.FullSpeedMove);

        // Determine move target
        IFleetNavigableDestination apMoveTgt = null;
        IFleetNavigableDestination moveOrderTgt = moveOrder.Target as IFleetNavigableDestination;
        D.AssertNotNull(moveOrderTgt);
        ISystem_Ltd systemMoveTgt = moveOrderTgt as ISystem_Ltd;
        if (systemMoveTgt != null) {
            // move target is a system
            if (Topography == Topography.System) {
                // fleet is currently in a system
                IntVector3 fleetSectorID;
                bool isValidSector = TryGetSectorID(out fleetSectorID);
                D.Assert(isValidSector);

                ISector_Ltd fleetSector = SectorGrid.Instance.GetSector(fleetSectorID);
                ISystem_Ltd fleetSystem = fleetSector.System;
                if (fleetSystem == systemMoveTgt) {
                    // move target of a system from inside the same system is the closest assembly station within that system
                    apMoveTgt = GameUtility.GetClosest(Position, systemMoveTgt.LocalAssemblyStations);
                }
            }
        }
        else {
            ISector_Ltd sectorMoveTgt = moveOrderTgt as ISector_Ltd;
            if (sectorMoveTgt != null) {
                // target is a sector

                IntVector3 fleetSectorID;
                if (TryGetSectorID(out fleetSectorID)) {
                    ISector_Ltd fleetSector = SectorGrid.Instance.GetSector(fleetSectorID);
                    if (fleetSector == sectorMoveTgt) {
                        // move target of a sector from inside the same sector is the closest assembly station within that sector
                        apMoveTgt = GameUtility.GetClosest(Position, sectorMoveTgt.LocalAssemblyStations);
                    }
                }
            }
        }
        if (apMoveTgt == null) {
            apMoveTgt = moveOrderTgt;
        }
        return apMoveTgt;
    }

    /// <summary>
    /// Gets the standoff distance for the provided moveTgt. 
    /// </summary>
    /// <param name="moveTgt">The move target.</param>
    /// <returns></returns>
    private float CalcApMoveTgtStandoffDistance(IFleetNavigableDestination moveTgt) {
        float standoffDistance = Constants.ZeroF;
        Player moveTgtOwner;
        var baseTgt = moveTgt as IUnitBaseCmd_Ltd;
        if (baseTgt != null) {
            // move target is a base
            if (baseTgt.TryGetOwner(Owner, out moveTgtOwner)) {
                if (Owner.IsEnemyOf(moveTgtOwner)) {
                    // its an enemy base
                    standoffDistance = TempGameValues.__MaxBaseWeaponsRangeDistance;
                }
            }
        }
        else {
            var fleetTgt = moveTgt as IFleetCmd_Ltd;
            if (fleetTgt != null) {
                // move target is a fleet
                if (fleetTgt.TryGetOwner(Owner, out moveTgtOwner)) {
                    if (Owner.IsEnemyOf(moveTgtOwner)) {
                        // its an enemy fleet
                        standoffDistance = TempGameValues.__MaxFleetWeaponsRangeDistance;
                    }
                }
            }
        }
        return standoffDistance;
    }

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToMove() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    #endregion

    void ExecuteMoveOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        D.AssertNotNull(CurrentOrder.Target);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = DetermineApMoveTarget(CurrentOrder);

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteMoveOrder_EnterState() {
        LogEvent();

        // move to the move target
        Speed speed = CurrentOrder.Directive == FleetDirective.FullSpeedMove ? Speed.Full : Speed.Standard;
        float standoffDistance = CalcApMoveTgtStandoffDistance(_fsmTgt);
        _moveHelper.PlotPilotCourse(_fsmTgt, speed, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at move target
            yield return null;
        }

        if (AssessWhetherToAssumeFormationAfterMoveTo(_fsmTgt)) {
            FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
            Call(FleetState.AssumingFormation);
            yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

            if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
                yield return null;
                D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
            }
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteMoveOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteMoveOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteMoveOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);

        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteMoveOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Move target {1}.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    void ExecuteMoveOrder_UponApTgtUncatchable() {
        LogEvent();
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteMoveOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteMoveOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteMoveOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, AssumingFormation will handle if Call()ed
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its move target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteMoveOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If RedAlert and in our path???
    }

    void ExecuteMoveOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteMoveOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteMoveOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteMoveOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // do nothing. Relations changes don't effect moving or entering high orbit.
    }

    void ExecuteMoveOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // do nothing. Owner access changes don't effect Move orders
    }

    void ExecuteMoveOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // do nothing. Owner changes don't effect Move orders
    }

    void ExecuteMoveOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);        // FIXME Will need to change when awareness beyond fleets is included
        if (item == _fsmTgt) {
            D.Assert(!OwnerAiMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are moving too without first losing awareness
            // our move target is the fleet we've lost awareness of
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteMoveOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueCmdStaffsAssumeFormationOrder();
        // TODO Communicate failure to boss?
    }

    [Obsolete("Not currently used")]
    void ExecuteMoveOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteMoveOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteMoveOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteMoveOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteMoveOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteAssumeFormationOrder

    #region ExecuteAssumeFormationOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToAssumeFormation() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    #endregion

    void ExecuteAssumeFormationOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target as IFleetNavigableDestination;
        // No reason to subscribe to _fsmTgt-related events as _fsmTgt is either StationaryLocation or null
        ChangeAvailabilityTo(NewOrderAvailability.EasilyAvailable);
    }

    IEnumerator ExecuteAssumeFormationOrder_EnterState() {
        LogEvent();

        if (_fsmTgt != null) {
            // a LocalAssyStation target was specified so move there together first
            D.Assert(_fsmTgt is StationaryLocation);

            _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, Constants.ZeroF);

            while (!_moveHelper.IsPilotEngaged) {
                // wait until course is plotted and pilot engaged
                yield return null;
            }

            while (_moveHelper.IsPilotEngaged) {
                // wait for pilot to arrive at target to assume formation
                yield return null;
            }

            _fsmTgt = null; // only used to Move to the target if any
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteAssumeFormationOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteAssumeFormationOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteAssumeFormationOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteAssumeFormationOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its target {1} where it is to assume formation.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteAssumeFormationOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteAssumeFormationOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, AssumingFormation will include it.
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its patrol target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteAssumeFormationOrder_UponAlertStatusChanged() {
        LogEvent();
        // Do nothing. Already getting into defensive formation
    }

    void ExecuteAssumeFormationOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteAssumeFormationOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteAssumeFormationOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteAssumeFormationOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    [Obsolete("Not currently used")]
    void ExecuteAssumeFormationOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No reason for _fsmTgt-related event handlers as _fsmTgt is either null or a StationaryLocation

    void ExecuteAssumeFormationOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteAssumeFormationOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAssumeFormationOrder_ExitState() {
        LogEvent();
        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region AssumingFormation

    // 7.2.16 Call()ed State with no need for _fsmTgt
    // 3.23.17 Existence of this state allows it to be Call()ed by other states without needing to issue
    // an AssumeFormation order. It also means Return() goes back to the state that Call()ed it.

    #region AssumingFormation Support Members

    private void DetermineShipsToReceiveAssumeStationOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.AssumeStation)) {
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
    }

    #endregion

    void AssumingFormation_UponPreconfigureState() {
        LogEvent();
        // 3.21.17 AssumingFormation doesn't care whether _fsmTgt is set or not. Eliminating this rqmt allows other
        // states to Call() it directly without issuing a AssumeFormation order
        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName(), includeFsmTgt: false);
        D.AssertNotNull(CurrentOrder);  // 11.24.17 Call()ed but there must be some order being executed

        DetermineShipsToReceiveAssumeStationOrder();
        // 12.7.17 Don't ChangeAvailabilityTo() in states that can be Call()ed by more than one ExecuteOrder state
    }

    IEnumerator AssumingFormation_EnterState() {
        LogEvent();

        var shipAssumeFormationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source, _executingOrderID);
        D.Log(ShowDebugLog, "{0} issuing {1} to all ships.", DebugName, shipAssumeFormationOrder.DebugName);
        _fsmShipsExpectedToCallbackWithOrderOutcome.ForAll(ship => ship.CurrentOrder = shipAssumeFormationOrder);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until all ships are onStation
            yield return null;
        }
        Return();
    }

    void AssumingFormation_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Present: {2}.",   //  2.14.18 Failed from _UponSubJoined, 5.6.18 Failed
                        DebugName, ship.DebugName, _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.OrdersChange:
            case OrderOutcome.NeedsRepair:
            // Ship will get repaired, but even if it goes to its formationStation to do so
            // it won't communicate its success back to Cmd since Captain ordered it, not Cmd
            case OrderOutcome.Death:
            case OrderOutcome.Ownership:
                isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Outcome: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.TgtDeath:
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void AssumingFormation_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void AssumingFormation_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void AssumingFormation_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void AssumingFormation_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // fleet still waiting on one or more ships to assume station
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.AssumeStation)) {
                // can issue AssumeStation order
                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);
                ShipOrder order = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff, _executingOrderID);
                D.Log("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                    CurrentState.GetValueName());
                // 5.6.18 Added Log to help diagnose 'not present to be removed' errors on line 1500
                D.Log("{0}: Ships that had not completed AssumingStation when {1} joined: {2}.", DebugName, subordinateShip.DebugName,
                    _fsmShipsExpectedToCallbackWithOrderOutcome.Except(subordinateShip).Select(s => s.DebugName).Concatenate());
                subordinateShip.CurrentOrder = order;
            }
        }
    }

    void AssumingFormation_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        // TODO
    }

    void AssumingFormation_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void AssumingFormation_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void AssumingFormation_UponEnemyDetected() {
        LogEvent();
    }

    void AssumingFormation_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void AssumingFormation_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    [Obsolete("Not currently used")]
    void AssumingFormation_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // No need for _fsmTgt-related event handlers as there is no _fsmTgt

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void AssumingFormation_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteExploreOrder

    #region ExecuteExploreOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_ExploringToExplore() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            // No longer allowed to explore target or fully explored
            {FsmCallReturnCause.TgtRelationship, () =>      {
                IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
                IssueCmdStaffsAssumeFormationOrder(closestLocalAssyStation);
            }                                                                                                           },
            // Fleet no longer has available, qualified ships to finish the exploration
            {FsmCallReturnCause.Qualifications, () =>       {
                IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable;
                var closestLocalAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
                IssueCmdStaffsAssumeFormationOrder(closestLocalAssyStation);
            }                                                                                                           },
            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Exploring.GetValueName());
    }

    #endregion

    void ExecuteExploreOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        IFleetExplorable fleetExploreTgt = CurrentOrder.Target as IFleetExplorable; // Fleet explorable targets are sectors, systems and UCenter
        D.AssertNotNull(fleetExploreTgt);
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));
        D.Assert(!fleetExploreTgt.IsFullyExploredBy(Owner));

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = fleetExploreTgt;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
        // IFleetExplorables cannot die

        bool toSubscribeToPlanetDiscovery = false;
        bool toSubscribeToStarbaseDiscovery = false;
        if (fleetExploreTgt is ISystem_Ltd) {
            toSubscribeToPlanetDiscovery = true;
        }
        else {
            var sectorExploreTgt = fleetExploreTgt as ISector_Ltd;
            if (sectorExploreTgt != null) {
                toSubscribeToStarbaseDiscovery = true;
                toSubscribeToPlanetDiscovery = sectorExploreTgt.System != null;
            }
        }

        if (toSubscribeToPlanetDiscovery) {
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAware_Planet);
            D.Assert(isSubscribed);
        }
        if (toSubscribeToStarbaseDiscovery) {
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);
            D.Assert(isSubscribed);

        }
        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteExploreOrder_EnterState() {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;

        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));  // should not fail using _fsmTgt and awareness event subscriptions

        // move to the explore target
        float standoffDistance = Constants.ZeroF;
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at explore target
            yield return null;
        }

        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));  // 7.28.18 Failed. Should not fail using _fsmTgt and awareness event subscriptions

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Exploring, CurrentState);
        Call(FleetState.Exploring);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        StationaryLocation closestAssyStation = GameUtility.GetClosest(Position, fleetExploreTgt.LocalAssemblyStations);
        D.Log(/*ShowDebugLog,*/ "{0} reports {1} has been explored. Assuming Formation.", DebugName, fleetExploreTgt.DebugName);
        IssueCmdStaffsAssumeFormationOrder(closestAssyStation);
    }

    void ExecuteExploreOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteExploreOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        if (_isWaitingToProcessReturn) {
            D.Warn("FYI. {0}: Ignoring leaked callback in 1 frame gap between Exploring and ExecuteExploreOrder.", DebugName);
            return;
        }
        D.Log(ShowDebugLog, "{0}.ExecuteExploreOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        if (!_moveHelper.IsPilotEngaged) {
            // 6.23.18 This should no longer occur now that I use _isWaitingToProcessReturn to filter out callbacks occurring in time gap
            D.Error("{0}: Leaked callback to ExecuteExploreOrder from Exploring.", DebugName);
        }
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteExploreOrder_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached its Explore target {1}, is DisengagingPilot and beginning exploration.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteExploreOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteExploreOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteExploreOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        // do nothing. No orders currently being executed
    }

    void ExecuteExploreOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Exploring will include it.
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its explore target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteExploreOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Red) {
            // We are probably spread out and vulnerable, so pull together for defense (UNCLEAR and entrench?)
            IssueCmdStaffsAssumeFormationOrder();
            // TODO probably shouldn't even take/qualify for an explore order when issued while at RedAlert
        }
    }

    void ExecuteExploreOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteExploreOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteExploreOrder_UponRelationsChangedWith(Player player) {
        LogEvent();

        bool toAssumeFormation = false;
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // 2.9.18 newly discovered or known owner/player now at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);
            // 7.28.18 fsmTgtOwner and player not necessarily the same - could be allies
            toAssumeFormation = true;
        }
        else if (fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // 2.9.18 relation changed player could have nothing to do with exploration already completed
            toAssumeFormation = true;
        }

        if (toAssumeFormation) {
            D.Log(/*ShowDebugLog,*/ "{0} {1} order to {2} discovered no longer valid when Relations with {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, player.DebugName, Owner.GetCurrentRelations(player).GetValueName());
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteExploreOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // intel coverage on the item increased to the point I now know the owner and they are at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteExploreOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // new known owner is either at war with us or an ally
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    // No need for _UponFsmTgtDeath() as IFleetExplorable targets cannot die

    void ExecuteExploreOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // 7.24.18 Confirming this does occur!
            D.Log("{0} {1} order for {2} is no longer valid due to discovery of {3}!", DebugName, CurrentOrder.Directive.GetValueName(),
                fleetExploreTgt.DebugName, item.DebugName);
            IssueCmdStaffsAssumeFormationOrder();
        }
        // otherwise, nothing to do as the discovery will be included when entering Exploring state
    }

    [Obsolete("Not currently used")]
    void ExecuteExploreOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // do nothing. Order Outcome callback will handle
    }

    void ExecuteExploreOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteExploreOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteExploreOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteExploreOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAware_Planet);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Exploring

    // 1.5.18 Call()ed State by ExecuteExploreOrder only

    #region Exploring Support Members

    private IDictionary<IShipExplorable, ShipItem> _fsmSystemExploreTgtAssignments;

    private IDictionary<IShipExplorable, ShipItem> _fsmSectorStarbaseExploreTgtAssignments;

    private bool IsShipExploreTargetPartOfSystem(IShipExplorable shipExploreTgt) {
        return (shipExploreTgt is IPlanet_Ltd || shipExploreTgt is IStar_Ltd);
    }

    private bool IsShipExploreTargetASectorStarbase(IShipExplorable shipExploreTgt) {
        return shipExploreTgt is IStarbaseCmd_Ltd;
    }

    /// <summary>
    /// Handles the condition where a planet in the system being explored was discovered while exploring.
    /// </summary>
    /// <param name="planetDiscovered">The planet discovered.</param>
    private void HandleDiscoveredPlanetWhileExploring(IShipExplorable planetDiscovered) {
        ISystem_Ltd systemTgt = _fsmTgt as ISystem_Ltd;
        if (systemTgt == null) {
            ISector_Ltd sectorTgt = _fsmTgt as ISector_Ltd;
            D.AssertNotNull(sectorTgt);
            systemTgt = sectorTgt.System;
        }
        D.AssertNotNull(systemTgt);
        D.Assert(!planetDiscovered.IsFullyExploredBy(Owner));
        D.Assert(!_fsmSystemExploreTgtAssignments.ContainsKey(planetDiscovered));

        ShipItem noAssignedShip = null;
        _fsmSystemExploreTgtAssignments.Add(planetDiscovered, noAssignedShip);
        D.Log(ShowDebugLog, "{0} has added {1}'s newly discovered planet {2} to explore.", DebugName, systemTgt.DebugName, planetDiscovered.DebugName);

        // 12.10.17 Removed assignment of ship to discovered planet as this can occur before fleet even starts moving to the system.
        // If it occurs that early, the system explore setup process discovers the already assigned ship and throws an error.
        // My analysis of the assignment processes below leads me to conclude that a ship will get assigned even if planet is added late.
    }

    /// <summary>
    /// Handles the condition where a starbase in the sector being explored was discovered while exploring.
    /// </summary>
    /// <param name="starbaseDiscovered">The starbase discovered.</param>
    private void HandleDiscoveredStarbaseWhileExploring(IShipExplorable starbaseDiscovered) {
        ISector_Ltd sectorTgt = _fsmTgt as ISector_Ltd;
        D.AssertNotNull(sectorTgt);
        D.Assert(!starbaseDiscovered.IsFullyExploredBy(Owner)); // can only discover a starbase 1 time as it never regresses below Basic coverage
        D.Assert(!_fsmSectorStarbaseExploreTgtAssignments.ContainsKey(starbaseDiscovered));

        if (starbaseDiscovered.IsExploringAllowedBy(Owner)) {
            ShipItem noAssignedShip = null;
            _fsmSectorStarbaseExploreTgtAssignments.Add(starbaseDiscovered, noAssignedShip);
            D.LogBold("{0} has added {1}'s newly discovered starbase {2} to explore.", DebugName, sectorTgt.DebugName, starbaseDiscovered.DebugName);
        }
    }

    /// <summary>
    /// Handles the condition where a ship is no longer available to explore. Returns <c>true</c>
    /// if another ship was assigned to replace this one (not necessarily with the same explore target),
    /// <c>false</c> if no more ships are currently available.
    /// <remarks>Typically this occurs when a ship fails to complete its assigned exploration mission
    /// either because it dies or is so wounded that it needs to repair.</remarks>
    /// </summary>
    /// <param name="unavailableShip">The unavailable ship.</param>
    /// <param name="shipExploreTgt">The explore target.</param>
    /// <returns></returns>
    private bool HandleShipNoLongerAvailableToExplore(ShipItem unavailableShip, IShipExplorable shipExploreTgt) {
        bool isExploringSystem = false;
        if (_fsmSystemExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
            isExploringSystem = true;
            if (_fsmSystemExploreTgtAssignments.Values.Contains(unavailableShip)) {
                // ship had explore assignment in system so remove it
                var unavailableShipTgt = _fsmSystemExploreTgtAssignments.Single(kvp => kvp.Value == unavailableShip).Key;
                _fsmSystemExploreTgtAssignments[unavailableShipTgt] = null;
            }
        }

        bool isExploringSector = false;
        if (_fsmSectorStarbaseExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
            isExploringSector = true;
            D.Assert(_fsmTgt is ISector_Ltd);
            if (_fsmSectorStarbaseExploreTgtAssignments.Values.Contains(unavailableShip)) {
                // ship had explore assignment in sector so remove it
                var unavailableShipTgt = _fsmSectorStarbaseExploreTgtAssignments.Single(kvp => kvp.Value == unavailableShip).Key;
                _fsmSectorStarbaseExploreTgtAssignments[unavailableShipTgt] = null;
            }
        }

        if (isExploringSystem || isExploringSector) {
            D.AssertNotEqual(isExploringSector, isExploringSystem); // only 1 can be true
        }

        bool isNewShipAssigned;
        IList<ShipItem> ships;
        if (TryGetShips(out ships, minAvailability: NewOrderAvailability.Available, avoidHQ: true, desiredQty: 1,
            priorityCats: TempGameValues.DesiredExplorationShipCategories)) {
            ShipItem newExploreShip = ships.First();
            if (isExploringSystem) {
                AssignShipToExploreSystemTgt(newExploreShip);
            }
            else if (isExploringSector) {
                AssignShipToExploreSectorStarbase(newExploreShip);
            }
            else {
                AssignShipToExploreItem(newExploreShip, shipExploreTgt);
            }
            isNewShipAssigned = true;
        }
        else {
            isNewShipAssigned = false;
            D.Log(/*ShowDebugLog, */"{0} found no available ships to explore {1} after {2} became unavailable.", DebugName,
                shipExploreTgt.DebugName, unavailableShip.DebugName);
        }
        return isNewShipAssigned;
    }

    private void ExploreSector(ISector_Ltd sector) {
        // If now told to explore the sector and it has a system, then we couldn't have gotten here without fully exploring the system first
        if (sector.System != null) {
            ISystem sectorSystem = sector.System as ISystem;
            D.AssertEqual(IntelCoverage.Comprehensive, sectorSystem.GetIntelCoverage(Owner), DebugName);
        }

        IList<IShipExplorable> sectorStarbaseTgtsToExplore =
            (from starbase in sector.AllStarbases
             where OwnerAiMgr.HasKnowledgeOf(starbase)
             let eStarbase = starbase as IShipExplorable
             where eStarbase.IsExploringAllowedBy(Owner) && !eStarbase.IsFullyExploredBy(Owner)
             select eStarbase).ToList();

        if (sectorStarbaseTgtsToExplore.Any()) {
            // Note: Knowledge of each starbase explore target in sector will be checked as soon as Ship gets explore order
            foreach (var exploreTgt in sectorStarbaseTgtsToExplore) {
                if (!_fsmSectorStarbaseExploreTgtAssignments.ContainsKey(exploreTgt)) {
                    if (exploreTgt.IsExploringAllowedBy(Owner) && exploreTgt.IsFullyExploredBy(Owner)) {
                        ShipItem noAssignedShip = null;
                        _fsmSectorStarbaseExploreTgtAssignments.Add(exploreTgt, noAssignedShip);
                    }
                }
                else {
                    D.Log(@"{0} found SectorExploreStarbaseKey {1} already present during explore setup for {2}. 
                        This is because it was discovered just prior to setup.", DebugName, exploreTgt.DebugName, sector.DebugName);
                }
            }

            int desiredExplorationShipQty = sectorStarbaseTgtsToExplore.Count;
            IList<ShipItem> ships = GetShips(desiredMinAvailability: NewOrderAvailability.Available, avoidHQ: true,
                desiredQty: desiredExplorationShipQty, priorityCats: TempGameValues.DesiredExplorationShipCategories);

            Stack<ShipItem> explorationShips = new Stack<ShipItem>(ships);
            while (explorationShips.Count > Constants.Zero) {
                bool wasAssigned = AssignShipToExploreSectorStarbase(explorationShips.Pop());
                if (!wasAssigned) {
                    break; // all remaining unexplored targets in the sector, if any, have assigned ships
                }
            }
        }
        else {
            // no starbases or no starbases we are allowed to explore, so we've done our job and 'fully explored'
            (sector as ISector).SetIntelCoverage(Owner, IntelCoverage.Comprehensive);
        }
    }

    private void ExploreSystem(ISystem_Ltd system) {
        IList<IShipExplorable> shipSystemTgtsToExplore =
            (from planet in system.Planets
             where OwnerAiMgr.HasKnowledgeOf(planet)
             let ePlanet = planet as IShipExplorable
             where !ePlanet.IsFullyExploredBy(Owner)
             select ePlanet).ToList();

        IShipExplorable star = system.Star as IShipExplorable;
        if (!star.IsFullyExploredBy(Owner)) {
            shipSystemTgtsToExplore.Add(star);
        }
        D.Assert(shipSystemTgtsToExplore.Count > Constants.Zero);  // OPTIMIZE System has already been validated for exploration
        // Note: Knowledge of each explore target in system will be checked as soon as Ship gets explore order
        foreach (var exploreTgt in shipSystemTgtsToExplore) {
            if (!_fsmSystemExploreTgtAssignments.ContainsKey(exploreTgt)) {
                // 5.11.17 Got a duplicate key exception during initial setup which almost certainly occurred because this fleet became
                // 'aware' of another planet in the system before this initial setup could occur, aka the gap between PreConfigure and EnterState
                ShipItem noAssignedShip = null;
                _fsmSystemExploreTgtAssignments.Add(exploreTgt, noAssignedShip);
            }
            else {
                //D.Log(ShowDebugLog, @"{0} found SystemExploreTargetKey {1} already present during explore setup for {2}. 
                //    This is because it was discovered just prior to setup.", DebugName, exploreTgt.DebugName, system.DebugName);
            }
        }

        int desiredExplorationShipQty = shipSystemTgtsToExplore.Count;
        IList<ShipItem> ships = GetShips(desiredMinAvailability: NewOrderAvailability.Available, avoidHQ: true,
            desiredQty: desiredExplorationShipQty, priorityCats: TempGameValues.DesiredExplorationShipCategories);

        Stack<ShipItem> explorationShips = new Stack<ShipItem>(ships);
        while (explorationShips.Count > Constants.Zero) {
            bool wasAssigned = AssignShipToExploreSystemTgt(explorationShips.Pop());
            if (!wasAssigned) {
                break; // all remaining unexplored targets in the system, if any, have assigned ships
            }
        }
    }

    /// <summary>
    /// Handles the situation where the provided ship has either successfully explored the provided target, 
    /// or the exploration of the target failed because the target is dead. Returns <c>true</c> if a new 
    /// exploration target was assigned to the ship, <c>false</c> otherwise. If the ship received no new 
    /// assignment, it has been instructed to gather at a nearby assembly station.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="exploreTgt">The explore target.</param>
    /// <returns></returns>
    private bool HandleTargetExploredOrDead(ShipItem ship, IShipExplorable exploreTgt) {
        bool isNowAssigned;
        if (IsShipExploreTargetASectorStarbase(exploreTgt)) {
            _fsmSectorStarbaseExploreTgtAssignments.Remove(exploreTgt);
            isNowAssigned = AssignShipToExploreSectorStarbase(ship);
        }
        else {
            D.Assert(IsShipExploreTargetPartOfSystem(exploreTgt));
            _fsmSystemExploreTgtAssignments.Remove(exploreTgt);
            isNowAssigned = AssignShipToExploreSystemTgt(ship);
        }
        if (!isNowAssigned) {
            // 11.24.17 Ship is done exploring, so no callback is desired on following orders. May throw errors if callback is stipulated
            // 12.17.17 No need to send Flagship to local assy station here as ship.ExecuteAssumeStationOrder 
            // handles that itself if Flagship in close orbit
            ShipOrder assumeStationOrder = new ShipOrder(ShipDirective.AssumeStation, CurrentOrder.Source);
            ship.CurrentOrder = assumeStationOrder;
        }
        return isNowAssigned;
    }

    /// <summary>
    /// Assigns the ship to explore any remaining unexplored targets in the System
    /// that don't already have an assigned ship. Returns <c>true</c> if the ship
    /// was assigned, <c>false</c> if not which means all unexplored remaining targets
    /// in the system, if any, have assigned ships.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    private bool AssignShipToExploreSystemTgt(ShipItem ship) {
        //D.Log(ShowDebugLog, "{0} is attempting to assign {1} to explore a system target.", DebugName, ship.DebugName);
        if (_fsmSystemExploreTgtAssignments.Values.Contains(ship)) {
            // 12.10.17 Added for debugging which was solved
            D.Error("{0} found {1} already assigned. \nExplore assignments: {2}.", DebugName, ship.DebugName,
                _fsmSystemExploreTgtAssignments.ToDebugString());
        }
        var tgtsWithoutAssignedShip = _fsmSystemExploreTgtAssignments.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
        if (tgtsWithoutAssignedShip.Any()) {
            var closestExploreTgt = GameUtility.GetClosest(ship.Position, tgtsWithoutAssignedShip);
            if (closestExploreTgt.IsFullyExploredBy(Owner)) {
                // in interim, target could have been fully explored by another fleet's ship of ours
                return HandleTargetExploredOrDead(ship, closestExploreTgt);
            }
            AssignShipToExploreItem(ship, closestExploreTgt);
            _fsmSystemExploreTgtAssignments[closestExploreTgt] = ship;
            return true;
        }
        return false;
    }

    private bool AssignShipToExploreSectorStarbase(ShipItem ship) {
        D.Log("{0} is attempting to assign {1} to explore a sector starbase.", DebugName, ship.DebugName);
        if (_fsmSectorStarbaseExploreTgtAssignments.Values.Contains(ship)) {
            // 6.26.18 Occurred shortly after adding sector exploring
            D.Error("{0} found {1} already assigned. \nExplore assignments: {2}.", DebugName, ship.DebugName,
                _fsmSectorStarbaseExploreTgtAssignments.ToDebugString());
        }
        var tgtsWithoutAssignedShip = _fsmSectorStarbaseExploreTgtAssignments.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key);
        if (tgtsWithoutAssignedShip.Any()) {
            var closestExploreTgt = GameUtility.GetClosest(ship.Position, tgtsWithoutAssignedShip);
            if (closestExploreTgt.IsFullyExploredBy(Owner)) {
                // in interim, target could have been fully explored by another fleet's ship of ours
                return HandleTargetExploredOrDead(ship, closestExploreTgt);
            }
            AssignShipToExploreItem(ship, closestExploreTgt);
            _fsmSectorStarbaseExploreTgtAssignments[closestExploreTgt] = ship;
            return true;
        }
        return false;
    }

    private void AssignShipToExploreItem(ShipItem ship, IShipExplorable item) {
        if (!item.IsExploringAllowedBy(Owner)) {
            D.Error("{0} attempting to assign {1} to illegally explore {2}.", DebugName, ship.DebugName, item.DebugName);
        }
        if (item.IsFullyExploredBy(Owner)) {
            D.Error("{0} attempting to assign {1} to explore {2} which is already explored.", DebugName, ship.DebugName, item.DebugName);
        }
        //D.Log(ShowDebugLog, "{0} has assigned {1} to explore {2}.", DebugName, ship.DebugName, item.DebugName);
        ShipOrder exploreOrder = new ShipOrder(ShipDirective.Explore, CurrentOrder.Source, _executingOrderID, item);
        ship.CurrentOrder = exploreOrder;
    }

    #endregion

    void Exploring_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));

        _fsmSystemExploreTgtAssignments = _fsmSystemExploreTgtAssignments ?? new Dictionary<IShipExplorable, ShipItem>();
        _fsmSectorStarbaseExploreTgtAssignments = _fsmSectorStarbaseExploreTgtAssignments ?? new Dictionary<IShipExplorable, ShipItem>();
        D.Assert(!_fsmSystemExploreTgtAssignments.Any());
        D.Assert(!_fsmSectorStarbaseExploreTgtAssignments.Any());
    }

    IEnumerator Exploring_EnterState() {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        D.Assert(fleetExploreTgt.IsExploringAllowedBy(Owner));  // should not fail using _fsmTgt event subscriptions

        // The fleetExploreTgt (Target in CurrentOrder) can either be a System or a Sector. If its a System and not already fully 
        // explored, just explore the System and leave it at that. If a Sector, explore the Sector's System if it has one and it 
        // needs exploring, then explore the rest of the Sector if it needs it.
        ISystem_Ltd systemExploreTgt = fleetExploreTgt as ISystem_Ltd;
        if (systemExploreTgt != null) {
            if (!fleetExploreTgt.IsFullyExploredBy(Owner)) { // could have become 'fully explored' while moving to fleetExploreTgt
                ExploreSystem(systemExploreTgt);
            }
        }
        else {
            ISector_Ltd sectorExploreTgt = fleetExploreTgt as ISector_Ltd;
            if (sectorExploreTgt != null) {
                if (sectorExploreTgt.System != null) {
                    IFleetExplorable sectorSystem = sectorExploreTgt.System as IFleetExplorable;
                    // Since allowed to explore sector, sectorOwner is either unknown or known and not at war with fleet owner.
                    // If System is owned, it must be the same as sectorOwner, so can't be at war with system owner either.
                    D.Assert(sectorSystem.IsExploringAllowedBy(Owner));
                    if (!sectorSystem.IsFullyExploredBy(Owner)) {
                        ExploreSystem(sectorExploreTgt.System);

                        while (!sectorSystem.IsFullyExploredBy(Owner)) {
                            yield return null;
                        }
                    }
                }
                // fleetExploreTgt (Sector) could have become fully explored while moving to Sector or after fully exploring sector's system
                if (!fleetExploreTgt.IsFullyExploredBy(Owner)) {
                    ExploreSector(sectorExploreTgt);
                }
            }
            else {
                IUniverseCenter_Ltd uCenterExploreTgt = fleetExploreTgt as IUniverseCenter_Ltd;
                D.AssertNotNull(uCenterExploreTgt);
                IList<ShipItem> exploreShips = GetShips(desiredMinAvailability: NewOrderAvailability.Available, avoidHQ: true, desiredQty: 1,
                    priorityCats: TempGameValues.DesiredExplorationShipCategories);
                IShipExplorable uCenterShipExploreTgt = uCenterExploreTgt as IShipExplorable;
                D.AssertNotNull(uCenterShipExploreTgt);
                if (!fleetExploreTgt.IsFullyExploredBy(Owner)) { // could have become 'fully explored' while moving to fleetExploreTgt
                    AssignShipToExploreItem(exploreShips[Constants.Zero], uCenterShipExploreTgt);    // HACK
                }
            }
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        while (!fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // wait here until target is fully explored. If exploration fails, state will Return() with a FsmReturnCause
            yield return null;
        }

        Return();
    }

    void Exploring_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();

        IShipExplorable shipExploreTgt = target as IShipExplorable;
        D.AssertNotNull(shipExploreTgt);

        if (IsShipExploreTargetPartOfSystem(shipExploreTgt)) {
            // exploreTgt is a planet or star
            if (!_fsmSystemExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
                string successMsg = outcome == OrderOutcome.Success ? "successfully" : "unsuccessfully";
                D.Log("{0}: FailureCause = {1}.", DebugName, outcome.GetValueName());
                D.Error("{0}: {1} {2} completed its exploration of {3} with no record of it being assigned. AssignedTgts: {4}, AssignedShips: {5}.",
                    DebugName, ship.DebugName, successMsg, shipExploreTgt.DebugName, _fsmSystemExploreTgtAssignments.Keys.Select(tgt => tgt.DebugName).Concatenate(),
                    _fsmSystemExploreTgtAssignments.Values.Select(shp => shp.DebugName).Concatenate());
            }
            if (outcome == OrderOutcome.Success) {
                HandleTargetExploredOrDead(ship, shipExploreTgt);
            }
            else {
                // unsuccessful
                bool isNewShipAssigned;
                bool testForAdditionalExploringShips = false;
                switch (outcome) {
                    case OrderOutcome.TgtRelationship:
                        var returnHandler = GetCurrentCalledStateReturnHandler();
                        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
                        Return();
                        break;
                    case OrderOutcome.TgtDeath:
                        HandleTargetExploredOrDead(ship, shipExploreTgt);
                        // This is effectively counted as a success and will show up during the _EnterState's
                        // continuous test System.IsFullyExplored. As not really a failure, no reason to issue a fleet recall.
                        break;
                    case OrderOutcome.NeedsRepair:
                    case OrderOutcome.OrdersChange:
                    case OrderOutcome.Ownership:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (ElementCount > Constants.One) {
                                // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                // more than one ship to explore a System, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, ElementCount);
                                // Unavailable ship is only one left in fleet so exploration failed
                                returnHandler = GetCurrentCalledStateReturnHandler();
                                returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                                Return();
                            }
                        }
                        break;
                    case OrderOutcome.Death:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (ElementCount > Constants.One) {    // >1 as dead ship has not yet been removed from fleet
                                                                   // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                                                   // more than one ship to explore a System, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, ElementCount);  // dead ship has not yet been removed from fleet
                                // Do nothing as Unit is about to die
                            }
                        }
                        break;
                    case OrderOutcome.TgtUnreachable:
                        D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                            OrderOutcome.TgtUnreachable.GetValueName());
                        break;
                    case OrderOutcome.TgtUnjoinable:
                    case OrderOutcome.TgtUncatchable:
                    // 4.15.17 Only ships pursued by ships can have a Ship.TgtUncatchable fail cause
                    case OrderOutcome.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
                }

                if (testForAdditionalExploringShips) {
                    var otherShipsCurrentlyExploring = Elements.Cast<ShipItem>().Except(ship).Where(s => s.IsCurrentOrderDirectiveAnyOf(ShipDirective.Explore));
                    if (otherShipsCurrentlyExploring.Any()) {
                        // Do nothing as there are other ships currently exploring so exploreTarget will eventually be assigned a ship
                    }
                    else {
                        // No remaining ships out exploring so no ships will become available in the future to explore
                        var returnHandler = GetCurrentCalledStateReturnHandler();
                        returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                        Return();
                    }
                }
            }
        }
        else if (IsShipExploreTargetASectorStarbase(shipExploreTgt)) {
            if (!_fsmSectorStarbaseExploreTgtAssignments.ContainsKey(shipExploreTgt)) {
                string successMsg = outcome == OrderOutcome.Success ? "successfully" : "unsuccessfully";
                D.Log("{0}: FailureCause = {1}.", DebugName, outcome.GetValueName());
                D.Error("{0}: {1} {2} completed its exploration of {3} with no record of it being assigned. AssignedTgts: {4}, AssignedShips: {5}.",
                    DebugName, ship.DebugName, successMsg, shipExploreTgt.DebugName, _fsmSectorStarbaseExploreTgtAssignments.Keys.Select(tgt => tgt.DebugName).Concatenate(),
                    _fsmSectorStarbaseExploreTgtAssignments.Values.Select(shp => shp.DebugName).Concatenate());
            }
            if (outcome == OrderOutcome.Success) {
                HandleTargetExploredOrDead(ship, shipExploreTgt);
            }
            else {
                // unsuccessful
                bool isNewShipAssigned;
                bool testForAdditionalExploringShips = false;
                switch (outcome) {
                    case OrderOutcome.TgtRelationship:
                        var returnHandler = GetCurrentCalledStateReturnHandler();
                        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
                        Return();
                        break;
                    case OrderOutcome.TgtDeath:
                        HandleTargetExploredOrDead(ship, shipExploreTgt);
                        // This is effectively counted as a success and will show up during the _EnterState's
                        // continuous test Sector.IsFullyExplored. As not really a failure, no reason to issue a fleet recall.
                        break;
                    case OrderOutcome.NeedsRepair:
                    case OrderOutcome.OrdersChange:
                    case OrderOutcome.Ownership:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (ElementCount > Constants.One) {
                                // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                // more than one ship to explore a Sector, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, ElementCount);
                                // Unavailable ship is only one left in fleet so exploration failed
                                returnHandler = GetCurrentCalledStateReturnHandler();
                                returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                                Return();
                            }
                        }
                        break;
                    case OrderOutcome.Death:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (ElementCount > Constants.One) {    // >1 as dead ship has not yet been removed from fleet
                                                                   // This is not the last ship in the fleet, but the others aren't available. Since it usually takes 
                                                                   // more than one ship to explore a Sector, the other ships might currently be exploring
                                testForAdditionalExploringShips = true;
                            }
                            else {
                                D.AssertEqual(Constants.One, ElementCount);  // dead ship has not yet been removed from fleet
                                // Do nothing as Unit is about to die
                            }
                        }
                        break;
                    case OrderOutcome.TgtUnreachable:
                        D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                            OrderOutcome.TgtUnreachable.GetValueName());
                        break;
                    case OrderOutcome.TgtUnjoinable:
                    case OrderOutcome.TgtUncatchable:
                    // 4.15.17 Only ships pursued by ships can have a Ship.TgtUncatchable fail cause
                    case OrderOutcome.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
                }

                if (testForAdditionalExploringShips) {
                    var otherShipsCurrentlyExploring = Elements.Cast<ShipItem>().Except(ship).Where(s => s.IsCurrentOrderDirectiveAnyOf(ShipDirective.Explore));
                    if (otherShipsCurrentlyExploring.Any()) {
                        // Do nothing as there are other ships currently exploring so exploreTarget will eventually be assigned a ship
                    }
                    else {
                        // No remaining ships out exploring so no ships will become available in the future to explore
                        var returnHandler = GetCurrentCalledStateReturnHandler();
                        returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                        Return();
                    }
                }
            }
        }
        else {
            // exploreTgt is UCenter
            D.Assert(shipExploreTgt is UniverseCenterItem);
            if (outcome == OrderOutcome.Success) {
                Return();
            }
            else {
                bool isNewShipAssigned;
                switch (outcome) {
                    case OrderOutcome.NeedsRepair:
                    case OrderOutcome.OrdersChange:
                    case OrderOutcome.Ownership:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            // No more ships are available to finish UCenter explore. Since it only takes one ship
                            // to explore UCenter, the other ships, if any, can't currently be exploring, so no reason to wait for them
                            // to complete their exploration. -> the exploration attempt has failed 
                            var returnHandler = GetCurrentCalledStateReturnHandler();
                            returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                            Return();
                        }
                        break;
                    case OrderOutcome.Death:
                        isNewShipAssigned = HandleShipNoLongerAvailableToExplore(ship, shipExploreTgt);
                        if (!isNewShipAssigned) {
                            if (ElementCount > Constants.One) {
                                // > 1 as dead ship has not yet been removed from fleet.
                                // This is not the last ship in the fleet, but the others aren't available. Since it only takes one ship
                                // to explore UCenter, the other ships can't currently be exploring, so no reason to wait for them
                                // to complete their exploration. -> the exploration attempt has failed
                                var returnHandler = GetCurrentCalledStateReturnHandler();
                                returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                                Return();
                            }
                            else {
                                D.AssertEqual(Constants.One, ElementCount);  // dead ship has not yet been removed from fleet
                                // Do nothing as Unit is about to die
                            }
                        }
                        break;
                    case OrderOutcome.TgtUnreachable:
                        D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                            OrderOutcome.TgtUnreachable.GetValueName());
                        break;
                    case OrderOutcome.TgtDeath:
                    case OrderOutcome.TgtUnjoinable:
                    case OrderOutcome.TgtRelationship:
                    case OrderOutcome.TgtUncatchable:
                    case OrderOutcome.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
                }
            }
        }
    }

    [Obsolete("Error Detection")]
    void Exploring_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Exploring_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Exploring_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Exploring_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.AssumeStation)) {
            // Assume station without callback and wait for next attempt to acquire ships to explore
            ShipOrder order = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
            D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                CurrentState.GetValueName());
            subordinateShip.CurrentOrder = order;
        }
    }

    void Exploring_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Exploring_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships don't require a proxy offset update as their movement is not fleetwide
    }

    void Exploring_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void Exploring_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void Exploring_UponRelationsChangedWith(Player player) {
        LogEvent();

        bool toReturn = false;
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // 2.9.18 newly discovered or known owner/player now at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);
            D.AssertEqual(fsmTgtOwner, player); // 2.9.18 UNCLEAR

            toReturn = true;
        }
        else if (fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // 2.9.18 relation changed player could have nothing to do with exploration already completed
            toReturn = true;
        }

        if (toReturn) {
            D.Log(/*ShowDebugLog,*/ "{0} {1} order to {2} discovered no longer valid when Relations with {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, player.DebugName, Owner.GetCurrentRelations(player).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Exploring_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();

        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            // intel coverage on the item increased to the point I now know the owner and they are at war with us
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Exploring_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
            // new known owner is either at war with us or an ally
            Player fsmTgtOwner;
            bool isFsmTgtOwnerKnown = fleetExploreTgt.TryGetOwner(Owner, out fsmTgtOwner);
            D.Assert(isFsmTgtOwnerKnown);

            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), fleetExploreTgt.DebugName, fsmTgtOwner, Owner.GetCurrentRelations(fsmTgtOwner).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    // No need for _UponFsmTgtDeath() as IFleetExplorable targets cannot die

    void Exploring_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();

        IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
        if (fleetExploreTgt.IsExploringAllowedBy(Owner)) {
            IPlanet_Ltd discoveredPlanet = item as IPlanet_Ltd;
            if (discoveredPlanet != null) {
                ISystem_Ltd systemTgt = _fsmTgt as ISystem_Ltd;
                if (systemTgt == null) {
                    ISector_Ltd sectorTgt = _fsmTgt as ISector_Ltd;
                    D.AssertNotNull(sectorTgt);
                    systemTgt = sectorTgt.System;
                }
                D.AssertNotNull(systemTgt);

                if (systemTgt.Planets.Contains(discoveredPlanet)) {
                    IShipExplorable systemTgtPlanet = discoveredPlanet as IShipExplorable;
                    HandleDiscoveredPlanetWhileExploring(systemTgtPlanet);
                }
            }
            else {
                IStarbaseCmd_Ltd discoveredStarbase = item as IStarbaseCmd_Ltd;
                if (discoveredStarbase != null) {    // could be a Settlement
                    ISector_Ltd sectorTgt = _fsmTgt as ISector_Ltd;
                    D.AssertNotNull(sectorTgt);

                    if (sectorTgt.AllStarbases.Contains(discoveredStarbase)) {
                        IShipExplorable sectorTgtStarbase = discoveredStarbase as IShipExplorable;
                        HandleDiscoveredStarbaseWhileExploring(sectorTgtStarbase);
                    }
                }
            }
        }
        else {
            // 7.24.18 Confirming this does occur!
            D.Log("{0} {1} order for {2} is no longer valid due to discovery of {3}!", DebugName, CurrentOrder.Directive.GetValueName(),
                fleetExploreTgt.DebugName, item.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    [Obsolete("Not currently used")]
    void Exploring_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Exploring_ExitState() {
        LogEvent();
        _fsmSystemExploreTgtAssignments.Clear();
        _fsmSectorStarbaseExploreTgtAssignments.Clear();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecutePatrolOrder

    #region ExecutePatrolOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_PatrollingToPatrol() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            // No longer allowed to patrol target
            {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }     },
            {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }             },
            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Patrolling.GetValueName());
    }

    #endregion

    void ExecutePatrolOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        var patrollableTgt = CurrentOrder.Target as IPatrollable;  // Patrollable targets are non-enemy owned sectors, systems, bases and UCenter
        D.AssertNotNull(patrollableTgt, CurrentOrder.Target.DebugName);
        D.Assert(patrollableTgt.IsPatrollingAllowedBy(Owner));

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = patrollableTgt as IFleetNavigableDestination;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecutePatrolOrder_EnterState() {
        LogEvent();

        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, apTgtStandoffDistance: Constants.ZeroF);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at target to be patrolled
            yield return null;
        }

        // arrived at patrol target so patrol
        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Patrolling, CurrentState);
        Call(FleetState.Patrolling);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }
        D.Error("Shouldn't get here as the Call()ed state should never successfully Return() with ReturnCause.None.");
    }

    void ExecutePatrolOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecutePatrolOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecutePatrolOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecutePatrolOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Patrol target {1} and is starting its patrol.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecutePatrolOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecutePatrolOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecutePatrolOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecutePatrolOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Patrolling will include it.
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its patrol target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecutePatrolOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecutePatrolOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecutePatrolOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecutePatrolOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecutePatrolOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(/*ShowDebugLog, */"{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a FsmTgtInfoAccessChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
               DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} received a UponFsmTgtOwnerChgd event while executing a patrol order.", DebugName);
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        if (!patrollableTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrollableTgt.DebugName, patrollableTgt.Owner_Debug, Owner.GetCurrentRelations(patrollableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecutePatrolOrder_UponFsmTgtDeath(IMortalItem deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IPatrollable patrollableTgt = _fsmTgt as IPatrollable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, patrollableTgt.LocalAssemblyStations);
        IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
    }

    [Obsolete("Not currently used")]
    void ExecutePatrolOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecutePatrolOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecutePatrolOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecutePatrolOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecutePatrolOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Patrolling

    // 7.2.16 Call()ed State by ExecutePatrolOrder only

    void Patrolling_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());

        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        D.AssertNotNull(patrolledTgt);    // the _fsmTgt starts out as IPatrollable
        D.Assert(patrolledTgt.IsPatrollingAllowedBy(Owner));
    }

    IEnumerator Patrolling_EnterState() {
        LogEvent();

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;

        var patrolStations = patrolledTgt.PatrolStations.ToList();
        StationaryLocation nextPatrolStation = GameUtility.GetClosest(Position, patrolStations);
        bool isRemoved = patrolStations.Remove(nextPatrolStation);
        D.Assert(isRemoved);
        var shuffledPatrolStations = patrolStations.Shuffle();
        var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
        patrolStationQueue.Enqueue(nextPatrolStation);   // shuffled queue with current patrol station at end

        D.Assert(!_moveHelper.IsPilotEngaged, _moveHelper.DebugName);
        Speed patrolSpeed = patrolledTgt.PatrolSpeed;
        IFleetNavigableDestination apTgt;
        while (true) {
            apTgt = nextPatrolStation;

            _moveHelper.PlotPilotCourse(apTgt, patrolSpeed, Constants.ZeroF);
            // wait here until _UponApCoursePlotSuccess() engages the AutoPilot
            while (!_moveHelper.IsPilotEngaged) {
                yield return null;
            }
            // wait here until _UponApTargetReached() disengages the AutoPilot
            while (_moveHelper.IsPilotEngaged) {
                yield return null;
            }

            nextPatrolStation = patrolStationQueue.Dequeue();
            patrolStationQueue.Enqueue(nextPatrolStation);
        }
    }

    void Patrolling_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void Patrolling_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Log(ShowDebugLog, "{0}.Patrolling_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void Patrolling_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached the next station of Patrol target {1}.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void Patrolling_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Patrolling_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Patrolling_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Patrolling_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.AssumeStation)) {
            // patrolling for ships is all about following fleetwide move orders so get into position and wait for next move order
            ShipOrder order = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
            D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                CurrentState.GetValueName());
            subordinateShip.CurrentOrder = order;
        }
    }

    void Patrolling_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Patrolling_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void Patrolling_UponEnemyDetected() {
        LogEvent();
    }

    void Patrolling_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void Patrolling_UponRelationsChangedWith(Player player) {
        LogEvent();
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IPatrollable patrolledTgt = _fsmTgt as IPatrollable;
        if (!patrolledTgt.IsPatrollingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), patrolledTgt.DebugName, patrolledTgt.Owner_Debug, Owner.GetCurrentRelations(patrolledTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Patrolling_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void Patrolling_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Patrolling_ExitState() {
        LogEvent();
        _moveHelper.DisengagePilot();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteGuardOrder

    #region ExecuteGuardOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_GuardingToGuard() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }     },
            {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }             },
            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Guarding.GetValueName());
    }

    #endregion

    void ExecuteGuardOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        IGuardable guardableTgt = CurrentOrder.Target as IGuardable;
        D.AssertNotNull(guardableTgt); // Guardable targets are non-enemy owned Sectors, Systems, Planets, Bases and UCenter
        D.Assert(guardableTgt.IsGuardingAllowedBy(Owner));

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = guardableTgt as IFleetNavigableDestination;

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteGuardOrder_EnterState() {
        LogEvent();
        //D.Log("{0}.ExecuteGuardOrder_EnterState() begun.", DebugName);

        // move to the target to guard first
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, apTgtStandoffDistance: Constants.ZeroF);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at target to be guarded
            yield return null;
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Guarding, CurrentState);
        Call(FleetState.Guarding);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }
        D.Error("Shouldn't get here as the Call()ed state should never successfully Return() with ReturnCause.None.");
    }

    void ExecuteGuardOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteGuardOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteGuardOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteGuardOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Guard target {1} and is positioning itself to guard.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteGuardOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteGuardOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteGuardOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteGuardOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Guarding will include it.
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its guard target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteGuardOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void ExecuteGuardOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteGuardOrder_UponEnemyDetected() {
        LogEvent();
        // TODO go intercept or wait to be fired on?
    }

    void ExecuteGuardOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteGuardOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        if (!guardableTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardableTgt.DebugName, guardableTgt.Owner_Debug, Owner.GetCurrentRelations(guardableTgt.Owner_Debug).GetValueName());
            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
        }
    }

    void ExecuteGuardOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IGuardable guardableTgt = _fsmTgt as IGuardable;
        StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, guardableTgt.LocalAssemblyStations);
        IssueCmdStaffsAssumeFormationOrder(target: closestLocalAssyStation);
    }

    [Obsolete("Not currently used")]
    void ExecuteGuardOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteGuardOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteGuardOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteGuardOrder_UponDeath() {
        LogEvent();
        // TODO
    }

    void ExecuteGuardOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Guarding

    // 7.2.16 Call()ed State by ExecuteGuardOrder only

    #region Guarding Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToGuarding() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    { IssueCmdStaffsRepairOrder(); }       },
        };
        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    #endregion

    void Guarding_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        D.AssertNotNull(guardedTgt);    // the _fsmTgt starts out as IGuardable
        D.Assert(guardedTgt.IsGuardingAllowedBy(Owner));
    }

    IEnumerator Guarding_EnterState() {
        LogEvent();

        D.Assert(!_moveHelper.IsPilotEngaged, _moveHelper.DebugName);

        // now move to the GuardStation
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        IFleetNavigableDestination apTgt = GameUtility.GetClosest(Position, guardedTgt.GuardStations);
        _moveHelper.PlotPilotCourse(apTgt, Speed.Standard, apTgtStandoffDistance: Constants.ZeroF);

        // wait here until the _UponApCoursePlotSuccess() engages the AutoPilot
        while (!_moveHelper.IsPilotEngaged) {
            yield return null;
        }

        // wait here until the _UponApTargetReached() disengages the AutoPilot
        while (_moveHelper.IsPilotEngaged) {
            yield return null;
        }
        // fleet has arrived at GuardStation

        var returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation); // avoids permanently leaving Guarding state
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // 4.12.17 Desired when a Call()ed state Call()s another state - avoids warning in GetCurrentReturnHandler
        RemoveReturnHandlerFromTopOfStack(returnHandler);
        // Fleet stays in Guarding state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    }

    void Guarding_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void Guarding_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Log(ShowDebugLog, "{0}.Guarding_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void Guarding_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached the station of its Guard target {1} where it will guard.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void Guarding_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Guarding_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Guarding_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Guarding_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.AssumeStation)) {
            // guarding for ships is all about hanging around a GuardStation
            ShipOrder order = new ShipOrder(ShipDirective.AssumeStation, OrderSource.CmdStaff);
            D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                CurrentState.GetValueName());
            subordinateShip.CurrentOrder = order;
        }
    }

    void Guarding_UponAlertStatusChanged() {
        LogEvent();
        // TODO If Red or Yellow, attack, but how find EnemyCmd?
    }

    void Guarding_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Ships are out of formation -> AssumeFormation and re-Guard same target
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        FleetOrder assumeFormationAndReguardTgtOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff) {
            FollowonOrder = new FleetOrder(FleetDirective.Guard, OrderSource.CmdStaff, guardedTgt as IFleetNavigableDestination)
        };
        CurrentOrder = assumeFormationAndReguardTgtOrder;
    }

    void Guarding_UponEnemyDetected() {
        LogEvent();
    }

    void Guarding_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void Guarding_UponRelationsChangedWith(Player player) {
        LogEvent();
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with newly accessed Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IGuardable guardedTgt = _fsmTgt as IGuardable;
        if (!guardedTgt.IsGuardingAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), guardedTgt.DebugName, guardedTgt.Owner_Debug, Owner.GetCurrentRelations(guardedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Guarding_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void Guarding_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Guarding_ExitState() {
        LogEvent();
        _moveHelper.DisengagePilot();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteAttackOrder

    #region ExecuteAttackOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AttackingToAttack() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            // No longer allowed to attack target
            {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
            // 1.5.18 No longer aware of tgtFleet                                                             
            {FsmCallReturnCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }             },
            // NeedsRepair: 2.5.18 Cmd no longer assesses need for unit repair
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Attacking.GetValueName());
    }

    #endregion

    void ExecuteAttackOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        IUnitAttackable unitAttackTgt = CurrentOrder.Target as IUnitAttackable;
        D.AssertNotNull(unitAttackTgt);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = unitAttackTgt;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteAttackOrder_EnterState() {
        LogEvent();
        //D.Log("{0}.ExecuteAttackOrder_EnterState() begun.", DebugName);
        IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
        float unitAttackTgtStandoffDistance = CalcApMoveTgtStandoffDistance(unitAttackTgt);
        // move to the target to attack first
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Full, unitAttackTgtStandoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at target to be attacked
            yield return null;
        }

        if (Data.AlertStatus < AlertStatus.Red) {
            var closestShip = GameUtility.GetClosest(unitAttackTgt.Position, Elements.Cast<IShipNavigableDestination>());
            float closestShipDistanceToTgt = Vector3.Distance(closestShip.Position, unitAttackTgt.Position);
            DiplomaticRelationship tgtOwnerRelations = unitAttackTgt.Owner_Debug.GetCurrentRelations(Owner);
            // 1.7.18 Occurred with AlertStatus.Yellow with closestShip being HQ 732 units from unitAttackTgt, relations = War
            D.Warn("{0} is about to initiate an Attack on {1} with AlertStatus = {2}! {3} is closest at {4:0.} units. TgtRelationship: {5}.",
                DebugName, unitAttackTgt.DebugName, Data.AlertStatus.GetValueName(), closestShip.DebugName, closestShipDistanceToTgt,
                tgtOwnerRelations.GetValueName());
        }

        // TEMP trying to determine why ships can't find enemy ships to attack. I know they are a long way away when this happens
        float distanceToAttackTgtSqrd = Vector3.SqrMagnitude(unitAttackTgt.Position - Position);
        if (distanceToAttackTgtSqrd > 10000) {   // 100
            // 1.7.18 Occurred at 732 units from unitAttackTgt, standoff = 20
            D.Warn("{0} is about to launch an attack against {1} from a distance of {2:0.} units! MoveStandoffDistance = {3:0.#}.",
                DebugName, unitAttackTgt.DebugName, Mathf.Sqrt(distanceToAttackTgtSqrd), unitAttackTgtStandoffDistance);
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Attacking, CurrentState);
        Call(FleetState.Attacking);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // Either unitAttackTgt is dead OR I have no more ships that can attack (IMPROVE: so Flee and Regroup?)
        if (unitAttackTgt.IsDead) {
            D.LogBold("{0} has successfully destroyed target {1}.", DebugName, unitAttackTgt.DebugName);
        }
        else {
            D.Log("{0} has no more ships that can attack target {1}.", DebugName, unitAttackTgt.DebugName);
        }
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteAttackOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteAttackOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        if (_isWaitingToProcessReturn) {
            D.Warn("FYI. {0}: Ignoring leaked callback in 1 frame gap between Attacking and ExecuteAttackOrder.", DebugName);
            return;
        }
        if (outcome != OrderOutcome.Success) {
            D.Log(ShowDebugLog, "{0}.ExecuteAttackOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
                DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        }
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteAttackOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Attack target {1} and is beginning its attack.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    void ExecuteAttackOrder_UponApTgtUncatchable() {
        LogEvent();
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteAttackOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteAttackOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteAttackOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteAttackOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Attacking will include it.
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its attack target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteAttackOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        Player attackedTgtOwner;
        bool isAttackedTgtOwnerKnown = attackedTgt.TryGetOwner(Owner, out attackedTgtOwner);
        D.Assert(isAttackedTgtOwnerKnown);

        if (player == attackedTgtOwner) {
            D.Assert(Owner.IsPreviouslyEnemyOf(player));
            // This attack must have started during ColdWar, so the only scenario it should continue is if now at War
            if (!attackedTgt.IsWarAttackAllowedBy(Owner)) {
                D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                    DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
                IssueCmdStaffsAssumeFormationOrder();
            }
        }
    }

    void ExecuteAttackOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (!attackedTgt.IsAttackAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as just lost access to Owner {3}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug);
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        // With an owner change, the attack should continue only if at War with new owner
        if (!attackedTgt.IsWarAttackAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus < AlertStatus.Red) {
            // WarEnemyCmd has moved out of SRSensor range so Move after the unit and relaunch the attack
            RestartState();
        }
        // else already doing what we should be doing
    }

    void ExecuteAttackOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 If ships in fleetwide Move, affected ships already RestartState to force proxy offset update.
        // If ships attacking, their moves are not fleetwide and don't require a proxy offset update.
        // Previously used Regroup to handle disruption from massive change in SRSensor coverage
    }

    void ExecuteAttackOrder_UponEnemyDetected() {
        LogEvent();
    }

    void ExecuteAttackOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteAttackOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);        // TEMP Will need to change when awareness beyond fleets is included
        if (item == _fsmTgt) {
            // our attack target is the fleet we've lost awareness of
            D.Assert(!OwnerAiMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are attacking without first losing awareness
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteAttackOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        IssueCmdStaffsAssumeFormationOrder();
    }

    [Obsolete("Not currently used")]
    void ExecuteAttackOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // do nothing. Order Outcome callback will handle once implemented
    }

    void ExecuteAttackOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteAttackOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteAttackOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteAttackOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Fleet);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Attacking

    // 1.5.18 Call()ed State only by ExecuteAttackOrder with no additional Fleetwide movement

    #region Attacking Support Members

    private void DetermineShipsToReceiveAttackOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.Attack)) {
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
    }

    #endregion

    void Attacking_UponPreconfigureState() {
        LogEvent();
        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());

        DetermineShipsToReceiveAttackOrder();
    }

    IEnumerator Attacking_EnterState() {
        LogEvent();

        // issue ship attack orders
        IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;

        D.Log("{0} is issuing Attack orders against {1} to all ships.", DebugName, unitAttackTgt.DebugName);
        var shipAttackOrder = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, _executingOrderID, unitAttackTgt as IShipNavigableDestination);
        _fsmShipsExpectedToCallbackWithOrderOutcome.ForAll(ship => ship.CurrentOrder = shipAttackOrder);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            yield return null;
        }
        Return();
    }

    void Attacking_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        switch (outcome) {
            case OrderOutcome.Success:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.NeedsRepair:
            case OrderOutcome.Disqualified:
            case OrderOutcome.Ownership:
            case OrderOutcome.Death:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Outcome: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtRelationship:
            // 1.9.18 Ship relies on FleetCmd to detect UnitAttackTgt relations change that should stop attack.
            // All other Ship TgtRelationship CallReturnCauses result in RestartState to find another shipAttackTgt
            case OrderOutcome.ConstructionCanceled:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtDeath:
            case OrderOutcome.TgtUnreachable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void Attacking_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Attacking_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // TODO
    }

    void Attacking_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            var subordinateShip = subordinateElement as ShipItem;
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Attack)) {
                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);
                ShipOrder order = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
                D.LogBold("{0} is issuing {1} to {2} after completing repairs during State {3}.", DebugName, order.DebugName,
                    subordinateShip.DebugName, CurrentState.GetValueName());    // 4.30.18 Confirming this occurs
                subordinateShip.CurrentOrder = order;
            }
        }
    }

    void Attacking_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Attack)) {
                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);
                ShipOrder order = new ShipOrder(ShipDirective.Attack, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
                D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                    CurrentState.GetValueName());
                subordinateShip.CurrentOrder = order;
            }
        }
    }

    void Attacking_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Attacking_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void Attacking_UponEnemyDetected() {
        LogEvent();
    }

    void Attacking_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void Attacking_UponRelationsChangedWith(Player player) {
        LogEvent();
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        Player attackedTgtOwner;
        bool isAttackedTgtOwnerKnown = attackedTgt.TryGetOwner(Owner, out attackedTgtOwner);
        D.Assert(isAttackedTgtOwnerKnown);

        if (player == attackedTgtOwner) {
            D.Assert(Owner.IsPreviouslyEnemyOf(player));
            // This attack must have started during ColdWar, so the only scenario it should continue is if now at War
            if (!attackedTgt.IsWarAttackAllowedBy(Owner)) {
                D.Log(/*ShowDebugLog,*/ "{0} {1} order for {2} is no longer valid as Relations with Owner {3} changed to {4}.",
                    DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
                D.Log("{0} is Return()ing to ExecuteAttackOrder in Frame {1}.", DebugName, Time.frameCount);
                Return();
            }
        }
    }

    [Obsolete("Not currently used")]
    void Attacking_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void Attacking_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        if (!attackedTgt.IsAttackAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as just lost access to Owner {3}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            D.Log("{0} is Return()ing to ExecuteAttackOrder in Frame {1}.", DebugName, Time.frameCount);
            Return();
        }
    }

    void Attacking_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IUnitAttackable attackedTgt = _fsmTgt as IUnitAttackable;
        // With an owner change, the attack should continue only if at War with new owner
        if (!attackedTgt.IsWarAttackAllowedBy(Owner)) {
            D.Log(ShowDebugLog, "{0} {1} order for {2} is no longer valid as Relations with new Owner {3} is {4}.",
                DebugName, CurrentOrder.Directive.GetValueName(), attackedTgt.DebugName, attackedTgt.Owner_Debug, Owner.GetCurrentRelations(attackedTgt.Owner_Debug).GetValueName());
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            D.Log("{0} is Return()ing to ExecuteAttackOrder in Frame {1}.", DebugName, Time.frameCount);
            Return();
        }
    }

    void Attacking_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        D.Assert(item is IFleetCmd_Ltd);        // TODO Will need to change when awareness beyond fleets is included

        if (item == _fsmTgt) {
            D.Assert(!OwnerAiMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are attacking without first losing awareness
            if (IsApplicationQuiting) {
                // During quit process, detection monitors drop detection of all items so this warning will occur if attacking
                return;
            }
            // 1.9.18 FIXME Occurred when distance > MR sensor range of 750
            D.Warn("{0} has lost awareness of {1} while attacking? UnitDistance = {2:0.}, UnitSensorRange = {3}.",
                DebugName, _fsmTgt.DebugName, Vector3.Distance(Position, _fsmTgt.Position), Data.UnitSensorRange.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtUncatchable;
            D.Log("{0} is Return()ing to ExecuteAttackOrder in Frame {1}.", DebugName, Time.frameCount);
            Return();
        }
    }

    void Attacking_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // 1.26.18 Death of target when attacking it is a success
        //D.Log("{0} is Return()ing to ExecuteAttackOrder in Frame {1}.", DebugName, Time.frameCount);
        Return();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Attacking_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteRegroupOrder - Not currently used

    #region ExecuteRegroupOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_AssumingFormationToRegroup() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.NeedsRepair, () =>    {
                if(AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
                    IssueCmdStaffsRepairOrder();
                }
                else {
                    RestartState();
                }
            }                                                                                               },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.AssumingFormation.GetValueName());
    }

    private IFleetNavigableDestination GetRegroupDestination(Vector3 preferredDirection) {
        preferredDirection.ValidateNormalized();
        // IMPROVE algorithm should emphasize core sector as they have bases and systems 

        float maxTravelDistanceAllowedSqrd = 360000F;    // 600 units

        IFleetNavigableDestination regroupDest = null;
        IUnitBaseCmd myClosestBase;
        if (OwnerAiMgr.TryFindMyClosestItem<IUnitBaseCmd>(Position, out myClosestBase)) {
            Vector3 vectorToDest = myClosestBase.Position - Position;
            float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
            if (destDirectionDesirability > 0F) {
                // direction is aligned with preferredDirection
                if (Vector3.SqrMagnitude(vectorToDest) <= maxTravelDistanceAllowedSqrd) {
                    regroupDest = myClosestBase as IFleetNavigableDestination;
                }
            }
        }

        if (regroupDest == null) {
            ISystem myClosestSystem;
            if (OwnerAiMgr.TryFindMyClosestItem<ISystem>(Position, out myClosestSystem)) {
                Vector3 vectorToDest = myClosestSystem.Position - Position;
                float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                if (destDirectionDesirability > 0F) {
                    // direction is aligned with preferredDirection
                    if (Vector3.SqrMagnitude(myClosestSystem.Position - Position) <= maxTravelDistanceAllowedSqrd) {
                        regroupDest = myClosestSystem as IFleetNavigableDestination;
                    }
                }
            }
        }

        if (regroupDest == null) {
            var systemsWithKnownOwners = OwnerAiMgr.Knowledge.Systems.Except(OwnerAiMgr.Knowledge.OwnerSystems.Cast<ISystem_Ltd>())
            .Where(sys => sys.IsOwnerAccessibleTo(Owner));

            float bestSystemDesirability = 0F;
            ISystem_Ltd bestSystem = null;
            foreach (var sysWithKnownOwner in systemsWithKnownOwners) {
                Player sysOwner;
                bool isOwnerKnown = sysWithKnownOwner.TryGetOwner(Owner, out sysOwner);
                D.Assert(isOwnerKnown);
                if (sysOwner.IsFriendlyWith(Owner)) {
                    Vector3 vectorToDest = sysWithKnownOwner.Position - Position;
                    float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                    if (destDirectionDesirability > 0F) {
                        // direction is aligned with preferredDirection
                        float distanceToDest = vectorToDest.magnitude;
                        float sysDesirability = destDirectionDesirability / distanceToDest; // higher is better (> 0, << 1)
                        if (sysDesirability > bestSystemDesirability) {
                            bestSystemDesirability = sysDesirability;
                            bestSystem = sysWithKnownOwner;
                        }
                    }
                }
            }
            if (bestSystem != null) {
                regroupDest = bestSystem as IFleetNavigableDestination;
            }
        }

        if (regroupDest == null) {
            // if all else fails, pick the most desirable clear point in a neighboring sector 
            var sectorGrid = SectorGrid.Instance;
            IntVector3 closestSectorID;
            if (!sectorGrid.TryGetSectorIDContaining(Position, out closestSectorID)) {
                closestSectorID = sectorGrid.GetClosestSectorIDTo(Position);
            }
            var neighboringSectors = sectorGrid.GetNeighboringSectors(closestSectorID);
            float bestDestDesirability = Mathf.NegativeInfinity;
            Vector3 bestDestInSector = Vector3.zero;    // can be in opposite direction of preferredDirection if that is only available
            foreach (var sector in neighboringSectors) {
                Vector3 destInSector = sector.GetClearRandomInsidePoint();
                Vector3 vectorToDest = destInSector - Position;
                float distanceToDest = vectorToDest.magnitude;
                float destDirectionDesirability = Vector3.Dot(vectorToDest.normalized, preferredDirection);
                float destDesirability = destDirectionDesirability / distanceToDest;    // higher is better, can be negative
                if (destDesirability > bestDestDesirability) {
                    bestDestDesirability = destDesirability;
                    bestDestInSector = destInSector;
                }
            }
            regroupDest = new StationaryLocation(bestDestInSector);
        }
        D.Log(ShowDebugLog, "{0} has picked {1} as its Regroup Destination.", DebugName, regroupDest.DebugName);
        return regroupDest;
    }

    #endregion

    void ExecuteRegroupOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target as IFleetNavigableDestination;
        D.AssertNotNull(_fsmTgt);

        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    [System.Obsolete("Regroup not currently used")]
    IEnumerator ExecuteRegroupOrder_EnterState() {
        LogEvent();

        if (Data.AlertStatus != AlertStatus.Red) {
            D.Warn("{0} has been ordered to {1} while {2} is {3}?",
                DebugName, FleetState.ExecuteRegroupOrder.GetValueName(), typeof(AlertStatus).Name, Data.AlertStatus.GetValueName());
        }

        D.Log(ShowDebugLog, "{0} is departing to regroup at {1}. Distance = {2:0.#}.", DebugName, _fsmTgt.DebugName,
            Vector3.Distance(_fsmTgt.Position, Position));

        // move to the target to regroup at
        float standoffDistance = Constants.ZeroF;    // regrouping occurs only at friendly or neutral destinations
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Full, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at target where we are to regroup
            yield return null;
        }

        // we've arrived so assume formation
        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.AssumingFormation, CurrentState);
        Call(FleetState.AssumingFormation);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
        D.Assert(!IsDead, "{0} is dead but about to initiate Idling!".Inject(DebugName));
        CurrentState = FleetState.Idling;
    }

    void ExecuteRegroupOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteRegroupOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteRegroupOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteRegroupOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Regroup target {1} and is starting to regroup.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteRegroupOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteRegroupOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteRegroupOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRegroupOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRegroupOrder_UponAlertStatusChanged() {
        LogEvent();
        if (Data.AlertStatus == AlertStatus.Normal) {
            // I'm safe, so OK to stop here
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteRegroupOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update
    }

    void ExecuteRegroupOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteRegroupOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing as already intending to repair
    }

    void ExecuteRegroupOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // UNCLEAR
    }

    void ExecuteRegroupOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // UNCLEAR
    }

    [System.Obsolete("Regroup not currently used")]
    void ExecuteRegroupOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the destination where we are trying to regroup
        IFleetNavigableDestination regroupDest = GetRegroupDestination(Data.CurrentHeading);
        IssueCmdStaffsRegroupOrder(regroupDest);
    }

    [Obsolete("Not currently used")]
    void ExecuteRegroupOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRegroupOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteRegroupOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteRegroupOrder_UponDeath() {
        LogEvent();
        // TODO This is the death of our fleet. If only one ship, it will always die. Communicate result to boss?
    }

    void ExecuteRegroupOrder_ExitState() {
        LogEvent();

        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteJoinFleetOrder

    #region ExecuteJoinFleetOrder Support Members

    #endregion

    void ExecuteJoinFleetOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        var fleetToJoin = CurrentOrder.Target as FleetCmdItem;
        D.AssertNotNull(fleetToJoin);
        D.AssertNotEqual(this, fleetToJoin, DebugName);    // 4.6.17 Added as possibly joining same fleet?
        D.Assert(fleetToJoin.IsJoinableBy(ElementCount), fleetToJoin.DebugName);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = fleetToJoin;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        // 11.14.17 Can't lose access to info if target fleet owned by us
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteJoinFleetOrder_EnterState() {
        LogEvent();

        // move to the fleet target to join
        float standoffDistance = Constants.ZeroF;    // can't join an enemy fleet
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at fleet target we are to join
            yield return null;
        }

        var fleetToJoin = _fsmTgt as FleetCmdItem;
        // we've arrived so transfer our ships to the fleet we are joining if its still joinable
        if (!fleetToJoin.IsJoinableBy(ElementCount)) {
            // FleetToJoin could no longer be joinable if other fleet(s) joined before us

            // 5.18.17 BUG: Idling triggered IsAvailable when not just dead, but destroyed???
            D.Assert(!IsDead, "{0} is dead but about to initiate Idling?".Inject(DebugName));
            CurrentState = FleetState.Idling;
            yield return null;
        }

        Join(fleetToJoin);
        D.Assert(IsDead);   // 11.14.17 removing all ships will immediately call FleetState.Dead
    }

    void ExecuteJoinFleetOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteJoinFleetOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteJoinFleetOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteJoinFleetOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its JoinFleet target {1} and is attempting to join.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteJoinFleetOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can our own fleet {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteJoinFleetOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, assessing whether still joinable will handle
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its fleet target to join
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteJoinFleetOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinFleetOrder_UponHQElementChanged() {
        // 11.14.17 Ignore as this can happen multiple times as our ships are removed and transferred
    }

    void ExecuteJoinFleetOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteJoinFleetOrder_UponUnitDamageIncurred() {
        LogEvent();
        // 2.5.18 Removed assessment of need for unit repair in favor of ship's handling repair.
        // TODO Cmd should assess likelihood of accomplishing mission
    }

    void ExecuteJoinFleetOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteJoinFleetOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // owner of the fleet we are trying to join is no longer us
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteJoinFleetOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the fleet we are trying to join. Communicate failure to boss?
        IssueCmdStaffsAssumeFormationOrder();
    }

    [Obsolete("Not currently used")]
    void ExecuteJoinFleetOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteJoinFleetOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteJoinFleetOrder_UponDeath() {
        LogEvent();
        // Do nothing as this will always occur once or more times
    }

    void ExecuteJoinFleetOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region ExecuteJoinHangerOrder

    #region ExecuteJoinHangerOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_JoiningHangerToJoinHanger() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>  { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.TgtDeath, () =>         { IssueCmdStaffsAssumeFormationOrder(); }   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.JoiningHanger.GetValueName());
    }

    #endregion

    void ExecuteJoinHangerOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        var baseToJoin = CurrentOrder.Target as AUnitBaseCmdItem;
        D.AssertNotNull(baseToJoin);
        D.Assert(baseToJoin.Hanger.IsJoinableBy(ElementCount), baseToJoin.Hanger.DebugName);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = baseToJoin;

        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        // 11.14.17 Can't lose access to info if target base owned by us
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteJoinHangerOrder_EnterState() {
        LogEvent();

        // move to the base target whose hanger we are to join
        float standoffDistance = Constants.ZeroF;    // can't join an enemy base's hanger
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at base target whose hanger we are to join
            yield return null;
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.JoiningHanger, CurrentState);
        Call(FleetState.JoiningHanger);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        if (!IsDead) {
            IAssemblySupported refittingBaseWithAssyStations = _fsmTgt as IAssemblySupported;
            D.AssertNotNull(refittingBaseWithAssyStations);
            IFleetNavigableDestination postRefitAssyStation = GameUtility.GetClosest(Position, refittingBaseWithAssyStations.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(postRefitAssyStation);
        }
    }

    void ExecuteJoinHangerOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteJoinHangerOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteJoinHangerOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteJoinHangerOrder_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached its JoinHanger target {1} and is attempting to join.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteJoinHangerOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteJoinHangerOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinHangerOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteJoinHangerOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteJoinHangerOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, JoiningHanger will handle
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its base target whose hanger we are to join
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteJoinHangerOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteJoinHangerOrder_UponHQElementChanged() {
        // 11.14.17 Ignore as this can happen multiple times as our ships are removed and transferred
    }

    void ExecuteJoinHangerOrder_UponEnemyDetected() {
        LogEvent();
        // Continue with existing order
    }

    void ExecuteJoinHangerOrder_UponUnitDamageIncurred() {
        LogEvent();
        if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
            IssueCmdStaffsRepairOrder();
        }
    }

    void ExecuteJoinHangerOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteJoinHangerOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        // owner of the base hanger we are trying to join is no longer us
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteJoinHangerOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        if (_fsmTgt != deadFsmTgt) {
            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
        }
        // This is the death of the Base whose hanger we are trying to join. Communicate failure to boss?
        IssueCmdStaffsAssumeFormationOrder();
    }

    [Obsolete("Not currently used")]
    void ExecuteJoinHangerOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteJoinHangerOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteJoinHangerOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteJoinHangerOrder_UponDeath() {
        LogEvent();
        // Do nothing as this will always occur once or more times
        D.LogBold("{0} has died, probably because it transferred all its ships to {1}'s hanger.", DebugName, _fsmTgt.DebugName);
    }

    void ExecuteJoinHangerOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region JoiningHanger

    // 12.31.17 Currently a Call()ed state only from ExecuteJoinFleetOrder with no additional fleet movement

    #region JoiningHanger Support Members

    private void DetermineShipsToReceiveEnterHangerOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.EnterHanger)) {
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
    }

    #endregion

    void JoiningHanger_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());

        DetermineShipsToReceiveEnterHangerOrder();
        ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
    }

    IEnumerator JoiningHanger_EnterState() {
        LogEvent();

        // Fleet ships should be in high orbit around Base
        var baseToJoin = _fsmTgt as AUnitBaseCmdItem;
        // we've arrived so instruct our ships to enter the base hanger
        ShipOrder enterHangerOrder = new ShipOrder(ShipDirective.EnterHanger, CurrentOrder.Source, _executingOrderID, baseToJoin);
        _fsmShipsExpectedToCallbackWithOrderOutcome.ForAll(ship => ship.CurrentOrder = enterHangerOrder);

        while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until ships have been transferred to the hanger
            yield return null;
        }

        D.Assert(!IsDead);   // won't get here if all ships were transferred as this fleetCmd will already be dead
        D.LogBold("{0} has transferred all ships capable of entering {1}'s hanger. {2} remain with fleet.", DebugName, _fsmTgt.DebugName, ElementCount);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        Return();
    }

    void JoiningHanger_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.Log(ShowDebugLog, "{0}.JoiningHanger_UponOrderOutcomeCallback() called from {1}. Outcome = {2}. Frame: {3}.",
            DebugName, ship.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Present: {2}.",
                        DebugName, ship.DebugName, _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.Death:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Ownership:
            case OrderOutcome.TgtUnjoinable:
                isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. FailCause: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtDeath:
                // Tgt Base change will be detected and handled by this.EnteringHanger_UponFsmTgtXXX()
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.NeedsRepair:
            // Should not occur as Ship knows entering hanger will repair damage if needed
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.ConstructionCanceled:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void JoiningHanger_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }


    void JoiningHanger_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void JoiningHanger_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void JoiningHanger_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // ships are still in the process of getting to hanger to join
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.EnterHanger)) {

                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);

                ShipOrder enterHangerOrder = new ShipOrder(ShipDirective.EnterHanger, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
                D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, enterHangerOrder.DebugName, subordinateShip.DebugName,
                    CurrentState.GetValueName());
                subordinateShip.CurrentOrder = enterHangerOrder;
            }
        }
    }

    void JoiningHanger_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void JoiningHanger_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void JoiningHanger_UponEnemyDetected() {
        LogEvent();
    }

    void JoiningHanger_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will initiate repair if needed
    }

    void JoiningHanger_UponRelationsChangedWith(Player player) {
        LogEvent();
        // do nothing as no effect
    }

    void JoiningHanger_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
        Return();
    }

    void JoiningHanger_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void JoiningHanger_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void JoiningHanger_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteRepairOrder

    #region ExecuteRepairOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RepairingToRepair() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>  {
                if (AssessNeedForRepair()) {
                    // find a new repairDest
                    IssueCmdStaffsRepairOrder();
                }
                else {
                    CurrentState = FleetState.Idling;
                }
            }                                                                           },
            {FsmCallReturnCause.TgtDeath, () =>         {
                if (AssessNeedForRepair()) {
                    // find a new repairDest
                    IssueCmdStaffsRepairOrder();
                }
                else {
                    CurrentState = FleetState.Idling;
                }
            }                                                                           },
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
            // No NeedsRepair as Repairing won't signal need for repair while repairing
        };
        return new FsmReturnHandler(taskLookup, FleetState.Repairing.GetValueName());
    }

    #endregion

    void ExecuteRepairOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();
        // 1.2.18 Can't Assert UnitHealth < 100% as RestartState can occur and ship(s) may have independently completed repairing
        D.AssertNotNull(CurrentOrder.Target);    // Target can be a Planet, Base or this FleetCmd (will repair in place)

        IShipRepairCapable repairDest = CurrentOrder.Target as IShipRepairCapable;
        if (repairDest != null) {
            // RepairDest is either an IShipRepairCapable Base or Planet
            bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, repairDest);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, repairDest);
            D.Assert(isSubscribed);
            isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, repairDest);
            D.Assert(isSubscribed);

            ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
        }
        else {
            // repair in place
            D.AssertEqual(this, CurrentOrder.Target);
            // 1.13.18 No need to ChangeAvailabilityTo() as Repairing will immediately handle it
        }
        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = CurrentOrder.Target as IFleetNavigableDestination;
        D.AssertNotNull(_fsmTgt);
    }

    IEnumerator ExecuteRepairOrder_EnterState() {
        LogEvent();

        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            D.Assert(_fsmTgt is IShipRepairCapable);

            // move to the repair target where we want to repair
            float standoffDistance = Constants.ZeroF;    // UNCLEAR
            _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

            while (!_moveHelper.IsPilotEngaged) {
                // wait until course is plotted and pilot engaged
                yield return null;
            }

            while (_moveHelper.IsPilotEngaged) {
                // wait for pilot to arrive at base target whose hanger we are to join
                yield return null;
            }
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Repairing, CurrentState);
        Call(FleetState.Repairing);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        // Can't assert OneHundredPercent as more hits can occur after repairing completed

        IFleetNavigableDestination postRepairAssyLocation = null;
        if (!toRepairInPlace) {
            // a Planet or Base
            IAssemblySupported tgtWithAssyStations = _fsmTgt as IAssemblySupported;
            D.AssertNotNull(tgtWithAssyStations);
            postRepairAssyLocation = GameUtility.GetClosest(Position, tgtWithAssyStations.LocalAssemblyStations);
        }
        IssueCmdStaffsAssumeFormationOrder(postRepairAssyLocation);
    }

    void ExecuteRepairOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteRepairOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteRepairOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteRepairOrder_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached its target {1} where it is initiating repair.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteRepairOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteRepairOrder_UponNewOrderReceived() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRepairOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRepairOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();

        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
            // rest of fleet when next leg of move order is issued. If no more legs, Repairing will handle
            if (_moveHelper.IsPilotEngaged) {
                // in this state, if pilot is engaged, the fleet is still trying to get to its repair target
                if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                    // one or more ships are still expected to arrive at the next waypoint or final target
                    if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                        var currentDestination = _moveHelper.CurrentDestination;
                        bool isFleetwideMove = false;
                        Speed speed = _moveHelper.ApSpeedSetting;
                        float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                        _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                        ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                            currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                        subordinateShip.CurrentOrder = moveOrder;
                    }
                }
            }
        }
        // else repair in place so Repairing will handle if not yet Call()ed
    }

    void ExecuteRepairOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteRepairOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteRepairOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void ExecuteRepairOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            var repairDest = _fsmTgt as IShipRepairCapable;
            if (!repairDest.IsRepairingAllowedBy(Owner)) {
                D.Log(/*ShowDebugLog,*/ "{0} {1} order at {2} is no longer valid as Relations with Owner have changed.",
                    DebugName, CurrentOrder.Directive.GetValueName(), repairDest.DebugName);
                if (AssessNeedForRepair()) {
                    // find a new repairDest
                    IssueCmdStaffsRepairOrder();
                }
                else {
                    CurrentState = FleetState.Idling;
                }
            }
        }
    }

    void ExecuteRepairOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        var repairDest = _fsmTgt as IShipRepairCapable;
        if (!repairDest.IsRepairingAllowedBy(Owner)) {
            if (AssessNeedForRepair()) {
                // find a new repairDest
                IssueCmdStaffsRepairOrder();
            }
            else {
                CurrentState = FleetState.Idling;
            }
        }
    }

    void ExecuteRepairOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        var repairDest = _fsmTgt as IShipRepairCapable;
        if (!repairDest.IsRepairingAllowedBy(Owner)) {
            if (AssessNeedForRepair()) {
                // find a new repairDest
                IssueCmdStaffsRepairOrder();
            }
            else {
                CurrentState = FleetState.Idling;
            }
        }
    }

    void ExecuteRepairOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);
        if (AssessNeedForRepair()) {
            // find a new repairDest
            IssueCmdStaffsRepairOrder();
        }
        else {
            CurrentState = FleetState.Idling;
        }
    }

    [Obsolete("Not currently used")]
    void ExecuteRepairOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRepairOrder_UponLosingOwnership() {
        LogEvent();
        // TODO
    }

    [Obsolete("Not currently used")]
    void ExecuteRepairOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteRepairOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRepairOrder_ExitState() {
        LogEvent();

        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
            D.Assert(isUnsubscribed);
            isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
            D.Assert(isUnsubscribed);
        }

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Repairing

    // 4.1.17 Currently a Call()ed state only from ExecuteRepairOrder with no additional fleet movement

    #region Repairing Support Members

    private void DetermineShipsToReceiveRepairOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.Repair) && !ship.IsRepairing) {
                // 1.10.18 If ship already repairing, it can complete repairing before order is issued and become unauthorized
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
    }

    #endregion

    void Repairing_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            IShipRepairCapable repairDest = _fsmTgt as IShipRepairCapable;
            D.AssertNotNull(repairDest);
            D.Assert(repairDest.IsRepairingAllowedBy(Owner));
        }

        DetermineShipsToReceiveRepairOrder();
        AssessAvailabilityStatus_Repair();
    }

    IEnumerator Repairing_EnterState() {
        LogEvent();

        // IMPROVE pick individual destination for each ship?
        // 12.13.17 _fsmTgt can be a Base, Planet or this FleetCmd. If this FleetCmd, ships will repair in place on their FormationStation
        ShipOrder shipRepairOrder = new ShipOrder(ShipDirective.Repair, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
        _fsmShipsExpectedToCallbackWithOrderOutcome.ForAll(ship => ship.CurrentOrder = shipRepairOrder);

        // 12.11.17 HQElement now handles CmdModule repair

        while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until ships are all repaired
            yield return null;
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        D.Log(ShowDebugLog, "{0}'s has completed repair of all Elements. UnitHealth = {1:P01}.", DebugName, Data.UnitHealth);
        Return();
    }

    void Repairing_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Present: {2}.",
                        DebugName, ship.DebugName, _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.Death:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Ownership:
                isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. FailCause: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtDeath:
            case OrderOutcome.TgtRelationship:
                // 4.15.17 Since Callback, the order to repair came from this Cmd. The repairDest can't be repair in place since 
                // this is the death or relationshipChg of the repairDest affecting all ships, so pick a new repairDest for all.
                IssueCmdStaffsRepairOrder();
                break;
            case OrderOutcome.TgtUncatchable:
                D.Error("{0}.Repairing_UponOrderOutcomeCallback received uncatchable failure from {1}. RprTgtToCmd distance = {2:0.}.",
                    DebugName, ship.DebugName, Vector3.Distance(Position, target.Position));
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.NeedsRepair:
            // 4.15.17 Ship will RestartState rather than report it if it encounters this from another Call()ed state
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void Repairing_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Repairing_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
        // Do nothing. If subordinateShip just became available it will initiate repair itself if needed
    }

    void Repairing_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Repairing_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        D.Assert(!subordinateShip.IsRepairing); // UNCLEAR How? If already repairing, IsRepairing test must be added below
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // ships are still repairing
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Repair)) {
                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);
                ShipOrder order = new ShipOrder(ShipDirective.Repair, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
                D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                    CurrentState.GetValueName());
                subordinateShip.CurrentOrder = order;
            }
        }
    }

    void Repairing_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Repairing_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 AssumeFormation order at end of ExecuteRepairOrder.EnterState will fix formation
    }

    void Repairing_UponEnemyDetected() {
        LogEvent();
    }

    void Repairing_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void Repairing_UponRelationsChangedWith(Player player) {
        LogEvent();

        bool toRepairInPlace = _fsmTgt == this as IFleetNavigableDestination;
        if (!toRepairInPlace) {
            IShipRepairCapable currentRepairDest = _fsmTgt as IShipRepairCapable;
            if (!currentRepairDest.IsRepairingAllowedBy(Owner)) {
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
                Return();
            }
        }
    }

    void Repairing_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IShipRepairCapable currentRepairDest = _fsmTgt as IShipRepairCapable;
        if (!currentRepairDest.IsRepairingAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Repairing_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
        IShipRepairCapable currentRepairDest = _fsmTgt as IShipRepairCapable;
        if (!currentRepairDest.IsRepairingAllowedBy(Owner)) {
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void Repairing_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        // if repair in place, there is no subscription
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);
        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void Repairing_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
        // do nothing. Order Outcome callback will handle
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Repairing_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteRefitOrder

    #region ExecuteRefitOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_RefittingToRefit() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>  { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.TgtDeath, () =>         { IssueCmdStaffsAssumeFormationOrder(); }   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Refitting.GetValueName());
    }

    #endregion

    void ExecuteRefitOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        IUnitBaseCmd refitDest = CurrentOrder.Target as IUnitBaseCmd;
        D.AssertNotNull(refitDest);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = refitDest as IFleetNavigableDestination;
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        // no need for subscription to FsmTgtInfoAccessChg for our own base
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteRefitOrder_EnterState() {
        LogEvent();

        // move to orbit the base where we are to refit
        float standoffDistance = Constants.ZeroF;    // can't refit at an enemy base
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive and orbit base target where we are to refit
            yield return null;
        }

        // Fleet ships should be in high orbit around Base
        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Refitting, CurrentState);
        Call(FleetState.Refitting);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        D.Assert(!IsDead);  // Previously, sending all ships to hanger to refit resulted in a dead Cmd
        IAssemblySupported refittingBaseWithAssyStations = _fsmTgt as IAssemblySupported;
        D.AssertNotNull(refittingBaseWithAssyStations);
        IFleetNavigableDestination postRefitAssyStation = GameUtility.GetClosest(Position, refittingBaseWithAssyStations.LocalAssemblyStations);
        IssueCmdStaffsAssumeFormationOrder(postRefitAssyStation);
    }

    void ExecuteRefitOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteRefitOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteRefitOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteRefitOrder_UponApTargetReached() {
        LogEvent();
        D.Log(ShowDebugLog, "{0} has reached its Refit target {1} and is entering high orbit to refit. DistanceToBase = {2:0.#}.",
            DebugName, _fsmTgt.DebugName, Vector3.Distance(Position, _fsmTgt.Position));
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteRefitOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteRefitOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state will change
    }

    void ExecuteRefitOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRefitOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteRefitOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Refitting will handle
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its refit target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteRefitOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteRefitOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteRefitOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteRefitOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void ExecuteRefitOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void ExecuteRefitOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();

        // RefitBase is no longer ours
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteRefitOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);

        // RefitBase has died
        IssueCmdStaffsAssumeFormationOrder();
    }

    [Obsolete("Not currently used")]
    void ExecuteRefitOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteRefitOrder_UponLosingOwnership() {
        LogEvent();
        // nothing to do
    }

    [Obsolete("Not currently used")]
    void ExecuteRefitOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteRefitOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteRefitOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Refitting

    // 12.8.17 Currently a Call()ed state only by ExecuteRefitOrder with no additional fleet movement

    #region Refitting Support Members

    /// <summary>
    /// Determines the ships to receive a refit order.
    /// </summary>
    private void DetermineShipsToReceiveRefitOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.Refit)) {
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
        D.Assert(_fsmShipsExpectedToCallbackWithOrderOutcome.Any());  // Should not have been called if no elements can be refit
    }

    /// <summary>
    /// Returns <c>true</c> if the CmdModule should be included in the refit.
    /// If true, the returned value indicates which ship should be ordered to include the CmdModule in its refit.
    /// </summary>
    /// <param name="candidates">The candidates to pick from.</param>
    /// <param name="ship">The ship selected to include the CmdModule refit.</param>
    /// <returns></returns>
    private bool TryPickShipToRefitCmdModule(IEnumerable<ShipItem> candidates, out ShipItem ship) {
        D.Assert(!candidates.IsNullOrEmpty());
        ship = null;
        bool toRefitCmdModule = OwnerAiMgr.Designs.AreUpgradeDesignsAvailable(Data.CmdModuleDesign);
        if (toRefitCmdModule) {
            ship = candidates.SingleOrDefault(s => s.IsHQ);
            if (ship == null) {
                ship = candidates.First();
            }
        }
        return toRefitCmdModule;
    }

    private void HaveUserPickRefitDesign(ShipItem ship) {
        string dialogText = "Pick the Design you wish to use to refit {0}. \nCancel to not refit.".Inject(ship.Name);
        var cancelDelegate = new EventDelegate(() => {
            var existingDesignIndicatingRefitCanceled = ship.Data.Design;
            HandleElementRefitDesignChosen(existingDesignIndicatingRefitCanceled);  // canceling dialog means don't refit ship
            DialogWindow.Instance.Hide();
        });

        var existingDesign = ship.Data.Design;
        DialogWindow.Instance.HaveUserPickElementRefitDesign(FormID.SelectShipDesignDialog, dialogText, cancelDelegate, existingDesign,
            (chosenRefitDesign) => HandleElementRefitDesignChosen(chosenRefitDesign), useUserActionButton: false);
    }

    #endregion

    void Refitting_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        D.AssertNull(_chosenElementRefitDesign);

        DetermineShipsToReceiveRefitOrder();
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator Refitting_EnterState() {
        LogEvent();

        int plannedRefitQty = _fsmShipsExpectedToCallbackWithOrderOutcome.Count;
        D.Log(ShowDebugLog, "{0} is issuing a RefitOrder to {1} Ships orbiting {2}: {3}.", DebugName, plannedRefitQty, _fsmTgt.DebugName,
            _fsmShipsExpectedToCallbackWithOrderOutcome.Select(f => f.DebugName).Concatenate());
        // Fleet ships should be in high orbit around Base

        // First choose the refit designs and record them allowing the User to cancel 
        // OPTIMIZE if User isn't going to manually choose and potentially cancel, no need for copy or lookup
        IDictionary<ShipItem, ShipDesign> shipDesignLookup = new Dictionary<ShipItem, ShipDesign>(plannedRefitQty);
        var shipsToRefitCopy = _fsmShipsExpectedToCallbackWithOrderOutcome.ToArray();
        //D.Log("{0} has planned refit of {1} ships: {2}.", DebugName, plannedRefitQty, shipsToRefitCopy.Select(s => s.DebugName).Concatenate());
        foreach (var ship in shipsToRefitCopy) {
            D.AssertNull(_chosenElementRefitDesign);
            var existingDesign = ship.Data.Design;

            if (Owner.IsUser && !_playerPrefsMgr.IsAiHandlesUserElementRefitDesignsEnabled) {
                HaveUserPickRefitDesign(ship);

                while (_chosenElementRefitDesign == null) {
                    // Wait until User makes refit design choice
                    yield return null;
                }

                if (_chosenElementRefitDesign == existingDesign) {
                    // User has canceled refit of this ship
                    bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                    D.Assert(isRemoved);
                    D.Log("{0}: {1} removed from refit plan as User canceled dialog.", DebugName, ship.DebugName);
                    _chosenElementRefitDesign = null;
                    continue;
                }
            }
            else {
                ShipDesign chosenDesign = OwnerAiMgr.ChooseRefitDesign(existingDesign);
                HandleElementRefitDesignChosen(chosenDesign);
            }
            D.AssertNotNull(_chosenElementRefitDesign);

            shipDesignLookup.Add(ship, _chosenElementRefitDesign as ShipDesign);
            _chosenElementRefitDesign = null;
        }

        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            int actualRefitQty = _fsmShipsExpectedToCallbackWithOrderOutcome.Count;
            ShipItem shipToRefitCmdModule;
            bool toRefitCmdModule = TryPickShipToRefitCmdModule(_fsmShipsExpectedToCallbackWithOrderOutcome, out shipToRefitCmdModule);

            // order the ships that can refit to do so in orbit
            foreach (var ship in _fsmShipsExpectedToCallbackWithOrderOutcome) {
                var refitDesign = shipDesignLookup[ship];

                bool toIncludeCmdModuleInThisShipsRefit = ship == shipToRefitCmdModule;
                if (toIncludeCmdModuleInThisShipsRefit) {
                    D.Assert(toRefitCmdModule);
                }

                var refitOrder = new ShipRefitOrder(CurrentOrder.Source, _executingOrderID, refitDesign, _fsmTgt as IShipNavigableDestination,
                    toIncludeCmdModuleInThisShipsRefit);
                ship.CurrentOrder = refitOrder;
            }

            if (toRefitCmdModule) {
                StartEffectSequence(EffectSequenceID.Refitting);
            }

            while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
                // Wait here until all refitable ships have completed their orbital refit
                yield return null;
            }

            string cmdModuleMsg = string.Empty;
            if (toRefitCmdModule) {
                StopEffectSequence(EffectSequenceID.Refitting);
                cmdModuleMsg = ", including the CmdModule";
            }
            D.LogBold("{0} has completed refit of {1} ships orbiting {2}{3}.", DebugName, actualRefitQty, _fsmTgt.DebugName, cmdModuleMsg);
        }
        else {
            D.Warn("FYI. {0} had all ship refits canceled resulting in cancellation of the fleet refit.", DebugName);
        }

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        Return();
    }

    void Refitting_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();

        D.Log(ShowDebugLog, "{0}.Refitting_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, ship.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Present: {2}.",
                        DebugName, ship.DebugName, _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.Death:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Ownership:
            case OrderOutcome.TgtUnjoinable:
                isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. FailCause: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtDeath:
                // Tgt RefitBase change will be detected and handled by this.Refitting_UponFsmTgtXXX()
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.NeedsRepair:
            // Should not occur as Ship knows finishing refit repairs all damage
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.ConstructionCanceled:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void Refitting_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void Refitting_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Refitting_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Refitting_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // 6.8.18 Eliminated issuing refit order to subordinateShip. New User select design system 
        // makes this difficult and of little value add
    }

    void Refitting_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Refitting_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void Refitting_UponEnemyDetected() {
        LogEvent();
    }

    void Refitting_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing. Completion will repair all damage
    }

    void Refitting_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void Refitting_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
        Return();
    }

    void Refitting_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void Refitting_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Refitting_ExitState() {
        LogEvent();
        StopEffectSequence(EffectSequenceID.Refitting);
        ResetAndValidateCommonCallableExitStateValues();
        _chosenElementRefitDesign = null;
    }

    #endregion

    #region ExecuteDisbandOrder

    #region ExecuteDisbandOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_DisbandingToDisband() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>  { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.TgtDeath, () =>         { IssueCmdStaffsAssumeFormationOrder(); }   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.Disbanding.GetValueName());
    }

    #endregion

    void ExecuteDisbandOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        IUnitBaseCmd disbandDest = CurrentOrder.Target as IUnitBaseCmd;
        D.AssertNotNull(disbandDest);

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = disbandDest as IFleetNavigableDestination;
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isSubscribed);
        // no need for subscription to FsmTgtInfoAccessChg for our own base
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteDisbandOrder_EnterState() {
        LogEvent();

        // move to the base target whose hanger we are to disband in
        float standoffDistance = Constants.ZeroF;    // can't disband in an enemy base's hanger
        _moveHelper.PlotPilotCourse(_fsmTgt, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at base target whose hanger we are to disband in
            yield return null;
        }

        // Fleet ships should be in high orbit around Base
        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.Disbanding, CurrentState);
        Call(FleetState.Disbanding);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        if (!IsDead) {
            IAssemblySupported disbandingBaseWithAssyStations = _fsmTgt as IAssemblySupported;
            D.AssertNotNull(disbandingBaseWithAssyStations);
            IFleetNavigableDestination postDisbandAssyStation = GameUtility.GetClosest(Position, disbandingBaseWithAssyStations.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(postDisbandAssyStation);
        }
    }

    void ExecuteDisbandOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteDisbandOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteDisbandOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteDisbandOrder_UponApTargetReached() {
        LogEvent();
        D.LogBold(ShowDebugLog, "{0} has reached its Disband target {1} and is attempting to join the hanger to disband.", DebugName, _fsmTgt.DebugName);
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteDisbandOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteDisbandOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state will change
    }

    void ExecuteDisbandOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteDisbandOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteDisbandOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs, Disbanding will handle
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its disband target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteDisbandOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteDisbandOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteDisbandOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteDisbandOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void ExecuteDisbandOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // nothing to do
    }

    void ExecuteDisbandOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();

        // DisbandBase is no longer ours
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteDisbandOrder_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);

        // DisbandBase has died
        IssueCmdStaffsAssumeFormationOrder();
    }

    [Obsolete("Not currently used")]
    void ExecuteDisbandOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteDisbandOrder_UponLosingOwnership() {
        LogEvent();
        // nothing to do
    }

    [Obsolete("Not currently used")]
    void ExecuteDisbandOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteDisbandOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteDisbandOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtDeath, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        // 1.11.18 This won't stop the ships from disbanding as those disbanding have already left this Cmd
        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region Disbanding

    // 12.13.17 Currently a Call()ed state by ExecuteDisbandOrder only with no additional fleet movement

    #region Disbanding Support Members

    private void DetermineShipsToReceiveDisbandOrder() {
        foreach (var e in Elements) {
            var ship = e as ShipItem;
            if (ship.IsAuthorizedForNewOrder(ShipDirective.Disband)) {
                _fsmShipsExpectedToCallbackWithOrderOutcome.Add(ship);
            }
        }
        D.Assert(_fsmShipsExpectedToCallbackWithOrderOutcome.Any());     // Should not have been called if no elements can be disbanded
    }

    #endregion

    void Disbanding_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());

        DetermineShipsToReceiveDisbandOrder();
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator Disbanding_EnterState() {
        LogEvent();

        // Fleet ships should be in high orbit around Base
        ShipOrder disbandOrder = new ShipOrder(ShipDirective.Disband, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
        _fsmShipsExpectedToCallbackWithOrderOutcome.ForAll(ship => ship.CurrentOrder = disbandOrder);

        // 12.13.17 HACK placeholder for disbanding cmd module as currently not supported
        StartEffectSequence(EffectSequenceID.Disbanding);
        StopEffectSequence(EffectSequenceID.Disbanding);

        while (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // Wait here until all disbandable ships have been transferred to the hanger or have otherwise indicated they can't fit
            yield return null;
        }
        D.Log("{0} has transferred all ships capable of disbanding that can fit to {1}'s hanger.", DebugName, _fsmTgt.DebugName);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        Return();
    }

    void Disbanding_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();

        D.Log(ShowDebugLog, "{0}.Disbanding_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, ship.DebugName, outcome.GetValueName(), Time.frameCount);

        switch (outcome) {
            case OrderOutcome.Success:
                bool isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. Present: {2}.",
                        DebugName, ship.DebugName, _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.Death:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Ownership:
            case OrderOutcome.TgtUnjoinable:
                isRemoved = _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
                if (!isRemoved) {
                    D.Error("{0}: {1} is not present to be removed. FailCause: {2}. Present: {3}.",
                        DebugName, ship.DebugName, outcome.GetValueName(), _fsmShipsExpectedToCallbackWithOrderOutcome.Concatenate());
                }
                break;
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtDeath:
                // Tgt DisbandBase change will be detected and handled by this.Disbanding_UponFsmTgtXXX()
                break;
            case OrderOutcome.TgtUnreachable:
                D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(OrderOutcome).Name,
                    OrderOutcome.TgtUnreachable.GetValueName());
                break;
            case OrderOutcome.NeedsRepair:
            // Should not occur as Ship knows finishing refit repairs all damage
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.ConstructionCanceled:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void Disbanding_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }


    void Disbanding_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Disbanding_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void Disbanding_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        if (_fsmShipsExpectedToCallbackWithOrderOutcome.Any()) {
            // ships are still in the process of getting to hanger for disband
            if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Disband)) {
                bool isAdded = _fsmShipsExpectedToCallbackWithOrderOutcome.Add(subordinateShip);
                D.Assert(isAdded);

                ShipOrder order = new ShipOrder(ShipDirective.Disband, CurrentOrder.Source, _executingOrderID, _fsmTgt as IShipNavigableDestination);
                D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
                    CurrentState.GetValueName());
                subordinateShip.CurrentOrder = order;
            }
        }
    }

    void Disbanding_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void Disbanding_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void Disbanding_UponEnemyDetected() {
        LogEvent();
    }

    void Disbanding_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void Disbanding_UponRelationsChangedWith(Player player) {
        LogEvent();
        // Do nothing as no effect
    }

    void Disbanding_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
        Return();
    }

    void Disbanding_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, deadFsmTgt as IFleetNavigableDestination);

        var returnHandler = GetCurrentCalledStateReturnHandler();
        returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
        Return();
    }

    [Obsolete("Not currently used")]
    void Disbanding_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void Disbanding_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteFoundSettlementOrder

    #region ExecuteFoundSettlementOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_FoundingToFoundSettlement() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>      { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.Qualifications, () =>       { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.NeedsRepair, () =>          { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.ConstructionCanceled, () => { IssueCmdStaffsAssumeFormationOrder(); }   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.FoundingSettlement.GetValueName());
    }

    #endregion

    void ExecuteFoundSettlementOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        SystemItem destinationSystem = CurrentOrder.Target as SystemItem;
        D.AssertNotNull(destinationSystem);
        D.Assert(destinationSystem.IsFoundingSettlementAllowedBy(Owner));

        var colonyShips = Elements.Where(e => (e as ShipItem).HullCategory == ShipHullCategory.Colonizer);
        D.Assert(colonyShips.Any());

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = destinationSystem;

        // no need for subscription to FsmTgtDeath for a system
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        var systemSector = SectorGrid.Instance.GetSector(destinationSystem.SectorID);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, systemSector);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteFoundSettlementOrder_EnterState() {
        LogEvent();

        SystemItem destinationSystem = _fsmTgt as SystemItem;

        // move to the system we are to colonize
        float standoffDistance = Constants.ZeroF;
        _moveHelper.PlotPilotCourse(destinationSystem, Speed.Standard, standoffDistance);

        D.Assert(destinationSystem.IsFoundingSettlementAllowedBy(Owner), DebugName);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at system
            yield return null;
        }
        // Should be in or just outside of destinationSystem

        D.Assert(destinationSystem.IsFoundingSettlementAllowedBy(Owner), DebugName);

        // now move just outside the SettlementStation
        StationaryLocation settlementStation = destinationSystem.GetClosestSettlementStationTo(Position);
        standoffDistance = Radius; // keep ships away from where Settlement will be founded
        _moveHelper.PlotPilotCourse(settlementStation, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive outside of settlement station
            yield return null;
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.FoundingSettlement, CurrentState);
        Call(FleetState.FoundingSettlement);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        var newSettlement = destinationSystem.Settlement;
        D.AssertNotNull(newSettlement);

        if (!IsDead) {
            IFleetNavigableDestination settlementAssyStation = GameUtility.GetClosest(Position, newSettlement.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(settlementAssyStation);
        }
    }

    void ExecuteFoundSettlementOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteFoundSettlementOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteFoundSettlementOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteFoundSettlementOrder_UponApTargetReached() {
        LogEvent();
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteFoundSettlementOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteFoundSettlementOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state will change
    }

    void ExecuteFoundSettlementOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteFoundSettlementOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteFoundSettlementOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs??
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its disband target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteFoundSettlementOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteFoundSettlementOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteFoundSettlementOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteFoundSettlementOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void ExecuteFoundSettlementOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // nothing to do
    }

    void ExecuteFoundSettlementOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        if (!(fsmTgt as ISystem_Ltd).IsFoundingSettlementAllowedBy(Owner)) {
            D.Log("{0} just became aware that {1} is no longer settleable. Canceling.", DebugName, fsmTgt.DebugName);
            // Whether settled by us via another fleet or someone else, System is no longer settleable
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteFoundSettlementOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        // Can't Assert fsmTgt.Owner != NoPlayer based on "we wouldn't be trying to settle it if someone else
        // owned it then lost it" because can try to settle without knowing owner

        if (fsmTgt == _fsmTgt) {
            // System Owner changed so a Sector Owner changed event could follow - no problem as state immediately changed
            var previouslySettleableSystem = fsmTgt as ISystem_Ltd;
            D.Assert(!previouslySettleableSystem.IsFoundingSettlementAllowedBy(Owner));
            // Whether settled by us via another fleet or someone else, System is no longer settleable
        }
        else {
            // Sector Owner changed without System owner changing
            var previouslySettleableSector = fsmTgt as ISector_Ltd;
            if (previouslySettleableSector.IsFoundingSettlementAllowedBy(Owner)) {
                // Still allowed to settle but owner may or may not be us as may not have access to owner yet
                Player sectorOwner;
                if (previouslySettleableSector.TryGetOwner(Owner, out sectorOwner)) {
                    if (sectorOwner == Owner) {
                        // Owner is us so continue Settlement attempt
                        var previouslySettleableSystem = _fsmTgt as ISystem_Ltd;
                        D.Assert(previouslySettleableSystem.IsFoundingSettlementAllowedBy(Owner));
                        return;
                    }
                }
                // no access to sectorOwner or have access but not us. Either way it isn't us so end settle attempt
            }
            else {
                var previouslySettleableSystem = _fsmTgt as ISystem_Ltd;
                D.Assert(!previouslySettleableSystem.IsFoundingSettlementAllowedBy(Owner));
                // System is no longer settleable since an opponent's starbase(s) grew enough to become established
            }
        }
        IssueCmdStaffsAssumeFormationOrder();
    }

    void ExecuteFoundSettlementOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        ISystem_Ltd targetedSystem = _fsmTgt as ISystem_Ltd;
        if (!targetedSystem.IsFoundingSettlementAllowedBy(Owner)) {
            // 7.29.18 Confirmed this occurs. The item discovered was a RuntimeStarbase owned by the owner of this fleet that
            // had just been founded. However, the sector is owned by another player so system is not settleable by us.
            D.Log("{0} just founded or discovered {1}. As a result {2} is no longer settleable. Canceling.",
                DebugName, item.DebugName, _fsmTgt.DebugName);
            // Whether settled by us via another fleet or someone else, System is no longer settleable
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    [Obsolete("Not currently used")]
    void ExecuteFoundSettlementOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteFoundSettlementOrder_UponLosingOwnership() {
        LogEvent();
        // nothing to do
    }

    [Obsolete("Not currently used")]
    void ExecuteFoundSettlementOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteFoundSettlementOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteFoundSettlementOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);

        SystemItem destinationSystem = _fsmTgt as SystemItem;
        var systemSector = SectorGrid.Instance.GetSector(destinationSystem.SectorID);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, systemSector);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region FoundingSettlement

    // 12.13.17 Currently a Call()ed state by ExecuteFoundSettlementOrder only with no additional fleet movement

    #region FoundingSettlement Support Members

    /// <summary>
    /// Handles the condition where a ColonyShip is no longer available to settle. Returns <c>true</c>
    /// if another ColonyShip was assigned to replace this one, <c>false</c> if no more ColonyShips are currently available.
    /// <remarks>Typically this occurs when a ship fails to complete its assigned settle mission
    /// either because it dies or is so wounded that it needs to repair.</remarks>
    /// </summary>
    /// <param name="system">The target system.</param>
    /// <param name="unavailableShip">The unavailable ship.</param>
    /// <returns></returns>
    private bool HandleShipNoLongerAvailableToFoundSettlement(SystemItem system, ShipItem unavailableShip) {
        var otherColonyShips = Elements.Cast<ShipItem>().Except(unavailableShip).Where(s => s.IsAuthorizedForNewOrder(ShipDirective.FoundSettlement));
        if (otherColonyShips.Any()) {
            var colonyShip = otherColonyShips.First();
            AssignColonyShipToFoundSettlement(system, colonyShip);
            return true;
        }
        return false;
    }

    private void AssignColonyShipToFoundSettlement(SystemItem system, ShipItem colonyShip) {
        ShipOrder settleOrder = new ShipOrder(ShipDirective.FoundSettlement, CurrentOrder.Source, _executingOrderID, system);
        colonyShip.CurrentOrder = settleOrder;
    }

    #endregion

    void FoundingSettlement_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());

        SystemItem system = _fsmTgt as SystemItem;
        D.Assert(system.IsFoundingSettlementAllowedBy(Owner), DebugName);

        var colonyShips = Elements.Cast<ShipItem>().Where(s => s.HullCategory == ShipHullCategory.Colonizer);
        D.Assert(colonyShips.Any());
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator FoundingSettlement_EnterState() {
        LogEvent();
        // Fleet ships should be just outside of the intended SettlementStation
        var availableColonyShips = Elements.Cast<ShipItem>().Where(s => s.IsAuthorizedForNewOrder(ShipDirective.FoundSettlement));
        if (!availableColonyShips.Any()) {
            // this occurs if the colony ship we know is present happens to be Unavailable
            D.Warn("{0} found no available ColonyShips to attempt settling {1}.", DebugName, _fsmTgt.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
            Return();
        }
        var colonyShip = availableColonyShips.First();
        AssignColonyShipToFoundSettlement(_fsmTgt as SystemItem, colonyShip);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // With assigning one ship at a time, no reason to wait on _fsmShipsExpectedToCallbackWithOrderOutcome and then Return().
        // Return() handled in UponOrderOutcomeCallback
    }

    void FoundingSettlement_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.AssertEqual(ShipHullCategory.Colonizer, ship.HullCategory);

        D.Log(ShowDebugLog, "{0}.FoundingSettlement_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, ship.DebugName, outcome.GetValueName(), Time.frameCount);

        FsmReturnHandler returnHandler;
        switch (outcome) {
            case OrderOutcome.Success:
                Return();
                break;
            case OrderOutcome.NeedsRepair:
                bool isAnotherColonyShipAssigned = HandleShipNoLongerAvailableToFoundSettlement(_fsmTgt as SystemItem, ship);
                if (!isAnotherColonyShipAssigned) {
                    returnHandler = GetCurrentCalledStateReturnHandler();
                    returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
                    Return();
                }
                break;
            case OrderOutcome.Ownership:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Death:
                isAnotherColonyShipAssigned = HandleShipNoLongerAvailableToFoundSettlement(_fsmTgt as SystemItem, ship);
                if (!isAnotherColonyShipAssigned) {
                    returnHandler = GetCurrentCalledStateReturnHandler();
                    returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                    Return();
                }
                break;
            case OrderOutcome.ConstructionCanceled:
                // Occurs when User cancels SelectDesigns dialog
                returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.ConstructionCanceled;
                Return();
                break;
            case OrderOutcome.TgtUnjoinable:
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtDeath:
            case OrderOutcome.TgtUnreachable:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void FoundingSettlement_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void FoundingSettlement_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void FoundingSettlement_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void FoundingSettlement_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // do nothing
    }

    void FoundingSettlement_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void FoundingSettlement_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void FoundingSettlement_UponEnemyDetected() {
        LogEvent();
    }

    void FoundingSettlement_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void FoundingSettlement_UponRelationsChangedWith(Player player) {
        LogEvent();
        // do nothing as no effect
    }

    void FoundingSettlement_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var settleableSystem = fsmTgt as ISystem_Ltd;
        if (!settleableSystem.IsFoundingSettlementAllowedBy(Owner)) {
            bool isSettledByUs = false;
            Player sysOwner;
            if (fsmTgt.TryGetOwner(Owner, out sysOwner)) {
                if (sysOwner == Owner) {
                    isSettledByUs = true;
                }
            }

            if (!isSettledByUs) {
                D.LogBold("{0} just became aware that {1} is not settleable. Canceling attempt.", DebugName, fsmTgt.DebugName);
                var returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            }
            Return();   // If no ReturnCause assigned we have success but by a ColonyShip from another of our fleets!
                        // Can't be our ship as it would have already returned success in its order outcome so wouldn't still be in this state
        }
    }

    void FoundingSettlement_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();

        // Can't Assert fsmTgt.Owner != NoPlayer based on "we wouldn't be trying to settle it if someone else
        // owned it then lost it" because can try to settle without knowing owner

        FsmCallReturnCause returnCause = FsmCallReturnCause.None;

        if (fsmTgt == _fsmTgt) {
            // System Owner changed so a Sector Owner changed event could follow - no problem as state immediately changed
            var previouslySettleableSystem = fsmTgt as ISystem_Ltd;
            D.Assert(!previouslySettleableSystem.IsFoundingSettlementAllowedBy(Owner));
            // Whether settled by us via another fleet or someone else, System is no longer settleable
            Player sysOwner;
            if (fsmTgt.TryGetOwner(Owner, out sysOwner)) {
                if (sysOwner != Owner) {
                    returnCause = FsmCallReturnCause.TgtRelationship;
                }
            }
        }
        else {
            var previouslySettleableSector = fsmTgt as ISector_Ltd;
            if (previouslySettleableSector.IsFoundingSettlementAllowedBy(Owner)) {
                // Sector changed owner to us because one of our starbase(s) grew enough to become established...
                D.Assert(previouslySettleableSector.IsOwnerAccessibleTo(Owner));
                D.AssertEqual(previouslySettleableSector.Owner_Debug, Owner);
                // ...so do nothing and continue Settlement attempt
                return;
            }
            else {
                var previouslySettleableSystem = _fsmTgt as ISystem_Ltd;
                D.Assert(!previouslySettleableSystem.IsFoundingSettlementAllowedBy(Owner));
                // Sector is no longer settleable since an opponent's starbase(s) grew enough to become established
                returnCause = FsmCallReturnCause.Qualifications;
            }
        }

        if (returnCause != FsmCallReturnCause.None) {
            // If no ReturnCause assigned we have success but by a ColonyShip from another of our fleets!
            // Can't be our ship as it would have already returned success in its order outcome so wouldn't still be in this state
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = returnCause;
        }
        Return();
    }

    void FoundingSettlement_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        ISystem_Ltd targetedSystem = _fsmTgt as ISystem_Ltd;
        if (!targetedSystem.IsFoundingSettlementAllowedBy(Owner)) {
            // 7.29.18 UNCLEAR if this is really necessary, aka ever happens. If it does, need to adopt how to Return approach above
            D.Error("FYI - NOT AN ERROR. {0} just discovered {1}. As a result {2} is no longer settleable. Canceling.",
                DebugName, item.DebugName, _fsmTgt.DebugName);
            // Whether settled by us via another fleet or someone else, System is no longer settleable
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;  // UNCLEAR
            Return();
        }
    }

    [Obsolete("Not currently used")]
    void FoundingSettlement_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void FoundingSettlement_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion

    #region ExecuteFoundStarbaseOrder

    #region ExecuteFoundStarbaseOrder Support Members

    private FsmReturnHandler CreateFsmReturnHandler_FoundingToFoundStarbase() {
        IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

            {FsmCallReturnCause.TgtRelationship, () =>      { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.Qualifications, () =>       { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.NeedsRepair, () =>          { IssueCmdStaffsAssumeFormationOrder(); }   },
            {FsmCallReturnCause.ConstructionCanceled, () => { IssueCmdStaffsAssumeFormationOrder(); }   },
            // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
            // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
        };
        return new FsmReturnHandler(taskLookup, FleetState.FoundingStarbase.GetValueName());
    }

    #endregion

    void ExecuteFoundStarbaseOrder_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonNonCallableEnterStateValues();

        CoreSector destinationSector = CurrentOrder.Target as CoreSector;
        D.AssertNotNull(destinationSector);
        D.Assert(destinationSector.IsFoundingStarbaseAllowedBy(Owner));

        var colonyShips = Elements.Where(e => (e as ShipItem).HullCategory == ShipHullCategory.Colonizer);
        D.Assert(colonyShips.Any());

        _executingOrderID = CurrentOrder.OrderID;
        _fsmTgt = destinationSector;
        // no need for subscription to FsmTgtDeath for a sector
        bool isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtVacancyChg, _fsmTgt);
        D.Assert(isSubscribed);
        isSubscribed = FsmEventSubscriptionMgr.AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);
        D.Assert(isSubscribed);

        ChangeAvailabilityTo(NewOrderAvailability.FairlyAvailable);
    }

    IEnumerator ExecuteFoundStarbaseOrder_EnterState() {
        LogEvent();

        CoreSector destinationSector = _fsmTgt as CoreSector;
        D.Assert(destinationSector.IsFoundingStarbaseAllowedBy(Owner));

        // move to the sector we are to colonize
        float standoffDistance = Constants.ZeroF;
        _moveHelper.PlotPilotCourse(destinationSector, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive at sector
            yield return null;
        }
        // Should be in or just outside of destinationSector

        D.Assert(destinationSector.IsFoundingStarbaseAllowedBy(Owner));

        // now move just outside a vacant Station
        StationaryLocation vacantStation;
        bool isVacantStationFound = destinationSector.TryGetRandomVacantStarbaseStation(out vacantStation);
        D.Assert(isVacantStationFound); // Vacancy change event should already have changed state if no more vacant stations

        standoffDistance = ClearanceRadius; // IMPROVE keep ships away from where Starbase will be founded
        _moveHelper.PlotPilotCourse(vacantStation, Speed.Standard, standoffDistance);

        while (!_moveHelper.IsPilotEngaged) {
            // wait until course is plotted and pilot engaged
            yield return null;
        }

        while (_moveHelper.IsPilotEngaged) {
            // wait for pilot to arrive outside of vacant station
            yield return null;
        }

        FsmReturnHandler returnHandler = GetInactiveReturnHandlerFor(FleetState.FoundingStarbase, CurrentState);
        Call(FleetState.FoundingStarbase);
        yield return null;  // reqd so Return()s here. Code that follows executed 1 frame later

        if (!returnHandler.WasCallSuccessful(ref _isWaitingToProcessReturn)) {
            yield return null;
            D.Error("{0} shouldn't get here as the ReturnCause {1} should generate a change of state.", DebugName, returnHandler.ReturnCause.GetValueName());
        }

        var newStarbase = destinationSector.GetStarbaseLocatedAt(vacantStation);
        D.AssertEqual(Owner, newStarbase.Owner);

        if (!IsDead) {
            IFleetNavigableDestination starbaseAssyStation = GameUtility.GetClosest(Position, newStarbase.LocalAssemblyStations);
            IssueCmdStaffsAssumeFormationOrder(starbaseAssyStation);
        }
    }

    void ExecuteFoundStarbaseOrder_UponApCoursePlotSuccess() {
        LogEvent();
        _moveHelper.EngagePilot();
    }

    void ExecuteFoundStarbaseOrder_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        D.Assert(!_isWaitingToProcessReturn, "A leaked callback?");
        D.Log(ShowDebugLog, "{0}.ExecuteFoundStarbaseOrder_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, Outcome = {3}, ShipCurrentOrder = {4}.",
            DebugName, LastState.GetValueName(), ship.DebugName, outcome.GetValueName(), ship.CurrentOrder.DebugName);
        _moveHelper.HandleOrderOutcomeCallback(ship, target, outcome);
    }

    void ExecuteFoundStarbaseOrder_UponApTargetReached() {
        LogEvent();
        _moveHelper.DisengagePilot();
    }

    [Obsolete("Error Detection")]
    void ExecuteFoundStarbaseOrder_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can target {1} be uncatchable?", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void ExecuteFoundStarbaseOrder_UponNewOrderReceived() {
        LogEvent();
        // do nothing as state will change
    }

    void ExecuteFoundStarbaseOrder_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteFoundStarbaseOrder_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void ExecuteFoundStarbaseOrder_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // IMPROVE Could also just order ship to AssumeStation without including it in expected arrivals. It will be included along with 
        // rest of fleet when next leg of move order is issued. If no more legs??
        if (_moveHelper.IsPilotEngaged) {
            // in this state, if pilot is engaged, the fleet is still trying to get to its disband target
            if (_moveHelper.AreAnyShipsStillExpectedToArrive) {
                // one or more ships are still expected to arrive at the next waypoint or final target
                if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
                    var currentDestination = _moveHelper.CurrentDestination;
                    bool isFleetwideMove = false;
                    Speed speed = _moveHelper.ApSpeedSetting;
                    float standoffDistance = _moveHelper.ApTargetStandoffDistance;
                    _moveHelper.AddShipToExpectedArrivals(subordinateShip);
                    ShipMoveOrder moveOrder = new ShipMoveOrder(CurrentOrder.Source, _executingOrderID,
                        currentDestination as IShipNavigableDestination, speed, isFleetwideMove, standoffDistance);
                    subordinateShip.CurrentOrder = moveOrder;
                }
            }
        }
    }

    void ExecuteFoundStarbaseOrder_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void ExecuteFoundStarbaseOrder_UponHQElementChanged() {
        LogEvent();
        // 4.15.17 Affected ships already RestartState to force proxy offset update if Moving.
        // AssumeFormation order at end of EnterState will fix formation if not.
    }

    void ExecuteFoundStarbaseOrder_UponEnemyDetected() {
        LogEvent();
        // TODO
    }

    void ExecuteFoundStarbaseOrder_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void ExecuteFoundStarbaseOrder_UponRelationsChangedWith(Player player) {
        LogEvent();
        // nothing to do
    }

    void ExecuteFoundStarbaseOrder_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var targetedSector = _fsmTgt as CoreSector;

        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            D.Log("{0} just became aware that {1} no longer allows us to found a starbase. Canceling.", DebugName, fsmTgt.DebugName);
            // Whether caused by us via another fleet or someone else, Sector no longer allows us to found a starbase
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteFoundStarbaseOrder_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var targetedSector = fsmTgt as CoreSector;
        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteFoundStarbaseOrder_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        var targetedSector = _fsmTgt as CoreSector;
        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            D.Log("{0} just became aware of {1}. As a result {2} no longer allows us to found a starbase. Canceling.",
                DebugName, item.DebugName, targetedSector.DebugName);
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    void ExecuteFoundStarbaseOrder_UponSectorStationVacancyChanged(StationaryLocation station, bool isVacant) {
        LogEvent();
        CoreSector targetedSector = _fsmTgt as CoreSector;
        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            if (!targetedSector.AreAnyStationsVacant) {
                D.Log("{0} just became aware that {1} has no more vacant stations to found a Starbase. Canceling.", DebugName,
                    targetedSector.DebugName);
            }
            else {
                D.Warn(@"Because of a StarbaseStation vacancy change, {0} is no longer allowed to found a Starbase in {1}.
                    Unexpectedly, it is not because there are no more vacant stations! Canceling.", DebugName, targetedSector.DebugName);
            }
            IssueCmdStaffsAssumeFormationOrder();
        }
    }

    [Obsolete("Not currently used")]
    void ExecuteFoundStarbaseOrder_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    void ExecuteFoundStarbaseOrder_UponLosingOwnership() {
        LogEvent();
        // nothing to do
    }

    [Obsolete("Not currently used")]
    void ExecuteFoundStarbaseOrder_UponResetOrderAndState() {
        LogEvent();
    }

    void ExecuteFoundStarbaseOrder_UponDeath() {
        LogEvent();
    }

    void ExecuteFoundStarbaseOrder_ExitState() {
        LogEvent();

        bool isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtOwnerChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtInfoAccessChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.FsmTgtVacancyChg, _fsmTgt);
        D.Assert(isUnsubscribed);
        isUnsubscribed = FsmEventSubscriptionMgr.AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode.OwnerAwareChg_Base);
        D.Assert(isUnsubscribed);

        ClearOrderAndStateForElementsExecuting(ref _executingOrderID);
        ResetAndValidateCommonNonCallableExitStateValues();
    }

    #endregion

    #region FoundingStarbase

    // 12.13.17 Currently a Call()ed state by ExecuteFoundStarbaseOrder only with no additional fleet movement

    #region FoundingStarbase Support Members

    /// <summary>
    /// Handles the condition where a ColonyShip is no longer available to found a starbase. Returns <c>true</c>
    /// if another ColonyShip was assigned to replace this one, <c>false</c> if no more ColonyShips are currently available.
    /// <remarks>Typically this occurs when a ship fails to complete its assigned founding mission
    /// either because it dies or is so wounded that it needs to repair.</remarks>
    /// </summary>
    /// <param name="sector">The target system.</param>
    /// <param name="unavailableShip">The unavailable ship.</param>
    /// <returns></returns>
    private bool HandleShipNoLongerAvailableToFoundStarbase(CoreSector sector, ShipItem unavailableShip) {
        var otherColonyShips = Elements.Cast<ShipItem>().Except(unavailableShip).Where(s => s.IsAuthorizedForNewOrder(ShipDirective.FoundStarbase));
        if (otherColonyShips.Any()) {
            var colonyShip = otherColonyShips.First();
            AssignColonyShipToFoundStarbase(sector, colonyShip);
            return true;
        }
        return false;
    }

    private void AssignColonyShipToFoundStarbase(CoreSector sector, ShipItem colonyShip) {
        ShipOrder foundStarbaseOrder = new ShipOrder(ShipDirective.FoundStarbase, CurrentOrder.Source, _executingOrderID, sector);
        colonyShip.CurrentOrder = foundStarbaseOrder;
    }

    #endregion

    void FoundingStarbase_UponPreconfigureState() {
        LogEvent();

        __ValidateCommonCallableEnterStateValues(CurrentState.GetValueName());
        CoreSector currentSector = _fsmTgt as CoreSector;
        D.AssertNotNull(currentSector);
        D.Assert(currentSector.IsFoundingStarbaseAllowedBy(Owner));

        var colonyShips = Elements.Cast<ShipItem>().Where(s => s.HullCategory == ShipHullCategory.Colonizer);
        D.Assert(colonyShips.Any());
        ChangeAvailabilityTo(NewOrderAvailability.Unavailable);
    }

    IEnumerator FoundingStarbase_EnterState() {
        LogEvent();
        // Fleet ships should be just outside of the intended and vacant Station
        var availableColonyShips = Elements.Cast<ShipItem>().Where(s => s.IsAuthorizedForNewOrder(ShipDirective.FoundStarbase));
        if (!availableColonyShips.Any()) {
            // this occurs if the colony ship we know is present happens to be Unavailable
            D.Warn("{0} found no available ColonyShips to attempt founding starbase in {1}.", DebugName, _fsmTgt.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
            Return();
        }
        var colonyShip = availableColonyShips.First();
        AssignColonyShipToFoundStarbase(_fsmTgt as CoreSector, colonyShip);

        // 3.19.17 Without yield return null; here, I see a Unity Coroutine error "Assertion failed on expression: 'm_CoroutineEnumeratorGCHandle == 0'"
        // I think it is because there is a rare scenario where no yield return is encountered below this. 
        // See http://answers.unity3d.com/questions/158917/error-quotmcoroutineenumeratorgchandle-0quot.html
        yield return null;

        // With assigning one ship at a time, no reason to wait on _fsmShipsExpectedToCallbackWithOrderOutcome and then Return().
        // Return() handled in UponOrderOutcomeCallback
    }

    void FoundingStarbase_UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        LogEvent();
        D.AssertEqual(ShipHullCategory.Colonizer, ship.HullCategory);

        D.Log(ShowDebugLog, "{0}.FoundingStarbase_UponOrderOutcomeCallback() called from {1}. FailCause = {2}. Frame: {3}.",
            DebugName, ship.DebugName, outcome.GetValueName(), Time.frameCount);

        FsmReturnHandler returnHandler;
        switch (outcome) {
            case OrderOutcome.Success:
                Return();
                break;
            case OrderOutcome.NeedsRepair:
                bool isAnotherColonyShipAssigned = HandleShipNoLongerAvailableToFoundStarbase(_fsmTgt as CoreSector, ship);
                if (!isAnotherColonyShipAssigned) {
                    returnHandler = GetCurrentCalledStateReturnHandler();
                    returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
                    Return();
                }
                break;
            case OrderOutcome.Ownership:
            case OrderOutcome.OrdersChange:
            case OrderOutcome.Death:
                isAnotherColonyShipAssigned = HandleShipNoLongerAvailableToFoundStarbase(_fsmTgt as CoreSector, ship);
                if (!isAnotherColonyShipAssigned) {
                    returnHandler = GetCurrentCalledStateReturnHandler();
                    returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
                    Return();
                }
                break;
            case OrderOutcome.ConstructionCanceled:
                // Occurs when User cancels SelectDesigns dialog
                returnHandler = GetCurrentCalledStateReturnHandler();
                returnHandler.ReturnCause = FsmCallReturnCause.ConstructionCanceled;
                Return();
                break;
            case OrderOutcome.TgtUnjoinable:    // Fleet determines out of vacant stations, not ship
            case OrderOutcome.TgtRelationship:
            case OrderOutcome.TgtDeath:
            case OrderOutcome.TgtUnreachable:
            case OrderOutcome.TgtUncatchable:
            case OrderOutcome.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outcome));
        }
    }

    [Obsolete("Error Detection")]
    void FoundingStarbase_UponApTgtUncatchable() {
        LogEvent();
        D.Error("{0}: How can this 'uncatchable' call occur? Target: {1}.", DebugName, _moveHelper.ApTarget.DebugName);
    }

    void FoundingStarbase_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void FoundingStarbase_UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        LogEvent();
    }

    void FoundingStarbase_UponSubordinateJoined(ShipItem subordinateShip) {
        LogEvent();
        // do nothing
    }

    void FoundingStarbase_UponAlertStatusChanged() {
        LogEvent();
        // TODO
    }

    void FoundingStarbase_UponHQElementChanged() {
        LogEvent();
        // TODO
    }

    void FoundingStarbase_UponEnemyDetected() {
        LogEvent();
    }

    void FoundingStarbase_UponUnitDamageIncurred() {
        LogEvent();
        // do nothing
    }

    void FoundingStarbase_UponRelationsChangedWith(Player player) {
        LogEvent();
        // do nothing as no effect
    }

    void FoundingStarbase_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var sector = fsmTgt as CoreSector;
        if (!sector.IsFoundingStarbaseAllowedBy(Owner)) {
            D.LogBold("{0} just became aware that can no longer found Starbase in {1}. Canceling.", DebugName, fsmTgt.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void FoundingStarbase_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        LogEvent();
        D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);

        var sector = fsmTgt as CoreSector;
        if (!sector.IsFoundingStarbaseAllowedBy(Owner)) {
            D.LogBold("{0} just became aware that can no longer found Starbase in {1}. Canceling.", DebugName, fsmTgt.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void FoundingStarbase_UponAwarenessChgd(IOwnerItem_Ltd item) {
        LogEvent();
        var targetedSector = _fsmTgt as CoreSector;
        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            // 7.29.18 Don't expect this to occur as Fleet should be quite close to VacantStation
            D.Warn("Unexpectedly, {0} just became aware of {1}? As a result {2} no longer allows us to found a starbase. Canceling.",
                DebugName, item.DebugName, targetedSector.DebugName);
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
            Return();
        }
    }

    void FoundingStarbase_UponSectorStationVacancyChanged(StationaryLocation station, bool isVacant) {
        LogEvent();

        CoreSector targetedSector = _fsmTgt as CoreSector;
        if (!targetedSector.IsFoundingStarbaseAllowedBy(Owner)) {
            if (!targetedSector.AreAnyStationsVacant) {
                D.Log("{0} just became aware that {1} has no more vacant stations to found a Starbase. Canceling.", DebugName,
                    targetedSector.DebugName);
            }
            else {
                D.Warn(@"Because of a StarbaseStation vacancy change, {0} is no longer allowed to found a Starbase in {1}.
                    Unexpectedly, it is not because there are no more vacant stations! Canceling.", DebugName, targetedSector.DebugName);
            }
            var returnHandler = GetCurrentCalledStateReturnHandler();
            returnHandler.ReturnCause = FsmCallReturnCause.Qualifications;
            Return();
        }
    }

    [Obsolete("Not currently used")]
    void FoundingStarbase_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        LogEvent();
    }

    // 4.8.17 Call()ed state _UponNewOrderReceived() eliminated as auto Return()ed prior to _UponNewOrderReceived()
    // 4.15.17 Call()ed state _UponDeath eliminated as auto Return()ed as part of death sequence
    // 4.15.17 Call()ed state _UponLosingOwnership eliminated as auto Return()ed prior to _UponLosingOwnership()

    void FoundingStarbase_ExitState() {
        LogEvent();
        ResetAndValidateCommonCallableExitStateValues();
    }

    #endregion


    #region Withdraw

    void Withdraw_EnterState() { }

    #endregion

    #region Retreat

    void GoRetreat_EnterState() { }

    #endregion

    #region Dead

    /*********************************************************************************
     * UNCLEAR whether Cmd will show a death effect or not. For now, I'm not going
     *  to use an effect. Instead, the DisplayMgr will just shut off the Icon and HQ highlight.
     ************************************************************************************/

    void Dead_UponPreconfigureState() {
        LogEvent();
    }

    void Dead_EnterState() {
        LogEvent();
        PrepareForDeathEffect();
        StartEffectSequence(EffectSequenceID.Dying);
        HandleDeathEffectBegun();
    }

    void Dead_UponEffectSequenceFinished(EffectSequenceID effectSeqID) {
        LogEvent();
        D.AssertEqual(EffectSequenceID.Dying, effectSeqID);
        HandleDeathEffectFinished();
        //D.Log("{0} initiating destruction in Frame {1}.", DebugName, Time.frameCount);
        DestroyMe(onCompletion: () => DestroyApplicableParents(5F));  // HACK long wait so last element can play death effect
    }

    #endregion

    #region Archived States

    #region Moving State Archive

    #region Moving FsmReturnHandler Archive

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToMove() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>      { IssueCmdStaffsRepairOrder(); }                               },
    ////        // Standoff distance needs adjustment
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { RestartState(); }                               },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // 4.15.17 Either no longer aware of tgtFleet or its progressively getting further away                                                                   
    ////        {FsmCallReturnCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }             },
    ////        {FsmCallReturnCause.TgtUnreachable, () => {
    ////            D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmCallReturnCause).Name,
    ////                FsmCallReturnCause.TgtUnreachable.GetValueName());
    ////        }                                                                                               },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToAssumeFormation() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { IssueCmdStaffsRepairOrder(); }       },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////        // 4.13.17 All AssumeFormation destinations are StationaryLocs so no TgtRelationship
    ////        // 4.15.17 No AssumeFormation destinations are Fleets so no TgtUncatchable
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToExplore() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { IssueCmdStaffsRepairOrder(); }                          },
    ////        // No longer allowed to explore target or fully explored
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }             },
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToGuard() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { IssueCmdStaffsRepairOrder(); }                  },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }     },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }             },
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToAttack() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { IssueCmdStaffsRepairOrder();   }                    },
    ////        // No longer allowed to attack target
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // 4.15.17 Either no longer aware of tgtFleet or its progressively getting further away                                                                   
    ////        {FsmCallReturnCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }             },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToRegroup() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    {
    ////            if(AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
    ////                IssueCmdStaffsRepairOrder();
    ////            }
    ////            else {
    ////                RestartState();
    ////            }
    ////        }                                                                                               },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    {
    ////            IFleetNavigableDestination newRegroupDest = GetRegroupDestination(Data.CurrentHeading);
    ////            IssueCmdStaffsRegroupOrder(newRegroupDest);
    ////        }                                                                                               },
    ////        {FsmCallReturnCause.TgtDeath, () =>   {
    ////            IFleetNavigableDestination newRegroupDest = GetRegroupDestination(Data.CurrentHeading);
    ////            IssueCmdStaffsRegroupOrder(newRegroupDest);
    ////        }                                                                                               },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////        // TgtUnreachable: 4.14.17 Currently this is simply an error
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable for other fleets
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToJoinFleet() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    {
    ////            if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
    ////                IssueCmdStaffsRepairOrder();
    ////            }
    ////            else {
    ////                RestartState();
    ////            }
    ////        }                                                                                               },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
    ////        {FsmCallReturnCause.TgtUnreachable, () => {
    ////            D.Error("{0}: {1}.{2} not currently handled.", DebugName, typeof(FsmCallReturnCause).Name,
    ////                FsmCallReturnCause.TgtUnreachable.GetValueName());
    ////        }                                                                                               },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // 2.8.17 Our fleet we are trying to join is getting progressively getting further away  
    ////        // 4.15.17 Only fleets owned by others currently get the uncatchable test so currently can't occur                                                                 
    ////        // {FsmCallReturnCause.TgtUncatchable, () => { IssueCmdStaffsAssumeFormationOrder(); }           },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToJoinHanger() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    {
    ////            if (AssessNeedForRepair(HealthThreshold_CriticallyDamaged)) {
    ////                IssueCmdStaffsRepairOrder();
    ////            }
    ////            else {
    ////                RestartState();
    ////            }
    ////        }                                                                                               },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToRepair() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { RestartState(); }               },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsRepairOrder(); }         },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsRepairOrder(); }                 },
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToRefit() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { RestartState(); }                                   },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    ////private FsmReturnHandler CreateFsmReturnHandler_MovingToDisband() {
    ////    IDictionary<FsmCallReturnCause, Action> taskLookup = new Dictionary<FsmCallReturnCause, Action>() {

    ////        {FsmCallReturnCause.NeedsRepair, () =>    { RestartState(); }                                   },
    ////        {FsmCallReturnCause.TgtRelationship, () =>    { IssueCmdStaffsAssumeFormationOrder(); }         },
    ////        {FsmCallReturnCause.TgtDeath, () =>   { IssueCmdStaffsAssumeFormationOrder(); }                 },
    ////        // TgtUncatchable:  4.15.17 Only Fleet targets are uncatchable to Cmds
    ////        // Death: 4.14.17 No longer a ReturnCause as InitiateDeadState auto Return()s out of Call()ed states
    ////    };
    ////    return new FsmReturnHandler(taskLookup, FleetState.Moving.GetValueName());
    ////}

    #endregion

    // 7.2.16 Call()ed State by ExecuteMoveOrder, ExecuteExploreOrder, ExecuteAssumeFormationOrder,  ExecutePatrolOrder,  
    // ExecuteGuardOrder,  ExecuteAttackOrder,  ExecuteRegroupOrder,  ExecuteJoinFleetOrder,  ExecuteJoinHangerOrder, 
    // ExecuteRepairOrder, ExecuteRefitOrder, ExecuteDisbandOrder
    // ~ 7.17 AssumingFormation, Patrolling and Guarding all handle movement themselves without using Moving

    /// <summary>
    /// The speed of the AutoPilot Move. Valid during the Moving state and during the state 
    /// that sets it and Call()s the Moving state until the Moving state Return()s.
    /// The state that sets this value during its EnterState() is not responsible for nulling 
    /// it during its ExitState() as that is handled by Moving_ExitState().
    /// </summary>
    ////private Speed _apMoveSpeed;

    /// <summary>
    /// The standoff distance from the target of the AutoPilot Move. Valid only in the Moving state.
    /// <remarks>Ship 'arrival' at some IFleetNavigableDestination targets should be further away than the amount the target would 
    /// normally designate when returning its AutoPilotTarget. IFleetNavigableDestination target examples include enemy bases and
    /// fleets where the ships in this fleet should 'arrive' outside of the enemy's max weapons range.</remarks>
    /// </summary>
    ////private float _apMoveTgtStandoffDistance;

    /// <summary>
    /// Utilized by Moving_UponRelationsChgdWith, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmCallReturnCause.TgtRelationship.
    /// </summary>
    /// <param name="player">The player whose relations changed with the Owner of this Cmd.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    ////private bool AssessReturnFromRelationsChangeWith(Player player) {
    ////    D.AssertNotNull(_fsmTgt);
    ////    bool toReturn = false;
    ////    switch (LastState) {
    ////        case FleetState.ExecuteExploreOrder:
    ////            IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
    ////            if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecutePatrolOrder:
    ////            IPatrollable patrolTgt = _fsmTgt as IPatrollable;
    ////            if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteGuardOrder:
    ////            IGuardable guardTgt = _fsmTgt as IGuardable;
    ////            if (!guardTgt.IsGuardingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteAttackOrder:
    ////            IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
    ////            if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
    ////                // Can no longer attack
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteMoveOrder:
    ////            var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
    ////            if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
    ////                Player unitCmdMoveTgtOwner;
    ////                if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
    ////                    if (unitCmdMoveTgtOwner == player) {
    ////                        if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
    ////                            // now known as an enemy
    ////                            if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                                // definitely Return() as changed from non-enemy to enemy
    ////                                toReturn = true;
    ////                            }
    ////                            // else no need to Return() as no change in being an enemy
    ////                        }
    ////                        else {
    ////                            // now known as not an enemy
    ////                            if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                                // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
    ////                                toReturn = true;
    ////                            }
    ////                        }
    ////                    }
    ////                }
    ////            }
    ////            // Could also be moving to 1) an AssemblyStation from within a System or Sector, 2) a System or Sector
    ////            // from outside, 3) a Planet, Star or UniverseCenter. Since none of these can fire on us, no reason to worry
    ////            // about recalculating StandoffDistance.
    ////            break;
    ////        case FleetState.ExecuteRepairOrder:
    ////            // 4.14.17  Can be either IUnitRepairCapable and IShipRepairCapable (Base) or just IShipRepairCapable (Planet)
    ////            var repairDest = _fsmTgt as IRepairCapable;
    ////            if (!repairDest.IsRepairingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;

    ////        case FleetState.ExecuteRefitOrder:
    ////        case FleetState.ExecuteDisbandOrder:
    ////            // 11.27.17 Never Return(). A RelationsChg can't affect our move to our own base
    ////            break;
    ////        case FleetState.ExecuteAssumeFormationOrder:
    ////            // 4.14.17 Never Return()
    ////            break;
    ////        case FleetState.ExecuteJoinFleetOrder:
    ////            // 5.5.17 Never Return(). A RelationsChg can't affect our move to our own fleet
    ////            break;
    ////        case FleetState.ExecuteJoinHangerOrder:
    ////            // 12.31.17 Never Return(). A RelationsChg can't affect our move to our own base
    ////            break;

    ////        //case FleetState.ExecuteRegroupOrder:
    ////        //    var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
    ////        //    if (regroupDest != null) {
    ////        //        Player regroupDestOwner;
    ////        //        if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
    ////        //            if (regroupDestOwner == player) {
    ////        //                // moving to system whose relations with us just changed
    ////        //                if (Owner.IsEnemyOf(regroupDestOwner)) {
    ////        //                    // now known as an enemy
    ////        //                    toReturn = true;
    ////        //                }
    ////        //            }
    ////        //        }
    ////        //    }
    ////        //    break; 
    ////        default:
    ////            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
    ////    }
    ////    return toReturn;
    ////}

    /// <summary>
    /// Utilized by Moving_UponFsmTgtInfoAccessChgd, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmCallReturnCause.TgtRelationship.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    ////private bool AssessReturnFromTgtInfoAccessChange() {
    ////    D.AssertNotNull(_fsmTgt);
    ////    bool toReturn = false;
    ////    switch (LastState) {
    ////        case FleetState.ExecuteExploreOrder:
    ////            IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
    ////            if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecutePatrolOrder:
    ////            IPatrollable patrolTgt = _fsmTgt as IPatrollable;
    ////            if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteGuardOrder:
    ////            IGuardable guardTgt = _fsmTgt as IGuardable;
    ////            if (!guardTgt.IsGuardingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteAttackOrder:
    ////            IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
    ////            if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
    ////                // Can no longer attack
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteMoveOrder:
    ////            var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
    ////            if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
    ////                Player unitCmdMoveTgtOwner;
    ////                if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
    ////                    if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
    ////                        // now known as an enemy
    ////                        if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                            // definitely Return() as changed from non-enemy to enemy
    ////                            toReturn = true;
    ////                        }
    ////                        // else no need to Return() as no change in being an enemy
    ////                    }
    ////                    else {
    ////                        // now known as not an enemy
    ////                        if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                            // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
    ////                            toReturn = true;
    ////                        }
    ////                    }
    ////                }
    ////            }
    ////            // Could also be moving to a System or Sector from outside, a Planet, Star or UniverseCenter. 
    ////            // Since none of these can fire on us, no reason to worry about recalculating StandoffDistance.
    ////            break;

    ////        case FleetState.ExecuteRepairOrder:
    ////            // 4.14.17  Can be either IUnitRepairCapable and IShipRepairCapable (Base) or just IShipRepairCapable (Planet)
    ////            var repairDest = _fsmTgt as IRepairCapable;
    ////            if (!repairDest.IsRepairingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteJoinFleetOrder:
    ////            // 12.31.17 Not possible. JoinFleetOrder to own fleet has no need for monitoring an InfoAccessChg event
    ////            break;
    ////        case FleetState.ExecuteJoinHangerOrder:
    ////            // 12.31.17 Not possible. JoinHangerOrder to own base has no need for monitoring an InfoAccessChg event
    ////            break;
    ////        //case FleetState.ExecuteRegroupOrder:
    ////        //    var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
    ////        //    D.AssertNotNull(regroupDest);   // 4.14.17 Can't be a StationaryLoc with a InfoAccessChg event
    ////        //    Player regroupDestOwner;
    ////        //    if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
    ////        //        // moving to a system with DiplomaticRelations.None whose owner just became accessible
    ////        //        if (Owner.IsEnemyOf(regroupDestOwner)) {
    ////        //            // now known as an enemy
    ////        //            toReturn = true;
    ////        //        }
    ////        //    }
    ////        //    break;            case FleetState.ExecuteRefitOrder:
    ////        //11.27.17 Not possible. RefitOrder to own base has no need for monitoring an InfoAccessChg event
    ////        case FleetState.ExecuteRefitOrder:
    ////        case FleetState.ExecuteDisbandOrder:
    ////        // 12.13.17 Not possible. Don't need InfoAccessChg for own bases
    ////        case FleetState.ExecuteAssumeFormationOrder:
    ////        // 4.14.17 Not possible. Can't get a InfoAccessChg event from a StationaryLoc
    ////        default:
    ////            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
    ////    }
    ////    return toReturn;
    ////}

    /// <summary>
    /// Utilized by Moving_UponFsmTgtOwnerChgd, returns <c>true</c> if Moving state should Return()
    /// to the state that Call()ed it with FsmCallReturnCause.TgtRelationship.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    ////private bool AssessReturnFromTgtOwnerChange() {
    ////    D.AssertNotNull(_fsmTgt);
    ////    bool toReturn = false;
    ////    switch (LastState) {
    ////        case FleetState.ExecuteExploreOrder:
    ////            IFleetExplorable fleetExploreTgt = _fsmTgt as IFleetExplorable;
    ////            if (!fleetExploreTgt.IsExploringAllowedBy(Owner) || fleetExploreTgt.IsFullyExploredBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecutePatrolOrder:
    ////            IPatrollable patrolTgt = _fsmTgt as IPatrollable;
    ////            if (!patrolTgt.IsPatrollingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteGuardOrder:
    ////            IGuardable guardTgt = _fsmTgt as IGuardable;
    ////            if (!guardTgt.IsGuardingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteAttackOrder:
    ////            IUnitAttackable unitAttackTgt = _fsmTgt as IUnitAttackable;
    ////            if (!unitAttackTgt.IsWarAttackAllowedBy(Owner) && (!OwnerAIMgr.IsPolicyToEngageColdWarEnemies || !unitAttackTgt.IsColdWarAttackAllowedBy(Owner))) {
    ////                // Can no longer attack
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteMoveOrder:
    ////            var unitCmdMoveTgt = _fsmTgt as IUnitCmd_Ltd;
    ////            if (unitCmdMoveTgt != null) {   // as this is about standoff distance, only Units have weapons
    ////                Player unitCmdMoveTgtOwner;
    ////                if (unitCmdMoveTgt.TryGetOwner(Owner, out unitCmdMoveTgtOwner)) {
    ////                    if (Owner.IsEnemyOf(unitCmdMoveTgtOwner)) {
    ////                        // now known as an enemy
    ////                        if (!Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                            // definitely Return() as changed from non-enemy to enemy
    ////                            toReturn = true;
    ////                        }
    ////                        // else no need to Return() as no change in being an enemy
    ////                    }
    ////                    else {
    ////                        // now known as not an enemy
    ////                        if (Owner.IsPreviouslyEnemyOf(unitCmdMoveTgtOwner)) {
    ////                            // changed from enemy to non-enemy so Return() as StandoffDistance can be shortened
    ////                            toReturn = true;
    ////                        }
    ////                    }
    ////                }
    ////            }
    ////            // Could also be moving to a System or Sector from outside or a Planet or Star. 
    ////            // Since none of these can fire on us, no reason to worry about recalculating StandoffDistance.
    ////            break;
    ////        case FleetState.ExecuteJoinFleetOrder:
    ////            IFleetCmd_Ltd tgtFleet = _fsmTgt as IFleetCmd_Ltd;
    ////            Player tgtFleetOwner;
    ////            if (tgtFleet.TryGetOwner(Owner, out tgtFleetOwner)) {
    ////                if (tgtFleetOwner != Owner) {
    ////                    // target fleet owner is no longer us
    ////                    toReturn = true;
    ////                }
    ////            }
    ////            else {
    ////                // don't have access to owner so clearly no longer us
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteJoinHangerOrder:
    ////            IUnitBaseCmd_Ltd tgtBase = _fsmTgt as IUnitBaseCmd_Ltd;
    ////            Player tgtBaseOwner;
    ////            if (tgtBase.TryGetOwner(Owner, out tgtBaseOwner)) {
    ////                if (tgtBaseOwner != Owner) {
    ////                    // target base owner is no longer us
    ////                    toReturn = true;
    ////                }
    ////            }
    ////            else {
    ////                // don't have access to owner so clearly no longer us
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteRepairOrder:
    ////            var repairDest = _fsmTgt as IRepairCapable;
    ////            if (!repairDest.IsRepairingAllowedBy(Owner)) {
    ////                toReturn = true;
    ////            }
    ////            break;
    ////        case FleetState.ExecuteRefitOrder:
    ////        case FleetState.ExecuteDisbandOrder:
    ////            // 11.27.17 an owner chg to our own base target ruins our refit/disband plans
    ////            toReturn = true;
    ////            break;
    ////        //case FleetState.ExecuteRegroupOrder:
    ////        //    var regroupDest = _fsmTgt as IOwnerItem_Ltd;  // 3.20.17 Current regroupTgts are MyBases/Systems, friendlySystems or StationaryLocs
    ////        //    D.AssertNotNull(regroupDest);   // 4.14.17 Can't be a StationaryLoc with an OwnerChg
    ////        //    Player regroupDestOwner;
    ////        //    if (regroupDest.TryGetOwner(Owner, out regroupDestOwner)) {
    ////        //        // moving to base/system we just lost or friendly system whose owner just changed
    ////        //        if (Owner.IsEnemyOf(regroupDestOwner)) {
    ////        //            // now known as an enemy
    ////        //            toReturn = true;
    ////        //        }
    ////        //    }
    ////        //    break;
    ////        case FleetState.ExecuteAssumeFormationOrder:
    ////        // 4.14.17 Not possible. Can't get an OwnerChg event from a StationaryLoc
    ////        default:
    ////            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(LastState));
    ////    }
    ////    return toReturn;
    ////}

    ////void Moving_UponPreconfigureState() {
    ////    LogEvent();

    ////    ValidateCommonCallableStateValues(CurrentState.GetValueName());
    ////    D.AssertNotDefault((int)_apMoveSpeed);

    ////    if (LastState == FleetState.ExecuteExploreOrder) {
    ////        if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
    ////            D.Warn("{0} entering Moving state with ExploreTgt {1} not explorable.", DebugName, _fsmTgt.DebugName);
    ////        }
    ////    }
    ////    // 12.7.17 Don't set Availability in states that can be Call()ed by more than one ExecuteOrder state
    ////}


    ////IEnumerator Moving_EnterState() {
    ////    LogEvent();

    ////    IFleetNavigableDestination apTgt = _fsmTgt;
    ////    ////_navigator.PlotPilotCourse(apTgt, _apMoveSpeed, _apMoveTgtStandoffDistance);
    ////    _navigator.PlotPilotCourse(apTgt);

    ////    while (!__isCoursePlotted) {
    ////        // wait until course plot is completed
    ////        yield return null;
    ////    }
    ////    __isCoursePlotted = false;

    ////    D.AssertNotEqual(Constants.Zero, _navigator.ApCourse.Count);

    ////    int currentApCourseIndex = Constants.One;  // must be kept current to allow RefreshCourse to properly place any added detour in Course
    ////    IFleetNavigableDestination currentWaypoint = _navigator.ApCourse[currentApCourseIndex];   // skip the course start position as the fleet is already there

    ////    // ***************************************************************************************************************************
    ////    // The following initial Obstacle Check has been extracted from the PilotNavigationJob to accommodate a Fleet Move Cmd issued 
    ////    // via ContextMenu while Paused. It starts the Job and then immediately pauses it. This test for an obstacle prior to the Job 
    ////    // starting allows the Course plot display to show the detour around the obstacle (if one is found) rather than show a 
    ////    // course plot into an obstacle.
    ////    // ***************************************************************************************************************************
    ////    IFleetNavigableDestination detour;
    ////    if (_navigator.__TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
    ////        // but there is an obstacle, so add a waypoint
    ////        _navigator.__RefreshApCourse(CourseRefreshMode.AddWaypoint, ref currentApCourseIndex, detour);
    ////    }

    ////    int apTgtCourseIndex = _navigator.ApCourse.Count - 1;
    ////    D.AssertEqual(Constants.One, currentApCourseIndex);
    ////    currentWaypoint = _navigator.ApCourse[currentApCourseIndex];
    ////    float waypointTransitDistanceSqrd = Vector3.SqrMagnitude(currentWaypoint.Position - Position);
    ////    D.Log(/*ShowDebugLog, */"{0}: first waypoint is {1}, {2:0.#} units away, in course with {3} waypoints reqd before final approach to Target {4}.",
    ////    DebugName, currentWaypoint.Position, Mathf.Sqrt(waypointTransitDistanceSqrd), apTgtCourseIndex - 1, apTgt.DebugName);

    ////    float waypointStandoffDistance = Constants.ZeroF;
    ////    if (currentApCourseIndex == apTgtCourseIndex) {
    ////        waypointStandoffDistance = _apMoveTgtStandoffDistance;
    ////    }
    ////    _fsmShipWaitForMoveCompletedCount = ElementCount;
    ////    __IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance); // All ships at this stage

    ////    ////int fleetTgtRecedingWaypointCount = Constants.Zero;
    ////    ////__RecordWaypointTransitStart(toCalcLastTransitDuration: false, lastTransitDistanceSqrd: waypointTransitDistanceSqrd);

    ////    ////IFleetNavigableDestination detour;
    ////    while (currentApCourseIndex <= apTgtCourseIndex) {
    ////        if (_fsmShipWaitForMoveCompletedCount == Constants.Zero) {
    ////            _fsmShipWaitForMoveCompletedCount = ElementCount;

    ////            ////__RecordWaypointTransitStart(toCalcLastTransitDuration: true, lastTransitDistanceSqrd: waypointTransitDistanceSqrd);

    ////            currentApCourseIndex++;
    ////            if (currentApCourseIndex == apTgtCourseIndex) {
    ////                waypointStandoffDistance = _apMoveTgtStandoffDistance;
    ////            }
    ////            else if (currentApCourseIndex > apTgtCourseIndex) {
    ////                continue;   // exit while
    ////            }
    ////            D.Log(/*ShowDebugLog,*/ "{0} has reached Waypoint_{1} {2}. Current destination is now Waypoint_{3} {4}.", Name,
    ////            currentApCourseIndex - 1, currentWaypoint.DebugName, currentApCourseIndex, _navigator.ApCourse[currentApCourseIndex].DebugName);

    ////            ////if (IsTargetPotentiallyUncatchable) {
    ////            ////    bool isUncatchable = __IsFleetTgtUncatchable(ref fleetTgtRecedingWaypointCount);
    ////            ////    if (isUncatchable) {
    ////            ////        HandleApTgtUncatchable();
    ////            ////    }
    ////            ////}

    ////            currentWaypoint = _navigator.ApCourse[currentApCourseIndex];
    ////            if (_navigator.__TryCheckForObstacleEnrouteTo(currentWaypoint, out detour)) {
    ////                // there is an obstacle en-route to the next waypoint, so use the detour provided instead
    ////                _navigator.__RefreshApCourse(CourseRefreshMode.AddWaypoint, ref currentApCourseIndex, detour);
    ////                currentWaypoint = detour;
    ////                apTgtCourseIndex = _navigator.ApCourse.Count - 1;
    ////            }
    ////            waypointTransitDistanceSqrd = Vector3.SqrMagnitude(currentWaypoint.Position - Position);

    ////            ////if (IsPathReplotNeeded) {
    ////            ////    ReplotPath();
    ////            ////}
    ////            ////else {
    ////            ////    IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);
    ////            ////}
    ////            __IssueMoveOrderToAllShips(currentWaypoint, waypointStandoffDistance);
    ////        }
    ////        yield return null;  // OPTIMIZE use WaitForHours, checking not currently expensive here
    ////                            // IMPROVE use ProgressCheckDistance to derive
    ////    }
    ////    // we've reached the target
    ////    D.LogBold("{0} has reached its ApTarget {1}.", DebugName, apTgt.DebugName);
    ////    Return();
    ////}

    ////void Moving_UponApCoursePlotSuccess() {
    ////    LogEvent();
    ////    _navigator.EngagePilot();
    ////}

    ////void Moving_UponApCoursePlotFailure() {
    ////    LogEvent();
    ////    var returnHandler = GetCurrentCalledStateReturnHandler();
    ////    returnHandler.ReturnCause = FsmCallReturnCause.TgtUnreachable;
    ////    Return();
    ////}

    ////void Moving_UponApTargetUnreachable() {
    ////    LogEvent();
    ////    var returnHandler = GetCurrentCalledStateReturnHandler();
    ////    returnHandler.ReturnCause = FsmCallReturnCause.TgtUnreachable;
    ////    Return();
    ////}

    ////void Moving_UponApTargetReached() {
    ////    LogEvent();
    ////    Return();
    ////}

    ////void Moving_UponApTargetUncatchable() {
    ////    LogEvent();
    ////    // 4.15.17 Occurs when FleetNavigator determines that the FleetTgt is getting progressively further away
    ////    D.Assert(_fsmTgt is IFleetCmd_Ltd);
    ////    var returnHandler = GetCurrentCalledStateReturnHandler();
    ////    returnHandler.ReturnCause = FsmCallReturnCause.TgtUncatchable;
    ////    Return();
    ////}

    ////void Moving_UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
    ////    LogEvent();
    ////}

    ////void Moving_UponSubordinateJoined(ShipItem subordinateShip) {
    ////    LogEvent();
    ////    if (subordinateShip.IsAuthorizedForNewOrder(ShipDirective.Move)) {
    ////        var shipTgt = _fsmTgt as IShipNavigableDestination; // FIXME this is direct to fleet target, not current leg of course
    ////        ShipMoveOrder order = new ShipMoveOrder(CurrentOrder.Source, shipTgt, Speed.Standard, isFleetwide: false, targetStandoffDistance: Constants.ZeroF);
    ////        D.LogBold("{0} is issuing {1} to {2} after being joined during State {3}.", DebugName, order.DebugName, subordinateShip.DebugName,
    ////            CurrentState.GetValueName());
    ////        subordinateShip.CurrentOrder = order;
    ////    }
    ////}

    ////void Moving_UponOrderOutcomeCallback(ShipItem ship, bool isSuccess, IShipNavigableDestination target, OrderOutcome failCause) {
    ////    // 12.9.17 Received warning from ExecuteExploreOrder state. 12.10.17 Solved -> Caused by assignment of ship 
    ////    // to explore newly discovered planet before ExecuteExploreOrder_EnterState began execution.
    ////    D.Warn("{0}.Moving_UponOrderOutcomeCallback() received. LastState = {1}, Ship = {2}, FailCause = {3}, ShipCurrentOrder = {4}.",
    ////        DebugName, LastState.GetValueName(), ship.DebugName, failCause.GetValueName(), ship.CurrentOrder.DebugName);
    ////}

    ////void Moving_UponAlertStatusChanged() {
    ////    LogEvent();
    ////}

    ////void Moving_UponHQElementChanged() {
    ////    LogEvent();
    ////    // 4.15.17 Affected ships already RestartState to force proxy offset update
    ////}

    ////void Moving_UponEnemyDetected() {
    ////    LogEvent();
    ////    // TODO determine state that Call()ed => LastState and go intercept if applicable
    ////    ////Return();
    ////}

    ////void Moving_UponUnitDamageIncurred() {
    ////    LogEvent();
    ////    if (AssessNeedForRepair(HealthThreshold_BadlyDamaged)) {
    ////        var returnHandler = GetCurrentCalledStateReturnHandler();
    ////        returnHandler.ReturnCause = FsmCallReturnCause.NeedsRepair;
    ////        Return();
    ////    }
    ////}

    ////void Moving_UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
    ////    LogEvent();
    ////    D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
    ////    //D.Log("{0}.Moving received an InfoAccessChgd event for {1}. Frame: {2}.", DebugName, fsmTgt.DebugName, Time.frameCount);

    ////    if (AssessReturnFromTgtInfoAccessChange()) {
    ////        var returnHandler = GetCurrentCalledStateReturnHandler();
    ////        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
    ////        Return();
    ////    }
    ////}

    ////void Moving_UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
    ////    LogEvent();
    ////    D.AssertEqual(_fsmTgt, fsmTgt as IFleetNavigableDestination);
    ////    //D.Log("{0}.Moving received an OwnerChgd event for {1}. Frame: {2}.", DebugName, fsmTgt.DebugName, Time.frameCount);

    ////    if (AssessReturnFromTgtOwnerChange()) {
    ////        var returnHandler = GetCurrentCalledStateReturnHandler();
    ////        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
    ////        Return();
    ////    }
    ////}

    ////void Moving_UponRelationsChangedWith(Player player) {
    ////    LogEvent();
    ////    //D.Log("{0}.Moving received an RelationsChgd event with {1}. Frame: {2}.", DebugName, player.DebugName, Time.frameCount);

    ////    if (AssessReturnFromRelationsChangeWith(player)) {
    ////        var returnHandler = GetCurrentCalledStateReturnHandler();
    ////        returnHandler.ReturnCause = FsmCallReturnCause.TgtRelationship;
    ////        Return();
    ////    }
    ////}

    ////void Moving_UponAwarenessChgd(IOwnerItem_Ltd item) {
    ////    LogEvent();
    ////    // 4.20.17 item can't be IFleetExplorable (Sector, System, UCenter) as awareness doesn't change
    ////    if (item == _fsmTgt) {
    ////        D.Assert(item is IFleetCmd_Ltd);
    ////        D.Assert(!OwnerAIMgr.HasKnowledgeOf(item)); // can't become newly aware of a fleet we are moving too without first losing awareness
    ////        // our move target is the fleet we've lost awareness of
    ////        var returnHandler = GetCurrentCalledStateReturnHandler();
    ////        returnHandler.ReturnCause = FsmCallReturnCause.TgtUncatchable;
    ////        Return();
    ////    }
    ////}

    ////void Moving_UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
    ////    LogEvent();
    ////    if (_fsmTgt is StationaryLocation) {
    ////        D.Assert(deadFsmTgt is IPatrollable || deadFsmTgt is IGuardable);
    ////    }
    ////    else {
    ////        if (_fsmTgt != deadFsmTgt) {
    ////            D.Error("{0}.target {1} is not dead target {2}.", DebugName, _fsmTgt.DebugName, deadFsmTgt.DebugName);
    ////        }
    ////    }
    ////    var returnHandler = GetCurrentCalledStateReturnHandler();
    ////    returnHandler.ReturnCause = FsmCallReturnCause.TgtDeath;
    ////    Return();
    ////}

    ////[Obsolete("Not currently used")]
    ////void Moving_UponSubordinateDeath(AUnitElementItem deadSubordinateElement) {
    ////    LogEvent();
    ////}

    ////void Moving_ExitState() {
    ////    LogEvent();
    ////    _apMoveSpeed = Speed.None;
    ////    _apMoveTgtStandoffDistance = Constants.ZeroF;
    ////    _navigator.DisengagePilot();

    ////    if (LastState == FleetState.ExecuteExploreOrder) {  // OPTIMIZE 4.16.17 BUG detection 
    ////        if (!(_fsmTgt as IFleetExplorable).IsExploringAllowedBy(Owner)) {
    ////            D.Assert((_fsmTgt as IFleetExplorable).IsOwnerAccessibleTo(Owner), "No longer allowed to explore but owner not accessible???");
    ////            // I know that failure causes like TgtRelationship will be handled properly by ExecuteExploreOrder so no need to warn
    ////            if (__GetCalledStateReturnHandlerFor(FleetState.Moving.GetValueName()).ReturnCause == FsmCallReturnCause.None) {
    ////                var targetOwner = (_fsmTgt as IOwnerItem).Owner;
    ////                D.Assert(Owner.IsKnown(targetOwner), "{0}: {1} Owner is accessible but I don't know them???".Inject(DebugName, _fsmTgt.DebugName));
    ////                D.Warn(@"{0} exiting Moving state with ExploreTgt {1} no longer explorable without a failure cause. CurrentFrame = {2}, 
    ////                    DistanceToExploreTgt = {3:0.}, HQ_SRSensorRange = {4:0.}, TargetOwner = {5}, Relationship = {6}, 
    ////                    DistanceToSettlement = {7:0.}, MRSensorsOperational = {8}.",
    ////                    DebugName, _fsmTgt.DebugName, Time.frameCount, Vector3.Distance(_fsmTgt.Position, Position),
    ////                    HQElement.SRSensorMonitor.RangeDistance, targetOwner, targetOwner.GetCurrentRelations(Owner).GetValueName(),
    ////                    Vector3.Distance((_fsmTgt as ISystem).Settlement.Position, Position), MRSensorMonitor.IsOperational);
    ////            }
    ////        }
    ////    }
    ////}

    #endregion

    #region Patrolling using Move state Archive

    //// 7.2.16 Call()ed State

    //private IPatrollable _patrolledTgt; // reqd to unsubscribe from death notification as _fsmTgt is a StationaryLocation

    //// Note: This state exists to differentiate between the Moving Call() from ExecutePatrolOrder which gets the
    //// fleet to the patrol target, and the continuous Moving Call()s from Patrolling which moves the fleet between
    //// the patrol target's PatrolStations. This distinction is important while Moving when an enemy is detected as
    //// the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    //// to be ignored, whereas detecting an enemy while actually patrolling the target is likely to result in an intercept.

    //IEnumerator Patrolling_EnterState() {
    //    LogEvent();
    //    D.Assert(_patrolledTgt == null);
    //    _patrolledTgt = _apTgt as IPatrollable;
    //    D.Assert(_patrolledTgt != null);    // the _fsmTgt starts out as IPatrollable

    //    // _fsmTgt will be a StationaryLocation while patrolling so we must wire the death here
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: true);

    //    var patrolStations = _patrolledTgt.PatrolStations;  // IPatrollable.PatrolStations is a copied list
    //    StationaryLocation nextPatrolStation = GameUtility.GetClosest(Position, patrolStations);
    //    bool isRemoved = patrolStations.Remove(nextPatrolStation);
    //    D.Assert(isRemoved);
    //    var shuffledPatrolStations = patrolStations.Shuffle();
    //    var patrolStationQueue = new Queue<StationaryLocation>(shuffledPatrolStations);
    //    patrolStationQueue.Enqueue(nextPatrolStation);   // shuffled queue with current patrol station at end
    //    Speed patrolSpeed = _patrolledTgt.PatrolSpeed;
    //    while (true) {
    //        _apTgt = nextPatrolStation;
    //        _apMoveSpeed = patrolSpeed;    // _fsmMoveSpeed set to None when exiting FleetState.Moving
    //        _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't patrol a target owned by an enemy
    //        Call(FleetState.Moving);
    //        yield return null;    // required so Return()s here

    //        if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    //            // there was a failure in Moving so pass it to the Call()ing state
    //            Return();
    //            yield return null;
    //        }

    //        if (!__ValidatePatrol(_patrolledTgt)) {
    //            StationaryLocation closestLocalAssyStation = GameUtility.GetClosest(Position, _patrolledTgt.LocalAssemblyStations);
    //            CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, closestLocalAssyStation);
    //            yield return null;
    //        }

    //        nextPatrolStation = patrolStationQueue.Dequeue();
    //        patrolStationQueue.Enqueue(nextPatrolStation);
    //    }
    //}

    //void Patrolling_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void Patrolling_UponEnemyDetected() {
    //    LogEvent();
    //}

    //void Patrolling_UponFsmTargetDeath(IMortalItem deadFsmTarget) {
    //    LogEvent();
    //    D.Assert(_patrolledTgt == deadFsmTarget, "{0}.target {1} is not dead target {2}.", DebugName, _patrolledTgt.DebugName, deadFsmTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void Patrolling_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void Patrolling_ExitState() {
    //    LogEvent();
    //    _apTgt = _patrolledTgt as IFleetNavigableDestination;
    //    _patrolledTgt = null;

    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: false);
    //}

    #endregion

    #region Guarding using Move state Archive

    //// 7.2.16 Call()ed State

    //private IGuardable _guardedTgt; // reqd to unsubscribe from death notification as _fsmTgt is a StationaryLocation

    //// Note: This state exists to differentiate between the Moving Call() from ExecuteGuardOrder which gets the
    //// fleet to the guard target, and the state of actually guarding at a guarded target's GuardStation.
    //// This distinction is important when an enemy is detected as
    //// the behaviour that results is likely to be different -> detecting an enemy when moving to the target is likely
    //// to be ignored, whereas detecting an enemy while actually guarding the target is likely to result in an intercept.

    //IEnumerator Guarding_EnterState() {
    //    LogEvent();
    //    D.Assert(_guardedTgt == null);
    //    _guardedTgt = _apTgt as IGuardable;
    //    D.Assert(_guardedTgt != null);    // the _fsmTgt starts out as IGuardable

    //    // _fsmTgt will be a StationaryLocation while guarding so we must wire the death here
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: true);

    //    // now move to the GuardStation
    //    _apTgt = GameUtility.GetClosest(Position, _guardedTgt.GuardStations);
    //    _apMoveSpeed = Speed.Standard;
    //    _apMoveTgtStandoffDistance = Constants.ZeroF;    // can't guard a target owned by an enemy
    //    Call(FleetState.Moving);
    //    yield return null;  // required so Return()s here

    //    if (_orderFailureCause != UnitItemOrderFailureCause.None) {
    //        // there was a failure in Moving so pass it to the Call()ing state
    //        Return();
    //        yield return null;
    //    }

    //    Call(FleetState.AssumingFormation); // avoids permanently leaving Guarding state
    //    yield return null; // required so Return()s here

    //    // Fleet stays in Guarding state, waiting to respond to UponEnemyDetected(), Ship is simply Idling
    //}

    //void Guarding_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void Guarding_UponEnemyDetected() {
    //    LogEvent();
    //}

    //void Guarding_UponFsmTargetDeath(IMortalItem deadFsmTarget) {
    //    LogEvent();
    //    D.Assert(_guardedTgt == deadFsmTarget, "{0}.target {1} is not dead target {2}.", DebugName, _guardedTgt.DebugName, deadFsmTarget.DebugName);
    //    _orderFailureCause = UnitItemOrderFailureCause.TgtDeath;
    //    Return();
    //}

    //void Guarding_UponDeath() {
    //    LogEvent();
    //    _orderFailureCause = UnitItemOrderFailureCause.UnitItemDeath;
    //    Return();
    //}

    //void Guarding_ExitState() {
    //    LogEvent();
    //    _apTgt = _guardedTgt as IFleetNavigableDestination;
    //    _guardedTgt = null;
    //    AttemptFsmTgtDeathSubscribeChange(_apTgt, toSubscribe: false);
    //}

    #endregion

    #region ExecuteCloseOrbitOrder Archive

    //IEnumerator ExecuteCloseOrbitOrder_EnterState() {
    //    LogEvent();

    //    if (_fsmTgt != null) {
    //        D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
    //    }

    //    var orbitTgt = CurrentOrder.Target as IShipCloseOrbitable;
    //    D.Assert(orbitTgt != null);
    //    if (!__ValidateOrbit(orbitTgt)) {
    //        // no need for a assumeFormationTgt as we haven't moved to the orbitTgt yet
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff);
    //        yield return null;
    //    }

    //    _apMoveSpeed = Speed.Standard;
    //    _fsmTgt = orbitTgt as IFleetNavigableDestination;
    //    _apTgtStandoffDistance = Constants.ZeroF;    // can't go into close orbit around an enemy
    //    Call(FleetState.Moving);
    //    yield return null;  // reqd so Return()s here

    //    D.Assert(!_fsmApMoveTgtUnreachable, "{0} ExecuteCloseOrbitOrder target {1} should always be reachable.", DebugName, _fsmTgt.DebugName);
    //    if (CheckForDeathOf(_fsmTgt)) {
    //        HandleApMoveTgtDeath(_fsmTgt);
    //        yield return null;
    //    }

    //    if (!__ValidateOrbit(orbitTgt)) {
    //        StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, assumeFormationTgt);
    //        yield return null;
    //    }

    //    Call(FleetState.AssumingCloseOrbit);
    //    yield return null;  // reqd so Return()s here

    //    CurrentState = FleetState.Idling;
    //}

    ///// <summary>
    ///// Checks the continued validity of the current orbit order of target and warns
    ///// if no longer valid. If no longer valid, returns false whereon the fleet should take an action
    ///// reflecting that the order it was trying to execute is no longer valid.
    ///// <remarks>Check is necessary every time there is another decision to make while executing the order as
    ///// 1) the diplomatic state between the owners can change.</remarks>
    ///// </summary>
    ///// <param name="orbitTgt">The orbit TGT.</param>
    //private bool __ValidateOrbit(IShipCloseOrbitable orbitTgt) {
    //    bool isValid = true;
    //    if (!orbitTgt.IsCloseOrbitAllowedBy(Owner)) {
    //        D.Warn("{0} Orbit order of {1} is no longer valid. Diplomatic state with Owner {2} must have changed and is now {3}.",
    //            DebugName, orbitTgt.DebugName, orbitTgt.Owner.LeaderName, Owner.GetRelations(orbitTgt.Owner).GetValueName());
    //        isValid = false;
    //    }
    //    return isValid;
    //}

    //void ExecuteCloseOrbitOrder_ExitState() {
    //    LogEvent();
    //    _fsmTgt = null;
    //    _fsmApMoveTgtUnreachable = false;
    //}

    #endregion

    #region AssumingCloseOrbit Archive

    //private int _fsmShipCountWaitingToOrbit;

    //void AssumingCloseOrbit_EnterState() {
    //    LogEvent();
    //    D.Assert(_fsmShipCountWaitingToOrbit == Constants.Zero);
    //    _fsmShipCountWaitingToOrbit = Elements.Count;
    //    IShipCloseOrbitable orbitTgt = _fsmTgt as IShipCloseOrbitable;

    //    var shipAssumeCloseOrbitOrder = new ShipOrder(ShipDirective.AssumeCloseOrbit, CurrentOrder.Source, orbitTgt);
    //    Elements.ForAll(e => {
    //        var ship = e as ShipItem;
    //        D.Log(ShowDebugLog, "{0} issuing {1} order to {2}.", DebugName, ShipDirective.AssumeCloseOrbit.GetValueName(), ship.DebugName);
    //        ship.CurrentOrder = shipAssumeCloseOrbitOrder;
    //    });
    //}

    //void AssumingCloseOrbit_UponShipOrbitAttemptFinished(ShipItem ship, bool isOrbitAttemptSuccessful) {
    //    if (isOrbitAttemptSuccessful) {
    //        _fsmShipCountWaitingToOrbit--;
    //        if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
    //            Return();
    //        }
    //    }
    //    else {
    //        // a ship's orbit attempt failed so ships are no longer allowed to orbit the orbitTgt
    //        IShipCloseOrbitable orbitTgt = _fsmTgt as IShipCloseOrbitable;
    //        StationaryLocation assumeFormationTgt = GameUtility.GetClosest(Position, orbitTgt.LocalAssemblyStations);
    //        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, CurrentOrder.Source, assumeFormationTgt);
    //    }
    //}

    //void AssumingCloseOrbit_UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
    //    _fsmShipCountWaitingToOrbit--;
    //    if (_fsmShipCountWaitingToOrbit == Constants.Zero) {
    //        Return();
    //    }
    //}

    //void AssumingCloseOrbit_UponNewOrderReceived() {
    //    LogEvent();
    //    Return();
    //}

    //void AssumingCloseOrbit_ExitState() {
    //    LogEvent();
    //    _fsmShipCountWaitingToOrbit = Constants.Zero;
    //}

    #endregion

    #endregion

    #region StateMachine Support Members

    /// <summary>
    /// The target the FSM uses to communicate between Call()ing and Call()ed states. 
    /// Valid during most Call()ed states (excluding AssumingFormation), and during the states that Call()ed them until nulled by that state.
    /// The ExecuteXXXState that sets this value during its UponPreconfigureState() is responsible for nulling it during its ExitState().
    /// </summary>Moving
    private IFleetNavigableDestination _fsmTgt;

    /// <summary>
    /// The ships expected to callback with an order outcome.
    /// <remarks>Used where FleetMoveHelper is not handling fleet move-related order outcome callbacks.</remarks>
    /// </summary>
    private HashSet<ShipItem> _fsmShipsExpectedToCallbackWithOrderOutcome = new HashSet<ShipItem>();

    /// <summary>
    /// Removes the specified ship from any collections used by the StateMachine.
    /// <remarks>Intended for use when the ship is removed from the Cmd. In most circumstances, 
    /// when a ship is removed from Cmd while it is executing Cmd's order, it will already have provided 
    /// Cmd with its OrderOutcome, and will therefore already be removed from these collections. 
    /// </summary>
    /// <param name="ship">The ship.</param>
    private void RemoveFromStateMachine(ShipItem ship) {
        _fsmShipsExpectedToCallbackWithOrderOutcome.Remove(ship);
        _moveHelper.__ValidateNotPresent(ship);
    }

    /// <summary>
    /// Convenience method that has the CmdStaff issue an AssumeFormation order to all ships.
    /// </summary>
    /// <param name="target">The target.</param>
    private void IssueCmdStaffsAssumeFormationOrder(IFleetNavigableDestination target = null) {
        if (Availability == NewOrderAvailability.Unavailable) {
            ChangeAvailabilityTo(NewOrderAvailability.BarelyAvailable);
        }
        CurrentOrder = new FleetOrder(FleetDirective.AssumeFormation, OrderSource.CmdStaff, target);
    }

    [System.Obsolete("Not currently used")]
    private void IssueCmdStaffsRegroupOrder(IFleetNavigableDestination destination) {
        CurrentOrder = new FleetOrder(FleetDirective.Regroup, OrderSource.CmdStaff, destination);
    }

    #region FsmReturnHandler System

    /// <summary>
    /// Lookup table for FsmReturnHandlers keyed by the state Call()ed and the state Return()ed too.
    /// </summary>
    private IDictionary<FleetState, IDictionary<FleetState, FsmReturnHandler>> _fsmReturnHandlerLookup
        = new Dictionary<FleetState, IDictionary<FleetState, FsmReturnHandler>>();

    /// <summary>
    /// Returns the cleared FsmReturnHandler associated with the provided states, 
    /// recording it onto the stack of _activeFsmReturnHandlers.
    /// <remarks>This version is intended for initial use when about to Call() a CallableState.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetInactiveReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FleetState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FleetState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        handler.Clear();
        _activeFsmReturnHandlers.Push(handler);
        return handler;
    }

    /// <summary>
    /// Returns the uncleared and already recorded FsmReturnHandler associated with the provided states. 
    /// <remarks>This version is intended for use in Return()ed states after the CallableState that it
    /// was used to Call() has Return()ed to the state that Call()ed it.</remarks>
    /// </summary>
    /// <param name="calledState">The Call()ed state.</param>
    /// <param name="returnedState">The state Return()ed too.</param>
    /// <returns></returns>
    private FsmReturnHandler GetActiveReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsCallableState(calledState));   // Can't validate returnedState as not Call()able due to nested Call()ed states
        IDictionary<FleetState, FsmReturnHandler> returnedStateLookup;
        if (!_fsmReturnHandlerLookup.TryGetValue(calledState, out returnedStateLookup)) {
            returnedStateLookup = new Dictionary<FleetState, FsmReturnHandler>();
            _fsmReturnHandlerLookup.Add(calledState, returnedStateLookup);
        }

        FsmReturnHandler handler;
        if (!returnedStateLookup.TryGetValue(returnedState, out handler)) {
            handler = CreateFsmReturnHandlerFor(calledState, returnedState);
            returnedStateLookup.Add(returnedState, handler);
        }
        return handler;
    }

    private FsmReturnHandler CreateFsmReturnHandlerFor(FleetState calledState, FleetState returnedState) {
        D.Assert(IsCallableState(calledState));

        if (calledState == FleetState.AssumingFormation) {
            if (returnedState == FleetState.ExecuteAssumeFormationOrder) {
                return CreateFsmReturnHandler_AssumingFormationToAssumeFormation();
            }
            if (returnedState == FleetState.ExecuteMoveOrder) {
                return CreateFsmReturnHandler_AssumingFormationToMove();
            }
            if (returnedState == FleetState.Guarding) {
                return CreateFsmReturnHandler_AssumingFormationToGuarding();
            }
            //if (returnedState == FleetState.ExecuteRegroupOrder) {
            //    return CreateFsmReturnHandler_AssumingFormationToRegroup();
            //}
        }

        if (calledState == FleetState.Exploring && returnedState == FleetState.ExecuteExploreOrder) {
            return CreateFsmReturnHandler_ExploringToExplore();
        }

        if (calledState == FleetState.Attacking && returnedState == FleetState.ExecuteAttackOrder) {
            return CreateFsmReturnHandler_AttackingToAttack();
        }
        if (calledState == FleetState.Repairing && returnedState == FleetState.ExecuteRepairOrder) {
            return CreateFsmReturnHandler_RepairingToRepair();
        }
        if (calledState == FleetState.Patrolling && returnedState == FleetState.ExecutePatrolOrder) {
            return CreateFsmReturnHandler_PatrollingToPatrol();
        }
        if (calledState == FleetState.Guarding && returnedState == FleetState.ExecuteGuardOrder) {
            return CreateFsmReturnHandler_GuardingToGuard();
        }
        if (calledState == FleetState.JoiningHanger && returnedState == FleetState.ExecuteJoinHangerOrder) {
            return CreateFsmReturnHandler_JoiningHangerToJoinHanger();
        }
        if (calledState == FleetState.Refitting && returnedState == FleetState.ExecuteRefitOrder) {
            return CreateFsmReturnHandler_RefittingToRefit();
        }
        if (calledState == FleetState.Disbanding && returnedState == FleetState.ExecuteDisbandOrder) {
            return CreateFsmReturnHandler_DisbandingToDisband();
        }
        if (calledState == FleetState.FoundingSettlement && returnedState == FleetState.ExecuteFoundSettlementOrder) {
            return CreateFsmReturnHandler_FoundingToFoundSettlement();
        }
        if (calledState == FleetState.FoundingStarbase && returnedState == FleetState.ExecuteFoundStarbaseOrder) {
            return CreateFsmReturnHandler_FoundingToFoundStarbase();
        }

        D.Error("{0}: No {1} found for CalledState {2} and ReturnedState {3}.",
            DebugName, typeof(FsmReturnHandler).Name, calledState.GetValueName(), returnedState.GetValueName());
        return null;
    }

    #endregion

    #region Order Outcome Callback System

    private void HandleSubordinateOrderOutcome(ShipItem ship, IElementNavigableDestination target, OrderOutcome outcome, Guid cmdOrderID) {
        D.AssertNotDefault(cmdOrderID);
        bool toPassCallback = _executingOrderID == cmdOrderID;
        if (outcome == OrderOutcome.Death) {
            if (!toPassCallback) {
                D.Warn("{0} is not passing a DEATH callback from {1} through to CurrentState {2}.", DebugName, ship.DebugName, CurrentState.GetValueName());
            }
        }
        if (toPassCallback) {
            // callback is intended for current state(s) executing the current order
            UponOrderOutcomeCallback(ship, target as IShipNavigableDestination, outcome);
        }
    }

    #endregion

    protected override void ResetAndValidateCommonCallableExitStateValues() {
        base.ResetAndValidateCommonCallableExitStateValues();
        _fsmShipsExpectedToCallbackWithOrderOutcome.Clear();
        __ValidateMoveHelperState(errorMsg: "exiting State {0}".Inject(LastState.GetValueName()));
    }

    protected override void ResetAndValidateCommonNonCallableExitStateValues() {
        base.ResetAndValidateCommonNonCallableExitStateValues();
        _moveHelper.DisengagePilot();
        _fsmTgt = null;
        _fsmShipsExpectedToCallbackWithOrderOutcome.Clear();
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        if (CurrentState == FleetState.Dead) {   // TEMP avoids 'method not found' warning spam
            UponEffectSequenceFinished(effectID);
        }
    }

    #region Relays

    internal void UponApCoursePlotSuccess() { RelayToCurrentState(); }

    internal void UponApTargetReached() { RelayToCurrentState(); }

    internal void UponApTgtUncatchable() { RelayToCurrentState(); }

    [Obsolete("1.6.18 Not used until there is something to do besides throw an error")]
    internal void UponApFailure(FleetMoveHelper.FleetMoveFailureMode failMode) { RelayToCurrentState(failMode); }

    private void UponOrderOutcomeCallback(ShipItem ship, IShipNavigableDestination target, OrderOutcome outcome) {
        RelayToCurrentState(ship, target, outcome);
    }

    /// <summary>
    /// Called when a ship 'joins' a fleet or when a ship changes its status in some way
    /// that 
    /// </summary>
    /// <param name="subordinateShip">The subordinate ship.</param>
    internal void UponSubordinateJoined(ShipItem subordinateShip) { RelayToCurrentState(subordinateShip); }

    #endregion

    #region Repair Support

    protected override bool AssessNeedForRepair(float unitHealthThreshold = Constants.OneHundredPercent) {
        bool isNeedForRepair = base.AssessNeedForRepair(unitHealthThreshold);
        if (isNeedForRepair) {
            // We don't want to reassess if there is a follow-on order to repair
            if (CurrentOrder != null) {
                FleetOrder followonOrder = CurrentOrder.FollowonOrder;
                if (followonOrder != null && followonOrder.Directive == FleetDirective.Repair) {
                    // Repair is already in the works
                    isNeedForRepair = false;
                }
            }
        }
        return isNeedForRepair;
    }

    /// <summary>
    /// Attempts to issue a repair order from the CmdStaff, returning <c>true</c> if successful, <c>false</c> otherwise.
    /// If successful, repair target will be either
    /// 1) IRepairCabable planet, 2) IRepairCapable base or 3) this Command indicating repair in place.
    /// <remarks>The only reason it can't issue a repair order is if the order is not authorized.</remarks>
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use AssessNeedForRepair and IssueCmdStaffsRepairOrder instead")]
    private bool AttemptToIssueCmdStaffsRepairOrder() {
        // 4.14.17 Removed Assert not allowing call from Repair states to allow finding another destination
        string failCause;
        if (__TryAuthorizeNewOrder(FleetDirective.Repair, out failCause)) {
            IssueCmdStaffsRepairOrder();
            return true;
        }
        D.Warn("FYI. {0} could not issue a RepairOrder. FailCause: {1}.", DebugName, failCause);
        return false;
    }

    /// <summary>
    /// Issues a repair order from the CmdStaff. Repair target will be either
    /// 1) IShipRepairCapable planet, 2) IShipRepairCapable base or 3) this Command indicating repair in place.
    /// </summary>
    private void IssueCmdStaffsRepairOrder() {
        // 4.14.17 Removed Assert not allowing call from Repair states to allow finding another destination
        IFleetNavigableDestination unitRepairDest;
        IShipRepairCapable itemRepairDest;  // planet or base
        if (TryGetRepairDestination(out itemRepairDest)) {
            unitRepairDest = itemRepairDest as IFleetNavigableDestination;
        }
        else {
            // RepairInPlace
            unitRepairDest = this;
        }

        FleetOrder repairOrder = new FleetOrder(FleetDirective.Repair, OrderSource.CmdStaff, unitRepairDest);
        CurrentOrder = repairOrder;
    }

    /// <summary>
    /// Returns <c>true</c> if finds a IShipRepairCapable repair destination, aka a &gt;= neutral
    /// base or planet, <c>false</c> otherwise. If false, the fleet should repair in place.
    /// <remarks>Bases are prioritized first, then planets.</remarks>
    /// </summary>
    /// <param name="unitRepairDest">The unit repair destination.</param>
    /// <returns></returns>
    private bool TryGetRepairDestination(out IShipRepairCapable unitRepairDest) {
        unitRepairDest = null;
        IUnitBaseCmd_Ltd closestBase;
        if (OwnerAiMgr.TryFindClosestFleetRepairBase(Position, out closestBase)) {
            unitRepairDest = closestBase as IShipRepairCapable;
        }
        else {
            IPlanet_Ltd closestPlanet;
            if (OwnerAiMgr.TryFindClosestFleetRepairPlanet(Position, out closestPlanet)) {
                unitRepairDest = closestPlanet as IShipRepairCapable;
            }
        }
        return unitRepairDest != null;
    }

    #endregion

    /// <summary>
    /// Tries to get ships from the fleet using the criteria provided. Returns <c>false</c> if no ships
    /// that meet the minAvailability criteria can be returned, otherwise returns <c>true</c> with ships
    /// containing up to desiredQty. If the ships that can be returned are not sufficient to meet desiredQty,
    /// non priority category ships and then the HQElement will be included in that order.
    /// </summary>
    /// <param name="ships">The returned ships.</param>
    /// <param name="minAvailability">The minimum availability.</param>
    /// <param name="avoidHQ">if set to <c>true</c> the ships returned will attempt to avoid including the HQ ship
    /// if it can meet the other criteria.</param>
    /// <param name="desiredQty">The desired qty.</param>
    /// <param name="priorityCats">The categories to emphasize in priority order.</param>
    /// <returns></returns>
    private bool TryGetShips(out IList<ShipItem> ships, NewOrderAvailability minAvailability, bool avoidHQ, int desiredQty,
        params ShipHullCategory[] priorityCats) {
        D.AssertNotEqual(Constants.Zero, desiredQty);
        ships = null;
        IEnumerable<AUnitElementItem> candidates = GetElements(minAvailability);
        int candidateCount = candidates.Count();
        if (candidateCount == Constants.Zero) {
            return false;
        }
        if (candidateCount <= desiredQty) {
            ships = new List<ShipItem>(candidates.Cast<ShipItem>());
            return true;
        }
        // more candidates than required
        if (avoidHQ) {
            candidates = candidates.Except(HQElement);
            candidateCount--;
        }
        if (candidateCount == desiredQty) {
            ships = new List<ShipItem>(candidates.Cast<ShipItem>());
            return true;
        }
        // more candidates after eliminating HQ than required
        if (priorityCats.IsNullOrEmpty()) {
            ships = new List<ShipItem>(candidates.Take(desiredQty).Cast<ShipItem>());
            return true;
        }
        List<ShipItem> priorityCandidates = new List<ShipItem>(desiredQty);
        int priorityCatIndex = 0;
        while (priorityCatIndex < priorityCats.Count()) {
            var priorityCatCandidates = candidates.Cast<ShipItem>().Where(ship => ship.HullCategory == priorityCats[priorityCatIndex]);
            priorityCandidates.AddRange(priorityCatCandidates);
            if (priorityCandidates.Count >= desiredQty) {
                ships = priorityCandidates.Take(desiredQty).ToList();
                return true;
            }
            priorityCatIndex++;
        }
        // all priority category ships are included but we still need more
        var remainingNonHQNonPriorityCatCandidates = candidates.Cast<ShipItem>().Except(priorityCandidates);
        priorityCandidates.AddRange(remainingNonHQNonPriorityCatCandidates);
        if (priorityCandidates.Count < desiredQty) {
            priorityCandidates.Add(HQElement);
        }

        ships = priorityCandidates.Count > desiredQty ? priorityCandidates.Take(desiredQty).ToList() : priorityCandidates;
        return true;
    }

    /// <summary>
    /// Returns ships from the fleet using the criteria provided. If no ships meet the desiredMinAvailability criteria, 
    /// the criteria is incrementally relaxed until one or more ships meet the criteria. If the ships that meet the [relaxed] 
    /// availability criteria are not sufficient to meet desiredQty, non priority category ships and then the HQElement 
    /// will be included in that order.
    /// <remarks>Throws an exception if no ships meet the relaxed criteria.</remarks>
    /// </summary>
    /// <param name="desiredMinAvailability">The desired minimum availability.</param>
    /// <param name="avoidHQ">if set to <c>true</c> the ships returned will attempt to avoid including the HQ ship
    /// if it can meet the other criteria.</param>
    /// <param name="desiredQty">The desired qty.</param>
    /// <param name="priorityCats">The categories to emphasize in priority order.</param>
    /// <returns></returns>
    /// <exception cref="System.IndexOutOfRangeException"></exception>
    private IList<ShipItem> GetShips(NewOrderAvailability desiredMinAvailability, bool avoidHQ, int desiredQty, params ShipHullCategory[] priorityCats) {
        IList<ShipItem> ships;
        if (TryGetShips(out ships, desiredMinAvailability, avoidHQ, desiredQty, priorityCats)) {
            return ships;
        }

        //D.Log("{0} is initiating iterative process to acquire ships.", DebugName);
        NewOrderAvailability availabilityToCheck = desiredMinAvailability;
        NewOrderAvailability lessRestrictiveAvailability;
        while (availabilityToCheck.TryGetLessRestrictiveAvailability(out lessRestrictiveAvailability)) {
            //D.Log("{0} is checking for ships with Availability >= {1}.", DebugName, lessRestrictiveAvailability.GetValueName());
            if (TryGetShips(out ships, lessRestrictiveAvailability, avoidHQ, desiredQty, priorityCats)) {
                D.Log("{0} had to reduce Availability restriction from {1} to {2} to acquire ships.",
                    DebugName, desiredMinAvailability.GetValueName(), lessRestrictiveAvailability.GetValueName());
                var shipOrdersAboutToBeInterrupted = ships.Select(s => s.CurrentOrder).Where(co => co != null).Select(co => co.DebugName).Concatenate();
                D.Log("{0}: CurrentState: {1}. Orders about to be interrupted = {2}.", DebugName, CurrentState.GetValueName(), shipOrdersAboutToBeInterrupted);
                return ships;
            }
            availabilityToCheck = lessRestrictiveAvailability;
        }
        throw new IndexOutOfRangeException("Should never get here as this indicates there are no ships in this fleet.");
    }

    #endregion

    #region Wait for Fleet to Align Service

    /// <summary>
    /// Waits for the ships in the fleet to align with the requested heading, then executes the provided callback.
    /// <remarks>
    /// Called by each of the ships in the fleet when they are preparing for collective departure to a destination
    /// ordered by FleetCmd. This single coroutine replaces a similar coroutine previously run by each ship.
    /// </remarks>
    /// </summary>
    /// <param name="fleetIsAlignedCallback">The fleet is aligned callback.</param>
    /// <param name="ship">The ship.</param>
    public void WaitForFleetToAlign(Action fleetIsAlignedCallback, IShip ship) {
        D.AssertNotNull(fleetIsAlignedCallback);
        _moveHelper.WaitForFleetToAlign(fleetIsAlignedCallback, ship);
    }

    /// <summary>
    /// Removes the 'fleet is now aligned' callback a ship may have requested by providing the ship's
    /// delegate that registered the callback.
    /// </summary>
    /// <param name="shipCallbackDelegate">The callback delegate from the ship.</param>
    /// <param name="ship">The ship.</param>
    public void RemoveFleetIsAlignedCallback(Action shipCallbackDelegate, IShip ship) {
        D.AssertNotNull(shipCallbackDelegate);
        _moveHelper.RemoveFleetIsAlignedCallback(shipCallbackDelegate, ship);
    }

    #endregion

    #endregion

    #region Cleanup

    protected override void DestroyApplicableParents(float delayInHours = Constants.ZeroF) {
        GameUtility.Destroy(UnitContainer.gameObject, delayInHours);
    }

    private void CleanupNavigator() {
        if (_moveHelper != null) {
            // a preset fleet that begins ops during runtime won't build its navigator until time for deployment
            _moveHelper.Dispose();
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _gameMgr.isPausedChanged -= CurrentOrderChangedWhilePausedUponResumeEventHandler;
    }

    protected override void Cleanup() {
        base.Cleanup();
        CleanupNavigator();
        __CleanupShowVelocityRay();
        __CleanupShowCoursePlot();
    }

    #endregion

    #region Debug

    protected override bool __IsLastStateCalled { get { return IsCallableState(LastState); } }

    /// <summary>
    /// Returns <c>true</c> if finds a IShipRepairCapable repair destination, aka a &gt;= neutral
    /// base or planet, <c>false</c> otherwise. If false, the fleet should repair in place.
    /// <remarks>Randomly investigates OwnerBases, OtherNonEnemyBases, NonEnemyPlanets and repairing in place.</remarks>
    /// </summary>
    /// <param name="unitRepairDest">The unit repair destination.</param>
    /// <returns></returns>
    [Obsolete("Not currently used")]
    private bool __TryGetRepairDestination(out IShipRepairCapable unitRepairDest) {
        unitRepairDest = null;
        int random = RandomExtended.Range(0, 3);
        switch (random) {
            case 0:
                IUnitBaseCmd myClosestBase; // IMPROVE not under attack by a warEnemy
                if (OwnerAiMgr.TryFindMyClosestItem<IUnitBaseCmd>(Position, out myClosestBase)) {
                    unitRepairDest = myClosestBase as IShipRepairCapable;
                }
                break;
            case 1:
                var otherBases = OwnerAiMgr.Knowledge.Bases.Where(b => b.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Neutral,
                    DiplomaticRelationship.Friendly, DiplomaticRelationship.Alliance));
                if (otherBases.Any()) {
                    unitRepairDest = GameUtility.GetClosest(Position, otherBases) as IShipRepairCapable;
                }
                break;
            case 2:
                var planets = OwnerAiMgr.Knowledge.Planets.Where(p => p.Owner_Debug.IsFriendlyWith(Owner)
                    || p.Owner_Debug.IsRelationshipWith(Owner, DiplomaticRelationship.Neutral));
                if (planets.Any()) {
                    unitRepairDest = GameUtility.GetClosest(Position, planets) as IShipRepairCapable;
                }
                break;
            case 3:
                // do nothing as this selects a null destination, aka RepairInPlace
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(random));
        }
        return unitRepairDest != null;
    }

    /// <summary>
    /// Forms a fleet from the provided ship that has just changed owners using the default FleetCmdModuleDesign.
    /// Throws an error if the provided ship is not currently part of this fleet.
    /// <remarks>Primary use is when making temporary throw away fleets because of a ship owner change.</remarks>
    /// <remarks>OPTIMIZE only real use is to allow validation of conditions for debug purposes.</remarks>
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns></returns>
    internal FleetCmdItem __FormNewOwnerFleetFrom(ShipItem ship) {
        bool toValidateSameOwners = true;
        if (ElementCount == Constants.One) {
            D.AssertEqual(HQElement, ship);
            // if ship is only remaining element, then Fleet's owner has already been changed to ship's new owner
            D.AssertEqual(Owner, ship.Owner);
        }
        else {
            D.AssertNotEqual(Owner, ship.Owner);
            toValidateSameOwners = false;
        }
        FleetCmdModuleDesign defaultCmdModDesign = _gameMgr.GetAIManagerFor(ship.Owner).Designs.GetFleetCmdModDefaultDesign();
        return FormFleetFrom("NewOwnerSingleShipFleet", defaultCmdModDesign, new ShipItem[] { ship }, toValidateSameOwners);
    }

    protected override void __ValidateCommonCallableEnterStateValues(string calledStateName, bool includeFsmTgt = true) {
        base.__ValidateCommonCallableEnterStateValues(calledStateName, includeFsmTgt);
        if (includeFsmTgt) {
            D.AssertNotNull(_fsmTgt);
            var mortalFsmTgt = _fsmTgt as IMortalItem_Ltd;
            if (mortalFsmTgt != null) {
                D.Assert(!mortalFsmTgt.IsDead, mortalFsmTgt.DebugName);
            }
        }
        D.Assert(!_fsmShipsExpectedToCallbackWithOrderOutcome.Any());
        __ValidateMoveHelperState(errorMsg: "entering State {0}".Inject(CurrentState.GetValueName()));
    }

    protected override void __ValidateCommonNonCallableEnterStateValues() {
        base.__ValidateCommonNonCallableEnterStateValues();
        if (_fsmTgt != null) {
            D.Error("{0} _fsmMoveTgt {1} should not already be assigned.", DebugName, _fsmTgt.DebugName);
        }
        D.Assert(!_fsmShipsExpectedToCallbackWithOrderOutcome.Any());
        __ValidateMoveHelperState(errorMsg: "entering State {0}".Inject(CurrentState.GetValueName()));
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateMoveHelperState(string errorMsg) {
        if (_moveHelper.IsPilotEngaged) {   // 6.4.18 Added to track down Pilot not engaged when ship replies with explore success 
            D.Error("{0} has its pilot engaged {1}!", _moveHelper.DebugName, errorMsg);
        }
    }

    protected override void __ValidateStateForSensorEventSubscription() {
        D.AssertNotEqual(FleetState.None, CurrentState);
        D.AssertNotEqual(FleetState.FinalInitialize, CurrentState);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __CheckForIdleWarnings() {
        if (_moveHelper.IsPilotEngaged) {
            D.Warn("{0}'s AutoPilot is still engaged entering Idling. SpeedSetting = {1}, ActualSpeedValue = {2:0.##}. LastState = {3}.",
                DebugName, Data.CurrentSpeedSetting.GetValueName(), Data.ActualSpeedValue, LastState.GetValueName());
        }
    }

    protected override void __ValidateCurrentStateWhenAssessingAvailabilityStatus_Repair() {
        D.AssertEqual(FleetState.Repairing, CurrentState);
    }

    protected override void __ValidateAddElement(AUnitElementItem element) {
        base.__ValidateAddElement(element);
        if (IsOperational && !element.IsOperational) {
            // 4.4.17 Acceptable combos: Both not operational during construction, both operational during runtime
            // and non-operational Cmd with operational element when forming a fleet from this fleet using UnitFactory.
            D.Error("{0}: Adding element {1} with unexpected IsOperational state.", DebugName, element.DebugName);
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateIncomingOrder(FleetOrder incomingOrder) {
        if (incomingOrder != null) {
            string failCause;
            if (!__TryAuthorizeNewOrder(incomingOrder.Directive, out failCause)) {
                D.Error("{0}'s incoming order {1} is not valid. FailCause = {2}, CurrentState = {3}.",
                    DebugName, incomingOrder.DebugName, failCause, CurrentState.GetValueName());
            }
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateKnowledgeOfOrderTarget(FleetOrder order) {
        var target = order.Target;
        if (target != null && !(target is StationaryLocation) && !(target is MobileLocation)) {
            if (target is StarItem || target is SystemItem || target is UniverseCenterItem || target is CoreSector) {
                return; // unnecessary knowledge check as all players have knowledge of these targets
            }
            if (!OwnerAiMgr.HasKnowledgeOf(target as IOwnerItem_Ltd)) {
                D.Warn("{0} received order {1} when {2} has no knowledge of target.", DebugName, order.DebugName, Owner.DebugName);
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the provided directive is authorized for use in a new order about to be issued.
    /// <remarks>Does not take into account whether consecutive order directives of the same value are allowed.
    /// If this criteria should be included, the client will need to include it manually.</remarks>
    /// <remarks>Warning: Do not use to Assert once CurrentOrder has changed and unpaused as order directives that
    /// result in Availability.Unavailable will fail the assert.</remarks>
    /// </summary>
    /// <param name="orderDirective">The order directive.</param>
    /// <param name="failCause">The fail cause.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    protected bool __TryAuthorizeNewOrder(FleetDirective orderDirective, out string failCause) {
        failCause = "None";
        if (orderDirective == FleetDirective.Scuttle) {
            D.Assert(Elements.All(e => (e as ShipItem).IsAuthorizedForNewOrder(ShipDirective.Scuttle)));
            return true;    // Scuttle orders never deferred while paused so no need for IsCurrentOrderDirective check
        }
        if (orderDirective == FleetDirective.ChangeHQ) {
            // Can be ordered to change HQ even if unavailable
            failCause = "ElementCount";
            return ElementCount > Constants.One;
        }

        if (Availability == NewOrderAvailability.Unavailable) {
            D.AssertNotEqual(FleetDirective.Cancel, orderDirective);
            failCause = "Unavailable";
            return false;
        }
        if (orderDirective == FleetDirective.Cancel) {
            D.Assert(IsPaused);
            return true;
        }
        if (orderDirective == FleetDirective.FoundSettlement) {
            var colonyShips = Elements.Cast<ShipItem>().Where(s => s.HullCategory == ShipHullCategory.Colonizer);
            if (colonyShips.Any()) {
                string failureCause = null;
                bool isAuthorized = colonyShips.Any(cShip => cShip.__TryAuthorizeNewOrder(ShipDirective.FoundSettlement, out failureCause));
                failCause = failureCause;
                return isAuthorized;
            }
            else {
                failCause = "No ColonyShips";
                return false;
            }
        }
        if (orderDirective == FleetDirective.FoundStarbase) {
            var colonyShips = Elements.Cast<ShipItem>().Where(s => s.HullCategory == ShipHullCategory.Colonizer);
            if (colonyShips.Any()) {
                string failureCause = null;
                bool isAuthorized = colonyShips.Any(cShip => cShip.__TryAuthorizeNewOrder(ShipDirective.FoundStarbase, out failureCause));
                failCause = failureCause;
                return isAuthorized;
            }
            else {
                failCause = "No ColonyShips";
                return false;
            }
        }
        if (orderDirective == FleetDirective.Attack) {
            // Can be ordered to attack even if already ordered to attack as can change target
            failCause = "No attackables";
            if (!OwnerAiMgr.Knowledge.AreAnyKnownFleetsAttackableByOwnerUnits) {
                return false;
            }

            failCause = "Not attack capable";
            if (!IsAttackCapable) {
                return false;
            }

            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };
            foreach (var e in Elements) {
                string eFailCause;
                if ((e as ShipItem).__TryAuthorizeNewOrder(ShipDirective.Attack, out eFailCause)) {
                    return true;
                }
                else {
                    elementFailCauses.Add(eFailCause);
                }
            }
            failCause = elementFailCauses.Concatenate();
            return false;
        }
        if (orderDirective == FleetDirective.Refit) {
            // Can be ordered to refit even if already ordered to refit as can change destination before arrival
            // Authorizing a Unit Refit requires at least 1 element to be refitable, independent of whether there is a CmdModule upgrade available
            bool toAuthorizeRefit = false;
            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };

            foreach (var e in Elements) {
                ShipItem ship = e as ShipItem;
                string eFailCause;
                toAuthorizeRefit = ship.__TryAuthorizeNewOrder(ShipDirective.Refit, out eFailCause);
                if (!toAuthorizeRefit) {
                    elementFailCauses.Add(eFailCause);
                    continue;
                }
                break;
            }

            if (!toAuthorizeRefit) {
                failCause = elementFailCauses.Concatenate();
            }
            else {
                if (!OwnerAiMgr.Knowledge.OwnerBases.Any()) {
                    toAuthorizeRefit = false;
                    failCause = "No owner bases available";
                }
            }
            return toAuthorizeRefit;
        }
        if (orderDirective == FleetDirective.Disband) {
            // Can be ordered to disband even if already ordered to disband as can change destination before arrival
            // 1.2.18 OPTIMIZE a ship in a fleet can always be disbanded?
            int disbandableShipCount = Constants.Zero;
            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };
            foreach (var e in Elements) {
                ShipItem ship = e as ShipItem;
                string eFailCause;
                if (ship.__TryAuthorizeNewOrder(ShipDirective.Disband, out eFailCause)) {
                    disbandableShipCount++;
                }
                else {
                    elementFailCauses.Add(eFailCause);
                }
            }

            if (disbandableShipCount > Constants.Zero) {
                if (OwnerAiMgr.Knowledge.AreAnyBaseHangersJoinable(disbandableShipCount)) {
                    return true;
                }
                failCause = "No hangers available to accommodate {0} ships.".Inject(disbandableShipCount);
            }
            else {
                failCause = elementFailCauses.Concatenate();
            }
            return false;
        }
        if (orderDirective == FleetDirective.Repair) {
            // Can be ordered to repair even if already ordered to repair as can change destination before arrival
            // No need to check for repair destinations as a fleet can repair in place, albeit slowly on their formation stations
            if (__debugSettings.DisableRepair) {
                failCause = "Repair disabled";
                return false;
            }
            // 12.9.17 _debugSettings.AllPlayersInvulnerable not needed as it keeps damage from being taken
            if (Data.UnitHealth < Constants.OneHundredPercent) {
                // one or more elements are damaged but element(s) could be otherwise occupied
                IList<string> elementFailCauses = new List<string> {
                    "No Authorized Elements: "
                };
                foreach (var e in Elements) {
                    string eFailCause;
                    if ((e as ShipItem).__TryAuthorizeNewOrder(ShipDirective.Repair, out eFailCause)) {
                        return true;
                    }
                    else {
                        elementFailCauses.Add(eFailCause);
                    }
                }
                failCause = elementFailCauses.Concatenate();
                return false;
            }
            failCause = "Perfect health";
            return false;
        }

        if (orderDirective == FleetDirective.Explore) {
            if (!OwnerAiMgr.Knowledge.AreAnyKnownItemsUnexploredByOwnerFleets) {
                failCause = "No explorables";
                return false;
            }
            IList<string> elementFailCauses = new List<string> {
                "No Authorized Elements: "
            };
            foreach (var e in Elements) {
                string eFailCause;
                if ((e as ShipItem).__TryAuthorizeNewOrder(ShipDirective.Explore, out eFailCause)) {
                    return true;
                }
                else {
                    elementFailCauses.Add(eFailCause);
                }
            }
            failCause = elementFailCauses.Concatenate();
            return false;
        }
        if (orderDirective == FleetDirective.FullSpeedMove || orderDirective == FleetDirective.Move) {
            return true;    // Ships in a fleet can't be in a condition where they can't move
        }
        if (orderDirective == FleetDirective.Guard) {
            failCause = "No guardables";
            return OwnerAiMgr.Knowledge.AreAnyKnownItemsGuardableByOwner;
        }
        if (orderDirective == FleetDirective.JoinFleet) {
            failCause = "No joinable fleets";
            return OwnerAiMgr.Knowledge.AreAnyFleetsJoinableBy(this);
        }
        if (orderDirective == FleetDirective.JoinHanger) {
            failCause = "No joinable base hangers";
            return OwnerAiMgr.Knowledge.AreAnyBaseHangersJoinableBy(this);
        }
        if (orderDirective == FleetDirective.Patrol) {
            failCause = "No patrollables";
            return OwnerAiMgr.Knowledge.AreAnyKnownItemsPatrollableByOwner;
        }

        if (orderDirective == FleetDirective.AssumeFormation) {
            return true;
        }

        if (orderDirective == FleetDirective.Retreat || orderDirective == FleetDirective.Withdraw) {
            failCause = "Not implemented";
            return false;   // Retreat/Withdraw (not implemented) 
        }
        // Regroup (not currently used)
        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderDirective));
    }

    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        if (_moveHelper != null) {
            _moveHelper.__ReportLongestWaypointTransitDuration();
        }
    }

    #region Debug Show Course Plot

    private const string __coursePlotNameFormat = "{0} CoursePlot";
    private CoursePlot3DLine __coursePlot;

    private GameColor __CoursePlotColor {
        get {
            return Data.InfoAccessCntlr.HasIntelCoverageReqdToAccess(_gameMgr.UserPlayer, ItemInfoID.Owner) ? Owner.Color : GameColor.White;
        }
    }

    private void __InitializeShowCoursePlot() {
        __debugCntls.showFleetCoursePlots += __ShowFleetCoursePlotChangedEventHandler;
        if (__debugCntls.ShowFleetCoursePlots) {
            __EnableShowCoursePlot(true);
        }
    }

    private void __EnableShowCoursePlot(bool toEnable) {
        if (toEnable) {
            if (__coursePlot == null) {
                string name = __coursePlotNameFormat.Inject(DebugName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                var course = _moveHelper.ApCourse.Cast<INavigableDestination>().ToList();
                __coursePlot = new CoursePlot3DLine(name, course, lineParent, __CoursePlotColor);
            }
            __AssessShowCoursePlot();
        }
        else {
            D.AssertNotNull(__coursePlot);
            __coursePlot.Dispose();
            __coursePlot = null;
        }
    }

    private void __AssessShowCoursePlot() {
        if (__coursePlot != null) {
            GameColor color = __CoursePlotColor;
            if (__coursePlot.Color != color) {
                __coursePlot.Color = color;
            }
            // 1.26.18 Always shows User-owned fleetPlots, other plots if FleetCmd discernible to user
            bool toShow = _moveHelper.ApCourse.Count > Constants.Zero && (IsDiscernibleToUser || Owner.IsUser);
            __coursePlot.Show(toShow);
        }
    }

    internal void __RefreshCoursePlotCourse() {
        if (__coursePlot != null) {
            var course = _moveHelper.ApCourse.Cast<INavigableDestination>().ToList();
            __coursePlot.RefreshCourse(course);
            __AssessShowCoursePlot();
        }
    }

    private void __ShowFleetCoursePlotChangedEventHandler(object sender, EventArgs e) {
        __EnableShowCoursePlot(__debugCntls.ShowFleetCoursePlots);
    }

    private void __CleanupShowCoursePlot() {
        if (__debugCntls != null) {
            __debugCntls.showFleetCoursePlots -= __ShowFleetCoursePlotChangedEventHandler;
        }
        if (__coursePlot != null) {
            __coursePlot.Dispose();
        }
    }

    #endregion

    #region Debug Show Velocity Ray

    private const string __velocityRayNameFormat = "{0} VelocityRay";
    private VelocityRay __velocityRay;

    private void __InitializeShowVelocityRay() {
        __debugCntls.showFleetVelocityRays += __ShowFleetVelocityRayChangedEventHandler;
        if (__debugCntls.ShowFleetVelocityRays) {
            __EnableShowVelocityRay(true);
        }
    }

    private void __EnableShowVelocityRay(bool toEnable) {
        if (toEnable) {
            if (__velocityRay == null) {
                Reference<float> fleetSpeed = new Reference<float>(() => Data.ActualSpeedValue);
                string name = __velocityRayNameFormat.Inject(DebugName);
                Transform lineParent = DynamicObjectsFolder.Instance.Folder;
                __velocityRay = new VelocityRay(name, transform, fleetSpeed, lineParent, width: 2F, color: GameColor.Green);
            }
            __AssessShowVelocityRay();
        }
        else {
            D.AssertNotNull(__velocityRay);
            __velocityRay.Dispose();
            __velocityRay = null;
        }
    }

    private void __AssessShowVelocityRay() {
        if (__velocityRay != null) {
            bool toShow = IsDiscernibleToUser;
            __velocityRay.Show(toShow);
        }
    }

    private void __ShowFleetVelocityRayChangedEventHandler(object sender, EventArgs e) {
        __EnableShowVelocityRay(__debugCntls.ShowFleetVelocityRays);
    }

    private void __CleanupShowVelocityRay() {
        if (__debugCntls != null) {
            __debugCntls.showFleetVelocityRays -= __ShowFleetVelocityRayChangedEventHandler;
        }
        if (__velocityRay != null) {
            __velocityRay.Dispose();
        }
    }

    #endregion

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Fleet can operate in.
    /// </summary>
    protected enum FleetState {

        None,

        FinalInitialize,

        Idling,

        /// <summary>
        /// State that executes the FleetOrder AssumeFormation.
        /// </summary>
        ExecuteAssumeFormationOrder,

        /// <summary>
        /// Call()ed state that exists while the ships of a fleet are assuming their formation station. 
        /// </summary>
        AssumingFormation,

        ExecuteExploreOrder,

        /// <summary>
        /// Call()ed state that exists while the ships of a fleet are exploring their assigned targets. 
        /// </summary>
        Exploring,

        /// <summary>
        /// State that executes the FleetOrder Move at a speed chosen by FleetCmd, typically FleetStandard.
        /// </summary>
        ExecuteMoveOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is moving from one position to another.
        /// This can occur as part of the execution process for a number of FleetOrders.
        /// </summary>
        [Obsolete]
        Moving,

        /// <summary>
        /// State that executes the FleetOrder Patrol which encompasses Moving and Patrolling.
        /// </summary>
        ExecutePatrolOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is patrolling around a target between multiple PatrolStations.
        /// </summary>
        Patrolling,

        /// <summary>
        /// State that executes the FleetOrder Guard which encompasses Moving and Guarding.
        /// </summary>
        ExecuteGuardOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is guarding a target at a GuardStation.
        /// </summary>
        Guarding,

        /// <summary>
        /// State that executes the FleetOrder Attack which encompasses moving and Attacking.
        /// </summary>
        ExecuteAttackOrder,

        /// <summary>
        /// Call()ed state that exists while an entire fleet is attacking a UnitAttackTarget.
        /// </summary>
        Attacking,

        [System.Obsolete("Not currently used")]
        ExecuteRegroupOrder,


        ExecuteJoinFleetOrder,
        ExecuteJoinHangerOrder,

        /// <summary>
        /// Call()ed state that exists while the ships of a fleet are attempting to enter a Base Hanger.
        /// </summary>
        JoiningHanger,

        ExecuteRepairOrder,
        Repairing,

        Entrenching,

        ExecuteRefitOrder,

        /// <summary>
        /// State that monitors ships while they are making their way to a hanger to refit.
        /// <remarks>12.8.17 Currently, this state does not monitor/wait for ship refitting to complete.</remarks>
        /// </summary>
        Refitting,

        ExecuteDisbandOrder,

        /// <summary>
        /// State that monitors ships while they are making their way to a hanger to disband.
        /// <remarks>12.30.17 Currently, this state does not monitor/wait for ship disbanding to complete.</remarks>
        /// </summary>
        Disbanding,

        ExecuteFoundSettlementOrder,
        FoundingSettlement,

        ExecuteFoundStarbaseOrder,
        FoundingStarbase,

        Dead

    }

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDamper { get { return CameraStat.FollowDistanceDamper; } }

    public float FollowRotationDamper { get { return CameraStat.FollowRotationDamper; } }

    #endregion

    #region INavigableDestination Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IFleetNavigableDestination Members

    public override float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        // IMPROVE OK to cast right to flagship as currently Ships aren't obstacles that can be discovered via casting
        return Vector3.Distance(fleetPosition, Position);
    }

    #endregion

    #region IShipNavigableDestination Members

    public override ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        float innerShellRadius = UnitMaxFormationRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of formation
        float outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This FleetCmd version assigns a FleetFormationStation to the ship after
    /// removing the existing station, if any. The ship will then assume its station by moving to its station when ordered
    /// to AssumeFormation. If this Cmd AND the ship are not yet operational, aka the Cmd and ship are being deployed 
    /// for the first time, the ship will be placed at the station's location and then have the FleetFormationStation 
    /// assigned so it is initially 'on station'.
    /// <remarks>11.16.17 Criteria for a 'teleport' placement was Cmd not operational before operational ships could
    /// be added to not yet operational Cmds.</remarks>
    /// </summary>
    /// <param name="element">The ship element.</param>
    /// <param name="stationSlotInfo">The station slot information.</param>
    protected override void PositionElementInFormation_Internal(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        ShipItem ship = element as ShipItem;
        if (!IsOperational && !element.IsOperational) {
            // Neither operational, so this positioning is occurring via a Creator using a supplied UnitConfiguration.
            // That means the UnitConfiguration has been determined during the game launch -> user can't observe the 'teleporting' placement
            ship.transform.localPosition = stationSlotInfo.LocalOffset;
            //D.Log(ShowDebugLog, "{0} positioned at {1}, offset by {2} from {3} at {4}.",
            //element.DebugName, element.Position, stationSlotInfo.LocalOffset, HQElement.DebugName, HQElement.Position);
        }

        if (RemoveAndRecycleFormationStation(ship)) {
            // this is normal when a formation or HQElement is changed
            D.Log(ShowDebugLog, "{0} had to remove and despawn {1}'s old {2}.", DebugName, ship.DebugName, typeof(FleetFormationStation).Name);
        }

        //D.Log(ShowDebugLog, "{0} is adding a new {1} with SlotID {2}.", DebugName, typeof(FleetFormationStation).Name, stationSlotInfo.SlotID.GetValueName());
        FleetFormationStation station = GamePoolManager.Instance.SpawnFormationStation(Position, Quaternion.identity, transform);
        station.StationInfo = stationSlotInfo;
        station.AssignedShip = ship;
        ship.FormationStation = station;
    }

    #endregion

    #region ISectorViewHighlightable Members

    public bool IsSectorViewHighlightShowing {
        get { return GetHighlightMgr(HighlightMgrID.SectorView).IsHighlightShowing; }
    }

    public void ShowSectorViewHighlight(bool toShow) {
        var sectorViewHighlightMgr = GetHighlightMgr(HighlightMgrID.SectorView) as SectorViewHighlightManager;
        if (!IsDiscernibleToUser) {
            if (sectorViewHighlightMgr.IsHighlightShowing) {
                //D.Log(ShowDebugLog, "{0} received ShowSectorViewHighlight({1}) when not discernible but showing. Sending Show(false) to sync HighlightMgr.", DebugName, toShow);
                sectorViewHighlightMgr.Show(false);
            }
            return;
        }
        sectorViewHighlightMgr.Show(toShow);
    }

    #endregion

    #region IFleetCmd Members

    float IFleetCmd.CmdEffectiveness { get { return Data.CurrentCmdEffectiveness; } }

    void IFleetCmd.__IssueCmdStaffsRepairOrder() {
        IssueCmdStaffsRepairOrder();
    }

    /// <summary>
    /// Returns <c>true</c> if there are ships slowing the fleet.
    /// The ships returned will either be 1) not FTL capable, 2) FTL capable but with a damaged and/or damped FTL engine.
    /// <remarks>Also returns all other ships present that don't meet the slowing criteria so the client
    /// can determine whether the fleet could move faster if the slowedShips were separated from the fleet.</remarks>
    /// </summary>
    /// <param name="slowedShips">The slowed ships.</param>
    /// <param name="remainingShips">The remaining ships.</param>
    /// <returns></returns>
    bool IFleetCmd.TryGetShipsSlowingFleet(out IEnumerable<IShip> slowedShips, out IEnumerable<IShip> remainingShips) {
        IEnumerable<ShipItem> slowedShipItems;
        IEnumerable<ShipItem> remainingShipItems;
        bool isSlowing = TryGetShipsSlowingFleet(out slowedShipItems, out remainingShipItems);
        slowedShips = slowedShipItems.Cast<IShip>();
        remainingShips = remainingShipItems.Cast<IShip>();
        return isSlowing;
    }

    #endregion

    #region IFleetCmd_Ltd Members

    Reference<float> IFleetCmd_Ltd.ActualSpeedValue_Debug { get { return new Reference<float>(() => Data.ActualSpeedValue); } }

    #endregion


}

