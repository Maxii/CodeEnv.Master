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

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for StarItems.
    /// </summary>
    public class StarReport : AIntelItemReport {

        public string ParentName { get; protected set; }

        public StarCategory Category { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }


        public StarReport(StarData data, Player player) : base(data, player) { }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var starData = data as StarData;
            SpecialResources = starData.SpecialResources;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            var starData = data as StarData;
            Capacity = starData.Capacity;
            Resources = starData.Resources;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageMinimal(data);
            var starData = data as StarData;
            Owner = starData.Owner;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageAware(data);
            var starData = data as StarData;
            Name = starData.Name;
            ParentName = starData.ParentName;
            Category = starData.Category;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

