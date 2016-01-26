// --------------------------------------------------------------------------------------------------------------------
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
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmdItem, IUnitAttackableTarget, IFormationMgrClient {

    /// <summary>
    /// The transform that normally contains all elements and commands assigned to the Unit.
    /// </summary>
    public Transform UnitContainer { get; private set; }

    public bool IsTrackingLabelEnabled { private get; set; }

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

    public new AUnitCmdItemData Data {
        get { return base.Data as AUnitCmdItemData; }
        set { base.Data = value; }
    }

    private AUnitElementItem _hqElement;
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", HQElementPropChangedHandler, HQElementPropChangingHandler); }
    }

    public IList<AUnitElementItem> Elements { get; private set; }

    public IList<ISensorRangeMonitor> SensorRangeMonitors { get; private set; }

    protected new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }

    private AFormationManager _formationMgr;
    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Elements = new List<AUnitElementItem>();
        SensorRangeMonitors = new List<ISensorRangeMonitor>();
        _formationMgr = InitializeFormationMgr();
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
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdItemData, Formation>(d => d.UnitFormation, UnitFormationPropChangedHandler));
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

    protected override ADisplayManager InitializeDisplayManager() {
        var dMgr = new UnitCmdDisplayManager(this, MakeIconInfo(), Owner.Color);
        SubscribeToIconEvents(dMgr.Icon);
        return dMgr;
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

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessIcon();
    }

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
        Subscribe(element);
        //TODO consider changing HQElement
        var unattachedSensors = element.Data.Sensors.Where(sensor => sensor.RangeMonitor == null);
        if (unattachedSensors.Any()) {
            //D.Log("{0} is attaching {1}'s sensors: {2}.", FullName, element.FullName, unattachedSensors.Select(s => s.Name).Concatenate());
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
        _formationMgr.AddAndPositionNonHQElement(element);
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
                //D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
                UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
            }
        });
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, "{0} not found.".Inject(element.FullName));
        Data.RemoveElement(element.Data);
        Unsubscribe(element);

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
        _formationMgr.RemoveElement(element);
    }

    private void Subscribe(AUnitElementItem element) {
        element.Data.userIntelCoverageChanged += ElementUserIntelCoverageChangedEventHandler;
        //element.onDeathOneShot += HandleSubordinateElementDeath;  // element now informs command AFTER it broadcasts its onDeathEvent
        // previously, sequencing issues surfaced, depending on the order in which subscribers signed up for the event
    }

    private void Unsubscribe(AUnitElementItem element) {
        element.Data.userIntelCoverageChanged -= ElementUserIntelCoverageChangedEventHandler;
    }

    public void HandleSubordinateElementDeath(IUnitElementItem deadSubordinateElement) {
        D.Log("{0} acknowledging {1} has been lost.", FullName, deadSubordinateElement.FullName);
        RemoveElement(deadSubordinateElement as AUnitElementItem);
    }

    protected abstract void AttachCmdToHQElement();

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            //D.Log("{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
            DisplayMgr.IconInfo = iconInfo;
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract IconInfo MakeIconInfo();

    private void ShowTrackingLabel(bool toShow) {
        //D.Log("{0}.ShowTrackingLabel({1}) called. IsTrackingLabelEnabled = {2}.", FullName, toShow, IsTrackingLabelEnabled);
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
    }

    #region Event and Property Change Handlers

    protected virtual void HQElementPropChangingHandler(AUnitElementItem newHQElement) {
        Arguments.ValidateNotNull(newHQElement);
        var previousHQElement = HQElement;
        if (previousHQElement != null) {
            previousHQElement.Data.IsHQ = false;
        }
        if (!Elements.Contains(newHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Warn("{0} assigned HQElement {1} that is not already present in Unit.", FullName, newHQElement.FullName);
            AddElement(newHQElement);
        }
    }

    private void HQElementPropChangedHandler() {
        HQElement.Data.IsHQ = true;
        Data.HQElementData = HQElement.Data;    // Data.Radius now returns Radius of new HQElement
        //D.Log("{0}'s HQElement is now {1}. Radius = {2:0.##}.", Data.ParentName, HQElement.Data.Name, Data.Radius);
        AttachCmdToHQElement(); // needs to occur before formation changed
        _formationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElementItem>().ToList());
        if (DisplayMgr != null) {
            DisplayMgr.ResizePrimaryMesh(Radius);
        }
    }

    protected void TargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem deadTarget = sender as IMortalItem;
        UponTargetDeath(deadTarget);
    }

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
        if (DisplayMgr != null) {
            DisplayMgr.Color = Owner.Color;
        }
        AssessIcon();
    }

    private void UnitFormationPropChangedHandler() {
        _formationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElementItem>().ToList());
    }

    protected override void UserIntelCoverageChangedEventHandler(object sender, EventArgs e) {
        base.UserIntelCoverageChangedEventHandler(sender, e);
        AssessIcon();   // UNCLEAR is this needed? How does IntelCoverage of Cmd change icon contents?
    }

    private void ElementUserIntelCoverageChangedEventHandler(object sender, EventArgs e) {
        AssessIcon();
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        ShowTrackingLabel(IsDiscernibleToUser);
    }

    protected override void IsSelectedPropChangedHandler() {
        base.IsSelectedPropChangedHandler();
        Elements.ForAll(e => e.AssessHighlighting());
    }

    #endregion

    # region StateMachine Support Methods

    protected override void HandleDeath() {
        base.HandleDeath();
    }

    protected void Dead_ExitState() {
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    private void UponTargetDeath(IMortalItem deadTarget) {
        RelayToCurrentState(deadTarget);
    }

    protected void UponEffectFinished(EffectID effectID) { RelayToCurrentState(effectID); }

    #endregion

    # region Combat Support Methods

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit. Returns true if 
    /// the Command takes damage.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> the command's HQ element is still alive.</param>
    /// <param name="elementDamageSustained">The damage sustained by the HQ Element.</param>
    /// <param name="elementDamageSeverity">The severity of the damage sustained by the HQ Element.</param>
    /// <returns></returns>
    public bool __CheckForDamage(bool isHQElementAlive, DamageStrength elementDamageSustained, float elementDamageSeverity) {
        //D.Log("{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
        //FullName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        var cmdMissedChance = Constants.OneHundredPercent - elementDamageSeverity;
        bool missed = (isHQElementAlive) ? RandomExtended.Chance(cmdMissedChance) : false;
        if (missed) {
            //D.Log("{0} avoided a hit.", FullName);
        }
        else {
            TakeHit(elementDamageSustained);
        }
        return !missed;
    }

    public override void TakeHit(DamageStrength elementDamageSustained) {
        if (DebugSettings.Instance.AllPlayersInvulnerable) {
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

    protected void DestroyUnitContainer(float delayInSeconds = 0F) {
        UnityUtility.Destroy(UnitContainer.gameObject, delayInSeconds);
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
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

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #endregion

    #region INavigableTarget Members

    public override string DisplayName { get { return Data.ParentName; } }

    // override reqd as AMortalItem base version accesses AItemData, not ACommandData
    // since ACommandData.Topography must use new rather than override
    //public override Topography Topography { get { return Data.Topography; } } // override reason no longer true?

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return Data.UnitMaxFormationRadius + Data.CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    public override bool IsRetainedFocusEligible { get { return GetUserIntelCoverage() != IntelCoverage.None; } }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Data.UnitMaxFormationRadius; } }

    public override float HighlightRadius { get { return Screen.height * 0.03F; } }

    #endregion

    #region IFormationMgrClient Members

    public virtual void PositionElementInFormation(IUnitElementItem element, Vector3 stationOffset) {
        (element as AUnitElementItem).transform.position = HQElement.Position + stationOffset;
        //D.Log("{0} positioned at {1}, offset by {2} from {3} at {4}.", element.FullName, element.Position, stationOffset, HQElement.FullName, HQElement.Position);
    }

    public virtual void CleanupAfterFormationChanges() { }

    #endregion

}

