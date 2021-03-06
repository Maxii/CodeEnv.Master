﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorExaminer.cs
// Singleton that displays the highlighted wireframe of a sector and provides a context menu for fleet commands
// relevant to the highlighted sector.
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
using UnityEngine;

/// <summary>
/// Singleton that displays the highlighted wireframe of a sector, provides a context menu for fleet commands
/// relevant to the highlighted sector and controls display of the Sector HUD.
/// </summary>
public class SectorExaminer : AMonoSingleton<SectorExaminer>, IWidgetTrackable {

    private const string SectorIDLabelText = "Sector {0}.";
    private const string DebugNameFormat = "{0}[{1}]";

    private const float HotSpotColliderSideLength = TempGameValues.SectorSideLength / 5F;  // 240

    [Tooltip("Controls showing the debug log for SectorExaminer and the highlighted sector.")]
    [SerializeField]
    private bool _showDebugLog = false;
    private bool ShowDebugLog { get { return _showDebugLog; } }

    /// <summary>
    /// The distance from the front of the camera (in sectors) this examiner uses to determine which sector to highlight.
    /// </summary>
    //[FormerlySerializedAs("distanceInSectorsFromCamera")]
    [Range(1, 3)]
    [Tooltip("The distance from the front of the camera (in sectors) this examiner uses to determine which sector to highlight.")]
    [SerializeField]
    private int _distanceInSectorsFromCamera = 2;

    [Tooltip("Click to show a box around the hot spot.")]
    [SerializeField]
    private bool _showHotSpot = false;

    public string DebugName {
        get {
            string sectorIdMsg = IsCurrentSectorIdValid ? CurrentSectorID.DebugName : "Invalid Sector";
            return DebugNameFormat.Inject(typeof(SectorExaminer).Name, sectorIdMsg);
        }
    }

    public bool IsCurrentSectorIdValid { get { return _currentSectorID != default(IntVector3); } }

    private bool _isCurrentSectorIdInitialized = false;
    private IntVector3 _currentSectorID = default(IntVector3);
    /// <summary>
    /// The Location of this SectorExaminer expressed as the ID of the Sector it is over.
    /// <remarks>7.16.18 Can be default value as ShowSectorUnderMouse now changes CurrentSectorID
    /// if over area without an ASector.</remarks>
    /// </summary>
    public IntVector3 CurrentSectorID {
        get {
            if (!_isCurrentSectorIdInitialized) {
                // First time initialization. Can't be done in Awake as it can run before SectorGrid.Awake?
                _currentSectorID = InitializeCurrentSectorID();
            }
            return _currentSectorID;
        }
        private set { SetProperty<IntVector3>(ref _currentSectorID, value, "CurrentSectorID", CurrentSectorIdPropChangedHandler, CurrentSectorIdPropChangingHandler); }
    }

    private bool IsSectorWireframeShowing { get { return _sectorWireframe != null && _sectorWireframe.IsShowing; } }

    private bool IsSectorHotSpotWireframeShowing { get { return _hotSpotWireframe != null && _hotSpotWireframe.IsShowing; } }

    private bool IsContextMenuShowing { get { return _ctxControl != null && _ctxControl.IsShowing; } }

    private float _distanceToHighlightedSector;
    private SectorGrid _sectorGrid;
    private CubeWireframe _sectorWireframe;
    private CubeWireframe _hotSpotWireframe;

