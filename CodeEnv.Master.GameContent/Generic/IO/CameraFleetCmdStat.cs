// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFleetCmdStat.cs
// Camera settings for a FleetCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera settings for a FleetCmd.
    /// </summary>
    public class CameraFleetCmdStat : CameraFollowableStat {

        /// <summary>
        /// ICameraFocusable's OptimalViewingDistance for a fleetCmd is calculated differently than most Items.
        /// Normally, the CameraFollowableStat value is used directly as the ICameraFollowable OptimalViewingDistance.
        /// A FleetCmd's ICameraFollowable OptimalViewingDistance is overridden and based off of its UnitRadius.
        /// As such, this value is used as an adder to the UnitRadius rather than as the OptimalViewingDistance itself.
        /// </summary>
        public float OptimalViewingDistanceAdder { get; private set; }

        /// <summary>
        /// The OptimalViewingDistance for a fleetCmd has no meaning and will throw an InvalidOperationException.
        /// Instead use OptimalViewingDistanceAdder.
        /// </summary>
        public override float OptimalViewingDistance { get { throw new InvalidOperationException(); } }

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
            : base(minViewDistance, Constants.ZeroF, fov, followDistanceDampener, followRotationDampener) {
            OptimalViewingDistanceAdder = optViewDistanceAdder;
        }

        protected override void ValidateOptimalViewingDistance() {
            // Does nothing. Overridden to avoid accessing OptimalViewingDistance
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

