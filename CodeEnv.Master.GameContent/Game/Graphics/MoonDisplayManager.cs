// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonDisplayManager.cs
// DisplayManager for Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Moons.
    /// </summary>
    public class MoonDisplayManager : ADisplayManager {

        private IRevolver _revolver;

        public MoonDisplayManager(GameObject itemGO) : base(itemGO) { }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var primaryMeshRenderer = itemGo.GetSingleComponentInChildren<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.PlanetoidCull);   // layer automatically handles showing

            var material = primaryMeshRenderer.material;
            InitializePrimaryMeshMaterial(material);
            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            // IMPROVE Smoothness settings should vary by Moon.Category with some random variation embedded
            // no need to enable RenderingMode.Opaque as it is the default
            material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Normal);
            material.SetFloat(UnityConstants.StdShader_MapKeyword_Metallic, 0.20F);
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

        // Once showing (aka DisplayMgr instance created when first discerned) a Planet/Moon never has to 
        // become invisible again so there is no need for the ability to change to an invisible color

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }

}