    private BoxCollider _hotSpotCollider;
    private Transform _hotSpotTransform;
    private ICtxControl _ctxControl;
    private ITrackingWidget _sectorIDLabel;
    private PlayerViewMode _viewMode;
    private InputManager _inputMgr;
    private GameInputHelper _inputHelper;
    private IList<IDisposable> _subscriptions;
    private MainCameraControl _mainCameraCntl;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _sectorGrid = SectorGrid.Instance;
        _inputHelper = GameInputHelper.Instance;
        _inputMgr = InputManager.Instance;
        _mainCameraCntl = MainCameraControl.Instance;
        _distanceToHighlightedSector = _distanceInSectorsFromCamera * TempGameValues.SectorSideLength;
        InitializeHotspot();
        Subscribe();
    }

    private void InitializeHotspot() {
        // 6.15.18 Ngui 3DWorld event raycasts don't detect Trigger Colliders. If you have 3DWorld objects moving around in 3D world
        // space (like FleetIcons, this hotSpot, etc), and you want them to respond to events (hover, etc.), the object's collider 
        // IsTrigger = false is required. To keep this collider from 'colliding' with other 3D world objects, you need to place it
        // on a layer that doesn't collide with the Default layer, in this case the TransparentFX layer. In addition, the 
        // WorldEventDispatchers (Non-UI Camera(s) with a UICamera script using EventType = 3DWorld) must be able to 'see' the
        // TransparentFX layer.
        _hotSpotTransform = gameObject.GetSingleComponentInImmediateChildren<Transform>();
        D.AssertEqual(Layers.TransparentFX, (Layers)_hotSpotTransform.gameObject.layer);

        _hotSpotCollider = UnityUtility.ValidateComponentPresence<BoxCollider>(_hotSpotTransform.gameObject);
        _hotSpotCollider.size = new Vector3(HotSpotColliderSideLength, HotSpotColliderSideLength, HotSpotColliderSideLength);   // 240x240x240 collider
        _hotSpotCollider.isTrigger = false;
        _hotSpotCollider.center = Vector3.zero;
        _hotSpotCollider.enabled = false;

        float hotSpotYOffset = (TempGameValues.SectorSideLength / 2F) - HotSpotColliderSideLength; // 360
        Vector3 hotSpotLocalOffset = new Vector3(0F, hotSpotYOffset, 0F);  // above the center to avoid systems in center
        _hotSpotTransform.localPosition = hotSpotLocalOffset;

        var hotSpotEventListener = MyEventListener.Get(_hotSpotTransform.gameObject);
        hotSpotEventListener.onHover += HotSpotHoverEventHandler;
        hotSpotEventListener.onPress += HotSpotPressEventHandler;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, PlayerViewModePropChangedHandler));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _mainCameraCntl.sectorIDChanged += CameraSectorIDChangedEventHandler;
            UICamera.onMouseMove += MouseMovedEventHandler;
        }
        else {
            _mainCameraCntl.sectorIDChanged -= CameraSectorIDChangedEventHandler;
            UICamera.onMouseMove -= MouseMovedEventHandler;
        }
    }

    private IntVector3 InitializeCurrentSectorID() {
        D.Assert(!_isCurrentSectorIdInitialized);
        _isCurrentSectorIdInitialized = true;
        IntVector3 sectorID;
        if (_sectorGrid.TryGetSectorIDContaining(Position, out sectorID)) {
            return sectorID;
        }
        D.Warn("{0} is initializing CurrentSectorID in a location without a Sector.", DebugName);
        return default(IntVector3);
    }

    private SectorCtxControl InitializeContextMenu() {
        return new SectorCtxControl(this);
    }

    #region Event and Property Change Handlers

    private void MouseMovedEventHandler(Vector2 delta) {
        ShowSectorUnderMouse();
    }

    private void CurrentSectorIdPropChangingHandler(IntVector3 newSectorID) {
        // Current Sector ID is about to change so turn off any Sector Highlights showing
        HighlightSectorContents(false);
        ShowSectorDebugLog(false);
    }

    private void CurrentSectorIdPropChangedHandler() {
        HandleCurrentSectorIdChanged();
    }

    private void CameraSectorIDChangedEventHandler(object sender, EventArgs e) {
        ShowSectorUnderMouse();
    }

    private void PlayerViewModePropChangedHandler() {
        HandleViewModeChanged();
    }

    private void HotSpotHoverEventHandler(GameObject go, bool isOver) {
        HandleHotSpotHoveredChanged(isOver);
    }

    private void HotSpotPressEventHandler(GameObject go, bool isDown) {
        if (_inputHelper.IsRightMouseButton && !isDown) {
            HandleHotSpotRightPressRelease();
        }
    }

    #endregion

    private void HandleViewModeChanged() {
        _viewMode = PlayerViews.Instance.ViewMode;
        switch (_viewMode) {
            case PlayerViewMode.SectorView:
                DynamicallySubscribe(true);
                _hotSpotCollider.enabled = true;
                break;
            case PlayerViewMode.NormalView:
                // turn off wireframe, sectorID label, collider, contextMenu, sector highlights and HUD
                DynamicallySubscribe(false);
                ShowSectorWireframes(false);

                if (_sectorWireframe != null) {
                    _sectorWireframe.Dispose();
                    _sectorWireframe = null;
                }
                if (_hotSpotWireframe != null) {
                    _hotSpotWireframe.Dispose();
                    _hotSpotWireframe = null;
                }
                _hotSpotCollider.enabled = false;
                if (IsContextMenuShowing) {
                    _ctxControl.Hide();
                }
                HighlightSectorContents(false);
                ShowSectorDebugLog(false);

                if (IsCurrentSectorIdValid) {
                    ASector sector = _sectorGrid.GetSector(CurrentSectorID);    // OPTIMIZE cache sector?
                    if (sector.IsHudShowing) {
                        sector.ShowHud(false);
                    }
                }
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_viewMode));
        }
    }

    private void HandleHotSpotHoveredChanged(bool isOver) {
        D.AssertEqual(PlayerViewMode.SectorView, _viewMode);
        if (IsCurrentSectorIdValid) {
            D.Log(ShowDebugLog, "SectorExaminer calling Sector {0}.ShowHud({1}).", CurrentSectorID, isOver);
            var sector = _sectorGrid.GetSector(CurrentSectorID);
            sector.ShowHud(isOver);
        }
        else {
            _sectorGrid.Sectors.Where(sector => sector.IsHudShowing).ForAll(sector => sector.ShowHud(false));
        }
    }

    private void HandleHotSpotRightPressRelease() {
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl = _ctxControl ?? InitializeContextMenu();
            _ctxControl.AttemptShowContextMenu();
        }
    }

    private void HandleCurrentSectorIdChanged() {
        if (IsCurrentSectorIdValid) {
            Vector3 sectorCenterLoc = _sectorGrid.GetSectorCenterLocation(CurrentSectorID);
            transform.position = sectorCenterLoc;
            ShowSectorWireframes(true);
            UpdateSectorIDLabel();
            HighlightSectorContents(true);
            ShowSectorDebugLog(true);
        }
        _hotSpotCollider.enabled = IsCurrentSectorIdValid;
    }

    private void UpdateSectorIDLabel() {
        D.Assert(IsCurrentSectorIdValid);
        _sectorIDLabel = _sectorIDLabel ?? InitializeSectorIDLabel();
        _sectorIDLabel.Set(SectorIDLabelText.Inject(CurrentSectorID));
    }

    private ITrackingWidget InitializeSectorIDLabel() {
        var sectorIDLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.Over);
        sectorIDLabel.Color = TempGameValues.SectorHighlightColor;
        return sectorIDLabel;
    }

    private void ShowSectorUnderMouse() {
        if (!IsContextMenuShowing) {   // don't change highlighted sector while context menu is showing
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = _distanceToHighlightedSector;
            Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            // mouseWorldPoint can be in cells without SectorIDs
            IntVector3 sectorIdUnderMouse;
            if (!_sectorGrid.TryGetSectorIDContaining(mouseWorldPoint, out sectorIdUnderMouse)) {
                // 7.14.18 Turn off these visible attributes if previous CurrentSectorID is valid BEFORE making it invalid
                HighlightSectorContents(false);
                ShowSectorWireframes(false);
                ShowSectorDebugLog(false);
            }
            if (CurrentSectorID != sectorIdUnderMouse) {    // avoid the SetProperty equivalent warnings
                CurrentSectorID = sectorIdUnderMouse;
            }
        }
    }

    private void ShowSectorWireframes(bool toShow) {
        //D.Log(ShowDebugLog, "{0}.ShowSectorWireframe({1})", DebugName, toShow);
        if (toShow != IsSectorWireframeShowing) {
            if (toShow) {
                _sectorWireframe = _sectorWireframe ?? new CubeWireframe("SectorWireframe", transform, TempGameValues.SectorSize, width: 2F,
                    color: TempGameValues.SectorHighlightColor);
                UpdateSectorIDLabel();
            }
            _sectorWireframe.Show(toShow);
            _sectorIDLabel.Show(toShow);
        }
        if (toShow != IsSectorHotSpotWireframeShowing) {
            bool toShowHotSpot = _showHotSpot && toShow;
            if (toShowHotSpot) {
                _hotSpotWireframe = _hotSpotWireframe ?? new CubeWireframe("HotSpotWireframe", _hotSpotTransform, _hotSpotCollider.size,
                    color: GameColor.Red);
            }
            _hotSpotWireframe.Show(toShowHotSpot);
        }
    }

    private void HighlightSectorContents(bool toShow) {
        if (IsCurrentSectorIdValid) {
            IEnumerable<ISectorViewHighlightable> highlightablesInSector;
            if (GameManager.Instance.UserAIManager.Knowledge.TryGetSectorViewHighlightables(CurrentSectorID, out highlightablesInSector)) {
                D.Log(ShowDebugLog, "{0} found {1} to highlight in Sector {2}.", GetType().Name, highlightablesInSector.Select(h => h.DebugName).Concatenate(), CurrentSectorID);
                highlightablesInSector.ForAll(highlightable => {
                    if (highlightable.IsSectorViewHighlightShowing != toShow) {
                        highlightable.ShowSectorViewHighlight(toShow);
                    }
                });
            }
        }
    }

    private void ShowSectorDebugLog(bool toShow) {
        if (ShowDebugLog && IsCurrentSectorIdValid) {
            var sector = _sectorGrid.GetSector(CurrentSectorID);
            sector.ShowDebugLog = toShow;
        }
    }

    protected override void Cleanup() {
        if (_sectorWireframe != null) {
            _sectorWireframe.Dispose();
        }
        if (_hotSpotWireframe != null) {
            _hotSpotWireframe.Dispose();
        }
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_sectorIDLabel);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
        UICamera.onMouseMove -= MouseMovedEventHandler;
        _mainCameraCntl.sectorIDChanged -= CameraSectorIDChangedEventHandler;

        var hotSpotEventListener = MyEventListener.Get(_hotSpotTransform.gameObject);
        hotSpotEventListener.onHover -= HotSpotHoverEventHandler;
        hotSpotEventListener.onPress -= HotSpotPressEventHandler;
    }

    public override string ToString() {
        return DebugName;
    }

    #region IWidgetTrackable Members

    public Vector3 GetOffset(WidgetPlacement placement) {

        switch (placement) {
            case WidgetPlacement.Above:
                return new Vector3(Constants.ZeroF, _hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveLeft:
                return new Vector3(-_hotSpotCollider.bounds.extents.x, _hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveRight:
                return new Vector3(_hotSpotCollider.bounds.extents.x, _hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Below:
                return new Vector3(Constants.ZeroF, -_hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowLeft:
                return new Vector3(-_hotSpotCollider.bounds.extents.x, -_hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowRight:
                return new Vector3(_hotSpotCollider.bounds.extents.x, -_hotSpotCollider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Left:
                return new Vector3(-_hotSpotCollider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Right:
                return new Vector3(_hotSpotCollider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Over:
                return _hotSpotCollider.center;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    public Vector3 Position { get { return transform.position; } }

    public bool IsMobile { get { return true; } }

    #endregion

    #region Archive

    // Dynamically subscribe approach using Property Change
    //private void DynamicallySubscribe(bool toSubscribe) {
    //    IDisposable d;
    //    if (toSubscribe) {
    //        d = MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, IntVector3>(cc => cc.SectorID, CameraSectorIdPropChangedHandler);
    //        D.Assert(!_subscriptions.Contains(d), DebugName);
    //        _subscriptions.Add(d);
    //        UICamera.onMouseMove += MouseMovedEventHandler;
    //    }
    //    else {
    //        d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
    //        bool isRemoved = _subscriptions.Remove(d);
    //        D.Assert(isRemoved, DebugName);
    //        d.Dispose();
    //        UICamera.onMouseMove -= MouseMovedEventHandler;
    //    }
    //}










    // The Wireframe Hot spot approach alternative to using a small collider
    ///// <summary>
    ///// Called when a mouse button is pressed and is not consumed by another object. This implementation
    ///// is a custom context menu picker for the SectorViewer.
    ///// </summary>
    ///// <param name="button">The Ngui mouse button.</param>
    ///// <param name="isDown">if set to <c>true</c> [is down].</param>
    //private void OnUnconsumedPress(NguiMouseButton button, bool isDown) {
    //    if (_viewMode == PlayerViewMode.SectorView && button == NguiMouseButton.Right && !isDown) {
    //        FleetView selectedFleetView = _selectionMgr.CurrentSelection as FleetView;
    //        if (selectedFleetView != null && _wireframe.IsMouseOverHotSpot) {
    //            _ctxObject.ShowMenu();
    //        }
    //    }
    //}

    #endregion

}

