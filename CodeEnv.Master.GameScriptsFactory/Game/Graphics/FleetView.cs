// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetView.cs
//  A class for managing the elements of a fleet's UI, those that are not already handled by 
//  the UI classes for ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a fleet's UI, those that are not already handled by the UI classes for ships.
/// </summary>
public class FleetView : ACommandView, ICameraFollowable, IHighlightTrackingLabel {
    //public class FleetView : AFocusableView, ICameraFollowable, IFleetViewable, ISelectable, IHighlightTrackingLabel {

    public new FleetPresenter Presenter {
        get { return base.Presenter as FleetPresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = false;
    private GuiTrackingLabel _trackingLabel;

    //public AudioClip dying;
    //private AudioSource _audioSource;

    private VelocityRay _velocityRay;

    //private Transform _fleetIconTransform;
    //private Vector3 _fleetIconPivotOffset;
    //private UISprite _fleetIconSprite;
    //private ScaleRelativeToCamera _fleetIconScaler;
    //private IIcon _fleetIcon;   // IMPROVE not really used for now
    //private Vector3 _iconSize;

    //private CtxObject _ctxObject;
    //private Billboard _billboard;

    //protected override void Awake() {
    //    base.Awake();
    //    _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
    //    _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
    //    _isCirclesRadiusDynamic = false;
    //    circleScaleFactor = 0.03F;
    //    minimumCameraViewingDistanceMultiplier = 0.8F;
    //    optimalCameraViewingDistanceMultiplier = 1.2F;
    //    InitializeFleetIcon();
    //    UpdateRate = FrameUpdateFrequency.Normal;
    //}

    //protected override void Start() {
    //    base.Start();
    //    __InitializeContextMenu();
    //    InitializeTrackingTarget();
    //}

    protected override void InitializePresenter() {
        Presenter = new FleetPresenter(this);
    }

    //private void InitializeTrackingTarget() {
    //    TrackingTarget = Presenter.GetFlagship();
    //}

    protected override void InitializeTrackingTarget() {
        TrackingTarget = Presenter.GetFlagship();
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.gameObject.SetActive(IsDiscernible);
        }
        //_collider.enabled = IsDiscernible;
        //_billboard.gameObject.SetActive(IsDiscernible);
        //ShowFleetIcon(IsDiscernible);
        ShowVelocityRay(IsDiscernible);
    }

    //private void OnTrackingTargetChanged() {
    //    _fleetIconPivotOffset = new Vector3(Constants.ZeroF, TrackingTarget.collider.bounds.extents.y, Constants.ZeroF);
    //    InitializeTrackingLabel();
    //}

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        InitializeTrackingLabel();
    }

    protected override void OnPlayerIntelContentChanged() {
        base.OnPlayerIntelContentChanged();
        Presenter.NotifyShipsOfIntelChange();
    }

    //void OnPress(bool isDown) {
    //    if (GameInputHelper.IsRightMouseButton()) {
    //        OnRightPress(isDown);
    //    }
    //}

    //private void OnRightPress(bool isDown) {
    //    if (IsDiscernible) {
    //        if (IsSelected) {
    //            Presenter.RequestContextMenu(isDown);
    //        }
    //    }
    //}

    protected override void RequestContextMenu(bool isDown) {
        Presenter.RequestContextMenu(isDown);
    }

    //protected override void OnClick() {
    //    base.OnClick();
    //    if (GameInputHelper.IsLeftMouseButton()) {
    //        OnLeftClick();
    //    }
    //}

