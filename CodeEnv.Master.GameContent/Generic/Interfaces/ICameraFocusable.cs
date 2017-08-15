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
        /// <remarks>Anyone can set this property to true to become the Item in focus. Only MainCameraControl 
        /// should set this property to false. If you wish to remove the focused state from an Item, you 
        /// should set MainCameraControl's CurrentFocus property to null.</remarks>
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

