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
/// A class for managing the elements of a fleet's UI, those that are not already handled by 
///  the UI classes for ships.
/// </summary>
public class FleetView : MovingView, IFleetViewable, ISelectable {

    protected new FleetPresenter Presenter {
        get { return base.Presenter as FleetPresenter; }
        set { base.Presenter = value; }
    }

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Indicates whether the item this view
    /// is associated with is detectable by the human player. 
    /// eg. a fleet the human player has no intel about is not detectable.
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
    }


    /// <summary>
    /// The [float] radius of this object in units measured as the distance from the
    /// center to the min or max extent. As bounds is a bounding box it is the longest
    /// diagonal from the center to a corner of the box.    
    /// Note the override - a fleet's collider and mesh (icon) both scale, thus AView's implementation can't be used
    /// </summary>
    public override float Radius { get { return 1.0F; } }   // TODO should reflect the rough radius of the fleet

    public bool enableTrackingLabel = false;
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.05F, Constants.ZeroF);
    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;
    public AudioClip dying;

    private AudioSource _audioSource;
    private GuiTrackingLabel _trackingLabel;
    private Vector3 _trackingLabelPivotOffset;

    private VelocityRay _velocityRay;

    private Transform _fleetIconTransform;
    private Vector3 _fleetIconPivotOffset;
    private UISprite _fleetIconSprite;
    private ScaleRelativeToCamera _fleetIconScaler;
    private IIcon _fleetIcon;   // IMPROVE not really used for now
    private Vector3 _iconSize;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        _isCirclesRadiusDynamic = false;
        circleScaleFactor = 0.03F;
        minimumCameraViewingDistanceMultiplier = 0.8F;
        optimalCameraViewingDistanceMultiplier = 1.2F;
        maxAnimateDistance = 1; // FIXME maxAnimateDistance not used, this is a dummy value to avoid the warning in AGraphics
        InitializeFleetIcon();
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    protected override void InitializePresenter() {
        Presenter = new FleetPresenter(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnNotDiscernible = new Component[1] { 
            collider 
        };
        disableGameObjectOnNotDiscernible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().gameObject
        };
    }

    private void OnTrackingTargetChanged() {
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, TrackingTarget.collider.bounds.extents.y, Constants.ZeroF);
        _trackingLabelPivotOffset = new Vector3(Constants.ZeroF, TrackingTarget.collider.bounds.extents.y, Constants.ZeroF);
        if (_trackingLabel != null) {
            _trackingLabel.TargetPivotOffset = _trackingLabelPivotOffset;
        }
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnInCameraLOSChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        IsDetectable = PlayerIntelLevel != IntelLevel.Nil;
        Presenter.OnIntelLevelChanged();
    }

    void OnPress(bool isDown) {
        if (IsSelected) {
            Presenter.OnPressWhileSelected(isDown);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (IsDetectable) {
            KeyCode notUsed;
            if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                Presenter.__SimulateAllShipsAttacked();
                return;
            }
            IsSelected = true;
        }
    }

    private void OnIsSelectedChanged() {
        AssessHighlighting();
        Presenter.OnIsSelectedChanged();
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            Presenter.__OnLeftDoubleClick();
        }
    }

    protected override void Update() {
        base.Update();
        KeepViewOverTarget();
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        KeepColliderOverFleetIcon();
    }

    private void KeepViewOverTarget() {
        if (TrackingTarget != null) {
            _transform.position = TrackingTarget.position;
            _transform.rotation = TrackingTarget.rotation;

            // Notes: _fleetIconPivotOffset is a worldspace offset to the top of the leadship collider and doesn't change with scale, position or rotation
            // The approach below will also work if we want a viewport offset that is a constant percentage of the viewport
            //Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShipTransform.position + _fleetIconPivotOffset);
            //Vector3 worldOffsetLocation = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconViewportOffset);
            //_fleetIconTransform.localPosition = worldOffsetLocation - _leadShipTransform.position;
            _fleetIconTransform.localPosition = _fleetIconPivotOffset;
        }
    }

    // starts getting called upon IsRunning
    private void KeepColliderOverFleetIcon() {
        (_collider as BoxCollider).size = Vector3.Scale(_iconSize, _fleetIconScaler.Scale);
        //D.Log("Fleet collider size now = {0}.", _collider.size);

        Vector3[] iconWorldCorners = _fleetIconSprite.worldCorners;
        Vector3 iconWorldCenter = iconWorldCorners[0] + (iconWorldCorners[2] - iconWorldCorners[0]) * 0.5F;
        // convert icon's world position to the equivalent local position on the fleetCmd transform
        (_collider as BoxCollider).center = _transform.InverseTransformPoint(iconWorldCenter);
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = base.EnableBasedOnDistanceToCamera(condition);
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            bool toShowTrackingLabel = false;
            if (condition) {
                distanceToCamera = distanceToCamera == Constants.Zero ? _transform.DistanceToCameraInt() : distanceToCamera;    // not really needed
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
            }
            //D.Log("FleetTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        // use LeadShip collider for the offset rather than the FleetCmd collider as the FleetCmd collider changes scale dynamically. 
        _trackingLabelPivotOffset = new Vector3(Constants.ZeroF, TrackingTarget.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(_transform, _trackingLabelPivotOffset, trackingLabelOffsetFromPivot);
        return trackingLabel;
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    public override void AssessHighlighting() {
        if (!IsDetectable || !InCameraLOS) {
            ShowFleetIcon(false);
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                ShowFleetIcon(true);
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            ShowFleetIcon(true);
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            ShowFleetIcon(true);
            Highlight(Highlights.Selected);
            return;
        }
        ShowFleetIcon(true);
        Highlight(Highlights.None);
    }

    private void ShowFleetIcon(bool toShow) {
        if (_fleetIconSprite != null) {
            _fleetIconSprite.gameObject.SetActive(toShow);
            // TODO audio on/off goes here
        }
        ShowVelocityRay(toShow);
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

    protected override float calcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor;
    }

    private void __InitializeContextMenu() {      // IMPROVE use of string
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
        ctxObject.contextMenu = generalMenu;
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        //D.Log("Initial Fleet collider size = {0}.", _collider.size);
    }

    private void InitializeFleetIcon() {
        _fleetIconSprite = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        _fleetIconTransform = _fleetIconSprite.transform;
        _fleetIconScaler = _fleetIconTransform.gameObject.GetSafeMonoBehaviourComponent<ScaleRelativeToCamera>();
        // I need the collider sitting over the fleet icon to be 3D as it's rotation tracks the Cmd object, not the billboarded icon
        Vector2 iconSize = _fleetIconSprite.localSize;
        _iconSize = new Vector3(iconSize.x, iconSize.y, iconSize.x);
    }

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

    #region IFleetViewable Members

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
        onShowCompletion();
    }

    private Transform _trackingTarget;
    /// <summary>
    /// The target transform that this FleetView tracks in worldspace. This is
    /// typically the flagship of the fleet.
    /// </summary>
    public Transform TrackingTarget {
        private get { return _trackingTarget; }
        set { SetProperty<Transform>(ref _trackingTarget, value, "TrackingTarget", OnTrackingTargetChanged); }
    }

    public void ChangeFleetIcon(IIcon icon, GameColor color) {
        _fleetIcon = icon;
        _fleetIconSprite.spriteName = icon.Filename;
        _fleetIconSprite.color = color.ToUnityColor();
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return IsDetectable;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get { return IsDetectable; }
    }

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    //protected override float CalcOptimalCameraViewingDistance() {
    //    return optimalFleetViewingDistance;
    //}

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

