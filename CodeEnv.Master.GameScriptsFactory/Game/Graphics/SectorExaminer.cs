// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorExaminer.cs
//  Singleton that displays the highlighted wireframe of a sector and provides a context menu for fleet commands
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
public class SectorExaminer : AMonoBaseSingleton<SectorExaminer>, IDisposable, IWidgetTrackable {

    public int distanceInSectorsFromCamera = 2;

    private Index3D _currentSectorIndex = new Index3D();
    /// <summary>
    /// The Location of this SectorViewer expressed as the index of the 
    /// Sector it is over.
    /// </summary>
    public Index3D CurrentSectorIndex {
        get { return _currentSectorIndex; }
        private set { SetProperty<Index3D>(ref _currentSectorIndex, value, "CurrentSectorIndex", OnCurrentSectorIndexChanged); }
    }

    private float _distanceToHighlightedSector;
    private SelectionManager _selectionMgr;
    private CubeWireframe _wireframe;
    /// <summary>
    /// The Collider over the center of this Examiner (which is over the Sector) used for
    /// actuation of the Context Menu.
    /// </summary>
    private BoxCollider _collider;

    private bool _isContextMenuShowing;
    private CtxObject _ctxObject;

    private string _sectorIDLabelText = "Sector {0}" + Constants.NewLine + "GridBox {1}.";
    private ITrackingWidget _sectorIDLabel;

