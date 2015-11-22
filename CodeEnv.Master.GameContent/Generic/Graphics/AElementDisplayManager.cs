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
                    OnColorChanged();
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
        private Color _primaryMeshColor;

        public AElementDisplayManager(IWidgetTrackable trackedElement, GameColor color)
            : base(trackedElement) {
            _color = color;
            _primaryMeshColor = color.ToUnityColor();
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject elementItemGo) {
            //D.Log("{0}.InitializePrimaryMesh({1}) called.", GetType().Name, elementItemGo.name);
            IHull hull = elementItemGo.GetSingleInterfaceInChildren<IHull>();
            var primaryMeshRenderer = hull.HullMesh.GetComponent<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == CullingLayer);    // layer automatically handles showing

            var material = primaryMeshRenderer.material;
            InitializePrimaryMeshMaterial(material);
            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
                material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
            }
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Metallic)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Metallic);
            }
            material.SetFloat(UnityConstants.StdShader_Property_MetallicFloat, 0.25F);
            material.SetFloat(UnityConstants.StdShader_Property_SmoothnessFloat, 0.4F);

            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Normal)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Normal);
            }
            material.SetFloat(UnityConstants.StdShader_Property_NormalScaleFloat, 1.25F);
        }

        protected override void InitializeSecondaryMeshes(GameObject elementItemGo) {
            base.InitializeSecondaryMeshes(elementItemGo);
            var hullGo = elementItemGo.GetSingleInterfaceInChildren<IHull>().transform.gameObject;
            _secondaryMeshRenderers = hullGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {
                // Mounts
                //D.Log("{0} is initializing Mount Renderers.", Name);
                _secondaryMeshRenderers.ForAll(r => {
                    D.Assert((Layers)r.gameObject.layer == CullingLayer);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    r.enabled = false;
                });
            }
        }

        protected override void ShowPrimaryMesh() {
            base.ShowPrimaryMesh();
            //D.Log("{0} showing primary mesh, color = {1}.", Name, _primaryMeshColor);
            _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            //D.Log("{0} hiding primary mesh, color = {1}.", Name, _hiddenMeshColor);
            _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _hiddenMeshColor);
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(r => r.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
            }
        }

        private void OnColorChanged() {
            _primaryMeshColor = Color.ToUnityColor();
            if (IsDisplayEnabled) {
                _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

