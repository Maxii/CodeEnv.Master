// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADisplayManager.cs
// Abstract base class for DisplayManagers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for DisplayManagers.
    /// </summary>
    public abstract class ADisplayManager : APropertyChangeTracking {

        protected static Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();

        private bool _isInMainCameraLOS = true;
        /// <summary>
        /// Indicates whether this item is within the main camera's Line Of Sight.
        /// Note: All items start out thinking they are in the main camera's LOS. This is because cameraLOSChangedListeners 
        /// automatically send out an onBecameInvisible event when they first awake, independant of whether they are
        /// visible or not. This is followed by a onBecameVisible event if they are in fact visible. This way, this property will
        /// always change at least once, communicating that change to any listeners.
        /// </summary>
        public bool IsInMainCameraLOS {
            get { return _isInMainCameraLOS; }
            protected set { SetProperty<bool>(ref _isInMainCameraLOS, value, "IsInMainCameraLOS"); }
        }

        private bool _isPrimaryMeshInMainCameraLOS = true;
        /// <summary>
        /// Indicates whether the primary mesh of this item is within the main camera's Line Of Sight.
        /// If <c>true</c>, then <c>IsInMainCameraLOS</c> will always be <c>true</c>. If <c>false</c> 
        /// then <c>IsInMainCameraLOS</c> can still be <c>true</c> as some Items support Icons that can
        /// be within the main camera's LOS even after the primary mesh's distance to the main camera
        /// is beyond its far culling distance.
        /// </summary>
        public bool IsPrimaryMeshInMainCameraLOS {
            get { return _isPrimaryMeshInMainCameraLOS; }
            protected set { SetProperty<bool>(ref _isPrimaryMeshInMainCameraLOS, value, "IsPrimaryMeshInMainCameraLOS", IsPrimaryMeshInMainCameraLosPropChangedHandler); }
        }

        private bool _isDisplayEnabled;
        /// <summary>
        /// Indicates whether the DisplayMgr is allowed to display material on the screen.
        /// True or false, InCameraLOS continues to operate.
        /// </summary>
        protected bool IsDisplayEnabled {
            get { return _isDisplayEnabled; }
            set { SetProperty<bool>(ref _isDisplayEnabled, value, "IsDisplayEnabled"); }
        }

        protected MeshRenderer _primaryMeshRenderer;
        private bool __isPrimaryMeshShowing;
        private Layers __meshLayer;
        private GameObject _itemGO;

        public ADisplayManager(GameObject itemGO, Layers meshLayer) {
            _itemGO = itemGO;
            __meshLayer = meshLayer;
        }

        public void Initialize() {
            _primaryMeshRenderer = InitializePrimaryMesh(_itemGO);
            _primaryMeshRenderer.enabled = true;

            var primaryMeshCameraLosChgdListener = _primaryMeshRenderer.gameObject.GetSafeInterface<ICameraLosChangedListener>();
            primaryMeshCameraLosChgdListener.inCameraLosChanged += PrimaryMeshInCameraLosChangedEventHandler;
            primaryMeshCameraLosChgdListener.enabled = true;

            InitializeSecondaryMeshes(_itemGO);
            InitializeOther(_itemGO);
            // AssessComponentsToShow(); no need to call here as EnableDisplay(true) is called immediately after initialization
            // Warning: if called here, derived class constructors will not have completed yet
        }

        protected abstract MeshRenderer InitializePrimaryMesh(GameObject itemGO);

        protected virtual void InitializeSecondaryMeshes(GameObject itemGO) { }

        protected virtual void InitializeOther(GameObject itemGO) { }

        /// <summary>
        /// Controls whether this DisplayMgr is allowed to display material to the screen.
        /// The optional <c>isDead</c> parameter when <c>true</c> disables the
        /// _primaryMeshRenderer thereby discontinuing InCameraLOS functionality.
        /// <remarks>Done this way as I currently have no effective method of making
        /// primary meshes disappear when the item is dead.</remarks>
        /// </summary>
        /// <param name="toEnable">if set to <c>true</c> [to enable].</param>
        /// <param name="isDead">if set to <c>true</c> [is dead].</param>
        public void EnableDisplay(bool toEnable, bool isDead = false) {
            D.Assert(!(toEnable && isDead));    // should never both be true
            if (IsDisplayEnabled != toEnable) {
                IsDisplayEnabled = toEnable;
                D.Log("{0}.IsDisplayEnabled changed.", GetType().Name);
                IsDisplayEnabledPropChangedHandler();
            }
            if (isDead) { _primaryMeshRenderer.enabled = false; }
        }

        #region Event and Property Change Handlers

        private void PrimaryMeshInCameraLosChangedEventHandler(object sender, EventArgs e) {
            IsPrimaryMeshInMainCameraLOS = (sender as ICameraLosChangedListener).InCameraLOS;
        }

        private void IsDisplayEnabledPropChangedHandler() {
            D.Log("{0}.IsDisplayEnabled changed to {1}.", GetType().Name, IsDisplayEnabled);
            AssessComponentsToShowOrOperate();
        }

        private void IsPrimaryMeshInMainCameraLosPropChangedHandler() {
            AssessInMainCameraLOS();
            AssessComponentsToShowOrOperate();
        }

        #endregion

        private void ShowPrimaryMesh(bool toShow) {
            // can't disable meshRenderer as lose OnMeshInCameraLOSChanged events
            if (__isPrimaryMeshShowing == toShow) {
                //D.Log("{0} recording duplicate call to ShowPrimaryMesh({1}).", GetType().Name, toShow);
                return;
            }
            if (toShow) {
                ShowPrimaryMesh();
            }
            else {
                HidePrimaryMesh();
            }
            __isPrimaryMeshShowing = toShow;
        }

        /// <summary>
        /// Shows the primary mesh. This base implementation does nothing as all primary MeshRenderers are
        /// always enabled because they must feed OnVisible events to this displayMgr. When not in cameraLOS,
        /// the MeshRenderer automatically knows not to render. This method is here to allow derived
        /// classes to change the primary mesh's material in cases where the mesh should or should not show even
        /// when it is in cameraLOS. e.g. The user has no IntelCoverage on the item.
        /// </summary>
        protected virtual void ShowPrimaryMesh() { }

        /// <summary>
        /// Hides the primary mesh. This base implementation does nothing as all primary MeshRenderers are
        /// always enabled because they must feed OnVisible events to this displayMgr. When not in cameraLOS,
        /// the MeshRenderer automatically knows not to render. This method is here to allow derived
        /// classes to change the primary mesh's material in cases where the mesh should or should not show even
        /// when it is in cameraLOS. e.g. The user has no IntelCoverage on the item.
        /// </summary>
        protected virtual void HidePrimaryMesh() { }

        protected virtual void AssessComponentsToShowOrOperate() {
            bool toShow = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            ShowPrimaryMesh(toShow);
        }

        protected virtual void AssessInMainCameraLOS() {
            IsInMainCameraLOS = IsPrimaryMeshInMainCameraLOS;
        }

        protected void __ValidateAndCorrectMeshLayer(GameObject meshGo) {
            if ((Layers)meshGo.layer != __meshLayer) {
                D.Warn("{0} mesh {1} layer improperly set to {2}. Changing to {3}.",
                    GetType().Name, meshGo.name, ((Layers)meshGo.layer).GetValueName(), __meshLayer.GetValueName());
                UnityUtility.SetLayerRecursively(meshGo.transform, __meshLayer);
            }
        }

    }
}


