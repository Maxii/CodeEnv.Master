// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CourseRefreshMode.cs
// Enum defining behaviour when refreshing a ship or fleet course.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining behaviour when refreshing a ship or fleet course.
    /// </summary>
    public enum CourseRefreshMode {

        /// <summary>
        /// Indicates this is a brand new course to a new destination.
        /// </summary>
        NewCourse,

        /// <summary>
        /// Indicates this is an existing course where the ship/fleet has encountered an
        /// Obstacle that requires the addition of a new waypoint (detour) to avoid it.
        /// </summary>
        AddWaypoint,

        /// <summary>
        /// Indicates this is an existing course where the ship/fleet has encountered an
        /// Obstacle while on a detour to avoid another obstacle previously encountered.
        /// The detour currently being pursued should be replaced by the new detour provided.
        /// </summary>
        ReplaceObstacleDetour,

        /// <summary>
        /// Indicates this is an existing course where the ship/fleet has successfully reached
        /// a waypoint which should now be removed.
        /// </summary>
        RemoveWaypoint,

        /// <summary>
        /// Indicates an existing course has been completed and should be cleared.
        /// </summary>
        ClearCourse

    }
}

