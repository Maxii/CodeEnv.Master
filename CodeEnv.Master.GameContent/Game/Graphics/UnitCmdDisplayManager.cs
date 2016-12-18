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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for UnitCommands.
    /// </summary>
    public class UnitCmdDisplayManager : AIconDisplayManager, IMortalDisplayManager {

        /******************************************************************************************
         * For detail on this color change system, see AElementDisplayManager
         ******************************************************************************************/

        private const float _primaryMeshAlpha = 0.1F;

        private GameColor _meshColor;
        /// <summary>
        /// The GameColor to use on the Cmd's primary mesh, aka HQ Element highlight mesh.
        /// Typically the color of the owner.
        /// </summary>
        public GameColor MeshColor {
            get { return _meshColor; }
            set { SetProperty<GameColor>(ref _meshColor, value, "MeshColor", MeshColorPropChangedHandler); }
        }

        public new IResponsiveTrackingSprite Icon { get { return base.Icon as IResponsiveTrackingSprite; } }

        protected override int IconDepth { get { return -3; } }

        private float _currentPrimaryMeshRadius;
        private MaterialPropertyBlock _primaryMeshMPB;
        private MaterialPropertyBlock _hiddenMeshMPB;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitCmdDisplayManager"/> class.
        /// </summary>
        /// <param name="trackedCmd">The tracked command.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <param name="color">The color of the owner.</param>
        public UnitCmdDisplayManager(IWidgetTrackable trackedCmd, Layers meshLayer)
            : base(trackedCmd, meshLayer) {
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            // The primary mesh of a UnitCmd is the sphere highlight that surrounds the HQ Element
            var primaryMeshRenderer = itemGo.GetSingleComponentInChildren<MeshRenderer>();
            _currentPrimaryMeshRadius = primaryMeshRenderer.bounds.size.x / 2F;
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            primaryMeshRenderer.receiveShadows = false;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);

            InitializePrimaryMeshMaterial(primaryMeshRenderer.material);
            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            if (material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
                material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
            }
        }

        private void InitializeColorChangeSystem(GameColor color) {
            Color32 primaryMeshColor = color.ToUnityColor(_primaryMeshAlpha);
            _primaryMeshMPB = new MaterialPropertyBlock();  // default color is black
            _primaryMeshRenderer.GetPropertyBlock(_primaryMeshMPB);
            // renderer's existing MaterialPropertyBlock color is also black, implying that the existing property block is the default, at least wrt color
            _primaryMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, primaryMeshColor);
            //D.Log("{0}.PrimaryMeshMPB color after initialization = {1}.", DebugName, _primaryMeshMPB.GetVector(UnityConstants.StdShader_Property_AlbedoColor));

            if (_hiddenMeshMPB == null) {
                _hiddenMeshMPB = new MaterialPropertyBlock();
                _primaryMeshRenderer.GetPropertyBlock(_hiddenMeshMPB);
                _hiddenMeshMPB.SetColor(UnityConstants.StdShader_Property_AlbedoColor, HiddenMeshColor);
            }
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
            //D.Log("{0}: Showing HQElement Highlight.", GetType().Name);
            _primaryMeshRenderer.SetPropertyBlock(_primaryMeshMPB);
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            //D.Log("{0}: Hiding HQElement Highlight.", GetType().Name);
            _primaryMeshRenderer.SetPropertyBlock(_hiddenMeshMPB);
        }

        /// <summary>
        /// Overridden to show the CmdIcon even when the Cmd's primary mesh (the
        /// highlight surrounding the HQ Element) is no longer showing due to clipping planes.
        /// </summary>
        /// <returns></returns>
        protected override bool ShouldIconShow() {
            return IsDisplayEnabled && _isIconInMainCameraLOS;
        }

        #region Event and Property Change Handlers

        private void MeshColorPropChangedHandler() {
            InitializeColorChangeSystem(MeshColor);
            if (IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS) {
                // change the renderer's color using the updated _primaryMeshMPB
                ShowPrimaryMesh();
            }
        }

        #endregion

        protected override void DestroyIcon() {
            throw new NotSupportedException("{0}'s cannot destroy Icons during runtime.".Inject(GetType().Name));
            // UnitCmd's use the Icon as the transform upon which to center highlighting
        }

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

