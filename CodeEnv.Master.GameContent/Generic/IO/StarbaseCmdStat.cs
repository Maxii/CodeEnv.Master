// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdStat.cs
// Immutable struct containing externally acquirable values for StarbaseCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for StarbaseCmds.
    /// </summary>
    public struct StarbaseCmdStat {

        public string Name { get; private set; }
        public float MaxHitPoints { get; private set; }
        public int MaxCmdEffectiveness { get; private set; }
        public Formation UnitFormation { get; private set; }
        public CombatStrength Strength { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdStat"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effect.</param>
        /// <param name="formation">The formation.</param>
        /// <param name="strength">The strength.</param>
        public StarbaseCmdStat(string name, float maxHitPts, int maxCmdEffect, Formation formation, CombatStrength strength)
            : this() {
            Name = name;
            MaxHitPoints = maxHitPts;
            MaxCmdEffectiveness = maxCmdEffect;
            UnitFormation = formation;
            Strength = strength;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

