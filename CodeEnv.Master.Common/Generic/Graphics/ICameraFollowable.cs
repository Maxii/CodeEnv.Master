// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraFollowable.cs
// Interface designating the game object it is attached too as an
// object that can be followed by the camera. Game objects that implement this
// interface will be moveable as following an object that can't move is pointless.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface designating the game object it is attached too as an
    /// object that can be followed by the camera. Game objects that implement this
    /// interface will be moveable as following an object that can't move is pointless.
    /// </summary>
    public interface ICameraFollowable : ICameraFocusable {

        float CameraFollowDistanceDampener { get; }

        float CameraFollowRotationDampener { get; }

    }
}

