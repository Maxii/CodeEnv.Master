// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileDisplayManager.cs
// DisplayManager for missile ordnance which handles the display of the ordnance's operating 
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
    /// DisplayManager for missile ordnance which handles the display of the ordnance's operating 
    /// effect which can be either ParticleSystems or Icons.
    /// </summary>
    public class MissileDisplayManager : AProjectileDisplayManager {

        private static readonly IntVector2 IconSize = new IntVector2(6, 6);

        public MissileDisplayManager(IWidgetTrackable trackedMissile, Layers meshLayer, ParticleSystem operatingEffect)
            : base(trackedMissile, meshLayer, operatingEffect) {
        }

        protected override TrackingIconInfo MakeIconInfo() {    // HACK
            return new TrackingIconInfo("Flat", AtlasID.MyGui, GameColor.White, IconSize, WidgetPlacement.Over, _meshLayer);
        }

    }
}

