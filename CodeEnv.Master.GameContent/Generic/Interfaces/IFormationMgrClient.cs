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
    public interface IFormationMgrClient {

        AUnitCmdItemData Data { get; }

        /// <summary>
        /// Positions the element in the formation at this world space offset
        /// relative to the HQ/Cmd.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="stationOffset">The station offset.</param>
        void PositionElementInFormation(IUnitElementItem element, Vector3 stationOffset);

        /// <summary>
        /// Opportunity to cleanup after selective formation changes.
        /// <remarks>Currently called when 1) the formation is changed or 2) the HQElement changes
        /// which causes all elements to be repositioned relative to the HQElement.</remarks>
        /// </summary>
        void CleanupAfterFormationChanges();

    }
}

