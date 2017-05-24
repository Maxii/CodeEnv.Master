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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        public IntVector3 SectorID { get; private set; }

        public StarReport(StarData data, Player player, IStar_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as StarData;
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
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.ParentName)) {
                ParentName = sData.ParentName;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = sData.Category;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                Resources = sData.Resources;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }
        }

    }
}

