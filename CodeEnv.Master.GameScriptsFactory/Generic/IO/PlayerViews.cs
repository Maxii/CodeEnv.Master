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
public class PlayerViews : AMonoBaseSingleton<PlayerViews> {

    // Special mode to allow viewing of sectors in space with this key combination activated
    public PlayerViewModeKeyConfiguration sectorViewMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.SectorView, viewMode = PlayerViewMode.SectorView, activate = true };
    //public PlayerViewModeKeyConfiguration sectorOrderMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.SectorOrder, viewMode = PlayerViewMode.SectorOrder, activate = true };
    public PlayerViewModeKeyConfiguration normalViewMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.NormalView, viewMode = PlayerViewMode.NormalView, activate = true };

    private PlayerViewMode _viewMode;
    public PlayerViewMode ViewMode {
        get { return _viewMode; }
        set { SetProperty<PlayerViewMode>(ref _viewMode, value, "ViewMode", OnViewModeChanged); }
    }

    // static so nested classes can use it
    private static ViewModeKeys _lastViewModeKeyPressed;

    //private LayerMask _sectorViewModeEventReceiverLayerMask;
    //private LayerMask _sectorViewModeCameraCullingLayerMask;
    //private LayerMask _normalViewModeEventReceiverLayerMask;
    //private LayerMask _normalViewModeCameraCullingLayerMask;
    //private UICamera _mainUICamera;
    private Camera _mainCamera;

    private IList<IDisposable> _subscribers;
    private PlayerViewModeKeyConfiguration[] _keyConfigs;
    private GameInput _gameInput;

    protected override void Awake() {
        base.Awake();
        _mainCamera = Camera.main;
        //_mainUICamera = _mainCamera.gameObject.GetSafeMonoBehaviourComponent<UICamera>();
        _gameInput = GameInput.Instance;
        _viewMode = PlayerViewMode.NormalView;
        _keyConfigs = new PlayerViewModeKeyConfiguration[] { sectorViewMode, /*sectorOrderMode,*/ normalViewMode };
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _gameInput.onViewModeKeyPressed += OnViewModeKeyPressed;
    }

    protected override void Start() {
        base.Start();
        Initialize();
    }

    private void Initialize() {
        // these masks should be acquired after we are sure the camera has set them up properly
        // _normalViewModeEventReceiverLayerMask = _mainUICamera.eventReceiverMask;
        // _normalViewModeCameraCullingLayerMask = _mainCamera.cullingMask;
        // _sectorViewModeEventReceiverLayerMask = _normalViewModeEventReceiverLayerMask.AddToMask(Layers.SectorView);
        // _sectorViewModeCameraCullingLayerMask = _normalViewModeCameraCullingLayerMask.AddToMask(Layers.SectorView);
    }

    private void OnViewModeKeyPressed(ViewModeKeys key) {
        ChangeViewMode(key);
    }

    private void ChangeViewMode(ViewModeKeys key) {
        _lastViewModeKeyPressed = key;
        PlayerViewModeKeyConfiguration activatedConfig = _keyConfigs.Single(config => config.IsActivated());
        D.Assert(activatedConfig != null, "Configuration for SpecialKey {0} is null.".Inject(_lastViewModeKeyPressed.GetName()), true);
        ViewMode = activatedConfig.viewMode;
    }

    private void OnViewModeChanged() {
        D.Log("ViewMode changed to {0}.", ViewMode.GetName());
        switch (ViewMode) {
            case PlayerViewMode.SectorView:
                // allow the camera to see the sectorViewMode layer so the UICamera can also see it
                //_mainCamera.cullingMask = _sectorViewModeCameraCullingLayerMask;
                //_mainUICamera.eventReceiverMask = _sectorViewModeEventReceiverLayerMask;
                break;
            case PlayerViewMode.NormalView:
                //_mainUICamera.eventReceiverMask = _normalViewModeEventReceiverLayerMask;
                //_mainCamera.cullingMask = _normalViewModeCameraCullingLayerMask;
                break;
            //case PlayerViewMode.SectorOrder:
            //break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ViewMode));
        }
    }

    protected override void Update() {
        base.Update();
        _gameInput.CheckForKeyActivity();
    }

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
        _gameInput.onViewModeKeyPressed -= OnViewModeKeyPressed;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    [Serializable]
    // Defines actions associated with the keys affecting the PlayerViewMode
    public class PlayerViewModeKeyConfiguration : AInputConfigurationBase {

        public PlayerViewMode viewMode;
        public ViewModeKeys viewModeKey;

        public override bool IsActivated() {
            return base.IsActivated() && viewModeKey == _lastViewModeKeyPressed;
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

