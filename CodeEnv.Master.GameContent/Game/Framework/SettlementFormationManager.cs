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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Formation Manager for a Settlement.
    /// </summary>
    public class SettlementFormationManager : AFormationManager {

        public SettlementFormationManager(IFormationMgrClient settlementCmd) : base(settlementCmd) { }

        protected override IList<FormationStationSlotInfo> GenerateFormationSlotInfo(Formation formation, Transform cmdTransform, out float formationRadius) {
            return GameReferences.FormationGenerator.GenerateBaseFormation(formation, cmdTransform, out formationRadius);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

