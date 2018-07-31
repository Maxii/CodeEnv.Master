// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemReport.cs
// Abstract class for Reports associated with an AMortalItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract class for Reports associated with an AMortalItem.
    /// </summary>
    public abstract class AMortalItemReport : AIntelItemReport {

        public float? MaxHitPoints { get; protected set; }

        public float? CurrentHitPoints { get; protected set; }

        public float? Health { get; protected set; }

        public CombatStrength? DefensiveStrength { get; protected set; }

        public AMortalItemReport(AMortalItemData data, Player player) : base(data, player) { }


    }
}

