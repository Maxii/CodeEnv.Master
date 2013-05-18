// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraTarget.cs
// Marker Interface indicating that the Camera can use this object as a target of camera movement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Interface indicating that the Camera can use this object as a target of camera movement.
    /// </summary>
    public interface ICameraTarget {

        float MinimumCameraApproachDistance { get; }

    }
}

