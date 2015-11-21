// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarStat.cs
// Immutable struct containing externally acquirable values for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Stars.
    /// </summary>
    public struct StarStat {

        // a Star's name is assigned when its parent system becomes known
        public StarCategory Category { get; private set; }
        public float Radius { get; private set; }
        public float LowOrbitRadius { get; private set; }
        public int Capacity { get; private set; }
        public ResourceYield Resources { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarStat" /> struct.
        /// </summary>
        /// <param name="category">The category of Star.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="lowOrbitRadius">The low orbit radius.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="resources">The resources.</param>
        public StarStat(StarCategory category, float radius, float lowOrbitRadius, int capacity, ResourceYield resources)
            : this() {
            Category = category;
            Radius = radius;
            LowOrbitRadius = lowOrbitRadius;
            Capacity = capacity;
            Resources = resources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

