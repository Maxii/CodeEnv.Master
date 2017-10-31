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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Immutable report on a sector.
    /// </summary>
    public class SectorReport : AIntelItemReport {

        public int? Capacity { get; private set; }

        public ResourcesYield Resources { get; private set; }

        public IntVector3 SectorID { get; private set; }

        public SectorReport(SectorData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as SectorData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {    // true if all members have access
                Capacity = sData.Capacity;
            }

            Resources = AssessResources(sData.Resources);
        }

    }
}

