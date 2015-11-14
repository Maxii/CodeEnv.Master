// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterDisplayManager.cs
// DisplayManager for the UniverseCenter.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for the UniverseCenter.
    /// </summary>
    public class UniverseCenterDisplayManager : ADisplayManager {

        private IRevolver _revolver;

        public UniverseCenterDisplayManager(GameObject itemGO) : base(itemGO) { }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var primaryMeshRenderer = itemGo.GetSingleComponentInChildren<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            var material = primaryMeshRenderer.material;
            InitializePrimaryMeshMaterial(material);
            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            // no need to enable RenderingMode.Opaque as it is the default            // for now, color is green
            if (material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Metallic)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Metallic);
            }
            material.SetFloat(UnityConstants.StdShader_Property_MetallicFloat, Constants.ZeroF);
            material.SetFloat(UnityConstants.StdShader_Property_SmoothnessFloat, 0.20F);

            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Normal)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Normal);
            }
            material.SetFloat(UnityConstants.StdShader_Property_NormalScaleFloat, 1F);
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSingleInterfaceInChildren<IRevolver>();
            _revolver.enabled = false;
            // TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
        }

        // Once showing (aka DisplayMgr instance created when first discerned) a UniverseCenter never has to 
        // become invisible again so there is no need for the ability to change to an invisible color

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

