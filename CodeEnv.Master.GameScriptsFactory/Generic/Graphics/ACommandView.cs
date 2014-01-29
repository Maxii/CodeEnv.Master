﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandView.cs
//  Abstract base class for managing the UI of a Command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for managing the UI of a Command.
/// </summary>
public abstract class ACommandView : AFocusableView, ICommandViewable, ISelectable {

    public AudioClip dying;
    private AudioSource _audioSource;

    private Vector3 _cmdIconPivotOffset;
    private UISprite _cmdIconSprite;
    private Transform _cmdIconTransform;
    private ScaleRelativeToCamera _cmdIconScaler;
    //private IIcon _cmdIcon;   // IMPROVE not really used for now
    private Vector3 _cmdIconSize;

    private CtxObject _ctxObject;
    private Billboard _billboard;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        _isCirclesRadiusDynamic = false;
        circleScaleFactor = 0.03F;
        minimumCameraViewingDistanceMultiplier = 0.8F;
        optimalCameraViewingDistanceMultiplier = 1.2F;
        InitializeCmdIcon();
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
        InitializeTrackingTarget();
    }

    protected abstract void InitializeTrackingTarget();

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _collider.enabled = IsDiscernible;
        _billboard.enabled = IsDiscernible; // can't deactivate billboard gameobject as that would disable the Icon's CameraLOSChangedRelay
        ShowCommandIcon(IsDiscernible);
    }

    protected virtual void OnTrackingTargetChanged() {
        _cmdIconPivotOffset = new Vector3(Constants.ZeroF, TrackingTarget.collider.bounds.extents.y, Constants.ZeroF);
        PositionIcon();
        KeepColliderOverIcon();
    }

    void OnPress(bool isDown) {
        if (IsDiscernible) {
            if (GameInputHelper.IsRightMouseButton()) {
                OnRightPress(isDown);
            }
        }
    }

    private void OnRightPress(bool isDown) {
        if (IsSelected) {
            RequestContextMenu(isDown);
        }
    }

    protected abstract void RequestContextMenu(bool isDown);

    protected override void OnClick() {
        base.OnClick();
        if (IsDiscernible) {
            if (GameInputHelper.IsLeftMouseButton()) {
                KeyCode notUsed;
                if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    OnAltLeftClick();
                }
                else {
                    OnLeftClick();
                }
            }
        }
    }

    protected virtual void OnLeftClick() { }

    protected virtual void OnAltLeftClick() { }

    protected virtual void OnIsSelectedChanged() {
        AssessHighlighting();
    }

    void OnDoubleClick() {
        if (IsDiscernible && GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() {
        __SimulateStealthToggle();
    }

    private void __SimulateStealthToggle() {
        PlayerIntel.Source = PlayerIntel.Source == IntelSource.None ? IntelSource.InfoNet : IntelSource.None;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        KeepColliderOverIcon();
    }

    private void KeepColliderOverIcon() {
        (_collider as BoxCollider).size = Vector3.Scale(_cmdIconSize, _cmdIconScaler.Scale);
        //D.Log("Fleet collider size now = {0}.", _collider.size);

        Vector3[] iconWorldCorners = _cmdIconSprite.worldCorners;
        Vector3 iconWorldCenter = iconWorldCorners[0] + (iconWorldCorners[2] - iconWorldCorners[0]) * 0.5F;
        // convert icon's world position to the equivalent local position on the fleetCmd transform
        (_collider as BoxCollider).center = _transform.InverseTransformPoint(iconWorldCenter);
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

    private void ShowCommandIcon(bool toShow) {
        if (_cmdIconSprite != null) {
            _cmdIconSprite.enabled = toShow;    // IMPROVE what is best way to stop UISprite from rendering?
            // TODO audio on/off goes here
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

    protected override float calcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor;
    }

    #region ContextMenu

    private void __InitializeContextMenu() {      // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
        _ctxObject.contextMenu = generalMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        //D.Log("Initial Fleet collider size = {0}.", _collider.size);
        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        // UNDONE
    }

    private void OnContextMenuSelection() {
        // int itemId = CtxObject.current.selectedItem;
        // D.Log("{0} selected context menu item {1}.", _transform.name, itemId);
        // UNDONE
    }

    private void OnContextMenuHide() {
        // UNDONE
    }

    #endregion

    protected virtual void InitializeCmdIcon() {
        _cmdIconSprite = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        _cmdIconTransform = _cmdIconSprite.transform;
        _cmdIconScaler = _cmdIconSprite.gameObject.GetSafeMonoBehaviourComponent<ScaleRelativeToCamera>();
        // I need the collider sitting over the CmdIcon to be 3D as it's rotation tracks the Cmd object, not the billboarded icon
        Vector2 iconSize = _cmdIconSprite.localSize;
        _cmdIconSize = new Vector3(iconSize.x, iconSize.y, iconSize.x);
    }

    protected void PositionIcon() {
        // Notes: _cmdIconPivotOffset is a worldspace offset to the top of the TrackingTarget collider and doesn't change with scale, position or rotation
        // The approach below will also work if we want a viewport offset that is a constant percentage of the viewport
        //Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(TrackingTarget.position + _cmdIconPivotOffset);
        //Vector3 worldOffsetLocation = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _cmdIconViewportOffset);
        //_cmdIconTransform.localPosition = worldOffsetLocation - TrackingTarget.position;
        _cmdIconTransform.localPosition = _cmdIconPivotOffset;
    }

    #region ICommandViewable Members

    public event Action onShowCompletion;

    public void ShowDying() {
        new Job(ShowingDying(), toStart: true);
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        _collider.enabled = false;
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;

        var sc = onShowCompletion;
        if (sc != null) {
            sc();
        }
    }

    private Transform _trackingTarget;
    /// <summary>
    /// The target transform that this FleetView tracks in worldspace. This is
    /// typically the flagship of the fleet.
    /// </summary>
    public Transform TrackingTarget {
        protected get { return _trackingTarget; }
        set { SetProperty<Transform>(ref _trackingTarget, value, "TrackingTarget", OnTrackingTargetChanged); }
    }

    public void ChangeCmdIcon(IIcon icon) {
        //_cmdIcon = icon;
        _cmdIconSprite.spriteName = icon.Filename;
        _cmdIconSprite.color = icon.Color.ToUnityColor();
    }

    /// <summary>
    /// The [float] radius of this object in units measured as the distance from the
    /// center to the min or max extent. As bounds is a bounding box it is the longest
    /// diagonal from the center to a corner of the box.    
    /// Note the override - a fleet's collider and mesh (icon) both scale, thus AView's implementation can't be used
    /// </summary>
    public override float Radius { get { return 1.0F; } }   // TODO should reflect the rough radius of the fleet

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return IsDiscernible;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get {
            return PlayerIntel.Source != IntelSource.None;
        }
    }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

