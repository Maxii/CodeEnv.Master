﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdItem.cs
// Abstract class for AMortalItem's that are Unit Commands.
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
/// Abstract class for AMortalItem's that are Unit Commands.
/// </summary>
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmd, IUnitCmd_Ltd, IFleetNavigable, IUnitAttackable, IFormationMgrClient {

    public Formation UnitFormation { get { return Data.UnitFormation; } }

    /// <summary>
    /// The maximum radius of this Unit's current formation, independant of the number of elements currently assigned a
    /// station in the formation or whether the Unit's elements are located on their formation station. 
    /// Value encompasses each element's "KeepoutZone" (Facility: AvoidableObstacleZone, Ship: CollisionDetectionZone) 
    /// when the element is OnStation. 
    /// </summary>
    public float UnitMaxFormationRadius {
        get { return Data.UnitMaxFormationRadius; }
        set { Data.UnitMaxFormationRadius = value; }
    }

    private Transform _unitContainer;
    /// <summary>
    /// The transform that normally contains all elements and commands assigned to the Unit.
    /// </summary>
    public Transform UnitContainer {
        get { return _unitContainer; }
        private set { SetProperty<Transform>(ref _unitContainer, value, "UnitContainer"); }
    }

    public abstract bool IsAvailable { get; }

    public bool IsAttackCapable { get { return Elements.Where(e => e.IsAttackCapable).Any(); } }

    private bool _isTrackingLabelEnabled;
    public bool IsTrackingLabelEnabled {
        private get { return _isTrackingLabelEnabled; }
        set { SetProperty<bool>(ref _isTrackingLabelEnabled, value, "IsTrackingLabelEnabled"); }
    }

    private bool __showHQDebugLog;
    /// <summary>
    /// Indicates whether the Cmd and HQELement should show their debug logs.
    /// </summary>
    public bool __ShowHQDebugLog {
        get { return __showHQDebugLog; }
        set { SetProperty<bool>(ref __showHQDebugLog, value, "__ShowHQDebugLog", __ShowHQDebugLogPropChangedHandler); }
    }

    public IconInfo IconInfo {
        get {
            if (DisplayMgr == null) {
                D.Warn("{0}.DisplayMgr is null when attempting access to IconInfo.", GetType().Name);
                return default(IconInfo);
            }
            return DisplayMgr.IconInfo;
        }
    }

    public override float Radius { get { return Data.Radius; } }

    public new AUnitCmdData Data {
        get { return base.Data as AUnitCmdData; }
        set { base.Data = value; }
    }

    private AUnitElementItem _hqElement;
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", HQElementPropChangedHandler, HQElementPropChangingHandler); }
    }

    public IList<AUnitElementItem> AvailableNonHQElements { get { return NonHQElements.Where(e => e.IsAvailable).ToList(); } }

    public IList<AUnitElementItem> AvailableElements { get { return Elements.Where(e => e.IsAvailable).ToList(); } }

    public IList<AUnitElementItem> NonHQElements { get { return Elements.Except(HQElement).ToList(); } }    // OPTIMIZE

    public IList<AUnitElementItem> Elements { get; private set; }

    public IList<ISensorRangeMonitor> SensorRangeMonitors { get; private set; }

    protected new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }
    protected AFormationManager FormationMgr { get; private set; }
    protected PlayerKnowledge OwnerKnowledge { get; private set; }

    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Elements = new List<AUnitElementItem>();
        SensorRangeMonitors = new List<ISensorRangeMonitor>();
        FormationMgr = InitializeFormationMgr();
    }

    protected abstract AFormationManager InitializeFormationMgr();

    protected override void InitializeOnData() {
        base.InitializeOnData();
        D.Assert(transform.parent != null);
        UnitContainer = transform.parent;
        // the only collider is for player interaction with the item's CmdIcon
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, Formation>(d => d.UnitFormation, UnitFormationPropChangedHandler));
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    private ITrackingWidget InitializeTrackingLabel() {
        D.Assert(HQElement != null);
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.Set(DisplayName);
        trackingLabel.Color = Owner.Color;
        return trackingLabel;
    }

    protected override IHighlighter InitializeHighlighter() {
        var iconTransform = DisplayMgr.Icon.WidgetTransform;
        return new Highlighter(this, iconTransform, false); // icon is constant size on the screen so no need for the highlight to dynamically adjust its size
    }

    protected sealed override ADisplayManager MakeDisplayManagerInstance() {
        return new UnitCmdDisplayManager(this, Layers.Cull_200);
    }

    protected sealed override void InitializeDisplayManager() {
        base.InitializeDisplayManager();
        DisplayMgr.MeshColor = Owner.Color;
        DisplayMgr.IconInfo = MakeIconInfo();
        SubscribeToIconEvents(DisplayMgr.Icon);
        DisplayMgr.ResizePrimaryMesh(Radius);
    }

    protected override EffectsManager InitializeEffectsManager() {
        return new UnitCmdEffectsManager(this);
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        OwnerKnowledge = _gameMgr.GetAIManagerFor(Owner).Knowledge;
        AssessIcon();
    }

    #endregion

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public virtual void AddElement(AUnitElementItem element) {
        D.Assert(!Elements.Contains(element), "{0} attempting to add {1} that is already present.".Inject(FullName, element.FullName));
        D.Assert(!element.IsHQ, "{0} adding element {1} already designated as the HQ Element.".Inject(FullName, element.FullName));
        D.Assert(element.IsOperational == IsOperational, "{0}: Adding element {1} with incorrect IsOperational state.", FullName, element.FullName);
        Elements.Add(element);
        Data.AddElement(element.Data);
        element.Command = this;
        element.AttachAsChildOf(UnitContainer);
        //TODO consider changing HQElement
        var unattachedSensors = element.Data.Sensors.Where(sensor => sensor.RangeMonitor == null);
        if (unattachedSensors.Any()) {
            //D.Log(ShowDebugLog, "{0} is attaching {1}'s sensors: {2}.", FullName, element.FullName, unattachedSensors.Select(s => s.Name).Concatenate());
            AttachSensorsToMonitors(unattachedSensors.ToList());
            // WARNING: IEnumerable<T> lazy evaluation GOTCHA! The IEnumerable unattachedSensors at this point (after AttachSensorsToMonitors
            // completes) no longer points to any sensors as they would now all be attached. This is because the IEnumerable is not an 
            // already constructed collection. It is a pointer to a sensor that when called evaluates the sensor against the criteria. 
            // Since the Attach method modifies the sensor, the sensor being evaluated will no longer meet the unattached criteria, 
            // so unattachedSensors will no longer point to anything. Using ToList() to feed the method delivers a fully evaluated 
            // collection to the method, but this doesn't change the fact that Sensors now has no sensors that are unattached, thus 
            // the unattachedSensors IEnumerable when used again no longer points to anything.
        }
        if (!IsOperational) {
            // avoid the following extra work if adding during Cmd construction
            D.Assert(HQElement == null);    // During Cmd construction, HQElement will be designated AFTER all Elements are
            return;                         // added resulting in _formationMgr adding all elements into the formation at once
        }
        AssessIcon();
        FormationMgr.AddAndPositionNonHQElement(element);
    }

    /// <summary>
    /// Attaches one or more sensors to this command's SensorRangeMonitors.
    /// Note: Sensors are part of a Unit's elements but the monitors they attach to
    /// are children of the Command. Thus sensor range is always measured from
    /// the Command, not from the element.
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    private void AttachSensorsToMonitors(IList<Sensor> sensors) {
        sensors.ForAll(sensor => {
            var monitor = UnitFactory.Instance.AttachSensorToCmdsMonitor(sensor, this);
            if (!SensorRangeMonitors.Contains(monitor)) {
                // only need to record and setup range monitors once. The same monitor can have more than 1 sensor
                SensorRangeMonitors.Add(monitor);
            }
        });
    }

    /// <summary>
    /// Detaches one or more sensors from this command's SensorRangeMonitors.
    /// Note: Sensors are part of a Unit's elements but the monitors they attach to
    /// are children of the Command. Thus sensor range is always measured from
    /// the Command, not from the element.
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    private void DetachSensorsFromMonitors(IList<Sensor> sensors) {
        sensors.ForAll(sensor => {
            var monitor = sensor.RangeMonitor;
            bool isRangeMonitorStillInUse = monitor.Remove(sensor);

            if (!isRangeMonitorStillInUse) {
                monitor.Reset();
                SensorRangeMonitors.Remove(monitor);
                //D.Log(ShowDebugLog, "{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
                GameUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
            }
        });
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, "{0} not found.".Inject(element.FullName));
        Data.RemoveElement(element.Data);

        DetachSensorsFromMonitors(element.Data.Sensors);
        if (!IsOperational) {
            return; // avoid the following work if removing during startup
        }
        if (Elements.Count == Constants.Zero) {
            D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(FullName));
            IsOperational = false;  // tell Cmd its dead
            return;
        }
        AssessIcon();
        FormationMgr.HandleElementRemoval(element);
    }

    /// <summary>
    /// Attempts to takeover this Cmd's ownership with player. Returns <c>true</c> if successful, <c>false</c> otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="takeoverStrength">The takeover strength. A value between 0 and 1.0.</param>
    /// <returns></returns>
    public bool __AttemptTakeover(Player player, float takeoverStrength) {
        D.Assert(player != Owner);
        Utility.ValidateForRange(takeoverStrength, Constants.ZeroPercent, Constants.OneHundredPercent);
        if (takeoverStrength > Data.CurrentCmdEffectiveness) {   // TEMP takeoverStrength vs loyalty?
            Data.Owner = player;
            return true;
        }
        return false;
    }

    public void HandleSubordinateElementDeath(IUnitElement deadSubordinateElement) {
        D.Log(ShowDebugLog, "{0} acknowledging {1} has been lost.", FullName, deadSubordinateElement.FullName);
        RemoveElement(deadSubordinateElement as AUnitElementItem);
        // state machine notification is after removal so attempts to acquire a replacement don't come up with same element
        if (IsOperational) {    // no point in notifying Cmd's Dead state of the subordinate element's death that killed it
            UponSubordinateElementDeath(deadSubordinateElement as AUnitElementItem);
        }
    }

    protected abstract void AttachCmdToHQElement();

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    public void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
            DisplayMgr.IconInfo = iconInfo;
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract IconInfo MakeIconInfo();

    private void ShowTrackingLabel(bool toShow) {
        //D.Log(ShowDebugLog, "{0}.ShowTrackingLabel({1}) called. IsTrackingLabelEnabled = {2}.", FullName, toShow, IsTrackingLabelEnabled);
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
    }

    // 7.20.16 Not needed as Cmd is not detectable. The only way Cmd IntelCoverage changes is when HQELement Coverage changes.
    // Icon needs to be assessed when any of Cmd's elements has its coverage changed as that can change which icon to show
    // protected override void HandleUserIntelCoverageChanged() {
    //    base.HandleUserIntelCoverageChanged();
    //    AssessIcon();   
    // }

    protected override void HandleDeathFromDeadState() {
        base.HandleDeathFromDeadState();
    }

    /// <summary>
    /// Handles a change in relations between players.
    /// <remarks> 7.14.16 Primary responsibility for handling Relations changes (existing relationship with a player changes) in Cmd
    /// and Element state machines rest with the Cmd. They implement HandleRelationsChanged and UponRelationsChanged.
    /// In all cases where the order is issued by either Cmd or User, the element FSM does not need to pay attention to Relations
    /// changes as their orders will be changed if a Relations change requires it, determined by Cmd. When the Captain
    /// overrides an order, those orders typically(so far) entail assuming station in one form or another, and/or repairing
    /// in place, sometimes in combination. A Relations change here should not affect any of these orders...so far.
    /// Upshot: Elements FSMs can ignore Relations changes.
    /// </remarks>
    /// </summary>
    /// <param name="otherPlayer">The other player.</param>
    /// <param name="priorRelationship">The prior relationship.</param>
    /// <param name="newRelationship">The new relationship.</param>
    public void HandleRelationsChanged(Player otherPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
        SensorRangeMonitors.ForAll(srm => srm.HandleRelationsChanged(otherPlayer, priorRelationship, newRelationship));
        Elements.ForAll(e => e.HandleRelationsChanged(otherPlayer, priorRelationship, newRelationship));
        UponRelationsChanged(otherPlayer, priorRelationship, newRelationship);
    }

    #region Event and Property Change Handlers

    protected virtual void HQElementPropChangingHandler(AUnitElementItem newHQElement) {
        Utility.ValidateNotNull(newHQElement);
        var previousHQElement = HQElement;
        if (previousHQElement != null) {
            previousHQElement.IsHQ = false;
            // don't remove previousHQElement.ShowDebugLog if ShowHQDebugLog as its probably dieing
        }
        else {
            // first assignment of HQ
            D.Assert(!IsOperational);
            // OPTIMIZE Just a FYI warning as formations currently assume this
            D.Warn(newHQElement.transform.rotation != Quaternion.identity, "{0} first HQ Element rotation = {1}.", FullName, newHQElement.transform.rotation);
        }
        if (!Elements.Contains(newHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Warn("{0} assigned HQElement {1} that is not already present in Unit.", FullName, newHQElement.FullName);
            AddElement(newHQElement);
        }
    }

    private void HQElementPropChangedHandler() {
        HQElement.IsHQ = true;
        Data.HQElementData = HQElement.Data;    // CmdData.Radius now returns Radius of new HQElement
        if (__ShowHQDebugLog) {
            HQElement.ShowDebugLog = true;
        }
        //D.Log(ShowDebugLog, "{0}'s HQElement is now {1}. Radius = {2:0.##}.", Data.ParentName, HQElement.Data.Name, Data.Radius);
        AttachCmdToHQElement(); // needs to occur before formation changed
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
        if (DisplayMgr != null) {
            DisplayMgr.ResizePrimaryMesh(Radius);
        }
    }

    protected void FsmTargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadFsmTgt = sender as IMortalItem;
        UponFsmTargetDeath(deadFsmTgt);
    }

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
        if (DisplayMgr != null) {
            DisplayMgr.MeshColor = Owner.Color;
        }
        AssessIcon();
        OwnerKnowledge = _gameMgr.GetAIManagerFor(Owner).Knowledge;
        UponOwnerChanged();
    }

    private void UnitFormationPropChangedHandler() {
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        ShowTrackingLabel(IsDiscernibleToUser);
    }

    protected override void IsSelectedPropChangedHandler() {
        base.IsSelectedPropChangedHandler();
        Elements.ForAll(e => e.AssessHighlighting());
    }

    private void __ShowHQDebugLogPropChangedHandler() {
        ShowDebugLog = __ShowHQDebugLog;
    }

    #endregion

    #region StateMachine Support Members

    /// <summary>
    /// The reported cause of a failure to complete execution of an Order.
    /// </summary>
    protected UnitItemOrderFailureCause _orderFailureCause;

    protected void Dead_ExitState() {
        LogEventWarning();
    }

    #region Relays

    private void UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) {
        RelayToCurrentState(deadSubordinateElement);
    }

    /// <summary>
    /// Called prior to entering the Dead state, this method notifies the current
    /// state that the unit is dying, allowing any current state housekeeping
    /// required before entering the Dead state.
    /// </summary>
    protected void UponDeath() { RelayToCurrentState(); }

    private void UponFsmTargetDeath(IMortalItem deadFsmTarget) { RelayToCurrentState(deadFsmTarget); }

    protected void UponEffectSequenceFinished(EffectSequenceID effectSeqID) { RelayToCurrentState(effectSeqID); }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    protected void UponRelationsChanged(Player otherPlayer, DiplomaticRelationship priorRelationship, DiplomaticRelationship newRelationship) {
        RelayToCurrentState(otherPlayer, priorRelationship, newRelationship);
    }

    protected void UponOwnerChanged() {
        RelayToCurrentState();
    }

    #endregion

    #region Combat Support

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit. Returns true if 
    /// the Command takes damage.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> the command's HQ element is still alive.</param>
    /// <param name="elementDamageSustained">The damage sustained by the HQ Element.</param>
    /// <param name="elementDamageSeverity">The severity of the damage sustained by the HQ Element.</param>
    /// <returns></returns>
    public bool __CheckForDamage(bool isHQElementAlive, DamageStrength elementDamageSustained, float elementDamageSeverity) {
        //D.Log(ShowDebugLog, "{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
        //FullName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        var cmdMissedChance = Constants.OneHundredPercent - elementDamageSeverity;
        bool missed = (isHQElementAlive) ? RandomExtended.Chance(cmdMissedChance) : false;
        if (missed) {
            //D.Log(ShowDebugLog, "{0} avoided a hit.", FullName);
        }
        else {
            TakeHit(elementDamageSustained);
        }
        return !missed;
    }

    public override void TakeHit(DamageStrength elementDamageSustained) {
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        DamageStrength damageToCmd = elementDamageSustained - Data.DamageMitigation;
        float unusedDamageSeverity;
        bool isCmdAlive = ApplyDamage(damageToCmd, out unusedDamageSeverity);
        D.Assert(isCmdAlive, "{0} should never die as a result of being hit.".Inject(Data.Name));
    }

    /// <summary>
    /// Applies the damage to the command and returns true if the command survived the hit.
    /// </summary>
    /// <param name="damageToCmd">The damage sustained.</param>
    /// <param name="damageSeverity">The damage severity.</param>
    /// <returns>
    ///   <c>true</c> if the command survived.
    /// </returns>
    protected override bool ApplyDamage(DamageStrength damageToCmd, out float damageSeverity) {
        var __combinedDmgToCmd = damageToCmd.Total;
        var minAllowedCurrentHitPoints = 0.5F * Data.MaxHitPoints;
        var proposedCurrentHitPts = Data.CurrentHitPoints - __combinedDmgToCmd;
        if (proposedCurrentHitPts < minAllowedCurrentHitPoints) {
            Data.CurrentHitPoints = minAllowedCurrentHitPoints;
        }
        else {
            Data.CurrentHitPoints -= __combinedDmgToCmd;
        }
        damageSeverity = Mathf.Clamp01(__combinedDmgToCmd / Data.CurrentHitPoints);
        if (Data.Health > Constants.ZeroPercent) {
            AssessCripplingDamageToEquipment(damageSeverity);
            return true;
        }
        D.Assert(false);    // should never happen as Commands can't die directly from a hit on the command
        return false;
    }

    /// <summary>
    /// Destroys any parents that are applicable to the Cmd. 
    /// <remarks>A Fleet and Starbase destroy their UnitContainer, but a Settlement 
    /// destroys its UnitContainer's parent, its CelestialOrbitSimulator.</remarks>
    /// </summary>
    /// <param name="delayInHours">The delay in hours.</param>
    protected virtual void DestroyApplicableParents(float delayInHours = Constants.ZeroF) {
        GameUtility.Destroy(UnitContainer.gameObject, delayInHours);
    }

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    // subscriptions contained completely within this gameObject (both subscriber
    // and subscribed) do not have to be cleaned up as all instances are destroyed

    #endregion

    #region INavigable Members

    public override string DisplayName { get { return Data.ParentName; } }

    #endregion

    #region IFleetNavigable Members

    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return UnitMaxFormationRadius + Data.CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    public override bool IsRetainedFocusEligible { get { return UserIntelCoverage != IntelCoverage.None; } }

    #endregion

    #region IHighlightable Members

    public override float SphericalHighlightEffectRadius { get { return UnitMaxFormationRadius; } }

    public override float CircleHighlightEffectRadius { get { return Screen.height * 0.03F; } }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This base class version simply places the 
    /// element at the designated offset location from the HQElement.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="stationSlotInfo">The slot information.</param>
    public virtual void PositionElementInFormation(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        (element as AUnitElementItem).transform.localPosition = stationSlotInfo.LocalOffset;
        //D.Log(ShowDebugLog, "{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //element.FullName, element.Position, stationSlotInfo.LocalOffset, HQElement.FullName, HQElement.Position);
    }

    #endregion


}

