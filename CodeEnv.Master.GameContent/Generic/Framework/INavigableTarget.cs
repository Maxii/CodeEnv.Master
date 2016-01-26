// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INavigableTarget.cs
// Interface for a target that one can navigate (move) to.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///Interface for a target that one can navigate (move) to.
    /// </summary>
    public interface INavigableTarget {

        /// <summary>
        /// The name to use for displaying in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The name to use for debugging.
        /// </summary>
        string FullName { get; }

        Vector3 Position { get; }

        bool IsMobile { get; }

        /// <summary>
        /// Readonly. The radius in units of the conceptual 'globe' that encompasses this Item. 
        /// </summary>
        float Radius { get; }

        Topography Topography { get; }

        /// <summary>
        /// The radius around this INavigableTarget that contains known obstacles.
        /// Used primarily by FleetNavigator to set the length of its obstacle detection castingRay
        /// so it doesn't determine the target itself as an obstacle.
        /// </summary>
        float RadiusAroundTargetContainingKnownObstacles { get; }

        /// <summary>
        /// Returns the distance from the target that is 'close enough' for a ship to deem itself 'arrived' at the target.
        /// <remarks>INavigableTargets that are IObstacles incorporate the shipCollisionDetectionRadius in their answers
        /// so the ship's CollisionDetection collider won't encounter the target's AvoidableObstacleZone. 
        /// Those that aren't IObstacles have no need to incorporate the value.</remarks>
        /// </summary>
        /// <param name="shipCollisionDetectionRadius">The collision detection radius of the inquiring ship.</param>
        /// <returns></returns>
        float GetShipArrivalDistance(float shipCollisionDetectionRadius);


    }
}

