// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmdModel.cs
// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
    /// </summary>
    public interface IFleetCmdModel : ICmdModel {

        FleetOrder CurrentOrder { get; set; }

        /// <summary>
        /// Indicates whether all ships in the fleet have assumed the bearing
        /// of the flagship. Currently used as a 'ready to depart' indicator so
        /// all fleet ships move together.
        /// </summary>
        bool IsBearingConfirmed { get; }

        void __OnHQElementEmergency();

    }
}

