// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitCmdDisplayManager.cs
// DisplayManager for UnitCommands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for UnitCommands.
    /// </summary>
    public class UnitCmdDisplayManager : AIconDisplayManager {

        private static Vector2 _cmdIconSize = new Vector2(24F, 24F);

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Above; } }

        protected override Vector2 IconSize { get { return _cmdIconSize; } }

        private Color _originalMeshColor_Main;
        private float _currentPrimaryMeshRadius;

        public UnitCmdDisplayManager(IWidgetTrackable trackedCmd, IconInfo iconInfo)
            : base(trackedCmd) {
            IconInfo = iconInfo;
            _originalMeshColor_Main = _primaryMeshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var primaryMeshRenderer = itemGo.GetFirstComponentInImmediateChildrenOnly<MeshRenderer>();
            _currentPrimaryMeshRadius = primaryMeshRenderer.bounds.size.x / 2F;
            primaryMeshRenderer.castShadows = false;
            primaryMeshRenderer.receiveShadows = false;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) != Layers.Default); // HACK    // layer automatically handles showing
            return primaryMeshRenderer;
        }

        /// <summary>
        /// Adjusts the size of the primary mesh. The primary mesh for a UnitCmd
        /// is the 'highlight' that encompasses the Cmd, and therefore the HQ Element.
        /// </summary>
        /// <param name="itemRadius">The item radius.</param>
        public void ResizePrimaryMesh(float itemRadius) {
            float scale = itemRadius / _currentPrimaryMeshRadius;
            _primaryMeshRenderer.transform.localScale = new Vector3(scale, scale, scale);
            _currentPrimaryMeshRadius = itemRadius;
        }

        protected override void ShowPrimaryMesh() {
            base.ShowPrimaryMesh();
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            _primaryMeshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
        }

        /// <summary>
        /// Overridden to show the CmdIcon even when the Cmd's primary mesh (the
        /// highlight surrounding the HQ Element) is no longer showing due to clipping planes.
        /// </summary>
        /// <returns></returns>
        protected override bool ShouldIconShow() {
            return IsDisplayEnabled && _isIconInMainCameraLOS;
        }

        protected override void DestroyIcon() {
            throw new NotSupportedException("{0}'s cannot destroy Icons during runtime.".Inject(GetType().Name));
            // UnitCmd's use the Icon as the transform upon which to center highlighting
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

