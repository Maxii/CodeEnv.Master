// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetFormationManager.cs
// Formation Manager for a Fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Formation Manager for a Fleet.
    /// </summary>
    public class FleetFormationManager : AFormationManager {

        protected override int MaxElementCountPerUnit { get { return TempGameValues.MaxShipsPerFleet; } }

        public FleetFormationManager(IFormationMgrClient fleetCmd) : base(fleetCmd) { }

        protected override IList<Vector3> GenerateFormationStationOffsets(Formation formation, out float maxFormationRadius) {
            return FormationFactory.Instance.GenerateMaxFleetFormation(formation, out maxFormationRadius);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

