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

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for StarItems.
    /// </summary>
    public class StarReport : AIntelItemReport {

        public StarCategory Category { get; private set; }

        public int? Capacity { get; private set; }

        public ResourcesYield Resources { get; private set; }

        public IntVector3 SectorID { get; private set; }

        public StarReport(StarData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as StarData;
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
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = sData.Category;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }

            Resources = AssessResources(sData.Resources);
        }

    }
}

