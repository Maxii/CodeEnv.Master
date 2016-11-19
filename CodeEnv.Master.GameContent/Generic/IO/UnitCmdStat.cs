// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdStat.cs
// Immutable stat containing externally acquirable values for UnitCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for UnitCmds.
    /// This version is sufficient by itself for Fleet and Starbase Cmds.
    /// </summary>
    public class UnitCmdStat {

        public string UnitName { get; private set; }
        public float MaxHitPoints { get; private set; }
        public float MaxCmdEffectiveness { get; private set; }
        public Formation UnitFormation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitCmdStat" /> class.
        /// </summary>
        /// <param name="unitName">The Unit's name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effectiveness.</param>
        /// <param name="formation">The formation.</param>
        public UnitCmdStat(string unitName, float maxHitPts, float maxCmdEffect, Formation formation) {
            UnitName = unitName;
            MaxHitPoints = maxHitPts;
            MaxCmdEffectiveness = maxCmdEffect;
            UnitFormation = formation;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

