// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityStat.cs
// Immutable struct containing externally acquirable values for Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Facilities.
    /// </summary>
    public struct FacilityStat {

        public string Name { get; private set; }
        public float Mass { get; private set; }
        public float MaxHitPoints { get; private set; }
        public FacilityCategory Category { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityStat"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="category">The category.</param>
        public FacilityStat(string name, float mass, float maxHitPts, FacilityCategory category)
            : this() {
            Name = name;
            Mass = mass;
            MaxHitPoints = maxHitPts;
            Category = category;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

