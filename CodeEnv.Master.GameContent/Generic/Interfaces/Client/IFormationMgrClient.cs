// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFormationMgrClient.cs
// Interface for clients of Formation Managers, aka UnitCmds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for clients of Formation Managers, aka UnitCmds.
    /// </summary>
    public interface IFormationMgrClient : IDebugable {

        Formation UnitFormation { get; }

        float UnitMaxFormationRadius { set; }

        Transform transform { get; }

        bool ShowDebugLog { get; }

        void PositionElementInFormation(IUnitElement element, FormationStationSlotInfo slotInfo);

    }
}

