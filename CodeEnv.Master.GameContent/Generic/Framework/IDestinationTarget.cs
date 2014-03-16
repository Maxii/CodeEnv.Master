﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDestinationTarget.cs
//  Interface for a target that is simply a destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for a target that is simply a destination.
    /// </summary>
    public interface IDestinationTarget {

        string Name { get; }

        Vector3 Position { get; }

        bool IsMovable { get; }

        float Radius { get; }

    }
}

