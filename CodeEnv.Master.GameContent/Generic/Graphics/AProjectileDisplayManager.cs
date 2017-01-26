// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProjectileDisplayManager.cs
// Abstract DisplayManager for projectile ordnance which handles the display of the ordnance's operating 
// effect which can be either ParticleSystems or Icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract DisplayManager for projectile ordnance which handles the display of the ordnance's operating 
    /// effect which can be either ParticleSystems or Icons.
    /// </summary>
    public abstract class AProjectileDisplayManager : ADisplayManager {

        private IWidgetTrackable _trackedProjectile;
        private ParticleSystem _operatingEffect;
        private IWorldTrackingSprite _icon;

        public AProjectileDisplayManager(IWidgetTrackable trackedProjectile, Layers meshLayer, ParticleSystem operatingEffect = null)
            : base(trackedProjectile.transform.gameObject, meshLayer) {
            _trackedProjectile = trackedProjectile;
            _operatingEffect = operatingEffect;
        }

        protected override MeshRenderer InitializePrimaryMesh(GameObject trackedProjectileGo) {
            ICameraLosChangedListener listener;
            if (_operatingEffect == null) {
                // no particle operating effect will be used so make a projectile icon to show
                IconInfo projectileIconInfo = MakeIconInfo();
                _icon = References.TrackingWidgetFactory.MakeWorldTrackingSprite_Independent(_trackedProjectile, projectileIconInfo);
                (_icon as IWorldTrackingSprite_Independent).DrawDepth = 7;  // HACK
                listener = _icon.CameraLosChangedListener;
            }
            else {
                // Note: Particle operating effect will be used so make a CameraLosChangedListener to tell it when to show.
                // Listener wiring done by ADisplayManager.
                listener = References.TrackingWidgetFactory.MakeInvisibleCameraLosChangedListener(_trackedProjectile, _meshLayer);
            }
            var primaryMeshRenderer = listener.transform.GetComponent<MeshRenderer>();

            primaryMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            primaryMeshRenderer.receiveShadows = false;
            __ValidateAndCorrectMeshLayer(primaryMeshRenderer.gameObject);
            return primaryMeshRenderer;
        }

        protected override void ShowPrimaryMesh() {
            base.ShowPrimaryMesh();
            if (_icon != null) {
                _icon.Show(true);
            }
            else {
                D.AssertNotNull(_operatingEffect);
                _operatingEffect.Play();
            }
        }

        protected override void HidePrimaryMesh() {
            base.HidePrimaryMesh();
            if (_icon != null) {
                _icon.Show(false);
            }
            else {
                D.AssertNotNull(_operatingEffect);
                _operatingEffect.Stop();
            }
        }

        protected abstract IconInfo MakeIconInfo();

    }
}

