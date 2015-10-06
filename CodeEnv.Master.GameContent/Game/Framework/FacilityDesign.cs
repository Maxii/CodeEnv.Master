// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDesign.cs
// Class holding the design of a facility for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class holding the design of a facility for a player.
    /// </summary>
    public class FacilityDesign : AElementDesign {

        public FacilityHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public FacilityHullStat HullStat { get; private set; }

        public FacilityDesign(Player player, string designName, FacilityHullStat hullStat, IEnumerable<WeaponDesign> weaponDesigns, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats,
            IEnumerable<ShieldGeneratorStat> shieldGenStats)
            : base(player, designName, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats) {
            HullStat = hullStat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

