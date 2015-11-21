// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitBaseCmdStat.cs
// Immutable stat containing externally acquirable values for UnitCmds that are Bases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for UnitCmds that are Bases.
    /// This version is sufficient by itself for StarbaseCmds.
    /// </summary>
    public class UnitBaseCmdStat : UnitCmdStat {

        public float LowOrbitRadius { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitBaseCmdStat"/> class.
        /// </summary>
        /// <param name="unitName">The Unit's name.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effectiveness.</param>
        /// <param name="formation">The formation.</param>
        /// <param name="lowOrbitRadius">The low orbit radius.</param>
        public UnitBaseCmdStat(string unitName, float maxHitPts, int maxCmdEffect, Formation formation, float lowOrbitRadius)
            : base(unitName, maxHitPts, maxCmdEffect, formation) {
            LowOrbitRadius = lowOrbitRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

