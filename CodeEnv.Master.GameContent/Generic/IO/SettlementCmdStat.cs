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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for SettlementCmds.
    /// </summary>
    public class SettlementCmdStat : UnitCmdStat {

        public int StartingPopulation { get; private set; }

        public float StartingApproval { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdStat" /> class.
        /// </summary>
        /// <param name="unitName">Name of the unit.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdEffect">The maximum command effectiveness.</param>
        /// <param name="formation">The formation.</param>
        /// <param name="startingPopulation">The starting population.</param>
        /// <param name="startingApproval">The starting approval.</param>
        public SettlementCmdStat(string unitName, float maxHitPts, float maxCmdEffect, Formation formation, int startingPopulation, float startingApproval)
            : base(unitName, maxHitPts, maxCmdEffect, formation) {
            StartingPopulation = startingPopulation;
            Utility.ValidateForRange(startingApproval, Constants.ZeroPercent, Constants.OneHundredPercent);
            StartingApproval = startingApproval;
        }


    }
}

