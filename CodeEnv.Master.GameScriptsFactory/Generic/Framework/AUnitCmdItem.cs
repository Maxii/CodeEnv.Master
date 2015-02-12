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

//#define DEBUG_LOG
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
public abstract class AUnitCmdItem : AMortalItemStateMachine, ICommandItem, ISelectable, IUnitAttackableTarget {

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

    /// <summary>
    /// The radius of the entire Unit. 
    /// This is not the radius of the Command which is the radius of the HQElement.
    /// </summary>
    public virtual float UnitRadius { get; protected set; }

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

    protected override float SphericalHighlightRadius { get { return UnitRadius; } }

    protected override float ItemTypeCircleScale { get { return 0.03F; } }

    protected override float RadiusOfHighlightCircle {
        // this override eliminates the Radius of the Item in the calculation as Cmd radius changes with HQ Element
        get { return Screen.height * ItemTypeCircleScale; }
    }

    protected IList<ISensorRangeMonitor> _sensorRangeMonitors = new List<ISensorRangeMonitor>();
    protected FormationGenerator _formationGenerator;

    private CommandTrackingSprite _icon;
    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Elements = new List<AUnitElementItem>();
        _formationGenerator = new FormationGenerator(this);
        _isCirclesRadiusDynamic = false;
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void InitializeModelMembers() {
        // the only collider is for player interaction with the item's CmdIcon
        UnitContainer = _transform.parent;
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AUnitCmdItemData, Formation>(d => d.UnitFormation, OnFormationChanged));
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeIcon();
    }

    private void InitializeIcon() {
        //D.Log("{0}.InitializeIcon() called.", FullName);
        _icon = TrackingWidgetFactory.Instance.CreateCmdTrackingSprite(this);
        // CmdIcon enabled state controlled by CmdIcon.Show()

        var cmdIconEventListener = _icon.EventListener;
        cmdIconEventListener.onHover += (cmdIconGo, isOver) => OnHover(isOver);
        cmdIconEventListener.onClick += (cmdIconGo) => OnClick();
        cmdIconEventListener.onDoubleClick += (cmdIconGo) => OnDoubleClick();
        cmdIconEventListener.onPress += (cmdIconGo, isDown) => OnPress(isDown);

        var cmdIconCameraLosChgdListener = _icon.CameraLosChangedListener;
        cmdIconCameraLosChgdListener.onCameraLosChanged += (cmdIconGo, inCameraLOS) => InCameraLOS = inCameraLOS;
        cmdIconCameraLosChgdListener.enabled = true;
    }

    private ITrackingWidget InitializeTrackingLabel() {
        D.Assert(HQElement != null);
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(this, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.Name = DisplayName + CommonTerms.Label;
        trackingLabel.Set(DisplayName);
        trackingLabel.Color = Owner.Color;
        return trackingLabel;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessCmdIcon();
    }

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public virtual void AddElement(AUnitElementItem element) {
        D.Assert(!Elements.Contains(element), "{0} attempting to add {1} that is already present.".Inject(FullName, element.FullName));
        D.Assert(!element.IsHQElement, "{0} adding element {1} already designated as the HQ Element.".Inject(FullName, element.FullName));
        // elements should already be enabled when added to a Cmd as that is commonly their state when transferred during runtime
        D.Assert((element as MonoBehaviour).enabled, "{0} is not yet enabled.".Inject(element.FullName));
        Elements.Add(element);
        Data.AddElement(element.Data);
        element.AttachElementAsChildOfUnitContainer(UnitContainer);
        // TODO consider changing HQElement
        var unattachedSensors = element.Data.Sensors.Where(sensor => sensor.RangeMonitor == null);
        if (unattachedSensors.Any()) {
            //D.Log("{0} is attaching {1}'s sensors: {2}.", FullName, element.FullName, unattachedSensors.Select(s => s.Name).Concatenate());
            var unattachedSensorsArray = unattachedSensors.ToArray();
            AttachSensorsToMonitors(unattachedSensorsArray);
            // WARNING: Donot use the IEnumerable unattachedSensors here as it will no longer point to any unattached sensors, since they are all attached now
            // This is the IEnumerable<T> lazy evaluation GOTCHA
        }
        if (IsAliveAndOperating) {
            // avoid the extra work if adding before beginning operations
            AssessCmdIcon();
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
            if (!_sensorRangeMonitors.Contains(monitor)) {
                // only need to record and setup range monitors once. The same monitor can have more than 1 sensor
                _sensorRangeMonitors.Add(monitor);
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
                _sensorRangeMonitors.Remove(monitor);
                D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(SensorRangeMonitor).Name, sensor.Name);
                UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
            }
        });
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, "{0} not found.".Inject(element.FullName));
        Data.RemoveElement(element.Data);

        DetachSensorsFromMonitors(element.Data.Sensors.ToArray());
        if (IsAliveAndOperating) {
            // avoid this work if removing during startup
            if (Elements.Count > Constants.Zero) {
                AssessCmdIcon();
            }
            else {
                D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(FullName));
                InitiateDeath();
            }
        }
    }

    public void OnSubordinateElementDeath(AUnitElementItem deadElement) {
        D.Assert(deadElement != null);
        D.Log("{0} acknowledging {1} has been lost.", FullName, deadElement.FullName);
        RemoveElement(deadElement);
    }

    protected virtual void OnHQElementChanging(AUnitElementItem newHQElement) {
        Arguments.ValidateNotNull(newHQElement);
        if (HQElement != null) {
            HQElement.IsHQElement = false;
        }
        if (!Elements.Contains(newHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Warn("{0} assigned HQElement {1} that is not already present in Unit.", FullName, newHQElement.FullName);
            AddElement(newHQElement);
        }
    }

    protected virtual void OnHQElementChanged() {
        HQElement.IsHQElement = true;
        Data.HQElementData = HQElement.Data;
        //D.Log("{0}'s HQElement is now {1}.", Data.ParentName, HQElement.Data.Name);
        Radius = HQElement.Radius;
        PlaceCmdUnderHQElement();
        _formationGenerator.RegenerateFormation();
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
    }

    private void OnFormationChanged() {
        _formationGenerator.RegenerateFormation();
    }

    protected override void OnDeath() {
        base.OnDeath();
        ShowCmdIcon(false);
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    private void PlaceCmdUnderHQElement() {
        D.Assert(UnitContainer != null);    // can't relocate Cmd until it has recorded the UnitContainer
        UnityUtility.AttachChildToParent(gameObject, HQElement.gameObject);
    }

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    protected internal virtual void PositionElementInFormation(AUnitElementItem element, Vector3 stationOffset) {
        element.Transform.position = HQElement.Position + stationOffset;
        //D.Log("{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //    element.FullName, element.Transform.position, stationOffset, HQElement.FullName, HQElement.Transform.position);
    }

    protected internal virtual void CleanupAfterFormationGeneration() { }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowCmdIcon(IsDiscernible);
        ShowTrackingLabel(IsDiscernible);
    }

    protected virtual void OnIsSelectedChanged() {
        AssessHighlighting();
        Elements.ForAll(e => e.AssessHighlighting());
        if (IsSelected) { SelectionManager.Instance.CurrentSelection = this; }
    }

    protected override void OnHumanPlayerIntelCoverageChanged() {
        base.OnHumanPlayerIntelCoverageChanged();
        AssessCmdIcon();
    }

    private void AssessCmdIcon() {
        //D.Log("{0}.AssessCmdIcon() called.", FullName);
        IIcon icon = MakeCmdIconInstance();
        ChangeCmdIcon(icon);
    }

    protected abstract IIcon MakeCmdIconInstance();

    private void ChangeCmdIcon(IIcon icon) {
        if (_icon != null) {
            _icon.Set(icon.Filename);
            _icon.Color = icon.Color;
            //D.Log("{0} Icon filename: {1}, color: {2}.", FullName, icon.Filename, icon.Color.GetName());
            return;
        }
        //D.Log("{0} attempting to change a CmdIcon that isn't initialized yet.", FullName);
    }

    private void ShowCmdIcon(bool toShow) {
        if (_icon != null) {
            //D.Log("{0}.ShowCmdIcon({1}) called.", FullName, toShow);
            _icon.Show(toShow);
        }
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected override void ShowCircle(bool toShow, Highlights highlight) {
        ShowCircle(toShow, highlight, _icon.WidgetTransform);
    }

    private void ShowTrackingLabel(bool toShow) {
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
    }

    #endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    #endregion

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected override void OnShowCompletion() { RelayToCurrentState(); }

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
        D.Log("{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
            FullName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.Chance(elementDamageSeverity) : true;
        if (isHit) {
            TakeHit(elementDamageSustained);
        }
        else {
            D.Log("{0} avoided a hit.", FullName);
        }
        return isHit;
    }

    public override void TakeHit(CombatStrength elementDamageSustained) {
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

    protected void DestroyUnitContainer() {
        Destroy(UnitContainer.gameObject);
        D.Log("{0}.UnitContainer has been destroyed.", DisplayName);
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
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

    public override bool IsRetainedFocusEligible { get { return GetHumanPlayerIntelCoverage() != IntelCoverage.None; } }

    public override float OptimalCameraViewingDistance { get { return UnitRadius * optViewDistanceFactor; } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

