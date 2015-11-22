// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonStat.cs
// Immutable stat containing externally acquirable values for Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for Moons.
    /// </summary>
    public class MoonStat : APlanetoidStat {

        public float ShipTransitBanRadius { get; private set; }

        public MoonStat(float radius, float mass, float maxHitPts, PlanetoidCategory category, int capacity, ResourceYield resources, float shipTransitBanRadius)
            : base(radius, mass, maxHitPts, category, capacity, resources) {
            ShipTransitBanRadius = shipTransitBanRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

