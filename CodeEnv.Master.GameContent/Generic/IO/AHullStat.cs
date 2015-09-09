﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHullStat.cs
// Immutable abstract base class for Hull stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable abstract base class for Hull stats.
    /// </summary>
    public abstract class AHullStat : AEquipmentStat {

        public float MaxHitPoints { get; private set; }
        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AHullStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The space available within this hull.</param>
        /// <param name="mass">The mass of this hull.</param>
        /// <param name="pwrRqmt">The power required to operate this hull.</param>
        /// <param name="expense">The expense consumed by this hull.</param>
        /// <param name="maxHitPts">The maximum hit points of this hull.</param>
        /// <param name="damageMitigation">The resistance to damage of this hull.</param>
        public AHullStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float expense, float maxHitPts, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense) {
            MaxHitPoints = maxHitPts;
            DamageMitigation = damageMitigation;
        }


    }
}

