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
            set { SetProperty<PlayerViewMode>(ref _viewMode, value, "ViewMode", OnViewModeChanged); }
        }

        public KeyCode[] ViewModeKeyCodes { get; private set; }

        private PlayerViewModeKeyConfiguration[] _keyConfigs;

        private PlayerViews() {
            Initialize();
        }

        protected override void Initialize() {
            var viewModeKeysExcludingDefault = Enums<ViewModeKeys>.GetValues().Except(default(ViewModeKeys));
            ViewModeKeyCodes = viewModeKeysExcludingDefault.Select(sk => (KeyCode)sk).ToArray<KeyCode>();

            _viewMode = PlayerViewMode.NormalView;
            _keyConfigs = new PlayerViewModeKeyConfiguration[] { sectorViewMode, /*sectorOrderMode,*/ normalViewMode };
        }

        public void OnViewModeKeyPressed(KeyCode key) {
            ChangeViewMode((ViewModeKeys)key);
        }

        private void ChangeViewMode(ViewModeKeys key) {
            _lastViewModeKeyPressed = key;
            PlayerViewModeKeyConfiguration activatedConfig = _keyConfigs.Single(config => config.IsActivated());
            D.Assert(activatedConfig != null, "Configuration for SpecialKey {0} is null.".Inject(_lastViewModeKeyPressed.GetValueName()), true);
            ViewMode = activatedConfig.viewMode;
        }

        private void OnViewModeChanged() {
            D.Log("ViewMode changed to {0}.", ViewMode.GetValueName());
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

        #region Cleaup

        private void Cleanup() {
            _lastViewModeKeyPressed = ViewModeKeys.None;
            OnDispose();    // PlayerViews has state and should not persist across scenes
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

            public override bool IsActivated() {
                return base.IsActivated() && viewModeKey == _lastViewModeKeyPressed;
            }
        }

        #endregion

        #region IDisposable
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

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
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
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
}

