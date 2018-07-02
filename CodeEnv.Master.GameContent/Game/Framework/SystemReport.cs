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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SystemItems.
    /// </summary>
    public class SystemReport : AIntelItemReport {

        public int? Capacity { get; private set; }

        public ResourcesYield Resources { get; private set; }

        public OutputsYield Outputs { get; private set; }

        public IntVector3 SectorID { get; private set; }

        public int? Population { get; private set; }

        public Hero Hero { get; private set; }

        // 7.10.16 Eliminated usage of Star, Settlement and Planetoid Reports to calculate partial System values.
        // Access to Owner, Capacity and Resources values now determined (in SystemAccessController) by whether 
        // Star, Settlement and Planetoid AccessControllers provide access. Partial values can be calculated if not.

        public SystemReport(SystemData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var sysData = data as SystemData;
            var accessCntlr = sysData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Name)) {
                Name = sysData.Name;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Position)) {
                Position = sysData.Position;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = sysData.SectorID;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Owner)) {
                // SystemAccessController grants access to owner if any System member has access 
                Owner = sysData.Owner;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                Capacity = sysData.Capacity;
            }
            else {
                Capacity = CalcCapacityFromSystemMembers(sysData);
            }

            // 6.12.18 Resource visibility in a SystemReport should always be derived from System
            // member reports as 1) System IntelCoverage can be different than individual member's
            // IntelCoverage, and 2) the assessment results using AssessResources(resourcesFromData)
            // can vary as each report can potentially respond differently to IsAwarenessOfPresenceAllowed(resID)
            // and IsAwarenessOfValueAllowed(resID), even if all coverages are the same.
            Resources = CalcResourcesFromSystemMemberReports(sysData);

            if (sysData.SettlementData != null) {
                var settlementReport = sysData.SettlementData.GetReport(Player);
                Outputs = settlementReport.UnitOutputs;
                Population = settlementReport.Population;
                Hero = settlementReport.Hero;
            }
        }

        #region Calc Values from System Members

        /// <summary>
        /// Calculates System resources from the reports of the star and planetoids.
        /// <remarks>10.21.17 Previously calculated using AssessResources(StarData) and AssessResources(PlanetoidData)
        /// which was erroneous as this Report's AssessResources() is customized for SystemData. Only Star Report's AssessResources()
        /// can be used for StarData and likewise for PlanetoidData since the criteria used by AssessResources() 
        /// in IsAwarenessOfPresenceAllowed(resID) and IsAwarenessOfValueAllowed(resID) can vary if overridden.</remarks>
        /// </summary>
        /// <param name="sysData">The system data.</param>
        /// <returns></returns>
        private ResourcesYield CalcResourcesFromSystemMemberReports(SystemData sysData) {
            IList<ResourcesYield> sysMembersResources = new List<ResourcesYield>();

            var starReport = sysData.StarData.GetReport(Player);
            sysMembersResources.Add(starReport.Resources);

            foreach (var pData in sysData.AllPlanetoidData) {
                var pReport = pData.GetReport(Player);
                sysMembersResources.Add(pReport.Resources);
            }
            // Settlements don't have inherent resources. Their Report.Resources value is the sum of the other members

            if (sysMembersResources.Any()) {
                return sysMembersResources.Sum();
            }
            return default(ResourcesYield);
        }

        private int? CalcCapacityFromSystemMembers(SystemData sysData) {
            IList<int> sysMembersCapacity = new List<int>();

            if (sysData.StarData.InfoAccessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                sysMembersCapacity.Add(sysData.StarData.Capacity);
            }
            foreach (var pData in sysData.AllPlanetoidData) {
                var accessCntlr = pData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                    sysMembersCapacity.Add(pData.Capacity);
                }
            }
            if (sysData.SettlementData != null && sysData.SettlementData.InfoAccessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                sysMembersCapacity.Add(sysData.SettlementData.Capacity);
            }

            if (sysMembersCapacity.Any()) {
                return sysMembersCapacity.Sum();
            }
            return null;
        }

        #endregion

        #region Debug

        protected override ResourcesYield AssessResources(ResourcesYield dataResources) {
            throw new System.NotImplementedException("AssessResources for SystemReport using member reports.");
        }

        protected override bool IsAwarenessOfPresenceAllowed(ResourceID resourceID) {
            throw new System.NotImplementedException("AssessResources for SystemReport using member reports.");
        }

        protected override bool IsAwarenessOfValueAllowed(ResourceID resourceID) {
            throw new System.NotImplementedException("AssessResources for SystemReport using member reports.");
        }

        #endregion

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

