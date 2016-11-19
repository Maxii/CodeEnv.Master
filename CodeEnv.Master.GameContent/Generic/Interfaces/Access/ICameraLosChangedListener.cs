// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraLosChangedListener.cs
// Interface for easy access to CameraLosChangedListeners.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    ///  Interface for easy access to CameraLosChangedListeners.
    /// </summary>
    public interface ICameraLosChangedListener {

        /// <summary>
        /// Occurs when Camera Line Of Sight state has changed on this GameObject.
        /// </summary>
        event EventHandler inCameraLosChanged;

        bool InCameraLOS { get; }

        bool enabled { get; set; }

        Transform transform { get; }

    }
}

