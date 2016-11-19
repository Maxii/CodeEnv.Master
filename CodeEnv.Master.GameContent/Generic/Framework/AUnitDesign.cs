// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitDesign.cs
// Abstract base class holding the design of an element or command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Abstract base class holding the design of an element or command for a player.
    /// </summary>
    public abstract class AUnitDesign {

        public Player Player { get; private set; }

        public string DesignName { get; private set; }

        public IEnumerable<PassiveCountermeasureStat> PassiveCmStats { get; private set; }

        public AUnitDesign(Player player, string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats) {
            Player = player;
            DesignName = designName;
            PassiveCmStats = passiveCmStats;
        }

    }
}

