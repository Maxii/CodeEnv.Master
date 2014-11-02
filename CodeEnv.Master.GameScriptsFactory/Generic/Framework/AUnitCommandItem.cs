// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandItem.cs
// Abstract base class for Unit Command items. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Unit Command items. 
/// </summary>
public abstract class AUnitCommandItem : AMortalItemStateMachine, ISelectable, IUnitTarget {

    /// <summary>
    /// The transform that normally contains all elements and commands assigned to the Unit.
    /// </summary>
    public Transform UnitContainer { get; private set; }

    public new ACommandData Data {
        get { return base.Data as ACommandData; }
        set { base.Data = value; }
    }

    private AUnitElementItem _hqElement;
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    private IWidgetTrackable _trackingTarget;
    /// <summary>
    /// The target that this UnitCommand tracks in worldspace. 
    /// </summary>
    public IWidgetTrackable TrackingTarget {
        protected get { return _trackingTarget; }
        set { SetProperty<IWidgetTrackable>(ref _trackingTarget, value, "TrackingTarget", OnTrackingTargetChanged); }
    }

    public IList<AUnitElementItem> Elements { get; private set; }

    public float minCameraViewDistanceMultiplier = 0.9F;    // just inside Unit's highlight sphere
    public float optimalCameraViewDistanceMultiplier = 2F;  // encompasses all elements of the Unit

    protected override float SphericalHighlightSizeMultiplier { get { return 1F; } }

    protected FormationGenerator _formationGenerator;

    private CommandTrackingSprite _cmdIcon;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Elements = new List<AUnitElementItem>();
        _formationGenerator = new FormationGenerator(this);
        _isCirclesRadiusDynamic = false;
        circleScaleFactor = 0.03F;
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void InitializeModelMembers() {
        // the only collider is for player interaction with the item's CmdIcon
        UnitContainer = _transform.parent;
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<ACommandData, Formation>(d => d.UnitFormation, OnFormationChanged));

        Data.onCompositionChanged += OnCompositionChanged;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        _cmdIcon = TrackingWidgetFactory.Instance.CreateCmdTrackingSprite(this);
        // CmdIcon enabled state controlled by CmdIcon.Show()

        var cmdIconEventListener = _cmdIcon.EventListener;
        cmdIconEventListener.onHover += (cmdGo, isOver) => OnHover(isOver);
        cmdIconEventListener.onClick += (cmdGo) => OnClick();
        cmdIconEventListener.onDoubleClick += (cmdGo) => OnDoubleClick();
        cmdIconEventListener.onPress += (cmdGo, isDown) => OnPress(isDown);

        var cmdIconCameraLosChgdListener = _cmdIcon.CameraLosChangedListener;
        cmdIconCameraLosChgdListener.onCameraLosChanged += (cmdGo, inCameraLOS) => InCameraLOS = inCameraLOS;
        cmdIconCameraLosChgdListener.enabled = true;
    }

    #endregion

    #region Model Methods

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
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.FullName));
        if (Elements.Count == Constants.Zero) {
            D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(FullName));
            KillCommand();
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
        TrackingTarget = HQElement as IWidgetTrackable;
        _formationGenerator.RegenerateFormation();
    }

    private void OnCompositionChanged() {
        AssessCmdIcon();
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

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> [is hq element alive].</param>
    /// <returns><c>true</c> if the Command has taken damage.</returns>
    public bool __CheckForDamage(bool isHQElementAlive) {   // HACK needs work. Cmds should be hardened to defend against weapons, so pass along attackerWeaponStrength?
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.SplitChance() : true;
        if (isHit) {
            TakeHit(new CombatStrength(RandomExtended<ArmamentCategory>.Choice(__offensiveArmamentCategories), UnityEngine.Random.Range(1F, Data.MaxHitPoints)));
        }
        else {
            D.Log("{0} avoided a hit.", FullName);
        }
        return isHit;
    }

    protected internal virtual void PositionElementInFormation(AUnitElementItem element, Vector3 stationOffset) {
        element.Transform.position = HQElement.Position + stationOffset;
        //D.Log("{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //    element.FullName, element.Transform.position, stationOffset, HQElement.FullName, HQElement.Transform.position);
    }

    protected internal virtual void CleanupAfterFormationGeneration() { }

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        float damage = Data.Strength - attackerWeaponStrength;
        bool isCmdAlive = ApplyDamage(damage);
        D.Assert(isCmdAlive, "{0} should never die as a result of being hit.".Inject(Data.Name));
    }

    /// <summary>
    /// Kills this Command by setting its state to Dead.
    /// </summary>
    protected abstract void KillCommand();

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowCmdIcon(IsDiscernible);
    }

    protected virtual void OnTrackingTargetChanged() {
        PositionCmdOverTrackingTarget();
    }

    protected virtual void OnIsSelectedChanged() {
        AssessHighlighting();
        Elements.ForAll(e => e.AssessHighlighting());
        if (IsSelected) { SelectionManager.Instance.CurrentSelection = this; }
    }

    protected virtual void PositionCmdOverTrackingTarget() {
        _transform.position = TrackingTarget.Transform.position;
        _transform.rotation = TrackingTarget.Transform.rotation;
    }

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        Elements.ForAll(e => e.PlayerIntel.CurrentCoverage = PlayerIntel.CurrentCoverage);  // IMPROVE
        AssessCmdIcon();
    }

    private void AssessCmdIcon() {
        IIcon icon = MakeCmdIconInstance();
        ChangeCmdIcon(icon);
    }

    protected abstract IIcon MakeCmdIconInstance();

    private void ChangeCmdIcon(IIcon icon) {
        if (_cmdIcon != null) {
            _cmdIcon.Set(icon.Filename);
            _cmdIcon.Color = icon.Color;
            //D.Log("{0} Icon color is {1}.", Presenter.FullName, icon.Color.GetName());
            return;
        }
        //D.Warn("Attempting to change a null {0} to {1}.", typeof(CommandTrackingSprite).Name, icon.Filename);
    }

    private void ShowCmdIcon(bool toShow) {
        if (_cmdIcon != null) {
            _cmdIcon.Show(toShow);
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
        ShowCircle(toShow, highlight, _cmdIcon.WidgetTransform);
    }

    protected override float CalcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor;
    }

    #endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (Data != null) { Data.Dispose(); }
    }

    #endregion


    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected void DestroyUnitContainer() {
        UnityUtility.WaitOneToExecute((jobWasKilled) => {
            Destroy(UnitContainer.gameObject);
            D.Log("{0}.UnitContainer has been destroyed.", FullName);
        });
    }

    #endregion

    # region StateMachine Callbacks

    protected override void OnShowCompletion() {
        RelayToCurrentState();
    }

    protected void OnTargetDeath(IMortalItem deadTarget) {
        RelayToCurrentState(deadTarget);
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IDestinationTarget Members

    public override string DisplayName { get { return Data.ParentName; } }

    // override reqd as AMortalItem base version accesses AItemData, not ACommandData
    // since ACommandData.Topography must use new rather than override
    public override SpaceTopography Topography { get { return Data.Topography; } }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return PlayerIntel.CurrentCoverage != IntelCoverage.None; } }

    public override float OptimalCameraViewingDistance { get { return Radius * optimalCameraViewDistanceMultiplier; } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

    #region IWidgetTrackable Members

    // IMPROVE Consider overriding GetOffset from AFocusableItemView and use TrackingTarget's GetOffset values instead
    // Currently, the Cmd's Radius is used to position the CmdIcon. As CmdRadius encompasses the whole cmd, the icon is 
    // quite a ways above the HQElement

    #endregion

}

