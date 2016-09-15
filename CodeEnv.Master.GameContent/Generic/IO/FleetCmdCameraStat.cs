// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdCameraStat.cs
// Camera stat for a FleetCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera stat for a FleetCmd.
    /// </summary>
    public class FleetCmdCameraStat : CmdCameraStat {

        public float FollowDistanceDampener { get; private set; }

        public float FollowRotationDampener { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdCameraStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder">ICameraFocusable's OptimalViewingDistance for a fleetCmd is calculated differently than most Items.
        /// Normally, the FollowableItemCameraStat value is used directly as the ICameraFollowable OptimalViewingDistance.
        /// A FleetCmd's ICameraFollowable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The field of view.</param>
        /// <param name="followDistanceDampener">The follow distance dampener. Default is 3F.</param>
        /// <param name="followRotationDampener">The follow rotation dampener. Default is 10F as Fleets can change directions pretty fast.</param>
        public FleetCmdCameraStat(float minViewDistance, float optViewDistanceAdder, float fov, float followDistanceDampener = 3F, float followRotationDampener = 10F)
            : base(minViewDistance, optViewDistanceAdder, fov) {
            D.Assert(followDistanceDampener > Constants.OneF);
            D.Assert(followRotationDampener > Constants.ZeroF);
            FollowDistanceDampener = followDistanceDampener;
            FollowRotationDampener = followRotationDampener;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

