// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorReport.cs
// Immutable report on a sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Immutable report on a sector.
    /// </summary>
    public class SectorReport : AIntelItemReport {

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public IntVector3 SectorID { get; private set; }

        public SectorReport(SectorData data, Player player, ISector_Ltd item)
            : base(data, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var sData = data as SectorData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {    // true if all members have access
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {   // true if all members have access
                Resources = sData.Resources;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

