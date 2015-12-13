// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraUnitBaseCmdStat.cs
// Camera settings for a UnitBaseCmd, aka Settlements and Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera settings for a UnitBaseCmd, aka Settlements and Starbases.
    /// </summary>
    [Obsolete]
    public class CameraUnitBaseCmdStat : CameraFocusableStat {

        /// <summary>
        /// ICameraFocusable's OptimalViewingDistance for a UnitBaseCmd is calculated differently than most Items.
        /// Normally, the CameraFocusableStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitBaseCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitRadius.
        /// As such, this value is used as an adder to the UnitRadius rather than as the OptimalViewingDistance itself.
        /// </summary>
        public float OptimalViewingDistanceAdder { get; private set; }

        /// <summary>
        /// The OptimalViewingDistance for a fleetCmd has no meaning and will throw an InvalidOperationException.
        /// Instead use OptimalViewingDistanceAdder.
        /// </summary>
        public override float OptimalViewingDistance { get { throw new InvalidOperationException(); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraUnitBaseCmdStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder">  ICameraFocusable's OptimalViewingDistance for a UnitBaseCmd is calculated differently than most Items.
        /// Normally, the CameraFocusableStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitBaseCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitRadius.
        /// As such, this value is used as an adder to the UnitRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The fov.</param>
        public CameraUnitBaseCmdStat(float minViewDistance, float optViewDistanceAdder, float fov)
            : base(minViewDistance, Constants.ZeroF, fov) {
            OptimalViewingDistanceAdder = optViewDistanceAdder;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

