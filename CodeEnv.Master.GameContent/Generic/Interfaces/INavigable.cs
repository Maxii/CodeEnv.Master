// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INavigable.cs
// Interface for a destination that Ships and Fleets can navigate to.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///Interface for a destination that Ships and Fleets can navigate to.
    /// </summary>
    public interface INavigable {

        /// <summary>
        /// The name to use for displaying in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The name to use for debugging.
        /// </summary>
        string FullName { get; }

        Vector3 Position { get; }

        bool IsMobile { get; }

    }
}

