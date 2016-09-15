// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdDesign.cs
// The design of a Fleet Command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The design of a Fleet Command for a player.
    /// </summary>
    public class FleetCmdDesign : ACommandDesign {

        public UnitCmdStat CmdStat { get; private set; }

        public FleetCmdDesign(Player player, string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats, UnitCmdStat cmdStat)
            : base(player, designName, passiveCmStats) {
            CmdStat = cmdStat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

