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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Formation Manager for a Starbase.
    /// </summary>
    public class StarbaseFormationManager : AFormationManager {

        public StarbaseFormationManager(IFormationMgrClient starbaseCmd) : base(starbaseCmd) { }

        protected override IList<FormationStationSlotInfo> GenerateFormationSlotInfo(Formation formation, Transform cmdTransform, out float formationRadius) {
            return GameReferences.FormationGenerator.GenerateBaseFormation(formation, cmdTransform, out formationRadius);
        }

    }
}

