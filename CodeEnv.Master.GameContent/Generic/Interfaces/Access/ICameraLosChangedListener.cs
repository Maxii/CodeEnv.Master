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

        /// <summary>
        /// Checks whether the InvisibleMesh's size should be changed if present.
        /// <remarks>Typically called after there is a change to the associated widget's mesh size.</remarks>
        /// </summary>
        void CheckForInvisibleMeshSizeChange();


    }
}

