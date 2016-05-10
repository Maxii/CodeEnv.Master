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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
/// Singleton that displays the highlighted wireframe of a sector and provides a context menu for fleet commands
/// relevant to the highlighted sector.
/// </summary>
public class SectorExaminer : AMonoSingleton<SectorExaminer>, IWidgetTrackable {

    private const string SectorIDLabelText = "Sector {0}\nGridBox {1}.";

    /// <summary>
    /// The distance from the front of the camera (in sectors) this examiner uses to determine which sector to highlight.
    /// </summary>
    //[FormerlySerializedAs("distanceInSectorsFromCamera")]
    [Range(1, 3)]
    [Tooltip("The distance from the front of the camera (in sectors) this examiner uses to determine which sector to highlight.")]
    [SerializeField]
    private int _distanceInSectorsFromCamera = 2;

    private Index3D _currentSectorIndex = new Index3D();
    /// <summary>
    /// The Location of this SectorViewer expressed as the index of the 
    /// Sector it is over.
    /// </summary>
    public Index3D CurrentSectorIndex {
        get { return _currentSectorIndex; }
        private set { SetProperty<Index3D>(ref _currentSectorIndex, value, "CurrentSectorIndex", CurrentSectorIndexPropChangedHandler); }
    }

    private bool IsSectorViewJobRunning { get { return _sectorViewJob != null && _sectorViewJob.IsRunning; } }

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
    private Job _sectorViewJob;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _sectorGrid = SectorGrid.Instance;
        _inputHelper = GameInputHelper.Instance;
        _inputMgr = InputManager.Instance;
        _distanceToHighlightedSector = _distanceInSectorsFromCamera * TempGameValues.SectorSideLength;
        InitializeCenterCollider();
        Subscribe();
    }

    private void InitializeCenterCollider() {
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        float colliderSideLength = TempGameValues.SectorSideLength / 30F;
        _collider.size = new Vector3(colliderSideLength, colliderSideLength, colliderSideLength);   // 40x40x40 center collider
        _collider.enabled = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, PlayerViewModePropChangedHandler));
    }

    // Note: no pausing of the sectorViewJob as I want to be able to inspect sectors when paused

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, Index3D>(cc => cc.SectorIndex, CameraSectorIndexPropChangedHandler));
        }
        else {
            IDisposable d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
            _subscriptions.Remove(d);
            d.Dispose();
        }
    }

    private SectorCtxControl InitializeContextMenu() {
        return new SectorCtxControl(this);
    }

    #region Event and Property Change Handlers

    private void CurrentSectorIndexPropChangedHandler() {
        transform.position = _sectorGrid.GetSector(CurrentSectorIndex).Position;
        UpdateSectorIDLabel();
    }

    private void CameraSectorIndexPropChangedHandler() {
        // does nothing for now
    }

    private void PlayerViewModePropChangedHandler() {
        _viewMode = PlayerViews.Instance.ViewMode;
        switch (_viewMode) {
            case PlayerViewMode.SectorView:
                DynamicallySubscribe(true);
                _sectorViewJob = new Job(ShowSectorUnderMouse(), toStart: true, jobCompleted: (wasKilled) => {
                    //TODO
                });
                _collider.enabled = true;
                break;
            case PlayerViewMode.NormalView:
                // turn off wireframe, sectorID label, collider, contextMenu and HUD
                DynamicallySubscribe(false);
                if (IsSectorViewJobRunning) {
                    _sectorViewJob.Kill();
                }
                if (IsSectorWireframeShowing) {
                    ShowSector(false);
                    _wireframe.Dispose();
                    _wireframe = null;
                }
                _collider.enabled = false;
                if (IsContextMenuShowing) {
                    _ctxControl.Hide();
                }

                // OPTIMIZE cache sector and sectorView
                var sector = _sectorGrid.GetSector(CurrentSectorIndex);
                if (sector != null) {  // can be null if camera is located where no sector object was created
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
        if (_viewMode == PlayerViewMode.SectorView) {
            //D.Log("SectorExaminer calling Sector {0}.ShowHud({1}).", CurrentSectorIndex, isOver);
            _sectorGrid.GetSector(CurrentSectorIndex).ShowHud(isOver);
        }
    }

    private void PressEventHandler(bool isDown) {
        if (_inputHelper.IsRightMouseButton && !isDown) {
            HandleRightPressRelease();
        }
    }

    private void HandleRightPressRelease() {
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            if (_ctxControl == null) {
                _ctxControl = InitializeContextMenu();
            }
            _ctxControl.TryShowContextMenu();
        }
    }

    void OnHover(bool isOver) {
        HoverEventHandler(isOver);
    }

    void OnPress(bool isDown) {
        PressEventHandler(isDown);
    }

    #endregion

    private void UpdateSectorIDLabel() {
        if (_sectorIDLabel == null) {
            _sectorIDLabel = InitializeSectorIDLabel();
        }
        _sectorIDLabel.Set(SectorIDLabelText.Inject(CurrentSectorIndex, _sectorGrid.GetGridBoxLocation(CurrentSectorIndex)));
    }

    private ITrackingWidget InitializeSectorIDLabel() {
        var sectorIDLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.Over);
        sectorIDLabel.Color = TempGameValues.SectorHighlightColor;
        return sectorIDLabel;
    }

    private IEnumerator ShowSectorUnderMouse() {    // IMPROVE use UICamera.onMouseMove
        while (true) {
            if (!IsContextMenuShowing) {   // don't change highlighted sector while context menu is showing

                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = _distanceToHighlightedSector;
                Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
                Index3D sectorIndexUnderMouse = _sectorGrid.GetSectorIndex(mouseWorldPoint);
                bool toShow;
                SectorItem notUsed;
                if (toShow = _sectorGrid.TryGetSector(sectorIndexUnderMouse, out notUsed)) {
                    if (CurrentSectorIndex != sectorIndexUnderMouse) {    // avoid the SetProperty equivalent warnings
                        CurrentSectorIndex = sectorIndexUnderMouse;
                    }
                }
                ShowSector(toShow);
            }
            yield return null;
        }
    }

    private void ShowSector(bool toShow) {
        //D.Log("ShowSector({0})", toShow);
        if (toShow == IsSectorWireframeShowing) {
            return;
        }

        if (toShow) {
            if (_wireframe == null) {
                _wireframe = new CubeWireframe("SectorWireframe", transform, TempGameValues.SectorSize, width: 2F, color: TempGameValues.SectorHighlightColor);
            }
            UpdateSectorIDLabel();
        }
        _wireframe.Show(toShow);
        _sectorIDLabel.Show(toShow);
    }

    protected override void Cleanup() {
        if (_wireframe != null) { _wireframe.Dispose(); }
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_sectorIDLabel);
        if (_sectorViewJob != null) { _sectorViewJob.Dispose(); }
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
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
                return Vector3.zero;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    public Vector3 Position { get { return transform.position; } }

    public string DisplayName { get { return GetType().Name; } }

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

