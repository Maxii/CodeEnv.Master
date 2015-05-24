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

    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Abstract DisplayManager for Elements.
    /// </summary>
    public abstract class AElementDisplayManager : AIconDisplayManager {

        private static Vector2 _elementIconSize = new Vector2(12F, 12F);

        private Color _originalMeshColor_Main;
        private Color _originalMeshColor_Specular;

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Below; } }

        protected override Vector2 IconSize { get { return _elementIconSize; } }

        public AElementDisplayManager(IWidgetTrackable trackedElement)
            : base(trackedElement) {
            _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
            _originalMeshColor_Specular = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            //D.Log("{0}.InitializePrimaryMesh({1}) called.", GetType().Name, itemGo.name);
            var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
            primaryMeshRenderer.castShadows = true;
            primaryMeshRenderer.receiveShadows = true;
            return primaryMeshRenderer;
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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

