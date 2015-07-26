﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Stars.
    /// </summary>
    public class StarDisplayManager : AIconDisplayManager {

        private static Vector2 _starIconSize = new Vector2(16F, 16F);

        private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
            Layers.ShipCull, Layers.FacilityCull, Layers.PlanetoidCull, Layers.StarCull, Layers.Ordnance);

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Over; } }

        protected override Vector2 IconSize { get { return _starIconSize; } }

        private IBillboard _glowBillboard;
        private IRevolver[] _revolvers;

        public StarDisplayManager(IWidgetTrackable trackedStar, IconInfo iconInfo)
            : base(trackedStar) {
            IconInfo = iconInfo;
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var primaryMeshRenderer = itemGo.GetFirstComponentInImmediateChildrenOnly<MeshRenderer>();
            primaryMeshRenderer.castShadows = true;
            primaryMeshRenderer.receiveShadows = true;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.StarCull);    // layer automatically handles showing
            return primaryMeshRenderer;
        }

        protected override void InitializeSecondaryMeshes(GameObject itemGo) {
            base.InitializeSecondaryMeshes(itemGo);

            var glowRenderers = itemGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            glowRenderers.ForAll(gr => {
                gr.castShadows = false;
                gr.receiveShadows = false;
                D.Assert((Layers)(gr.gameObject.layer) == Layers.StarCull); // layer automatically handles showing
                gr.enabled = true;
            });
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _glowBillboard = itemGo.GetSafeInterfaceInChildren<IBillboard>();

            var starLight = itemGo.GetComponentInChildren<Light>();
            starLight.range = References.GameManager.GameSettings.UniverseSize.Radius();
            starLight.intensity = 0.5F;
            starLight.cullingMask = _starLightCullingMask;
            starLight.enabled = true;

            _revolvers = itemGo.GetSafeInterfacesInChildren<IRevolver>();
            // TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _glowBillboard.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            _revolvers.ForAll(r => r.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

