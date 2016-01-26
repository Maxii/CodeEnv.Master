// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdStat.cs
// Immutable stat containing externally acquirable values for SettlementCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for SettlementCmds.
    /// </summary>
    public class SettlementCmdStat : UnitCmdStat {

        public int Population { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdStat"/> class.
        /// </summary>
        /// <param name="unitName">Name of the unit.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effect.</param>
        /// <param name="formation">The formation.</param>
        /// <param name="population">The population.</param>
        public SettlementCmdStat(string unitName, float maxHitPts, int maxCmdEffect, Formation formation, int population)
            : base(unitName, maxHitPts, maxCmdEffect, formation) {
            D.Assert(formation == Formation.Circle, "{0} {1} = {2}.", unitName, typeof(Formation).Name, formation.GetValueName());
            Population = population;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

