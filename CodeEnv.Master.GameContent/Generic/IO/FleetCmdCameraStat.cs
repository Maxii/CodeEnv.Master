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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera stat for a FleetCmd.
    /// </summary>
    public class FleetCmdCameraStat : CmdCameraStat {

        public float FollowDistanceDamper { get; private set; }

        public float FollowRotationDamper { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdCameraStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder">ICameraFocusable's OptimalViewingDistance for a fleetCmd is calculated differently than most Items.
        /// Normally, the FollowableItemCameraStat value is used directly as the ICameraFollowable OptimalViewingDistance.
        /// A FleetCmd's ICameraFollowable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The field of view.</param>
        /// <param name="followDistanceDamper">The follow distance damper. Default is 3F.</param>
        /// <param name="followRotationDamper">The follow rotation damper. Default is 10F as Fleets can change directions pretty fast.</param>
        public FleetCmdCameraStat(float minViewDistance, float optViewDistanceAdder, float fov, float followDistanceDamper = 3F,
            float followRotationDamper = 10F)
            : base(minViewDistance, optViewDistanceAdder, fov) {
            D.Assert(followDistanceDamper > Constants.OneF);
            D.Assert(followRotationDamper > Constants.ZeroF);
            FollowDistanceDamper = followDistanceDamper;
            FollowRotationDamper = followRotationDamper;
        }

    }
}

