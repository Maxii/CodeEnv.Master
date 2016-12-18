// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraFocusable.cs
// Interface for items that can become the focus of the camera, thereby allowing the 
// camera to approach, view and orbit the object. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for items that can become the focus of the camera, thereby allowing the 
    /// camera to approach, view and orbit the object. 
    /// </summary>
    public interface ICameraFocusable : ICameraTargetable {

        /// <summary>
        /// Indicates whether this item is the current focus of the camera.
        /// Set this value to become or remove the camera's focus.
        /// </summary>
        bool IsFocus { get; set; }

        bool IsRetainedFocusEligible { get; }

        float OptimalCameraViewingDistance { get; set; }

        /// <summary>
        /// The field of view setting that the cameras should adopt.
        /// </summary>
        float FieldOfView { get; }

        string Name { get; }

        string DebugName { get; }

    }
}

