// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandView.cs
//  Abstract base class for managing the UI of a Command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for managing the UI of a Command.
/// </summary>
public abstract class AUnitCommandView : AMortalItemView, ICommandViewable, ISelectable {

    public new AUnitCommandPresenter Presenter {
        get { return base.Presenter as AUnitCommandPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override float SphericalHighlightSizeMultiplier { get { return 1F; } }

    public float minCameraViewDistanceMultiplier = 0.9F;    // just inside Unit's highlight sphere
    public float optimalCameraViewDistanceMultiplier = 2F;  // encompasses all elements of the Unit

    private CommandTrackingSprite _cmdIcon;

    protected override void Awake() {
        base.Awake();
        _isCirclesRadiusDynamic = false;
        circleScaleFactor = 0.03F;
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void InitializeVisualMembers() {
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

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowCmdIcon(IsDiscernible);
    }

    protected virtual void OnTrackingTargetChanged() {
        PositionCmdOverTrackingTarget();
    }

    protected virtual void OnIsSelectedChanged() {
        AssessHighlighting();
    }

    protected virtual void PositionCmdOverTrackingTarget() {
        _transform.position = TrackingTarget.Transform.position;
        _transform.rotation = TrackingTarget.Transform.rotation;
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

    private void ShowCmdIcon(bool toShow) {
        if (_cmdIcon != null) {
            _cmdIcon.Show(toShow);
        }
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

    #region Intel Stealth Testing

    private IntelCoverage __normalIntelCoverage;
    private void __ToggleStealthSimulation() {
        if (__normalIntelCoverage == IntelCoverage.None) {
            __normalIntelCoverage = PlayerIntel.CurrentCoverage;
        }
        PlayerIntel.CurrentCoverage = PlayerIntel.CurrentCoverage == __normalIntelCoverage ? IntelCoverage.Aware : __normalIntelCoverage;
    }

    #endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        //D.Log("{0}.OnLeftClick().", Presenter.FullName);
        IsSelected = true;
    }

    protected override void OnLeftDoubleClick() {
        base.OnLeftDoubleClick();
        __ToggleStealthSimulation();
    }

    #endregion

    #region ICommandViewable Members

    private IWidgetTrackable _trackingTarget;
    /// <summary>
    /// The target that this UnitCommand tracks in worldspace. 
    /// </summary>
    public IWidgetTrackable TrackingTarget {
        protected get { return _trackingTarget; }
        set { SetProperty<IWidgetTrackable>(ref _trackingTarget, value, "TrackingTarget", OnTrackingTargetChanged); }
    }

    public void ChangeCmdIcon(IIcon icon) {
        if (_cmdIcon != null) {
            _cmdIcon.Set(icon.Filename);
            _cmdIcon.Color = icon.Color;
            //D.Log("{0} Icon color is {1}.", Presenter.FullName, icon.Color.GetName());
            return;
        }
        //D.Warn("Attempting to change a null {0} to {1}.", typeof(CommandTrackingSprite).Name, icon.Filename);
    }

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

    #region IMortalViewable Members

    public override void OnDeath() {
        base.OnDeath();
        ShowCmdIcon(false);
    }

    #endregion

    #region IWidgetTrackable Members

    // IMPROVE Consider overriding GetOffset from AFocusableItemView and use TrackingTarget's GetOffset values instead
    // Currently, the Cmd's Radius is used to position the CmdIcon. As CmdRadius encompasses the whole cmd, the icon is 
    // quite a ways above the HQElement

    #endregion

}

