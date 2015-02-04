// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseReport.cs
// Immutable report on a starbase reflecting a specific player's IntelCoverage of
// the starbase's command and its elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report on a starbase reflecting a specific player's IntelCoverage of
    /// the starbase's command and its elements.
    /// </summary>
    public class StarbaseReport : ACmdReport {

        public StarbaseCategory Category { get; private set; }

        public BaseUnitComposition? UnitComposition { get; private set; }

        public StarbaseReport(StarbaseCmdData cmdData, Player player, FacilityReport[] facilityReports)
            : base(cmdData, player, facilityReports) { }

        protected override void AssignValuesFrom(AElementItemReport[] elementReports, ACommandData cmdData) {
            base.AssignValuesFrom(elementReports, cmdData);
            var knownElementCategories = elementReports.Cast<FacilityReport>().Select(r => r.Category).Where(cat => cat != FacilityCategory.None);
            UnitComposition = new BaseUnitComposition(knownElementCategories);
            Category = UnitComposition.HasValue ? (cmdData as StarbaseCmdData).GenerateCmdCategory(UnitComposition.Value) : StarbaseCategory.None;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

