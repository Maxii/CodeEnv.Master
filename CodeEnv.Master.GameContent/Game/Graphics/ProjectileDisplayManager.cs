// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ProjectileDisplayManager.cs
// DisplayManager for projectile ordnance which handles the display of the ordnance's operating 
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
    /// DisplayManager for projectile ordnance which handles the display of the ordnance's operating 
    /// effect which can be either ParticleSystems or Icons.
    /// </summary>
    public class ProjectileDisplayManager : AProjectileDisplayManager {

        private static readonly Vector2 IconSize = new Vector2(4F, 4F);

        public ProjectileDisplayManager(IWidgetTrackable trackedProjectile, Layers meshLayer, ParticleSystem operatingEffect)
            : base(trackedProjectile, meshLayer, operatingEffect) {
        }

        protected override IconInfo MakeIconInfo() {    // HACK
            return new IconInfo("Flat", AtlasID.MyGui, GameColor.White, IconSize, WidgetPlacement.Over, _meshLayer);
        }


    }
}

