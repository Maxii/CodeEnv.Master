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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ICameraControl {

        /// <summary>
        /// The position of the camera in world space.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// The object the camera is currently focused on if it has one.
        /// </summary>
        ICameraFocusable CurrentFocus { get; set; }

        /// <summary>
        /// The distance from the camera's target point to the camera's focal plane.
        /// </summary>
        float DistanceToCameraTarget { get; }

        Camera MainCamera_Near { get; }

        Camera MainCamera_Far { get; }

        /// <summary>
        /// Returns <c>true</c> if the Camera's current position is within the radius
        /// of the universe and therefore has a valid SectorID, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorID">The sectorID.</param>
        /// <returns></returns>
        bool TryGetSectorID(out IntVector3 sectorID);

    }
}

