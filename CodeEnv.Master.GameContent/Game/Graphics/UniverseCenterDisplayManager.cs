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
            var primaryMeshRenderer = itemGo.GetFirstComponentInImmediateChildrenOnly<MeshRenderer>();
            primaryMeshRenderer.castShadows = true;
            primaryMeshRenderer.receiveShadows = true;
            return primaryMeshRenderer;
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSafeInterfaceInChildren<IRevolver>();
            // TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

