// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDisplayManager.cs
// DisplayManager for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Systems.
    /// </summary>
    public class SystemDisplayManager : ADisplayManager {

        // IMPROVE primaryMeshRenderer's sole purpose right now is to allow receipt of visibility changes by CameraLosChangedListener 
        // Other ideas could include making an invisible bounds mesh for the plane like done for UIWidgets in CameraLosChangedListener

        public SystemDisplayManager(GameObject trackedItemGo, Layers meshLayer) : base(trackedItemGo, meshLayer) { }

        protected override MeshRenderer InitializePrimaryMesh(GameObject trackedItemGo) {
            var orbitalPlaneMeshCollider = trackedItemGo.GetComponentInChildren<MeshCollider>();   // IMPROVE don't use MeshCollider
            var primaryMeshRenderer = orbitalPlaneMeshCollider.gameObject.GetComponent<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            primaryMeshRenderer.receiveShadows = false;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);

            var material = primaryMeshRenderer.material;
            InitializePrimaryMeshMaterial(material);
            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_CutoutTransparency)) {
                material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_CutoutTransparency);
            }
            material.SetFloat(UnityConstants.StdShader_Property_AlphaCutoffFloat, 0.2F);
        }

        // Line Renderers removed and replaced by Stevie's grid texture using StdShader in CutoutTransparency Rendering Mode

        // Once showing (aka DisplayMgr instance created when first discerned) a OrbitalPlane never has to 
        // become invisible again so there is no need for the ability to change to an invisible color

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


