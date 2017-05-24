// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdDesign.cs
// The design of a Starbase Command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The design of a Starbase Command for a player.
    /// </summary>
    public class StarbaseCmdDesign : ACommandDesign {

        public UnitCmdStat CmdStat { get; private set; }

        public StarbaseCmdDesign(Player player, string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
            IEnumerable<SensorStat> sensorStats, FtlDampenerStat ftlDampenerStat, UnitCmdStat cmdStat)
            : base(player, designName, passiveCmStats, sensorStats, ftlDampenerStat) {
            CmdStat = cmdStat;
        }

    }
}

