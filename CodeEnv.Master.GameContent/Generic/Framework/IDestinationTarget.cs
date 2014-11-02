﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDestinationTarget.cs
//  Interface for a target that can be a movement destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for a target that can be a movement destination.
    /// </summary>
    public interface IDestinationTarget {

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

        /// <summary>
        /// The radius in units of the conceptual 'globe' that encompasses this Item. Readonly.
        /// </summary>
        float Radius { get; }

        SpaceTopography Topography { get; }

    }
}

