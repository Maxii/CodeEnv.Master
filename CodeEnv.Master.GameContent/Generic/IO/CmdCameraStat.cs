// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CmdCameraStat.cs
// CameraStat for a UnitCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// CameraStat for a UnitCmd.
    /// </summary>
    public class CmdCameraStat : AItemCameraStat {

        /// <summary>
        /// ICameraFocusable's OptimalViewingDistance for a UnitCmd is calculated differently than most Items.
        /// Normally, the FocusableItemCameraStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.
        /// </summary>
        public float OptimalViewingDistanceAdder { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdCameraStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistanceAdder"> ICameraFocusable's OptimalViewingDistance for a UnitCmd is calculated differently than most Items.
        /// Normally, the FocusableItemCameraStat value is used directly as the ICameraFocusable OptimalViewingDistance.
        /// A UnitCmd's ICameraFocusable OptimalViewingDistance is overridden and based off of its UnitFormationRadius.
        /// As such, this value is used as an adder to the UnitFormationRadius rather than as the OptimalViewingDistance itself.</param>
        /// <param name="fov">The field of view.</param>
        public CmdCameraStat(float minViewDistance, float optViewDistanceAdder, float fov)
            : base(minViewDistance, fov) {
            Utility.ValidateNotNegative(optViewDistanceAdder);
            OptimalViewingDistanceAdder = optViewDistanceAdder;
        }

    }
}

