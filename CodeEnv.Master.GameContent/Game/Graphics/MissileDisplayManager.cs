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

        private static readonly Vector2 IconSize = new Vector2(6F, 6F);

        public MissileDisplayManager(IWidgetTrackable trackedMissile, Layers meshLayer, ParticleSystem operatingEffect)
            : base(trackedMissile, meshLayer, operatingEffect) {
        }

        protected override IconInfo MakeIconInfo() {    // HACK
            return new IconInfo("Flat", AtlasID.MyGui, GameColor.White, IconSize, WidgetPlacement.Over, _meshLayer);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

