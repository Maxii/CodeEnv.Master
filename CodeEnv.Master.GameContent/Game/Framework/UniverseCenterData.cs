// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterData.cs
// Class for Data associated with the UniverseCenterItem..
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with the UniverseCenterItem..
    /// </summary>
    public class UniverseCenterData : AIntelItemData {

        public float Mass { get; private set; }

        public UniverseCenterData(Transform ucTransform, string name, float mass)
            : base(ucTransform, name, TempGameValues.NoPlayer) {
            Mass = mass;
            ucTransform.rigidbody.mass = mass;
            base.Topography = Topography.OpenSpace;
        }

        public override bool HasPlayerInvestigated(Player player) {
            return GetIntelCoverage(player) > IntelCoverage.Aware;
        }

        protected override AIntel InitializeIntelState(Player player) {
            var coverage = IsAllIntelCoverageComprehensive || Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.Aware;
            return new ImprovingIntel(coverage);
        }

        protected override void OnOwnerChanged() {
            throw new System.InvalidOperationException("Illegal attempt by {0} to set Owner: {1}.".Inject(FullName, Owner.LeaderName));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

