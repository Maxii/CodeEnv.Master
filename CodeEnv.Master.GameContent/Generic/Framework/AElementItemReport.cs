// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementItemReport.cs
// Abstract class for ElementItem Reports.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract class for ElementItem Reports.
    /// </summary>
    public abstract class AElementItemReport : AMortalItemReport {

        public string ParentName { get; protected set; }

        public CombatStrength? OffensiveStrength { get; protected set; }

        public float? MaxWeaponsRange { get; protected set; }

        public float? MaxSensorRange { get; protected set; }

        public AElementItemReport(AUnitElementItemData data, Player player) : base(data, player) { }

    }
}

