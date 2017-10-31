// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementReport.cs
// Abstract class for Reports associated with UnitElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports associated with UnitElementItems.
    /// </summary>
    public abstract class AUnitElementReport : AMortalItemReport {

        public string UnitName { get; protected set; }

        public CombatStrength? OffensiveStrength { get; protected set; }

        public RangeDistance? WeaponsRange { get; protected set; }

        public AlertStatus AlertStatus { get; protected set; }

        public float? Mass { get; protected set; }

        public OutputsYield Outputs { get; protected set; }

        public float? ConstructionCost { get; protected set; }

        public AUnitElementReport(AUnitElementData data, Player player) : base(data, player) { }

    }
}

