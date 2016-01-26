// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraUnitCmdStat.cs
// CameraStat for a UnitCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// CameraStat for a UnitCmd.
    /// </summary>
    public class CameraUnitCmdStat : ACameraItemStat {

        /// <summary>
        /// ICameraFocusable's OptimalViewingDistance for a UnitCmd is calculated differently than most Items.
        /// Normally, the CameraFocusableStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.
        /// </summary>
        public float OptimalViewingDistanceAdder { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraUnitCmdStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder"> ICameraFocusable's OptimalViewingDistance for a UnitCmd is calculated differently than most Items.
        /// Normally, the CameraFocusableStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The fov.</param>
        public CameraUnitCmdStat(float minViewDistance, float optViewDistanceAdder, float fov)
            : base(minViewDistance, fov) {
            Arguments.ValidateNotNegative(optViewDistanceAdder);
            OptimalViewingDistanceAdder = optViewDistanceAdder;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

