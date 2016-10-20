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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Provides alternative mode views of the Universe for the player. The first implemented is a view of sectors.
    /// </summary>
    public class PlayerViews : AGenericSingleton<PlayerViews>, IDisposable {

        // static so nested classes can use it
        private static ViewModeKeys _lastViewModeKeyPressed;

        // Special mode to allow viewing of sectors in space with this key combination activated
        public PlayerViewModeKeyConfiguration sectorViewMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.SectorView, viewMode = PlayerViewMode.SectorView, activate = true };
        public PlayerViewModeKeyConfiguration normalViewMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.NormalView, viewMode = PlayerViewMode.NormalView, activate = true };
        //public PlayerViewModeKeyConfiguration sectorOrderMode = new PlayerViewModeKeyConfiguration { viewModeKey = ViewModeKeys.SectorOrder, viewMode = PlayerViewMode.SectorOrder, activate = true };

        private PlayerViewMode _viewMode;
        public PlayerViewMode ViewMode {
            get { return _viewMode; }
            set { SetProperty<PlayerViewMode>(ref _viewMode, value, "ViewMode", ViewModePropChangedHandler); }
        }

        public KeyCode[] ViewModeKeyCodes { get; private set; }

        private PlayerViewModeKeyConfiguration[] _keyConfigs;

        private PlayerViews() {
            Initialize();
        }

        protected sealed override void Initialize() {
            var viewModeKeysExcludingDefault = Enums<ViewModeKeys>.GetValues().Except(default(ViewModeKeys));
            ViewModeKeyCodes = viewModeKeysExcludingDefault.Select(sk => (KeyCode)sk).ToArray<KeyCode>();

            _viewMode = PlayerViewMode.NormalView;
            _keyConfigs = new PlayerViewModeKeyConfiguration[] { sectorViewMode, /*sectorOrderMode,*/ normalViewMode };
        }

        public void HandleViewModeKeyPressed(KeyCode key) {
            ChangeViewMode((ViewModeKeys)key);
        }

        private void ChangeViewMode(ViewModeKeys key) {
            _lastViewModeKeyPressed = key;
            PlayerViewModeKeyConfiguration activatedConfig = _keyConfigs.Single(config => config.IsActivated);
            D.Assert(activatedConfig != null, "{0} configuration for SpecialKey {1} is null.", Name, _lastViewModeKeyPressed.GetValueName());
            ViewMode = activatedConfig.viewMode;
        }

        #region Event and Property Change Handlers

        private void ViewModePropChangedHandler() {
            D.Log("{0} ViewMode changed to {1}.", Name, ViewMode.GetValueName());
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

        #endregion

        #region Cleanup

        private void Cleanup() {
            _lastViewModeKeyPressed = ViewModeKeys.None;
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        /// <summary>
        /// Enum that translates KeyCodes into 'special' named modes.
        /// </summary>
        public enum ViewModeKeys {

            //[EnumAttribute("")]
            None = KeyCode.None,

            SectorView = KeyCode.F1,

            //SectorOrder = KeyCode.F2,

            NormalView = KeyCode.Escape

        }

        [Serializable]
        // Defines actions associated with the keys affecting the PlayerViewMode
        public class PlayerViewModeKeyConfiguration : AInputConfigurationBase {

            public PlayerViewMode viewMode;
            public ViewModeKeys viewModeKey;

            public override bool IsActivated {
                get {
                    return base.IsActivated && viewModeKey == _lastViewModeKeyPressed;
                }
            }
        }

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
                CallOnDispose();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion


    }
}

