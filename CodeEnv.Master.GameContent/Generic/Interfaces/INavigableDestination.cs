// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INavigableDestination.cs
// Base Interface for a destination that can be navigated to.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Base Interface for a destination that can be navigated to.
    /// </summary>
    public interface INavigableDestination {

        /// <summary>
        /// The name to use for display.
        /// </summary>
        string Name { get; }

        string DebugName { get; }

        Vector3 Position { get; }

        bool IsMobile { get; }

    }
}

