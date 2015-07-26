// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementItemReport.cs
// Abstract class for Reports associated with UnitElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports associated with UnitElementItems.
    /// </summary>
    public abstract class AElementItemReport : AMortalItemReport {

        public string ParentName { get; protected set; }

        public CombatStrength? OffensiveStrength { get; protected set; }

        public RangeDistance? WeaponsRange { get; protected set; }

        public RangeDistance? SensorRange { get; protected set; }

        public float? Science { get; protected set; }
        public float? Culture { get; protected set; }
        public float? Income { get; protected set; }
        public float? Expense { get; protected set; }

        public AElementItemReport(AUnitElementItemData data, Player player, IUnitElementItem item) : base(data, player, item) { }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            AUnitElementItemData eData = data as AUnitElementItemData;
            SectorIndex = eData.SectorIndex;
        }

    }
}

