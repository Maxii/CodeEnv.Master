﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidStat.cs
// Immutable struct containing externally acquirable values for Planetoids, aka Planets and Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Planetoids, aka Planets and Moons.
    /// </summary>
    public struct PlanetoidStat {

        // A Planetoid's name is assigned once its parent's name and its orbit are known
        public float Mass { get; private set; }
        public float MaxHitPoints { get; private set; }
        public PlanetoidCategory Category { get; private set; }
        public int Capacity { get; private set; }
        public ResourceYield Resources { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidStat" /> struct.
        /// </summary>
        /// <param name="mass">The mass.</param>
        /// <param name="maxHitPts">The maximum hit points.</param>
        /// <param name="category">The category.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="resources">The resources.</param>
        public PlanetoidStat(float mass, float maxHitPts, PlanetoidCategory category, int capacity, ResourceYield resources)
            : this() {
            Mass = mass;
            MaxHitPoints = maxHitPts;
            Category = category;
            Capacity = capacity;
            Resources = resources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

