// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandDesign.cs
// Abstract base class holding the design of a command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Abstract base class holding the design of a command for a player.
    /// </summary>
    public abstract class ACommandDesign : AUnitDesign {

        public FtlDampenerStat FtlDampenerStat { get; private set; }

        public ACommandDesign(Player player, string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats, FtlDampenerStat ftlDampenerStat)
            : base(player, designName, passiveCmStats) {
            FtlDampenerStat = ftlDampenerStat;
        }

    }
}

