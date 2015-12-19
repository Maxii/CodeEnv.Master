// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFleetCmdStat.cs
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
    public class CameraFleetCmdStat : ACameraItemStat {

        /// <summary>
        /// ICameraFocusable's OptimalViewingDistance for a fleetCmd is calculated differently than most Items.
        /// Normally, the CameraFollowableStat value is used directly as the ICameraFollowable OptimalViewingDistance.
        /// A FleetCmd's ICameraFollowable OptimalViewingDistance is overridden and based off of its UnitRadius.
        /// As such, this value is used as an adder to the UnitRadius rather than as the OptimalViewingDistance itself.
        /// </summary>
        public float OptimalViewingDistanceAdder { get; private set; }

        public float FollowDistanceDampener { get; private set; }

        public float FollowRotationDampener { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraFleetCmdStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder">ICameraFocusable's OptimalViewingDistance for a fleetCmd is calculated differently than most Items.
        /// Normally, the CameraFollowableStat value is used directly as the ICameraFollowable OptimalViewingDistance.
        /// A FleetCmd's ICameraFollowable OptimalViewingDistance is overridden and based off of its UnitRadius.
        /// As such, this value is used as an adder to the UnitRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The fov.</param>
        /// <param name="followDistanceDampener">The follow distance dampener. Default is 3F.</param>
        /// <param name="followRotationDampener">The follow rotation dampener. Default is 1F.</param>
        public CameraFleetCmdStat(float minViewDistance, float optViewDistanceAdder, float fov, float followDistanceDampener = 3F, float followRotationDampener = 1F)
            : base(minViewDistance, fov) {
            Arguments.ValidateNotNegative(optViewDistanceAdder);
            D.Assert(followDistanceDampener > Constants.OneF);
            D.Assert(followRotationDampener > Constants.ZeroF);
            FollowDistanceDampener = followRotationDampener;
            FollowRotationDampener = followRotationDampener;
            OptimalViewingDistanceAdder = optViewDistanceAdder;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

