// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerViews.cs
// Provides alternative mode views of the Universe for the player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Provides alternative mode views of the Universe for the player. The first implemented is a view of sectors.
/// </summary>
public class PlayerViews : AGameInputConfiguration<PlayerViews> {

    // Special mode to allow viewing of sectors in space with this key combination activated
    public PlayerViewModeKeyConfiguration sectorViewMode = new PlayerViewModeKeyConfiguration { key = SpecialKeys.SectorViewMode, viewMode = PlayerViewMode.SectorView, activate = true };
    public PlayerViewModeKeyConfiguration normalViewMode = new PlayerViewModeKeyConfiguration { key = SpecialKeys.NormalViewMode, viewMode = PlayerViewMode.NormalView, activate = true };

    private PlayerViewMode _viewMode;
    public PlayerViewMode ViewMode {
        get { return _viewMode; }
        set { SetProperty<PlayerViewMode>(ref _viewMode, value, "ViewMode", OnViewModeChanged); }
    }

    private static SpecialKeys _lastSpecialKeyReceived;

    private LayerMask _sectorViewModeEventReceiverLayerMask;
    private LayerMask _sectorViewModeCameraCullingLayerMask;
    private LayerMask _normalViewModeEventReceiverLayerMask;
    private LayerMask _normalViewModeCameraCullingLayerMask;
    private UICamera _mainUICamera;
    private Camera _mainCamera;

    private Sector[] _sectors;
    private IList<IDisposable> _subscribers;
    private PlayerViewModeKeyConfiguration[] _keyConfigs;

    protected override void Awake() {
        base.Awake();
        _mainCamera = Camera.main;
        _mainUICamera = _mainCamera.gameObject.GetSafeMonoBehaviourComponent<UICamera>();
        _viewMode = PlayerViewMode.NormalView;
        _keyConfigs = new PlayerViewModeKeyConfiguration[2] { sectorViewMode, normalViewMode };
        _sectors = Sectors.Folder.gameObject.GetSafeMonoBehaviourComponentsInChildren<Sector>(includeInactive: false);
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(gameInput.SubscribeToPropertyChanged<GameInput, KeyCode>(gi => gi.SpecialKeyPressed, OnSpecialKeyPressedChanged));
    }

    protected override void Start() {
        base.Start();
        Initialize();
    }

    private void Initialize() {
        // these masks should be acquired after we are sure the camera has set them up properly
        _normalViewModeEventReceiverLayerMask = _mainUICamera.eventReceiverMask;
        _normalViewModeCameraCullingLayerMask = _mainCamera.cullingMask;
        _sectorViewModeEventReceiverLayerMask = _normalViewModeEventReceiverLayerMask.AddToMask(Layers.SectorView);
        _sectorViewModeCameraCullingLayerMask = _normalViewModeCameraCullingLayerMask.AddToMask(Layers.SectorView);
    }

    private void OnSpecialKeyPressedChanged() {
        if (ViewMode != PlayerViewMode.NormalView && (SpecialKeys)gameInput.SpecialKeyPressed != SpecialKeys.NormalViewMode) {
            // we are in a special mode already and the player has hit another special mode key besides the NormalViewMode special key
            D.Warn("Press the {0} key first to return to {1}.", SpecialKeys.NormalViewMode.GetName(), PlayerViewMode.NormalView);
            return;
        }
        _lastSpecialKeyReceived = (SpecialKeys)gameInput.SpecialKeyPressed;
        PlayerViewModeKeyConfiguration activatedConfig = _keyConfigs.Single(config => config.IsActivated());
        D.Assert(activatedConfig != null, "Configuration for SpecialKey {0} is null.".Inject(_lastSpecialKeyReceived.GetName()), true);
        ViewMode = activatedConfig.viewMode;
    }

    private void OnViewModeChanged() {
        D.Log("ViewMode changed to {0}.", ViewMode.GetName());
        switch (ViewMode) {
            case PlayerViewMode.SectorView:
                // allow the camera to see the sectorViewMode layer so the UICamera can also see it
                _mainCamera.cullingMask = _sectorViewModeCameraCullingLayerMask;
                _mainUICamera.eventReceiverMask = _sectorViewModeEventReceiverLayerMask;
                _sectors.ForAll(s => s.ShowSector(true));
                break;
            case PlayerViewMode.NormalView:
                _mainUICamera.eventReceiverMask = _normalViewModeEventReceiverLayerMask;
                _mainCamera.cullingMask = _normalViewModeCameraCullingLayerMask;
                _sectors.ForAll(s => s.ShowSector(false));
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ViewMode));
        }
    }

    // UNDONE
    //private bool TryFindCamera(out Sector sector) {
    //    sector = null;
    //    Sector[] allSectors = Sectors.Instance.AllSectors;
    //    foreach (var s in allSectors) {
    //        if (s.ContainsCamera()) {
    //            sector = s;
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    [Serializable]
    // Defines actions associated with the SpecialKeys affecting the PlayerViewMode
    public class PlayerViewModeKeyConfiguration : ConfigurationBase {
        public PlayerViewMode viewMode;
        public SpecialKeys key;

        public override bool IsActivated() {
            return base.IsActivated() && key == _lastSpecialKeyReceived;
        }
    }

    #endregion

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

