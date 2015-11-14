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

        private static float _primaryMeshAlpha = 0.1F;

        private GameColor _color;
        /// <summary>
        /// The GameColor to use on the Cmd's primary mesh, aka HQ Element highlight mesh.
        /// Typically the color of the owner.
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

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Above; } }

        protected override Vector2 IconSize { get { return _cmdIconSize; } }

        private Color _primaryMeshColor;
        private float _currentPrimaryMeshRadius;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitCmdDisplayManager"/> class.
        /// </summary>
        /// <param name="trackedCmd">The tracked command.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <param name="color">The color of the owner.</param>
        public UnitCmdDisplayManager(IWidgetTrackable trackedCmd, IconInfo iconInfo, GameColor color)
            : base(trackedCmd) {
            IconInfo = iconInfo;
            _color = color;
            _primaryMeshColor = color.ToUnityColor(_primaryMeshAlpha);
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            // The primary mesh of a UnitCmd is the sphere highlight that surrounds the HQ Element
            var primaryMeshRenderer = itemGo.GetSingleComponentInChildren<MeshRenderer>();
            _currentPrimaryMeshRadius = primaryMeshRenderer.bounds.size.x / 2F;
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            primaryMeshRenderer.receiveShadows = false;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) != Layers.Default); // HACK    // layer automatically handles showing

            var material = primaryMeshRenderer.material;
            InitializePrimaryMeshMaterial(material);

            return primaryMeshRenderer;
        }

        private void InitializePrimaryMeshMaterial(Material material) {
            if (material.IsKeywordEnabled(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency)) {
                material.EnableKeyword(UnityConstants.StdShader_RenderModeKeyword_FadeTransparency);
            }
            if (!material.IsKeywordEnabled(UnityConstants.StdShader_MapKeyword_Metallic)) {
                material.EnableKeyword(UnityConstants.StdShader_MapKeyword_Metallic);
            }
            material.SetFloat(UnityConstants.StdShader_Property_MetallicFloat, Constants.ZeroF);
            material.SetFloat(UnityConstants.StdShader_Property_SmoothnessFloat, Constants.ZeroF);
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
            _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _hiddenMeshColor);
        }

        /// <summary>
        /// Overridden to show the CmdIcon even when the Cmd's primary mesh (the
        /// highlight surrounding the HQ Element) is no longer showing due to clipping planes.
        /// </summary>
        /// <returns></returns>
        protected override bool ShouldIconShow() {
            return IsDisplayEnabled && _isIconInMainCameraLOS;
        }

        private void OnColorChanged() {
            _primaryMeshColor = Color.ToUnityColor(_primaryMeshAlpha);
            if (IsDisplayEnabled) {
                _primaryMeshRenderer.material.SetColor(UnityConstants.StdShader_Property_AlbedoColor, _primaryMeshColor);
            }
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

