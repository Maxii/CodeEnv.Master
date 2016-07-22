// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarReport.cs
// Immutable report for StarItems.
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
    /// Immutable report for StarItems.
    /// </summary>
    public class StarReport : AIntelItemReport {

        public string ParentName { get; protected set; }

        public StarCategory Category { get; private set; }

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public Index3D SectorIndex { get; private set; }

        public StarReport(StarData data, Player player, IStar_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as StarData;
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
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.ParentName)) {
                ParentName = sData.ParentName;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Category)) {
                Category = sData.Category;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Capacity)) {
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Resources)) {
                Resources = sData.Resources;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SectorIndex)) {
                SectorIndex = sData.SectorIndex;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    var starData = data as StarData;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    var starData = data as StarData;
        //    Capacity = starData.Capacity;
        //    Resources = starData.Resources;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageEssential(data);
        //    var starData = data as StarData;
        //    Owner = starData.Owner;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBasic(data);
        //    var starData = data as StarData;
        //    Name = starData.Name;
        //    ParentName = starData.ParentName;
        //    Position = starData.Position;
        //    Category = starData.Category;
        //    SectorIndex = starData.SectorIndex;
        //}

        #endregion

    }
}