    //private void OnLeftClick() {
    //    if (IsDiscernible) {
    //        KeyCode notUsed;
    //        if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
    //            Presenter.__SimulateAllShipsAttacked();
    //            return;
    //        }
    //        IsSelected = true;
    //    }
    //}

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAllShipsAttacked();
    }

    //private void OnIsSelectedChanged() {
    //    AssessHighlighting();
    //    Presenter.OnIsSelectedChanged();
    //}

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        Presenter.OnIsSelectedChanged();
    }

    //void OnDoubleClick() {
    //    if (GameInputHelper.IsLeftMouseButton()) {
    //        OnLeftDoubleClick();
    //    }
    //}

    //private void OnLeftDoubleClick() {
    //    if (IsDiscernible) {
    //        Presenter.__RandomChangeOfHeadingAndSpeed();
    //        __SimulateStealthToggle();
    //    }
    //}

    //private void __SimulateStealthToggle() {
    //    PlayerIntel.Source = PlayerIntel.Source == IntelSource.None ? IntelSource.InfoNet : IntelSource.None;
    //}

    protected override void Update() {
        base.Update();
        KeepViewOverTarget();
    }

    //protected override void OccasionalUpdate() {
    //    base.OccasionalUpdate();
    //    KeepColliderOverFleetIcon();
    //}

    //private void KeepViewOverTarget() {
    //    if (TrackingTarget != null) {
    //        _transform.position = TrackingTarget.position;
    //        _transform.rotation = TrackingTarget.rotation;

    //        // Notes: _fleetIconPivotOffset is a worldspace offset to the top of the leadship collider and doesn't change with scale, position or rotation
    //        // The approach below will also work if we want a viewport offset that is a constant percentage of the viewport
    //        //Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShipTransform.position + _fleetIconPivotOffset);
    //        //Vector3 worldOffsetLocation = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconViewportOffset);
    //        //_fleetIconTransform.localPosition = worldOffsetLocation - _leadShipTransform.position;
    //        _fleetIconTransform.localPosition = _fleetIconPivotOffset;
    //    }
    //}

    //private void KeepColliderOverFleetIcon() {
    //    (_collider as BoxCollider).size = Vector3.Scale(_iconSize, _fleetIconScaler.Scale);
    //    //D.Log("Fleet collider size now = {0}.", _collider.size);

    //    Vector3[] iconWorldCorners = _fleetIconSprite.worldCorners;
    //    Vector3 iconWorldCenter = iconWorldCorners[0] + (iconWorldCorners[2] - iconWorldCorners[0]) * 0.5F;
    //    // convert icon's world position to the equivalent local position on the fleetCmd transform
    //    (_collider as BoxCollider).center = _transform.InverseTransformPoint(iconWorldCenter);
    //}

    //private void InitializeTrackingLabel() {
    //    if (enableTrackingLabel) {
    //        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
    //        string fleetName = Presenter.Item.FleetName;
    //        _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(TrackingTarget, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance, Mathf.Infinity, fleetName);
    //    }
    //}

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            string fleetName = Presenter.Item.PieceName;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(TrackingTarget, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance, Mathf.Infinity, fleetName);
        }
    }


    //public override void AssessHighlighting() {
    //    if (!IsDiscernible) {
    //        Highlight(Highlights.None);
    //        return;
    //    }
    //    if (IsFocus) {
    //        if (IsSelected) {
    //            Highlight(Highlights.SelectedAndFocus);
    //            return;
    //        }
    //        Highlight(Highlights.Focused);
    //        return;
    //    }
    //    if (IsSelected) {
    //        Highlight(Highlights.Selected);
    //        return;
    //    }
    //    Highlight(Highlights.None);
    //}


    //private void ShowFleetIcon(bool toShow) {
    //    if (_fleetIconSprite != null) {
    //        _fleetIconSprite.gameObject.SetActive(toShow);
    //        // TODO audio on/off goes here
    //    }
    //}

    //protected override void Highlight(Highlights highlight) {
    //    switch (highlight) {
    //        case Highlights.Focused:
    //            ShowCircle(false, Highlights.Selected);
    //            ShowCircle(true, Highlights.Focused);
    //            break;
    //        case Highlights.Selected:
    //            ShowCircle(true, Highlights.Selected);
    //            ShowCircle(false, Highlights.Focused);
    //            break;
    //        case Highlights.SelectedAndFocus:
    //            ShowCircle(true, Highlights.Selected);
    //            ShowCircle(true, Highlights.Focused);
    //            break;
    //        case Highlights.None:
    //            ShowCircle(false, Highlights.Selected);
    //            ShowCircle(false, Highlights.Focused);
    //            break;
    //        case Highlights.General:
    //        case Highlights.FocusAndGeneral:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
    //    }
    //}

    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableFleetVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> fleetSpeed = Presenter.GetFleetSpeed();
                _velocityRay = new VelocityRay("FleetVelocityRay", _transform, fleetSpeed, parent: DynamicObjects.Folder,
                    width: 2F, color: GameColor.Green);
            }
            _velocityRay.Show(toShow);
        }
    }


    //protected override float calcNormalizedCircleRadius() {
    //    return Screen.height * circleScaleFactor;
    //}

    //#region ContextMenu

    //private void __InitializeContextMenu() {      // IMPROVE use of string
    //    _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
    //    CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
    //    _ctxObject.contextMenu = generalMenu;
    //    D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
    //    UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    //    //D.Log("Initial Fleet collider size = {0}.", _collider.size);
    //    EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
    //    EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
    //    EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    //}

    //private void OnContextMenuShow() {
    //    // UNDONE
    //}

    //private void OnContextMenuSelection() {
    //    // int itemId = CtxObject.current.selectedItem;
    //    // D.Log("{0} selected context menu item {1}.", _transform.name, itemId);
    //    // UNDONE
    //}

    //private void OnContextMenuHide() {
    //    // UNDONE
    //}

    //#endregion

    //private void InitializeFleetIcon() {
    //    _fleetIconSprite = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
    //    _fleetIconTransform = _fleetIconSprite.transform;
    //    _fleetIconScaler = _fleetIconTransform.gameObject.GetSafeMonoBehaviourComponent<ScaleRelativeToCamera>();
    //    // I need the collider sitting over the fleet icon to be 3D as it's rotation tracks the Cmd object, not the billboarded icon
    //    Vector2 iconSize = _fleetIconSprite.localSize;
    //    _iconSize = new Vector3(iconSize.x, iconSize.y, iconSize.x);
    //}

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
            _trackingLabel = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region IFleetViewable Members

    //public event Action onShowCompletion;

    //public void ShowDying() {
    //    new Job(ShowingDying(), toStart: true);
    //}

    //private IEnumerator ShowingDying() {
    //    if (dying != null) {
    //        _audioSource.PlayOneShot(dying);
    //    }
    //    _collider.enabled = false;
    //    //animation.Stop();
    //    //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
    //    yield return null;

    //    var sc = onShowCompletion;
    //    if (sc != null) {
    //        sc();
    //    }
    //}

    //private Transform _trackingTarget;
    ///// <summary>
    ///// The target transform that this FleetView tracks in worldspace. This is
    ///// typically the flagship of the fleet.
    ///// </summary>
    //public Transform TrackingTarget {
    //    private get { return _trackingTarget; }
    //    set { SetProperty<Transform>(ref _trackingTarget, value, "TrackingTarget", OnTrackingTargetChanged); }
    //}

    //public void ChangeFleetIcon(IIcon icon, GameColor color) {
    //    _fleetIcon = icon;
    //    _fleetIconSprite.spriteName = icon.Filename;
    //    _fleetIconSprite.color = color.ToUnityColor();
    //}

    ///// <summary>
    ///// The [float] radius of this object in units measured as the distance from the
    ///// center to the min or max extent. As bounds is a bounding box it is the longest
    ///// diagonal from the center to a corner of the box.    
    ///// Note the override - a fleet's collider and mesh (icon) both scale, thus AView's implementation can't be used
    ///// </summary>
    //public override float Radius { get { return 1.0F; } }   // TODO should reflect the rough radius of the fleet

    //#endregion

    //#region ICameraTargetable Members

    //public override bool IsEligible {
    //    get {
    //        return IsDiscernible;
    //    }
    //}

    //#endregion

    //#region ICameraFocusable Members

    //public override bool IsRetainedFocusEligible {
    //    get {
    //        return PlayerIntel.Source != IntelSource.None;
    //    }
    //}

    //#endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion


    //#region ISelectable Members

    //private bool _isSelected;
    //public bool IsSelected {
    //    get { return _isSelected; }
    //    set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    //}

    //#endregion

    #region IHighlightTrackingLabel Members

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    #endregion

}

