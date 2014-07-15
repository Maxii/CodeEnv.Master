﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiTrackable.cs
// Interface for Views that are trackable by GUI constructs such as Icons and Labels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Views that are trackable by GUI constructs
    /// such as Icons and Labels.
    /// </summary>
    public interface IGuiTrackable {

        Vector3 LeftExtent { get; }

        Vector3 RightExtent { get; }

        Vector3 UpperExtent { get; }

        Vector3 LowerExtent { get; }

        Transform Transform { get; }

    }
}

