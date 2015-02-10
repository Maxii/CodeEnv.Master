// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseReport.cs
// Immutable report for StarbaseCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for StarbaseCmdItems.
    /// </summary>
    public class StarbaseReport : ACmdReport {

        public StarbaseCategory Category { get; private set; }

        public BaseComposition UnitComposition { get; private set; }

        public StarbaseReport(StarbaseCmdItemData cmdData, Player player, FacilityReport[] facilityReports)
            : base(cmdData, player, facilityReports) { }

        protected override void AssignValuesFrom(AElementItemReport[] elementReports, AUnitCmdItemData cmdData) {
            base.AssignValuesFrom(elementReports, cmdData);
            var knownElementCategories = elementReports.Cast<FacilityReport>().Select(r => r.Category).Where(cat => cat != FacilityCategory.None);
            UnitComposition = new BaseComposition(knownElementCategories);
            Category = UnitComposition != null ? (cmdData as StarbaseCmdItemData).GenerateCmdCategory(UnitComposition) : StarbaseCategory.None;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

