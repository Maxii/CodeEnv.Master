﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesign.cs
// Class holding the design of a ship for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class holding the design of a ship for a player.
    /// </summary>
    public class ShipDesign : AElementDesign {

        public ShipHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public ShipHullStat HullStat { get; private set; }

        public EnginesStat EnginesStat { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public ShipDesign(Player player, string designName, ShipHullStat hullStat, EnginesStat enginesStat, ShipCombatStance combatStance,
            IEnumerable<WeaponDesign> weaponDesigns, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
            IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats)
            : base(player, designName, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats) {
            HullStat = hullStat;
            EnginesStat = enginesStat;
            CombatStance = combatStance;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

