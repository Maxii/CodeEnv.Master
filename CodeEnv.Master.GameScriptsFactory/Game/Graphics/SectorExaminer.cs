// --------------------------------------------------------------------------------------------------------------------
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

    private IntVector3 _currentSectorID;
    /// <summary>
    /// The Location of this SectorExaminer expressed as the ID of the Sector it is over.
    /// </summary>
    public IntVector3 CurrentSectorID {
        get {
            if (_currentSectorID == default(IntVector3)) {
                // First time initialization. Can't be done in Awake as it can run before SectorGrid.Awake?
                _currentSectorID = _sectorGrid.GetSectorIdThatContains(Position);
            }
            return _currentSectorID;
        }
        private set { SetProperty<IntVector3>(ref _currentSectorID, value, "CurrentSectorID", CurrentSectorIdPropChangedHandler, CurrentSectorIdPropChangingHandler); }
    }

    private bool IsSectorWireframeShowing { get { return _wireframe != null && _wireframe.IsShowing; } }

    private bool IsContextMenuShowing { get { return _ctxControl != null && _ctxControl.IsShowing; } }

    private float _distanceToHighlightedSector;
    private SectorGrid _sectorGrid;
    private CubeWireframe _wireframe;
    /// <summary>
    /// The Collider over the center of this Examiner (which is over the Sector) used for
    /// actuation of the Context Menu.
    /// </summary>
    private BoxCollider _collider;
    private ICtxControl _ctxControl;
    private ITrackingWidget _sectorIDLabel;
    private PlayerViewMode _viewMode;
    private InputManager _inputMgr;
    private GameInputHelper _inputHelper;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _sectorGrid = SectorGrid.Instance;
        _inputHelper = GameInputHelper.Instance;
        _inputMgr = InputManager.Instance;
        _distanceToHighlightedSector = _distanceInSectorsFromCamera * TempGameValues.SectorSideLength;
        InitializeColliderHotspot();
        Subscribe();
    }

    private void InitializeColliderHotspot() {
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        float sectorSideLength = TempGameValues.SectorSideLength;
        float colliderSideLength = sectorSideLength / 5F;
        _collider.size = new Vector3(colliderSideLength, colliderSideLength, colliderSideLength);   // 240x240x240 collider
        float colliderOffsetDistance = (sectorSideLength / 2F) - colliderSideLength;
        Vector3 colliderLocalOffset = new Vector3(0F, colliderOffsetDistance, 0F);  // above the center to avoid systems in center
        _collider.center = colliderLocalOffset;
        _collider.enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, PlayerViewModePropChangedHandler));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        IDisposable d;
        if (toSubscribe) {
            d = MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, IntVector3>(cc => cc.SectorID, CameraSectorIdPropChangedHandler);
            D.Assert(!_subscriptions.Contains(d), DebugName);
            _subscriptions.Add(d);
            UICamera.onMouseMove += MouseMovedEventHandler;
        }
        else {
            d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
            bool isRemoved = _subscriptions.Remove(d);
            D.Assert(isRemoved, DebugName);
            d.Dispose();
            UICamera.onMouseMove -= MouseMovedEventHandler;
        }
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

    private void HandleCurrentSectorIdChanged() {
        Vector3 sectorPosition;
        bool isPositionFound = _sectorGrid.__TryGetSectorPosition(CurrentSectorID, out sectorPosition);
        D.Assert(isPositionFound);  // CurrentSectorID doesn't change if no sector is present
        transform.position = sectorPosition;
        ShowSectorWireframe(true);
        UpdateSectorIDLabel();
        HighlightSectorContents(true);
        ShowSectorDebugLog(true);
    }

    private void CameraSectorIdPropChangedHandler() {
        ShowSectorUnderMouse();
    }

    private void PlayerViewModePropChangedHandler() {
        HandleViewModeChanged();
    }

    private void HandleViewModeChanged() {
        _viewMode = PlayerViews.Instance.ViewMode;
        switch (_viewMode) {
            case PlayerViewMode.SectorView:
                DynamicallySubscribe(true);
                _collider.enabled = true;
                break;
            case PlayerViewMode.NormalView:
                // turn off wireframe, sectorID label, collider, contextMenu, sector highlights and HUD
                DynamicallySubscribe(false);
                ShowSectorWireframe(false);

                if (_wireframe != null) {
                    _wireframe.Dispose();
                    _wireframe = null;
                }
                _collider.enabled = false;
                if (IsContextMenuShowing) {
                    _ctxControl.Hide();
                }
                HighlightSectorContents(false);
                ShowSectorDebugLog(false);

                // OPTIMIZE cache sector
                Sector sector;
                if (_sectorGrid.__TryGetSector(CurrentSectorID, out sector)) {
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

    private void HoverEventHandler(bool isOver) {
        HandleHoveredChanged(isOver);
    }

    private void HandleHoveredChanged(bool isOver) {
        if (_viewMode == PlayerViewMode.SectorView) {
            D.Log(ShowDebugLog, "SectorExaminer calling Sector {0}.ShowHud({1}).", CurrentSectorID, isOver);
            Sector sector;
            if (_sectorGrid.__TryGetSector(CurrentSectorID, out sector)) {
                sector.ShowHud(isOver);
            }
        }
    }

    private void PressEventHandler(bool isDown) {
        if (_inputHelper.IsRightMouseButton && !isDown) {
            HandleRightPressRelease();
        }
    }

    void OnHover(bool isOver) {
        HoverEventHandler(isOver);
    }

    void OnPress(bool isDown) {
        PressEventHandler(isDown);
    }

    private void HandleRightPressRelease() {
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            if (_ctxControl == null) {
                _ctxControl = InitializeContextMenu();
            }
            _ctxControl.AttemptShowContextMenu();
        }
    }

    #endregion

    private void UpdateSectorIDLabel() {
        if (_sectorIDLabel == null) {
            _sectorIDLabel = InitializeSectorIDLabel();
        }
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
            IntVector3 sectorIdUnderMouse = _sectorGrid.GetSectorIdThatContains(mouseWorldPoint);
            if (_sectorGrid.__IsSectorPresentAt(sectorIdUnderMouse)) {
                if (CurrentSectorID != sectorIdUnderMouse) {    // avoid the SetProperty equivalent warnings
                    CurrentSectorID = sectorIdUnderMouse;
                }
            }
            else {
                HighlightSectorContents(false);
                ShowSectorWireframe(false);
                ShowSectorDebugLog(false);
            }
        }
    }

    private void ShowSectorWireframe(bool toShow) {
        if (toShow == IsSectorWireframeShowing) {
            return;
        }
        //D.Log(ShowDebugLog, "{0}.ShowSectorWireframe({1})", GetType().Name, toShow);

        if (toShow) {
            if (_wireframe == null) {
                _wireframe = new CubeWireframe("SectorWireframe", transform, TempGameValues.SectorSize, width: 2F, color: TempGameValues.SectorHighlightColor);
            }
            UpdateSectorIDLabel();
        }
        _wireframe.Show(toShow);
        _sectorIDLabel.Show(toShow);
    }

    private void HighlightSectorContents(bool toShow) {
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

    private void ShowSectorDebugLog(bool toShow) {
        if (_showDebugLog) {
            Sector sector;
            if (_sectorGrid.__TryGetSector(CurrentSectorID, out sector)) {
                sector.ShowDebugLog = toShow;
            }
        }
    }

    protected override void Cleanup() {
        if (_wireframe != null) { _wireframe.Dispose(); }
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
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IWidgetTrackable Members

    public Vector3 GetOffset(WidgetPlacement placement) {

        switch (placement) {
            case WidgetPlacement.Above:
                return new Vector3(Constants.ZeroF, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveLeft:
                return new Vector3(-_collider.bounds.extents.x, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.AboveRight:
                return new Vector3(_collider.bounds.extents.x, _collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Below:
                return new Vector3(Constants.ZeroF, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowLeft:
                return new Vector3(-_collider.bounds.extents.x, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.BelowRight:
                return new Vector3(_collider.bounds.extents.x, -_collider.bounds.extents.y, Constants.ZeroF);
            case WidgetPlacement.Left:
                return new Vector3(-_collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Right:
                return new Vector3(_collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Over:
                return _collider.center;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    public Vector3 Position { get { return transform.position; } }

    public bool IsMobile { get { return true; } }

    public string DebugName { get { return GetType().Name; } }

    #endregion

    #region Archive

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

