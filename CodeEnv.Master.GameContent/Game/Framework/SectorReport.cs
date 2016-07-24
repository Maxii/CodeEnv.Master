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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Immutable report on a sector.
    /// </summary>
    public class SectorReport : AIntelItemReport {

        public Index3D SectorIndex { get; private set; }

        public SectorReport(SectorData data, Player player, ISector_Ltd item)
            : base(data, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var sData = data as SectorData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SectorIndex)) {
                SectorIndex = sData.SectorIndex;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //public SectorReport(SectorData data, Player player, ISector item)
        //    : base(player, item) {
        //    AssignValues(data);
        //}

        //private void AssignValues(AItemData data) {
        //    var sData = data as SectorData;
        //    Name = sData.Name;
        //    Owner = sData.Owner;
        //    Position = sData.Position;
        //    SectorIndex = sData.SectorIndex;
        //    //Density = sData.Density;
        //}

        #endregion

    }
}

