// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HangerFormationManager.cs
// Formation Manager for a Base's Hanger.
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
    /// Formation Manager for a Base's Hanger.
    /// </summary>
    public class HangerFormationManager : AFormationManager {

        public HangerFormationManager(IFormationMgrClient hangerCmd) : base(hangerCmd) { }

        protected override List<FormationStationSlotInfo> GenerateFormationSlotInfo(Formation formation, Transform followTransform, out float formationRadius) {
            D.AssertEqual(Formation.Hanger, formation);
            return GameReferences.FormationGenerator.GenerateHangerFormation(formation, followTransform, out formationRadius);
        }


    }
}

