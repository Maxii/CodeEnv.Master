// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdStat.cs
// Immutable struct containing externally acquirable values for FleetCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for FleetCmds.
    /// </summary>
    [System.Obsolete]
    public struct FleetCmdStat {

        public string Name { get; private set; }
        public float MaxHitPoints { get; private set; }
        public int MaxCmdEffectiveness { get; private set; }
        public Formation UnitFormation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effect.</param>
        /// <param name="formation">The formation.</param>
        public FleetCmdStat(string name, float maxHitPts, int maxCmdEffect, Formation formation)
            : this() {
            Name = name;
            MaxHitPoints = maxHitPts;
            MaxCmdEffectiveness = maxCmdEffect;
            UnitFormation = formation;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

