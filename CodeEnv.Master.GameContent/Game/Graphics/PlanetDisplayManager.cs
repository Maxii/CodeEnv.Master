// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetDisplayManager.cs
// DisplayManager for Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Planets.
    /// </summary>
    public class PlanetDisplayManager : AIconDisplayManager {

        private static Vector2 _planetIconSize = new Vector2(12F, 12F);

        protected override WidgetPlacement IconPlacement { get { return WidgetPlacement.Over; } }

        protected override Vector2 IconSize { get { return _planetIconSize; } }

        private IRevolver _revolver;
        private IEnumerable<MeshRenderer> _secondaryMeshRenderers;

        public PlanetDisplayManager(IWidgetTrackable trackedPlanet, IconInfo iconInfo)
            : base(trackedPlanet) {
            IconInfo = iconInfo;
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var meshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>();
            var primaryMeshRenderer = meshRenderers.Single(mr => mr.gameObject.GetInterface<IRevolver>() != null);
            primaryMeshRenderer.castShadows = true;
            primaryMeshRenderer.receiveShadows = true;
            D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.PlanetoidCull);   // layer automatically handles showing
            return primaryMeshRenderer;
        }

        protected override void InitializeSecondaryMeshes(GameObject itemGo) {
            base.InitializeSecondaryMeshes(itemGo);

            _secondaryMeshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {  // some planets may not have atmosphere or rings
                _secondaryMeshRenderers.ForAll(r => {
                    r.gameObject.layer = (int)Layers.PlanetoidCull;  // layer automatically handles showing
                    r.castShadows = true;
                    r.receiveShadows = true;
                    r.enabled = true;
                });
            }
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSafeInterfaceInImmediateChildren<IRevolver>();
            // TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(smr => smr.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Hide Primary Mesh Archive

        //private Color _originalMeshColor_AtmosNear;
        //private Color _originalMeshColor_AtmosFar;

        //public PlanetDisplayManager(GameObject itemGO)
        //    : base(itemGO) {
        //    _originalMeshColor_AtmosNear = _primaryMeshRenderer.material.GetColor(SpaceUnityConstants.MaterialColor_AtmosNear);
        //    _originalMeshColor_AtmosFar = _primaryMeshRenderer.material.GetColor(SpaceUnityConstants.MaterialColor_AtmosFar);
        //}

        //protected override void ShowPrimaryMesh() {
        //    base.ShowPrimaryMesh();
        //    _primaryMeshRenderer.material.SetColor(SpaceUnityConstants.MaterialColor_AtmosNear, _originalMeshColor_AtmosNear);
        //    _primaryMeshRenderer.material.SetColor(SpaceUnityConstants.MaterialColor_AtmosFar, _originalMeshColor_AtmosFar);
        //}

        //protected override void HidePrimaryMesh() {
        //    base.HidePrimaryMesh();
        //    _primaryMeshRenderer.material.SetColor(SpaceUnityConstants.MaterialColor_AtmosNear, _hiddenMeshColor);
        //    _primaryMeshRenderer.material.SetColor(SpaceUnityConstants.MaterialColor_AtmosFar, _hiddenMeshColor);
        //}

        #endregion

    }

}

