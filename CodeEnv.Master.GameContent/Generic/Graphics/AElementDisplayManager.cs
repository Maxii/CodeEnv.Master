// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementDisplayManager.cs
// Abstract DisplayManager for Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract DisplayManager for Elements.
    /// </summary>
    public abstract class AElementDisplayManager : AIconDisplayManager, IMortalDisplayManager {

        private static readonly IntVector2 ReworkIconSize = new IntVector2(24, 24);

        private GameColor _meshColor;
        /// <summary>
        /// The GameColor to use on the element's primary mesh. Typically the color of the owner.
        /// </summary>
        public GameColor MeshColor {
            get { return _meshColor; }
            set { SetProperty<GameColor>(ref _meshColor, value, "MeshColor", MeshColorPropChangedHandler); }
        }

        public new IInteractiveWorldTrackingSprite TrackingIcon { get { return base.TrackingIcon as IInteractiveWorldTrackingSprite; } }

        protected override int TrackingIconDepth { get { return 1; } }

        private IWorldTrackingSprite _reworkIcon;
        private IEnumerable<MeshRenderer> _secondaryMeshRenderers;
        private MaterialPropertyBlock _primaryMeshMPB;

        public AElementDisplayManager(IWidgetTrackable trackedElement, Layers meshLayer)
            : base(trackedElement, meshLayer) {
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject elementItemGo) {
            //D.Log("{0}.InitializePrimaryMesh({1}) called.", DebugName, elementItemGo.name);
            IHull hull = elementItemGo.GetSingleInterfaceInChildren<IHull>();
            var primaryMeshRenderer = hull.HullMesh.GetComponent<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);

            // Note: currently could use renderer.sharedMaterial here for greater efficiency, but only until each element gets assigned its own material
            InitializePrimaryMeshMaterial(primaryMeshRenderer.material);
            return primaryMeshRenderer;
        }

        /// <summary>
        /// Initializes the primary mesh material.
        /// Note: for good explanation of Renderer.materials see http://answers.unity3d.com/questions/228744/material-versus-shared-material.html
        /// </summary>
        /// <param name="material">The material.</param>
        private void InitializePrimaryMeshMaterial(Material material) {
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
                material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
            }
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Metallic)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Metallic);
            }
            /*******************************************************************************************
            * These values were set when I wasn't using the metal material's MetallicMap, opting
            * instead to set these. UNCLEAR Why I don't know, but not using the MatallicMap kept the
            * colors from showing up on the element unless the shader tab was clicked in the inspector.
            *
            * if (material.GetFloat(UnityConstants.StdShader_Property_MetallicFloat) != 0.25F) {
            *     material.SetFloat(UnityConstants.StdShader_Property_MetallicFloat, 0.25F);
            * }
            * if (material.GetFloat(UnityConstants.StdShader_Property_SmoothnessFloat) != 0.4F) {
            *     material.SetFloat(UnityConstants.StdShader_Property_SmoothnessFloat, 0.4F);
            * }
            *******************************************************************************************/

            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Normal)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Normal);
            }
            if (material.GetFloat(UnityConstants.StdShader_Property_NormalScaleFloat) != 1.25F) {
                material.SetFloat(UnityConstants.StdShader_Property_NormalScaleFloat, 1.25F);
            }
        }

        /// <summary>
        /// Initializes the secondary meshes.
        /// <remarks>The secondary meshes of an element are its weapon mounts.</remarks>
        /// </summary>
        /// <param name="elementItemGo">The element item go.</param>
        protected override void InitializeSecondaryMeshes(GameObject elementItemGo) {
            base.InitializeSecondaryMeshes(elementItemGo);
            var hullGo = elementItemGo.GetSingleInterfaceInChildren<IHull>().transform.gameObject;
            _secondaryMeshRenderers = hullGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {
                //string rNames = _secondaryMeshRenderers.Select(r => r.gameObject.name).Concatenate();
                //D.Log("{0} is initializing Mount Renderers: {1}.", DebugName, rNames);
                _secondaryMeshRenderers.ForAll(smr => {
                    __ValidateAndCorrectMeshLayer(smr.gameObject);
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.receiveShadows = true;
                    smr.enabled = false;
                });
            }
        }

        protected override void InitializeOther(GameObject itemGO) {
            base.InitializeOther(itemGO);
            InitializeColorChangeSystem();
        }

        private void InitializeColorChangeSystem() {
            _primaryMeshMPB = new MaterialPropertyBlock();  // default color is black
            _primaryMeshRenderer.GetPropertyBlock(_primaryMeshMPB);
            _primaryMeshMPB.Clear();    // in case the renderer had properties other than the default properties
                                        // renderer's existing MaterialPropertyBlock color is also black, implying that the existing property block is the default, at least wrt color
            _primaryMeshRenderer.SetPropertyBlock(_primaryMeshMPB);
        }

        protected override void ShowPrimaryMesh() {
            base.ShowPrimaryMesh();
            /*******************************************************************************************************************
             * Legacy expensive material color change approach making a copy of renderer.material.
             * var materialCopy = _primaryMeshRenderer.material; 
             * materialCopy.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
             * Note: no need for _primaryMeshRenderer.material = materialCopy as this happens automatically when the copy is made
             ************************************************************************************************************************/
            //D.Log("{0}.ShowPrimaryMesh() called.", DebugName);
            //_primaryMeshRenderer.GetPropertyBlock(_primaryMeshMPB);
            //_primaryMeshMPB.Clear();
            _primaryMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, MeshColor.ToUnityColor());
            //D.Log("{0}.PrimaryMeshMPB color after show = {1}.", DebugName, _primaryMeshMPB.GetVector(UnityConstants.StdShader_Property_AlbedoColor));
            _primaryMeshRenderer.SetPropertyBlock(_primaryMeshMPB);
            // Note: using a MaterialPropertyBlock containing a color changes the color the renderer shows, but does not change the color contained in the material
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            //D.Log("{0}.HidePrimaryMesh() called.", DebugName);
            //_primaryMeshRenderer.GetPropertyBlock(_primaryMeshMPB);
            //_primaryMeshMPB.Clear();
            _primaryMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, HiddenMeshColor);
            //D.Log("{0}.PrimaryMeshMPB color after hide = {1}.", DebugName, _primaryMeshMPB.GetVector(UnityConstants.StdShader_Property_AlbedoColor));
            _primaryMeshRenderer.SetPropertyBlock(_primaryMeshMPB);
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(r => {
                    r.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
                    //D.Log("{0} just changed {1}.renderer.enabled to {2}.", DebugName, r.gameObject.name, r.enabled);
                });
            }
            AssessReworkIconShowing();
        }

        #region Event and Property Change Handlers

        private void MeshColorPropChangedHandler() {
            if (IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS) {
                // change the renderer's color using the updated _primaryMeshMPB
                ShowPrimaryMesh();
            }
        }

        #endregion

        #region Rework Icon

        /// <summary>
        /// Initiates showing of 'rework' visuals when display is enabled.
        /// <remarks>UNDONE make use of reworkMode.</remarks>
        /// </summary>
        /// <param name="reworkMode">The rework mode.</param>
        public void BeginReworkingVisuals(ReworkingMode reworkMode) {
            D.AssertNull(_reworkIcon);
            _reworkIcon = InitializeReworkIcon();
        }

        /// <summary>
        /// Refreshes 'rework' visuals when display is enabled.
        /// <remarks>UNDONE make use of completionPercentage</remarks>
        /// </summary>
        /// <param name="completionPercentage">The completion percentage used when toShow is true.</param>
        public void RefreshReworkingVisuals(float completionPercentage) {
            D.AssertNotNull(_reworkIcon);
            // TODO
        }

        private IWorldTrackingSprite InitializeReworkIcon() {
            TrackingIconInfo reworkIconInfo = MakeReworkIconInfo();
            IWorldTrackingSprite reworkIcon = GameReferences.TrackingWidgetFactory.MakeWorldTrackingSprite_Independent(_trackedItem, reworkIconInfo);
            (reworkIcon as IWorldTrackingSprite_Independent).DrawDepth = TrackingIconDepth;

            // listener not used except to acquire MeshRenderer for initialization. MeshRenderer rendering is determined by culling layer
            ICameraLosChangedListener listener = reworkIcon.CameraLosChangedListener;
            var reworkIconMeshRenderer = listener.transform.GetComponent<MeshRenderer>();
            reworkIconMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            reworkIconMeshRenderer.receiveShadows = false;
            __ValidateAndCorrectMeshLayer(reworkIconMeshRenderer.gameObject);
            return reworkIcon;
        }

        private TrackingIconInfo MakeReworkIconInfo() {    // HACK
            return new TrackingIconInfo(TempGameValues.ReworkIconFilename, AtlasID.MyGui, GameColor.White, ReworkIconSize, WidgetPlacement.AboveRight, _meshLayer);
        }

        public void HideReworkingVisuals() {
            // 11.25.17 Debugging null _reworkIcon found in RefreshReworkingVisuals from element repair job
            D.Log("{0} is nulling {1}.", DebugName, _reworkIcon.DebugName);
            D.AssertNotNull(_reworkIcon);
            _reworkIcon.Destroy();
            _reworkIcon = null;
        }

        private void AssessReworkIconShowing() {
            if (_reworkIcon != null) {
                bool toShow = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
                _reworkIcon.Show(toShow);
            }
        }

        #endregion

        #region Debug

        protected override List<MeshRenderer> __GetMeshRenderers() {
            List<MeshRenderer> result = base.__GetMeshRenderers();
            result.AddRange(_secondaryMeshRenderers);
            return result;
        }

        #endregion

        #region IMortalDisplayManager Members

        /// <summary>
        /// Called on the death of the client. Disables the display and ends all InCameraLOS calls.
        /// </summary>
        public void HandleDeath() {
            IsDisplayEnabled = false;
            _primaryMeshRenderer.enabled = false;
            DestroyIcon();
        }

        #endregion

    }

}

