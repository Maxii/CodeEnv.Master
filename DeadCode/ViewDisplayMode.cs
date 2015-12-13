// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ViewDisplayMode.cs
// Enum indicating the mode of display for Views.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum indicating the mode of display for Views.
    /// </summary>
    public enum ViewDisplayMode {

        /// <summary>
        /// Default for error detection.
        /// </summary>
        None,

        /// <summary>
        /// No visual display.
        /// </summary>
        Hide,

        /// <summary>
        /// 2D visual display.
        /// </summary>
        TwoD,

        /// <summary>
        /// 3D visual display without Animations.
        /// </summary>
        ThreeD,

        /// <summary>
        /// 3D visual display with Animations.
        /// </summary>
        ThreeDAnimation

    }
}

