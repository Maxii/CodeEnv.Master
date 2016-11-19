// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementDesign.cs
// Abstract base class holding the design of an element for a player.
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using Common;

    /// <summary>
    /// Abstract base class holding the design of an element for a player.
    /// </summary>
    public abstract class AElementDesign : AUnitDesign {

        public IEnumerable<WeaponDesign> WeaponDesigns { get; private set; }

        public IEnumerable<ActiveCountermeasureStat> ActiveCmStats { get; private set; }

        public IEnumerable<SensorStat> SensorStats { get; private set; }

        public IEnumerable<ShieldGeneratorStat> ShieldGeneratorStats { get; private set; }

        public Priority HQPriority { get; private set; }

        public AElementDesign(Player player, string designName, IEnumerable<WeaponDesign> weaponDesigns, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, Priority hqPriority)
            : base(player, designName, passiveCmStats) {
            WeaponDesigns = weaponDesigns;
            ActiveCmStats = activeCmStats;
            SensorStats = sensorStats;
            ShieldGeneratorStats = shieldGenStats;
            HQPriority = hqPriority;
        }

    }
}

