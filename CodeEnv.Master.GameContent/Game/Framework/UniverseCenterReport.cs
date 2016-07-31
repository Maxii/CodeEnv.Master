// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterReport.cs
// Immutable report for UniverseCenterItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable report for UniverseCenterItems.
    /// </summary>
    public class UniverseCenterReport : AIntelItemReport {

        public UniverseCenterReport(UniverseCenterData data, Player player, IUniverseCenter_Ltd item)
            : base(data, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var ucData = data as UniverseCenterData;
            var accessCntlr = ucData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = ucData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = ucData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = ucData.Owner;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

