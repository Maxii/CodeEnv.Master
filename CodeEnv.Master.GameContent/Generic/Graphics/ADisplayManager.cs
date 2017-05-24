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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for DisplayManagers.
    /// </summary>
    public abstract class ADisplayManager : APropertyChangeTracking, IDisposable {

        protected const string DebugNameFormat = "{0}.{1}";

        protected static readonly Color HiddenMeshColor = GameColor.Clear.ToUnityColor();

        protected virtual string DebugName { get { return DebugNameFormat.Inject(_trackedItemGo.name, GetType().Name); } }

        private bool _isInMainCameraLOS = true;
        /// <summary>
        /// Indicates whether this item is within the main camera's Line Of Sight.
        /// Note: All items start out thinking they are in the main camera's LOS. This is because cameraLOSChangedListeners 
        /// automatically send out an onBecameInvisible event when they first awake, independent of whether they are
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
        /// Indicates whether the DisplayMgr is allowed to display to the screen.
        /// True or false, InCameraLOS continues to operate.
        /// <remarks>If disabling because of client death, use IMortalDisplayManager.HandleDeath().</remarks>
        /// </summary>
        public bool IsDisplayEnabled {
            get { return _isDisplayEnabled; }
            set { SetProperty<bool>(ref _isDisplayEnabled, value, "IsDisplayEnabled", IsDisplayEnabledPropChangedHandler); }
        }

        protected MeshRenderer _primaryMeshRenderer;
        protected Layers _meshLayer;

        private bool __isPrimaryMeshShowing;
        private ICameraLosChangedListener _primaryMeshCameraLosChgdListener;
        private GameObject _trackedItemGo;

        public ADisplayManager(GameObject _trackedItemGo, Layers meshLayer) {
            this._trackedItemGo = _trackedItemGo;
            _meshLayer = meshLayer;
        }

        public void Initialize() {
            _primaryMeshRenderer = InitializePrimaryMesh(_trackedItemGo);
            _primaryMeshRenderer.enabled = true;

            _primaryMeshCameraLosChgdListener = _primaryMeshRenderer.GetComponent<ICameraLosChangedListener>();
            _primaryMeshCameraLosChgdListener.inCameraLosChanged += PrimaryMeshInCameraLosChangedEventHandler;
            _primaryMeshCameraLosChgdListener.enabled = true;

            InitializeSecondaryMeshes(_trackedItemGo);
            InitializeOther(_trackedItemGo);
            // AssessComponentsToShow(); no need to call here as EnableDisplay(true) is called immediately after initialization
        }

        protected abstract MeshRenderer InitializePrimaryMesh(GameObject trackedItemGo);

        protected virtual void InitializeSecondaryMeshes(GameObject trackedItemGo) { }

        protected virtual void InitializeOther(GameObject trackedItemGo) { }

        #region Event and Property Change Handlers

        private void PrimaryMeshInCameraLosChangedEventHandler(object sender, EventArgs e) {
            HandlePrimaryMeshInCameraLosChanged(sender as ICameraLosChangedListener);
        }

        private void HandlePrimaryMeshInCameraLosChanged(ICameraLosChangedListener cameraLosChgdListener) {
            IsPrimaryMeshInMainCameraLOS = cameraLosChgdListener.InCameraLOS;
        }

        private void IsDisplayEnabledPropChangedHandler() {
            //D.Log("{0}.IsDisplayEnabled changed to {1}.", DebugName, IsDisplayEnabled);
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
                //D.Log("{0} recording duplicate call to ShowPrimaryMesh({1}).", DebugName, toShow);
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

        #region Debug

        protected void __ValidateAndCorrectMeshLayer(GameObject meshGo) {
            if ((Layers)meshGo.layer != _meshLayer) {
                if (__CheckForCameraLosChangedListener(meshGo)) {
                    return;
                }
                D.Warn("{0} mesh {1} layer improperly set to {2}. Changing to {3}. AllMeshRenderers: {4}.",
                    DebugName, meshGo.name, ((Layers)meshGo.layer).GetValueName(), _meshLayer.GetValueName(),
                    __GetMeshRenderers().Select(mr => mr.name).Concatenate());
                UnityUtility.SetLayerRecursively(meshGo.transform, _meshLayer);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a CameraLosChangedListener if found on or under meshGo.
        /// <remarks>When this occurs it is typically an InvisibleCameraLosChangedListener attached to the
        /// end of a BeamOrdnance.</remarks>
        /// </summary>
        /// <param name="meshGo">The mesh GameObject.</param>
        /// <returns></returns>
        private bool __CheckForCameraLosChangedListener(GameObject meshGo) {
            return meshGo.GetComponentInChildren<ICameraLosChangedListener>() != null;
        }

        /// <summary>
        /// Returns all the MeshRenderers that are controlled by this ADisplayManager.
        /// <remarks>Used for debugging when a MeshRenderer on an unexpected layer is found.
        /// 2.18.17 Found a InvisibleCameraLosChangedListener attached to the end of a Beam. Only occurred
        /// when the ship was created, immediately launched a BeamOrdnance, and then the DisplayMgr was created
        /// finding the LosChangedListener on a layer different than _meshLayer.</remarks>
        /// </summary>
        /// <returns></returns>
        protected virtual List<MeshRenderer> __GetMeshRenderers() {
            return new List<MeshRenderer>() { _primaryMeshRenderer };
        }

        #endregion

        #region Cleanup

        protected virtual void Cleanup() {
            Unsubscribe();
        }

        protected virtual void Unsubscribe() {
            _primaryMeshCameraLosChgdListener.inCameraLosChanged -= PrimaryMeshInCameraLosChangedEventHandler;
        }

        #endregion

        public sealed override string ToString() {
            return DebugName;
        }

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
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion


    }
}


