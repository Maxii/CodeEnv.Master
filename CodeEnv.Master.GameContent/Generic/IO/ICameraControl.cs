// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraControl.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ICameraControl {

        /// <summary>
        /// Readonly. The location of the camera in sector space.
        /// </summary>
        Index3D SectorIndex { get; }

        /// <summary>
        /// The position of the camera in world space.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// The object the camera is currently focused on if it has one.
        /// </summary>
        ICameraFocusable CurrentFocus { get; set; }

        /// <summary>
        /// Tries to show the context menu. 
        /// NOTE: This is a preprocess method for ContextMenuPickHandler.OnPress(isDown) which is designed to show
        /// the context menu if the method is called both times (isDown = true, then isDown = false) over the same object.
        /// Unfortunately, that also means the context menu will show if a drag starts and ends over the same 
        /// ISelectable object. Therefore, this preprocess method is here to detect whether a drag is occurring before 
        /// passing it on to show the context menu.
        /// </summary>
        /// <param name="isDown">if set to <c>true</c> [is down].</param>
        void ShowContextMenuOnPress(bool isDown);

        /// <summary>
        /// The distance from the camera's target point to the camera's focal plane.
        /// </summary>
        float DistanceToCamera { get; }

    }
}

