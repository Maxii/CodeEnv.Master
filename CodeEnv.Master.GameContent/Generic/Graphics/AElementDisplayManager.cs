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

        private Color _originalMeshColor_Main;
        private Color _originalMeshColor_Specular;

        private IEnumerable<MeshRenderer> _secondaryMeshRenderers;

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Below; } }

        protected override Vector2 IconSize { get { return _elementIconSize; } }

        /// <summary>
        /// The Layer used to cull this element's meshes.
        /// </summary>
        protected abstract Layers CullingLayer { get; }

        public AElementDisplayManager(IWidgetTrackable trackedElement)
            : base(trackedElement) {
            _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
            _originalMeshColor_Specular = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject elementItemGo) {
            //D.Log("{0}.InitializePrimaryMesh({1}) called.", GetType().Name, elementItemGo.name);
            IHull hull = elementItemGo.GetSingleInterfaceInChildren<IHull>();
            var primaryMeshRenderer = hull.HullMesh.gameObject.GetComponent<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            return primaryMeshRenderer;
        }

        protected override void InitializeSecondaryMeshes(GameObject elementItemGo) {
            base.InitializeSecondaryMeshes(elementItemGo);
            var hullGo = elementItemGo.GetSingleInterfaceInChildren<IHull>().transform.gameObject;
            _secondaryMeshRenderers = hullGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {
                // Mounts
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
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(r => r.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

