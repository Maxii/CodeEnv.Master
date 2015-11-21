﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHullStat.cs
// Immutable stat containing externally acquirable hull values for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable stat containing externally acquirable hull values for Ships.
    /// </summary>
    public class ShipHullStat : AHullStat {

        public ShipHullCategory HullCategory { get; private set; }
        /// <summary>
        /// The drag of this hull in Topography.OpenSpace.
        /// </summary>
        public float Drag { get; private set; }
        public float Science { get; private set; }
        public float Culture { get; private set; }
        public float Income { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHullStat" /> class.
        /// </summary>
        /// <param name="hullCategory">The category.</param>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="drag">The drag of this hull in Topography.OpenSpace.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        /// <param name="hullDimensions">The hull dimensions.</param>
        /// <param name="science">The science generated by this hull, if any.</param>
        /// <param name="culture">The culture generated by this hull, if any.</param>
        /// <param name="income">The income generated by this hull, if any.</param>
        public ShipHullStat(ShipHullCategory hullCategory, string name, AtlasID imageAtlasID, string imageFilename, string description,
            float size, float mass, float drag, float pwrRqmt, float expense, float maxHitPts, DamageStrength damageMitigation,
            Vector3 hullDimensions, float science, float culture, float income)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, maxHitPts, damageMitigation, hullDimensions) {
            HullCategory = hullCategory;
            Drag = drag;
            Science = science;
            Culture = culture;
            Income = income;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

