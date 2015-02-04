// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemReport.cs
// Abstract class for MortalItem Reports.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract class for MortalItem Reports.
    /// </summary>
    public abstract class AMortalItemReport : AItemReport {

        //public IList<Countermeasure> Countermeasures { get; protected set; }

        public string ParentName { get; protected set; }

        public float? MaxHitPoints { get; protected set; }

        public float? CurrentHitPoints { get; protected set; }

        public float? Health { get; protected set; }

        public CombatStrength? DefensiveStrength { get; protected set; }

        public float? Mass { get; protected set; }

        public AMortalItemReport(AMortalItemData data, Player player)
            : base(data, player) {
        }

    }
}

