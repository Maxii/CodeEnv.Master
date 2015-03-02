// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HighlightID.cs
// Enum identifying the different highlights.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum identifying the different highlights.
    /// </summary>
    public enum HighlightID {

        None,

        Focused,

        Selected,

        /// <summary>
        /// Highlight to show on element when Cmd selected.
        /// </summary>
        UnitElement

    }
}

