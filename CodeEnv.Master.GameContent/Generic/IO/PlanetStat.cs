// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetStat.cs
// Immutable stat containing externally acquirable values for Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for Planets.
    /// </summary>
    public class PlanetStat : PlanetoidStat {

        public float LowOrbitRadius { get; private set; }

        public PlanetStat(float radius, float mass, float maxHitPts, PlanetoidCategory category, int capacity, ResourceYield resources, float lowOrbitRadius)
            : base(radius, mass, maxHitPts, category, capacity, resources) {
            LowOrbitRadius = lowOrbitRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