    private PlayerViewMode _viewMode;
    private Job _sectorViewJob;

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _selectionMgr = SelectionManager.Instance;
        _distanceToHighlightedSector = distanceInSectorsFromCamera * TempGameValues.SectorSideLength;
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
        _subscribers = new List<IDisposable>();
        _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
            //GameInput.Instance.onUnconsumedPress += OnUnconsumedPress;
        }
        else {
            IDisposable d = _subscribers.Single(s => s as DisposePropertyChangedSubscription<CameraControl> != null);
            _subscribers.Remove(d);
            d.Dispose();
            //GameInput.Instance.onUnconsumedPress -= OnUnconsumedPress;
        }
    }

    protected override void Start() {
        base.Start();
        InitializeContextMenu();
    }

    void OnHover(bool isOver) {
        if (_viewMode == PlayerViewMode.SectorView) {
            //D.Log("SectorExaminer calling Sector {0}.ShowHud({1}).", CurrentSectorIndex, isOver);
            SectorGrid.GetSector(CurrentSectorIndex).gameObject.GetSafeMonoBehaviourComponent<SectorView>().ShowHud(isOver);
        }
    }

    #region ContextMenu

    void OnPress(bool isDown) {
        if (GameInputHelper.Instance.IsRightMouseButton() && !isDown) {
            OnRightPressRelease();
        }
    }

    private void OnRightPressRelease() {
        if (_viewMode == PlayerViewMode.SectorView) {
            FleetCmdView selectedFleetView = _selectionMgr.CurrentSelection as FleetCmdView;
            if (selectedFleetView != null) {
                _ctxObject.ShowMenu();
            }
        }
    }

    // The Wireframe Hotspot approach alternative to using a small collider
    ///// <summary>
    ///// Called when a mouse button is pressed and is not consumed by another object. This implementation
    ///// is a custom context menu picker for the SectorViewer.
    ///// </summary>
    ///// <param name="button">The Ngui mousebutton.</param>
    ///// <param name="isDown">if set to <c>true</c> [is down].</param>
    //private void OnUnconsumedPress(NguiMouseButton button, bool isDown) {
    //    if (_viewMode == PlayerViewMode.SectorView && button == NguiMouseButton.Right && !isDown) {
    //        FleetView selectedFleetView = _selectionMgr.CurrentSelection as FleetView;
    //        if (selectedFleetView != null && _wireframe.IsMouseOverHotSpot) {
    //            _ctxObject.ShowMenu();
    //        }
    //    }
    //}

    private void InitializeContextMenu() {    // IMPROVE string use
        _ctxObject = UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(gameObject);
        CtxMenu sectorMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "SectorMenu");
        _ctxObject.contextMenu = sectorMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        _isContextMenuShowing = true;
    }

    private void OnContextMenuSelection() {
        int menuId = CtxObject.current.selectedItem;
        FleetCmdView_Player selectedFleetView = _selectionMgr.CurrentSelection as FleetCmdView_Player;
        IFleetCmdModel selectedFleet = selectedFleetView.Presenter.Model;
        if (menuId == 4) {  // UNDONE
            SectorModel sector = SectorGrid.GetSector(CurrentSectorIndex);
            selectedFleet.CurrentOrder = new FleetOrder(FleetDirective.MoveTo, sector, Speed.FleetStandard);
        }
    }

    private void OnContextMenuHide() {
        _isContextMenuShowing = false;
    }

    #endregion

    private void UpdateSectorIDLabel() {
        if (_sectorIDLabel == null) {
            _sectorIDLabel = InitializeSectorIDLabel();
        }
        _sectorIDLabel.Set(_sectorIDLabelText.Inject(CurrentSectorIndex, SectorGrid.GetGridBoxLocation(CurrentSectorIndex)));
    }

    private ITrackingWidget InitializeSectorIDLabel() {
        var sectorIDLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(this, WidgetPlacement.Over);
        sectorIDLabel.Color = UnityDebugConstants.SectorHighlightColor;
        return sectorIDLabel;
    }

    private void OnCurrentSectorIndexChanged() {
        _transform.position = SectorGrid.GetSector(CurrentSectorIndex).Position;
        UpdateSectorIDLabel();
    }

    private void OnCameraSectorIndexChanged() {
        // does nothing for now
    }

    private void OnPlayerViewModeChanged() {
        _viewMode = PlayerViews.Instance.ViewMode;
        switch (_viewMode) {
            case PlayerViewMode.SectorView:
                DynamicallySubscribe(true);
                if (_sectorViewJob == null) {
                    _sectorViewJob = new Job(ShowSectorUnderMouse(), toStart: false, onJobComplete: delegate {
                        // TODO
                    });
                }
                _sectorViewJob.Start();
                _collider.enabled = true;
                break;
            case PlayerViewMode.NormalView:
                // turn off wireframe, sectorID label, collider, contextMenu and Hud
                DynamicallySubscribe(false);
                if (_sectorViewJob != null && _sectorViewJob.IsRunning) {
                    _sectorViewJob.Kill();
                    ShowSector(false);
                }
                _collider.enabled = false;
                _ctxObject.HideMenu();

                // OPTIMIZE cache sector and sectorView
                SectorModel sectorModel = SectorGrid.GetSector(CurrentSectorIndex);
                if (sectorModel != null) {  // can be null if camera is located where no sector object was created
                    SectorView sectorView = sectorModel.gameObject.GetSafeMonoBehaviourComponent<SectorView>();
                    if (sectorView.HudPublisher.IsHudShowing) {
                        sectorView.ShowHud(false);
                    }
                }
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_viewMode));
        }
    }

    private IEnumerator ShowSectorUnderMouse() {
        while (true) {
            if (!_isContextMenuShowing) {   // don't change highlighted sector while context menu is showing
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = _distanceToHighlightedSector;
                Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
                Index3D sectorIndexUnderMouse = SectorGrid.GetSectorIndex(mouseWorldPoint);
                bool toShow;
                SectorModel notUsed;
                if (toShow = SectorGrid.TryGetSector(sectorIndexUnderMouse, out notUsed)) {
                    if (!CurrentSectorIndex.Equals(sectorIndexUnderMouse)) {
                        CurrentSectorIndex = sectorIndexUnderMouse; // avoid the SetProperty equivalent warnings
                    }
                }
                ShowSector(toShow);
            }
            yield return null;
        }
    }

    private void ShowSector(bool toShow) {
        //D.Log("ShowSector({0})", toShow);
        if (!toShow && _wireframe == null) {
            return;
        }
        if (_wireframe == null) {
            _wireframe = new CubeWireframe("SectorWireframe", _transform, TempGameValues.SectorSize, width: 2F, color: UnityDebugConstants.SectorHighlightColor);
        }
        if (_sectorIDLabel == null) {
            UpdateSectorIDLabel();
        }
        _wireframe.Show(toShow);
        _sectorIDLabel.Show(toShow);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_wireframe != null) { _wireframe.Dispose(); }
        UnityUtility.ExecuteIfNotNullOrDestroyed(_sectorIDLabel, Destroy);
        if (_sectorViewJob != null) { _sectorViewJob.Kill(); }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

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

    public Transform Transform { get { return _transform; } }

    #endregion

}

