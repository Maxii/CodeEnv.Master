﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITopographyChangeListener.cs
// Interface for mobile objects (ships and projectileOrdnance) that listen for 
// notification of Topography transitions from Topography Monitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for mobile objects (ships and projectileOrdnance) that listen for 
    /// notification of Topography transitions from Topography Monitors.
    /// <remarks>Could also be called ITopographyMonitorCLient.</remarks>
    /// </summary>
    public interface ITopographyChangeListener {

        string DebugName { get; }

        Vector3 Position { get; }

        void ChangeTopographyTo(Topography newTopography);

    }
}

