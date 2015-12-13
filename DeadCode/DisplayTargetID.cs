// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DisplayTargetID.cs
// Identifier for the display target that is to receive output for display.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Identifier for the display target that is to receive output to display.
    /// Display targets can be labels, screens, popup menus, etc. Each unique
    /// target has an ID.
    /// </summary>
    [System.Obsolete]
    public enum DisplayTargetID {

        None,

        CursorHud,

        SystemsScreen,

        FleetsScreen,

        BasesScreen

    }
}

