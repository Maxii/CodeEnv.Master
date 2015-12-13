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

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
    /// </summary>
    [Obsolete]
    public interface IFleetCmdModel : ICmdModel {

        //FleetOrder CurrentOrder { get; set; }

        /// <summary>
        /// Indicates whether all ships in the fleet have assumed the bearing
        /// of the flagship. Currently used as a 'ready to depart' indicator so
        /// all fleet ships move together.
        /// </summary>
        //bool IsBearingConfirmed { get; }

        //void __OnHQElementEmergency();

        /// <summary>
        /// The fleet's current course as a list of points.
        /// </summary>
        //IList<Vector3> Course { get; }

        /// <summary>
        /// Reference to the potentially moving destination of the fleet.
        /// </summary>
        //Reference<Vector3> Destination { get; }

        /// <summary>
        /// Occurs when the fleet's course is changed, including when the
        /// course is cleared.
        /// </summary>
        //event Action onCoursePlotChanged;

    }
}

