﻿// --------------------------------------------------------------------------------------------------------------------
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

        ////public RangeDistance? SensorRange { get; protected set; } // makes no sense

        public float? Mass { get; protected set; }

        public float? Science { get; protected set; }
        public float? Culture { get; protected set; }
        public decimal? Income { get; protected set; }
        public decimal? Expense { get; protected set; }
        public float? ConstructionCost { get; protected set; }

        public AUnitElementReport(AUnitElementData data, Player player, IUnitElement_Ltd item) : base(data, player, item) { }

    }
}

