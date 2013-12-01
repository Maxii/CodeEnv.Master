// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorViewer.cs
//  Singleton that displays the highlighted wireframe of a sector and provides a context menu for fleet commands
// relevant to the highlighted sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

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
public class SectorViewer : AMonoBaseSingleton<SectorViewer>, IDisposable {

    public int distanceInSectorsFromCamera = 2;

    private Index3D _location = new Index3D();
    /// <summary>
    /// The Location of this SectorViewer expressed as the index of the 
    /// Sector it is over.
    /// </summary>
    public Index3D Location {
        get { return _location; }
        private set { SetProperty<Index3D>(ref _location, value, "Location", OnLocationChanged); }
    }

    private string _sectorIDLabelText;
    private float _distanceToHighlightedSector;
    private SelectionManager _selectionMgr;
    private CubeWireframe _wireframe;
    private BoxCollider _collider;
    private CtxObject _ctxObject;

    private GuiTrackingLabel _sectorIDLabel;

    private PlayerViewMode _viewMode;
    private Job _sectorViewJob;

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _selectionMgr = SelectionManager.Instance;
        _distanceToHighlightedSector = distanceInSectorsFromCamera * TempGameValues.SectorSideLength;
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
            _subscribers.Add(GameInput.Instance.SubscribeToPropertyChanged<GameInput, UnconsumedMouseButtonPress>(gi => gi.UnconsumedPress, OnUnconsumedPressChanged));
        }
        else {
            IDisposable d = _subscribers.Single(s => s as DisposePropertyChangedSubscription<CameraControl> != null);
            _subscribers.Remove(d);
            d.Dispose();
            d = _subscribers.Single(s => s as DisposePropertyChangedSubscription<GameInput> != null);
            _subscribers.Remove(d);
            d.Dispose();
        }
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
        _collider.size = TempGameValues.SectorSize;
        _collider.enabled = false;  // interferes with Ngui event system (Click, etc.) when on
    }

    private void __ValidateCtxObjectSettings() {
        _ctxObject = UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(gameObject);
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    private GuiTrackingLabel InitializeSectorIDLabel() {
        GuiTrackingLabel sectorIDLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(_transform);
        sectorIDLabel.Color = UnityDebugConstants.SectorHighlightColor;
        _sectorIDLabelText = "Sector {0}" + Constants.NewLine + "GridBox {1}.".Inject(Location, SectorGrid.GetGridBoxLocation(Location));
        sectorIDLabel.Set(_sectorIDLabelText);
        return sectorIDLabel;
    }

    private void OnUnconsumedPressChanged() {
        UnconsumedMouseButtonPress pressed = GameInput.Instance.UnconsumedPress;
        if (_viewMode == PlayerViewMode.SectorView && pressed.Button == NguiMouseButton.Right && !pressed.IsDown) {
            //FleetManager selectedFleetMgr = _selectionMgr.CurrentSelection as FleetManager;
            FleetView selectedFleetMgr = _selectionMgr.CurrentSelection as FleetView;
            if (selectedFleetMgr != null && _wireframe.IsMouseOverHotSpot) {
                _ctxObject.ShowMenu();
            }
        }
    }

    private void OnLocationChanged() {
        _transform.position = SectorGrid.GetSector(Location).Position;
        if (_sectorIDLabel != null) {
            _sectorIDLabel.Set(_sectorIDLabelText);
        }
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
                _collider.enabled = false;
                break;
            //case PlayerViewMode.SectorOrder:
            //    if (!_toViewSectorUnderMouse) {
            //        StartCoroutine(ShowSectorUnderMouse());
            //    }
            //    _collider.enabled = true;
            //    break;
            case PlayerViewMode.NormalView:
                // turn off wireframe
                DynamicallySubscribe(false);
                if (_sectorViewJob != null && _sectorViewJob.IsRunning) {
                    _sectorViewJob.Kill();
                    ShowSector(false);
                }
                _collider.enabled = false;
                _ctxObject.HideMenu();
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_viewMode));
        }
    }

    private IEnumerator ShowSectorUnderMouse() {
        while (true) {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = _distanceToHighlightedSector;
            Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            Index3D sectorIndexUnderMouse = SectorGrid.GetSectorIndex(mouseWorldPoint);
            bool toShow;
            Sector notUsed;
            if (toShow = SectorGrid.TryGetSector(sectorIndexUnderMouse, out notUsed)) {
                if (!Location.Equals(sectorIndexUnderMouse)) {
                    Location = sectorIndexUnderMouse; // avoid the SetProperty equivalent warnings
                }
            }
            ShowSector(toShow);
            yield return null;
        }
    }

    private void ShowSector(bool toShow) {
        if (!toShow && _wireframe == null) {
            return;
        }
        if (_wireframe == null) {
            _wireframe = new CubeWireframe("SectorWireframe", _transform, TempGameValues.SectorSize, parent: DynamicObjects.Folder,
                width: 2F, color: UnityDebugConstants.SectorHighlightColor);
        }
        if (_sectorIDLabel == null) {
            _sectorIDLabel = InitializeSectorIDLabel();
        }
        _wireframe.Show(toShow);
        _sectorIDLabel.IsShowing = toShow;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_wireframe != null) {
            _wireframe.Dispose();
        }
        if (_sectorIDLabel != null) {
            Destroy(_sectorIDLabel.gameObject);
            _sectorIDLabel = null;
        }
        if (_sectorViewJob != null) {
            _sectorViewJob.Kill();
        }
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

}

