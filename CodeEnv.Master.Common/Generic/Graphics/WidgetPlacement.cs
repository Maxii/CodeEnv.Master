// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WidgetPlacement.cs
// Placement of an Ngui Widget relative to a target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Placement of an Ngui Widget relative to a target.
    /// </summary>
    public enum WidgetPlacement {

        None,

        /// <summary>
        /// To the right of the target.
        /// </summary>
        Right,

        /// <summary>
        /// To the left of the target.
        /// </summary>
        Left,

        Above,

        AboveRight,

        AboveLeft,

        Below,

        BelowRight,

        BelowLeft,

        /// <summary>
        /// Directly over the center of the target.
        /// </summary>
        Over

    }
}

