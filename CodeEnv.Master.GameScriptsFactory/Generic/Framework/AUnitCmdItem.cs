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
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmdItem, ISelectable, IUnitAttackableTarget {

    [Range(0.5F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 0.9F;    // just inside Unit's highlight sphere

    [Range(1.5F, 5.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 2F;  // encompasses all elements of the Unit

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

    /// <summary>
    /// The radius of the entire Unit. 
    /// This is not the radius of the Command which is the radius of the HQElement.
    /// </summary>
    public abstract float UnitRadius { get; }

    public new AUnitCmdItemData Data {
        get { return base.Data as AUnitCmdItemData; }
        set { base.Data = value; }
    }

    private AUnitElementItem _hqElement;
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    public IList<AUnitElementItem> Elements { get; private set; }

    public IList<ISensorRangeMonitor> SensorRangeMonitors { get; private set; }

    protected new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }

    protected FormationGenerator _formationGenerator;

    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Elements = new List<AUnitElementItem>();
        SensorRangeMonitors = new List<ISensorRangeMonitor>();
        _formationGenerator = new FormationGenerator(this);
    }

    protected override void InitializeModelMembers() {
        // the only collider is for player interaction with the item's CmdIcon
        UnitContainer = _transform.parent;
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdItemData, Formation>(d => d.UnitFormation, OnFormationChanged));
    }

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
        var displayMgr = new UnitCmdDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(displayMgr.Icon);
        return displayMgr;
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (go, isOver) => OnHover(isOver);
        iconEventListener.onClick += (go) => OnClick();
        iconEventListener.onDoubleClick += (go) => OnDoubleClick();
        iconEventListener.onPress += (go, isDown) => OnPress(isDown);
    }

    protected override EffectsManager InitializeEffectsManager() {
        return new UnitCmdEffectsManager(this);
    }

    #endregion

    #region Model Methods

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
        // elements should already be enabled when added to a Cmd as that is commonly their state when transferred during runtime
        D.Assert(element.enabled, "{0} is not yet enabled.".Inject(element.FullName));
        Elements.Add(element);
        Data.AddElement(element.Data);
        element.AttachAsChildOf(UnitContainer);
        Subscribe(element);
        // TODO consider changing HQElement
        var unattachedSensors = element.Data.Sensors.Where(sensor => sensor.RangeMonitor == null);
        if (unattachedSensors.Any()) {
            //D.Log("{0} is attaching {1}'s sensors: {2}.", FullName, element.FullName, unattachedSensors.Select(s => s.Name).Concatenate());
            var unattachedSensorsArray = unattachedSensors.ToArray();
            AttachSensorsToMonitors(unattachedSensorsArray);
            // WARNING: Donot use the IEnumerable unattachedSensors here as it will no longer point to any unattached sensors, since they are all attached now
            // This is the IEnumerable<T> lazy evaluation GOTCHA
        }
        if (IsOperational) {
            // avoid the extra work if adding before beginning operations
            AssessIcon();
        }
    }

    /// <summary>
    /// Attaches one or more sensors to this command's SensorRangeMonitors.
    /// Note: Sensors are part of a Unit's elements but the monitors they attach to
    /// are children of the Command. Thus sensor range is always measured from
    /// the Command, not from the element.
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    public void AttachSensorsToMonitors(params Sensor[] sensors) {
        sensors.ForAll(sensor => {
            var monitor = UnitFactory.Instance.MakeMonitorInstance(sensor, this);
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
    public void DetachSensorsFromMonitors(params Sensor[] sensors) {
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

        DetachSensorsFromMonitors(element.Data.Sensors.ToArray());
        if (IsOperational) {
            // avoid this work if removing during startup
            if (Elements.Count > Constants.Zero) {
                AssessIcon();
            }
            else {
                D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(FullName));
                InitiateDeath();
            }
        }
    }

    private void Subscribe(AUnitElementItem element) {
        element.Data.onUserIntelCoverageChanged += OnElementUserIntelCoverageChanged;
        //element.onDeathOneShot += OnSubordinateElementDeath;  // element now informs command AFTER it broadcasts its onDeathEvent
        // previously, sequencing issues surfaced, depending on the order in which subscribers signed up for the event
    }

    private void Unsubscribe(AUnitElementItem element) {
        element.Data.onUserIntelCoverageChanged -= OnElementUserIntelCoverageChanged;
    }

    public void OnSubordinateElementDeath(IUnitElementItem deadSubordinateElement) {
        D.Log("{0} acknowledging {1} has been lost.", FullName, deadSubordinateElement.FullName);
        RemoveElement(deadSubordinateElement as AUnitElementItem);
    }

    protected virtual void OnHQElementChanging(AUnitElementItem newHQElement) {
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

    private void OnHQElementChanged() {
        HQElement.Data.IsHQ = true;
        Data.HQElementData = HQElement.Data;
        Radius = HQElement.Radius;
        //D.Log("{0}'s HQElement is now {1}. Radius = {2}.", Data.ParentName, HQElement.Data.Name, Radius);
        AttachCmdToHQElement(); // needs to occur before formation changed
        _formationGenerator.RegenerateFormation();
        if (DisplayMgr != null) {
            DisplayMgr.ResizePrimaryMesh(Radius);
        }
    }

    protected abstract void AttachCmdToHQElement();

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
        AssessIcon();
    }

    private void OnFormationChanged() {
        _formationGenerator.RegenerateFormation();
    }

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    protected internal virtual void PositionElementInFormation(AUnitElementItem element, Vector3 stationOffset) {
        element.Transform.position = HQElement.Position + stationOffset;
        //D.Log("{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //element.FullName, element.Transform.position, stationOffset, HQElement.FullName, HQElement.Transform.position);
    }

    protected internal virtual void CleanupAfterFormationGeneration() { }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleToUserChanged() {
        base.OnIsDiscernibleToUserChanged();
        ShowTrackingLabel(IsDiscernibleToUser);
    }

    protected virtual void OnIsSelectedChanged() {
        if (IsSelected) {
            ShowSelectedItemHud();
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
        Elements.ForAll(e => e.AssessHighlighting());
    }

    /// <summary>
    /// Shows the SelectedItemHudWindow for this item.
    /// </summary>
    /// <remarks>This method must be called prior to notifying SelectionMgr of the selection change. 
    /// HoveredItemHudWindow subscribes to the change and needs the SelectedItemHud to already 
    /// be resized and showing so it can position itself properly. Hiding the SelectedItemHud is 
    /// handled by the SelectionMgr when there is no longer an item selected.
    /// </remarks>
    protected abstract void ShowSelectedItemHud();

    protected override void OnUserIntelCoverageChanged() {
        base.OnUserIntelCoverageChanged();
        AssessIcon();    // UNCLEAR is this needed? How does IntelCoverage of Cmd change icon contents?
    }

    private void OnElementUserIntelCoverageChanged() {
        AssessIcon();
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

    public override void AssessHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.Selected);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowHighlights(HighlightID.Selected);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    private void ShowTrackingLabel(bool toShow) {
        //D.Log("{0}.ShowTrackingLabel({1}) called. IsTrackingLabelEnabled = {2}.", FullName, toShow, IsTrackingLabelEnabled);
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
    }

    #endregion

    #region Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    #endregion

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected void OnTargetDeath(IMortalItem deadTarget) { RelayToCurrentState(deadTarget); }

    void OnDetectedEnemy() { RelayToCurrentState(); }   // TODO connect to sensors when I get them

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
    public bool __CheckForDamage(bool isHQElementAlive, CombatStrength elementDamageSustained, float elementDamageSeverity) {
        //D.Log("{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
        //FullName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.Chance(elementDamageSeverity) : true;
        if (isHit) {
            TakeHit(elementDamageSustained);
        }
        else {
            //D.Log("{0} avoided a hit.", FullName);
        }
        return isHit;
    }

    public override void TakeHit(CombatStrength elementDamageSustained) {
        if (DebugSettings.Instance.AllPlayersInvulnerable) {
            return;
        }
        CombatStrength damageToCmd = elementDamageSustained - Data.DefensiveStrength;
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
    protected override bool ApplyDamage(CombatStrength damageToCmd, out float damageSeverity) {
        var __combinedDmgToCmd = damageToCmd.Combined;
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
        iconEventListener.onHover -= (go, isOver) => OnHover(isOver);
        iconEventListener.onClick -= (go) => OnClick();
        iconEventListener.onDoubleClick -= (go) => OnDoubleClick();
        iconEventListener.onPress -= (go, isDown) => OnPress(isDown);
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #endregion

    #region INavigableTarget Members

    public override string DisplayName { get { return Data.ParentName; } }

    // override reqd as AMortalItem base version accesses AItemData, not ACommandData
    // since ACommandData.Topography must use new rather than override
    public override Topography Topography { get { return Data.Topography; } }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return Data.GetUserIntelCoverage() != IntelCoverage.None; } }

    public override float OptimalCameraViewingDistance { get { return UnitRadius * optViewDistanceFactor; } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return UnitRadius; } }

    public override float HighlightRadius { get { return Screen.height * 0.03F; } }

    #endregion

}

