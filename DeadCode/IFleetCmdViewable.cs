// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmdViewable.cs
// Interface used by a FleetCmdPresenter to communicate with their associated FleetCmdView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface used by a FleetCmdPresenter to communicate with their associated FleetCmdView.
    /// </summary>
    public interface IFleetCmdViewable : ICommandViewable {

        /// <summary>
        /// Assesses whether the plotted course for this fleet should be shown.
        /// </summary>
        /// <param name="course">The course.</param>
        void AssessShowPlottedPath(IList<Vector3> course);

    }
}

