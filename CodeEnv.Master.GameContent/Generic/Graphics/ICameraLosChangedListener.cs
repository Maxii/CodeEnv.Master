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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    ///  Interface for easy access to CameraLosChangedListeners.
    /// </summary>
    public interface ICameraLosChangedListener {

        event Action<GameObject, bool> onCameraLosChanged;

        bool enabled { get; set; }

    }
}

