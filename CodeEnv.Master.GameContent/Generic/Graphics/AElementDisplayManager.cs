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

        private GameColor _meshColor;
        /// <summary>
        /// The GameColor to use on the element's primary mesh. Typically the color of the owner.
        /// </summary>
        public GameColor MeshColor {
            get { return _meshColor; }
            set { SetProperty<GameColor>(ref _meshColor, value, "MeshColor", MeshColorPropChangedHandler); }
        }

        public new IResponsiveTrackingSprite Icon { get { return base.Icon as IResponsiveTrackingSprite; } }

        protected override int IconDepth { get { return -5; } }

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

        protected override void InitializeSecondaryMeshes(GameObject elementItemGo) {   // Mounts
            base.InitializeSecondaryMeshes(elementItemGo);
            var hullGo = elementItemGo.GetSingleInterfaceInChildren<IHull>().transform.gameObject;
            _secondaryMeshRenderers = hullGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {
                //D.Log("{0} is initializing Mount Renderers.", DebugName);
                _secondaryMeshRenderers.ForAll(r => {
                    __ValidateAndCorrectMeshLayer(r.gameObject);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    r.enabled = false;
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
        }

        #region Event and Property Change Handlers

        private void MeshColorPropChangedHandler() {
            if (IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS) {
                // change the renderer's color using the updated _primaryMeshMPB
                ShowPrimaryMesh();
            }
        }

        #endregion

        #region IMortalDisplayManager Members

        /// <summary>
        /// Called on the death of the client. Disables the display and ends all InCameraLOS calls.
        /// </summary>
        public void HandleDeath() {
            IsDisplayEnabled = false;
            _primaryMeshRenderer.enabled = false;
        }

        #endregion

    }

}

