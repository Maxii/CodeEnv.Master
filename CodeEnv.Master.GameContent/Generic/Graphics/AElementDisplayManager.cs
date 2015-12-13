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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract DisplayManager for Elements.
    /// </summary>
    public abstract class AElementDisplayManager : AIconDisplayManager {

        private static Vector2 _elementIconSize = new Vector2(12F, 12F);

        private GameColor _color;
        /// <summary>
        /// The GameColor to use on the element's primary mesh. Typically the color of the owner.
        /// </summary>
        public GameColor Color {
            get { return _color; }
            set {
                if (_color != value) {
                    _color = value;
                    ColorPropChangedHandler();
                }
            }
        }

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Below; } }

        protected override Vector2 IconSize { get { return _elementIconSize; } }

        /// <summary>
        /// The Layer used to cull this element's meshes.
        /// </summary>
        protected abstract Layers CullingLayer { get; }

        private IEnumerable<MeshRenderer> _secondaryMeshRenderers;
        private MaterialPropertyBlock _primaryMeshMPB;
        private MaterialPropertyBlock _hiddenMeshMPB;

        public AElementDisplayManager(IWidgetTrackable trackedElement, GameColor color)
            : base(trackedElement) {
            Color = color;  // will result in ColorPropChangedHandler() which will initialize the ColorChangeSystem
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject elementItemGo) {
            //D.Log("{0}.InitializePrimaryMesh({1}) called.", GetType().Name, elementItemGo.name);
            IHull hull = elementItemGo.GetSingleInterfaceInChildren<IHull>();
            var primaryMeshRenderer = hull.HullMesh.GetComponent<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == CullingLayer);    // layer automatically handles showing

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
            if (material.GetFloat(UnityConstants.StdShader_Property_MetallicFloat) != 0.25F) {
                material.SetFloat(UnityConstants.StdShader_Property_MetallicFloat, 0.25F);
            }
            if (material.GetFloat(UnityConstants.StdShader_Property_SmoothnessFloat) != 0.4F) {
                material.SetFloat(UnityConstants.StdShader_Property_SmoothnessFloat, 0.4F);
            }

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
                //D.Log("{0} is initializing Mount Renderers.", Name);
                _secondaryMeshRenderers.ForAll(r => {
                    D.Assert((Layers)r.gameObject.layer == CullingLayer);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    r.enabled = false;
                });
            }
        }

        private void InitializeColorChangeSystem(GameColor color) {
            Color primaryMeshColor = color.ToUnityColor();
            _primaryMeshMPB = new MaterialPropertyBlock();  // default color is black
            _primaryMeshRenderer.GetPropertyBlock(_primaryMeshMPB);
            // renderer's existing MaterialPropertyBlock color is also black, implying that the existing property block is the default, at least wrt color
            _primaryMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, primaryMeshColor);
            //D.Log("{0}.PrimaryMeshMPB color after init = {1}.", Name, _primaryMeshMPB.GetVector(UnityConstants.StdShader_Property_AlbedoColor));

            if (_hiddenMeshMPB == null) {
                _hiddenMeshMPB = new MaterialPropertyBlock();
                _primaryMeshRenderer.GetPropertyBlock(_hiddenMeshMPB);
                _hiddenMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _hiddenMeshColor);
            }
        }

        protected override void ShowPrimaryMesh() {
            base.ShowPrimaryMesh();
            /*******************************************************************************************************************
             * Legacy expensive material color change approach making a copy of renderer.material.
             * var materialCopy = _primaryMeshRenderer.material; 
             * materialCopy.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
             * Note: no need for _primaryMeshRenderer.material = materialCopy as this happens automatically when the copy is made
             ************************************************************************************************************************/
            _primaryMeshRenderer.SetPropertyBlock(_primaryMeshMPB);
            // Note: using a MaterialPropertyBlock containing a color changes the color the renderer shows, but does not change the color contained in the material
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            _primaryMeshRenderer.SetPropertyBlock(_hiddenMeshMPB);
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(r => r.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
            }
        }

        #region Event and Property Change Handlers

        private void ColorPropChangedHandler() {
            InitializeColorChangeSystem(Color);
            if (IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS) {
                // change the renderer's color using the updated _primaryMeshMPB
                ShowPrimaryMesh();
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

