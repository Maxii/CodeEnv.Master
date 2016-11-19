// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraFollowable.cs
// Interface for items that can be followed by the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for items that can be followed by the camera. Items that implement this
    /// interface will be mobile as following an object that can't move is pointless.
    /// </summary>
    public interface ICameraFollowable : ICameraFocusable {

        float FollowDistanceDampener { get; }

        float FollowRotationDampener { get; }

    }
}

