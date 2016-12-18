// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INavigable.cs
// Base Interface for a destination that can be navigated to.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Base Interface for a destination that can be navigated to.
    /// </summary>
    public interface INavigable : IDebugable {

        /// <summary>
        /// The name to use for display.
        /// </summary>
        string Name { get; }

        Vector3 Position { get; }

        bool IsMobile { get; }

    }
}

