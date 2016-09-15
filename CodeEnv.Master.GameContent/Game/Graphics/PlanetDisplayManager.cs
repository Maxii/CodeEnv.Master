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
    public class PlanetDisplayManager : AIconDisplayManager, IMortalDisplayManager {

        protected override int IconDepth { get { return -6; } }

        private IRevolver _revolver;
        private IEnumerable<MeshRenderer> _secondaryMeshRenderers;

        public PlanetDisplayManager(IWidgetTrackable trackedPlanet, Layers meshLayer)
            : base(trackedPlanet, meshLayer) {
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
            var meshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>();
            var primaryMeshRenderer = meshRenderers.Single(mr => mr.GetComponent<IRevolver>() != null);
            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            primaryMeshRenderer.receiveShadows = true;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);
            // Note: using custom SpaceUnity Shaders for now
            return primaryMeshRenderer;
        }

        protected override void InitializeSecondaryMeshes(GameObject itemGo) {
            base.InitializeSecondaryMeshes(itemGo);
            _secondaryMeshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>().Except(_primaryMeshRenderer);
            if (_secondaryMeshRenderers.Any()) {  // some planets may not have atmosphere or rings
                _secondaryMeshRenderers.ForAll(smr => {
                    __ValidateAndCorrectMeshLayer(smr.gameObject);
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.receiveShadows = true;
                    smr.enabled = false;
                    // Note: using custom SpaceUnity Shaders for now
                });
            }
        }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSingleInterfaceInImmediateChildren<IRevolver>();  // avoids moon revolvers
            //_revolver.IsActivated = false;    // enabled = false in Awake
            //TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.IsActivated = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
            if (_secondaryMeshRenderers.Any()) {
                _secondaryMeshRenderers.ForAll(smr => smr.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS);
            }
        }

        protected override ITrackingSprite MakeIconInstance() {
            return References.TrackingWidgetFactory.MakeConstantSizeTrackingSprite(_trackedItem, IconInfo);
        }


        #region Hide Primary Mesh Archive

        // Once showing (aka DisplayMgr instance created when first discerned) a Planet/Moon never has to 
        // become invisible again so there is no need for the ability to change to an invisible color

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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IMortalDisplayManager Members

        /// <summary>
        /// Called on the death of the client AFTER the death effect has begun. 
        /// Disables the display and ends all InCameraLOS calls.
        /// </summary>
        public void HandleDeath() {
            IsDisplayEnabled = false;
            _primaryMeshRenderer.enabled = false;
        }

        #endregion

    }

}

