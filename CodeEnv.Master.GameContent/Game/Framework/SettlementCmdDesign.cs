// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdDesign.cs
// The design of a Settlement Command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The design of a Settlement Command for a player.
    /// </summary>
    public class SettlementCmdDesign : ACommandDesign {

        public SettlementCmdStat CmdStat { get; private set; }

        public SettlementCmdDesign(Player player, string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
            IEnumerable<SensorStat> sensorStats, FtlDampenerStat ftlDampenerStat, SettlementCmdStat cmdStat)
            : base(player, designName, passiveCmStats, sensorStats, ftlDampenerStat) {
            CmdStat = cmdStat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

