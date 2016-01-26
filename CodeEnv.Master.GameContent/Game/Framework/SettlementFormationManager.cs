// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementFormationManager.cs
// Formation Manager for a Settlement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Formation Manager for a Settlement.
    /// </summary>
    public class SettlementFormationManager : AFormationManager {

        protected override int MaxElementCountPerUnit { get { return TempGameValues.MaxFacilitiesPerBase; } }

        public SettlementFormationManager(IFormationMgrClient settlementCmd) : base(settlementCmd) { }

        protected override IList<Vector3> GenerateFormationStationOffsets(Formation formation, out float maxFormationRadius) {
            return FormationFactory.Instance.GenerateMaxSettlementFormation(formation, out maxFormationRadius);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

