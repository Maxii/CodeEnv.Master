// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetStat.cs
// Data container holding externally acquirable values for Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container holding externally acquirable values for Planets.
    /// </summary>
    public class PlanetStat : PlanetoidStat {

        public float CloseOrbitInnerRadius { get; private set; }

        public PlanetStat(float radius, float mass, float maxHitPts, PlanetoidCategory category, int capacity, ResourceYield resources, float closeOrbitInnerRadius)
            : base(radius, mass, maxHitPts, category, capacity, resources) {
            CloseOrbitInnerRadius = closeOrbitInnerRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

