// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemReport.cs
// Immutable report for SystemItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SystemItems.
    /// </summary>
    public class SystemReport : AIntelItemReport {

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public IntVector3 SectorID { get; private set; }

        // 7.10.16 Eliminated usage of Star, Settlement and Planetoid Reports to calculate partial System values.
        // Access to Owner, Capacity and Resources values now determined (in SystemAccessController) by whether 
        // Star, Settlement and Planetoid AccessControllers provide access. Partial values can be calc'd if not.

        public SystemReport(SystemData data, Player player, ISystem_Ltd item)
            : base(data, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var sData = data as SystemData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {    // true if any member has access
                Owner = sData.Owner;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {    // true if all members have access
                Capacity = sData.Capacity;
            }
            else {
                Capacity = CalcPartialCapacity(sData.StarData, sData.AllPlanetoidData, sData.SettlementData);
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {   // true if all members have access
                Resources = sData.Resources;
            }
            else {
                Resources = CalcPartialResources(sData.StarData, sData.AllPlanetoidData, sData.SettlementData);
            }
        }

        private int? CalcPartialCapacity(StarData starData, IEnumerable<PlanetoidData> planetoidsData, SettlementCmdData settlementData) {
            int count = settlementData != null ? planetoidsData.Count() + 2 : planetoidsData.Count() + 1;
            IList<int> sysMembersCapacity = new List<int>(count);

            if (starData.InfoAccessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                sysMembersCapacity.Add(starData.Capacity);
            }
            foreach (var pData in planetoidsData) {
                var accessCntlr = pData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                    sysMembersCapacity.Add(pData.Capacity);
                }
            }
            if (settlementData != null && settlementData.InfoAccessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                sysMembersCapacity.Add(settlementData.Capacity);
            }

            if (sysMembersCapacity.Any()) {
                return sysMembersCapacity.Sum();
            }
            return null;
        }

        private ResourceYield? CalcPartialResources(StarData starData, IEnumerable<PlanetoidData> planetoidsData, SettlementCmdData settlementData) {
            int count = settlementData != null ? planetoidsData.Count() + 2 : planetoidsData.Count() + 1;
            IList<ResourceYield> sysMembersResources = new List<ResourceYield>(count);

            if (starData.InfoAccessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                sysMembersResources.Add(starData.Resources);
            }
            foreach (var pData in planetoidsData) {
                var accessCntlr = pData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                    sysMembersResources.Add(pData.Resources);
                }
            }
            if (settlementData != null && settlementData.InfoAccessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                sysMembersResources.Add(settlementData.Resources);
            }

            if (sysMembersResources.Any()) {
                return sysMembersResources.Sum();
            }
            return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //public SystemReport(SystemData data, Player player, ISystem item)
        //    : base(player, item) {
        //    StarReport = item.GetStarReport(player);
        //    SettlementCmdReport = item.GetSettlementReport(player);
        //    PlanetoidReports = item.GetPlanetoidReports(player);
        //    AssignValues(data);
        //    AssignValuesFromMemberReports();
        //}

        //private void AssignValues(AItemData data) {
        //    var sysData = data as SystemData;
        //    Name = sysData.Name;
        //    SectorID = sysData.SectorID;
        //    Position = sysData.Position;
        //}

        //private void AssignValuesFromMemberReports() {
        //    Owner = SettlementCmdReport != null ? SettlementCmdReport.Owner : null;        // IMPROVE NoPlayer?, other Settlement info?
        //    Capacity = StarReport.Capacity.NullableSum(PlanetoidReports.Select(pr => pr.Capacity).ToArray());
        //    Resources = StarReport.Resources.NullableSum(PlanetoidReports.Select(pr => pr.Resources).ToArray());
        //}

        #endregion

    }
}

