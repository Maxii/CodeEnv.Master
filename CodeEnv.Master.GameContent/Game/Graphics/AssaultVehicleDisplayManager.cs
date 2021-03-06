﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AssaultVehicleDisplayManager.cs
// DisplayManager for assault vehicle ordnance which handles the display of the ordnance's operating 
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
    /// DisplayManager for assault vehicle ordnance which handles the display of the ordnance's operating 
    /// effect which can be either ParticleSystems or Icons.
    /// </summary>
    public class AssaultVehicleDisplayManager : AProjectileDisplayManager {

        private static readonly IntVector2 IconSize = new IntVector2(8, 8);

        public AssaultVehicleDisplayManager(IWidgetTrackable trackedMissile, Layers meshLayer, ParticleSystem operatingEffect)
            : base(trackedMissile, meshLayer, operatingEffect) {
        }

        protected override TrackingIconInfo MakeIconInfo() {    // HACK
            return new TrackingIconInfo("Flat", AtlasID.MyGui, GameColor.White, IconSize, WidgetPlacement.Over, _meshLayer);
        }

    }
}

