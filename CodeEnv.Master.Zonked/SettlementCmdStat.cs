// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdStat.cs
// Immutable struct containing externally acquirable values for SettlementCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for SettlementCmds.
    /// </summary>
    [System.Obsolete]
    public struct SettlementCmdStat {

        public string Name { get; private set; }
        public float MaxHitPoints { get; private set; }
        public int MaxCmdEffectiveness { get; private set; }
        public Formation UnitFormation { get; private set; }
        public int Population { get; private set; }
        public float LowOrbitDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="lowOrbitDistance">The low orbit distance.</param>
        /// <param name="maxCmdEffect">The maximum command effect.</param>
        /// <param name="formation">The formation.</param>
        /// <param name="population">The population.</param>
        public SettlementCmdStat(string name, float maxHitPts, float lowOrbitDistance, int maxCmdEffect, Formation formation, int population)
            : this() {
            Name = name;
            MaxHitPoints = maxHitPts;
            LowOrbitDistance = lowOrbitDistance;
            MaxCmdEffectiveness = maxCmdEffect;
            UnitFormation = formation;
            Population = population;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

