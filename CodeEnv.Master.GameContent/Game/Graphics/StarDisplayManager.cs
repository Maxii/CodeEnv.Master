// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayManager.cs
// DisplayManager for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Stars.
    /// </summary>
    public class StarDisplayManager : AIconDisplayManager {

        private static readonly LayerMask StarLightCullingMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
            Layers.Cull_Tiny, Layers.Cull_1, Layers.Cull_2, Layers.Cull_3, Layers.Cull_4, Layers.Cull_8, Layers.Cull_15, Layers.Cull_200,
            Layers.Cull_400, Layers.Cull_1000, Layers.Cull_3000, Layers.Projectiles, Layers.Shields, Layers.SystemOrbitalPlane);

        public new IInteractiveWorldTrackingSprite Icon { get { return base.Icon as IInteractiveWorldTrackingSprite; } }

        protected override int IconDepth { get { return 3; } }

        private IEnumerable<MeshRenderer> __secondaryMeshRenderers;
        private IBillboard _glowBillboard;
        private IRevolver[] _revolvers; // star mesh and 2 glows

        public StarDisplayManager(IWidgetTrackable trackedStar, Layers meshLayer)
            : base(trackedStar, meshLayer) {
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var primaryMeshRenderer = itemGo.GetSingleComponentInImmediateChildren<MeshRenderer>();
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;    // A star can't possibly cast a shadow of itself on another object
            primaryMeshRenderer.receiveShadows = false; // A star can't possibly display a shadow from another object on its surface
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);
            return primaryMeshRenderer;
        }

        protected override void InitializeSecondaryMeshes(GameObject itemGo) {  // Glow Renderers
            base.InitializeSecondaryMeshes(itemGo);

            __secondaryMeshRenderers = itemGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            __secondaryMeshRenderers.ForAll(gr => {
                gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gr.receiveShadows = false;
                __ValidateAndCorrectMeshLayer(gr.gameObject);
                gr.enabled = true;
            });
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _glowBillboard = itemGo.GetSingleInterfaceInChildren<IBillboard>();

            var starLight = itemGo.GetComponentInChildren<Light>();
            // UNCLEAR no runtime assessable option to set Baking = Realtime
            starLight.type = LightType.Point;
            starLight.range = References.GameManager.GameSettings.UniverseSize.Radius(); //References.DebugControls.UniverseSize.Radius();
            starLight.intensity = 1F;
            //starLight.bounceIntensity = 1F; // bounce light shadowing not currently supported for point lights
            starLight.shadows = LightShadows.None;  // point light shadows are expensive
            starLight.renderMode = LightRenderMode.Auto;
            starLight.cullingMask = StarLightCullingMask;
            starLight.enabled = true;

            _revolvers = itemGo.GetSafeInterfacesInChildren<IRevolver>();
            //_revolvers.ForAll(r => r.IsActivated = false);  // enabled = false in Awake
            //TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _glowBillboard.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            _revolvers.ForAll(r => r.IsActivated = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
        }

        /// <summary>
        /// Changes the primary mesh and 2 glow layers to <c>layer</c>.
        /// <remarks>TEMP method to make stars visible from further away when star icons aren't used.</remarks>
        /// </summary>
        /// <param name="layer">The layer.</param>
        public void __ChangeMeshLayersTo(Layers layer) {
            _revolvers.ForAll(r => {
                (r as Component).gameObject.layer = (int)layer;
            });
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        protected override List<MeshRenderer> __GetMeshRenderers() {
            List<MeshRenderer> result = base.__GetMeshRenderers();
            result.AddRange(__secondaryMeshRenderers);
            return result;
        }

        #endregion

    }

}

