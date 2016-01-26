// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseFormationManager.cs
// Formation Manager for a Starbase.
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
    /// Formation Manager for a Starbase.
    /// </summary>
    public class StarbaseFormationManager : AFormationManager {

        protected override int MaxElementCountPerUnit { get { return TempGameValues.MaxFacilitiesPerBase; } }

        public StarbaseFormationManager(IFormationMgrClient starbaseCmd) : base(starbaseCmd) { }

        protected override IList<Vector3> GenerateFormationStationOffsets(Formation formation, out float maxFormationRadius) {
            return FormationFactory.Instance.GenerateMaxStarbaseFormation(formation, out maxFormationRadius);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

