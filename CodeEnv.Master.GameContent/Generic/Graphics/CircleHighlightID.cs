// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CircleHighlightID.cs
// Enum identifying the different highlights implemented by Vectrosity Circles.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum identifying the different highlights implemented by Vectrosity Circles.
    /// </summary>
    public enum CircleHighlightID {

        None,

        Focused,

        Selected,

        /// <summary>
        /// Highlight to show on element when Cmd selected.
        /// </summary>
        UnitElement

    }
}

