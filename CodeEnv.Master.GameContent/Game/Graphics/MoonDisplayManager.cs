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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Moons.
    /// </summary>
    public class MoonDisplayManager : ADisplayManager, IMortalDisplayManager {

        private IRevolver _revolver;
        private IOrbitSimulator _orbitSimulator;

        public MoonDisplayManager(GameObject trackedItemGo, Layers meshLayer) : base(trackedItemGo, meshLayer) { }

        protected override MeshRenderer InitializePrimaryMesh(GameObject trackedItemGo) {
            var primaryMeshRenderer = trackedItemGo.GetSingleComponentInChildren<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);

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

        protected override void InitializeOther(GameObject trackedItemGo) {
            base.InitializeOther(trackedItemGo);
            _revolver = trackedItemGo.GetSingleInterfaceInChildren<IRevolver>();
            //TODO Revolver settings
            _orbitSimulator = trackedItemGo.GetComponent<IMoon>().CelestialOrbitSimulator;
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.IsActivated = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            _orbitSimulator.IsActivated = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            //D.Log("{0}: Revolver and OrbitSimulator IsActivated = {1}.", DebugName, IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
        }

        // Once showing (aka DisplayMgr instance created when first discerned) a Planet/Moon never has to 
        // become invisible again so there is no need for the ability to change to an invisible color

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

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

