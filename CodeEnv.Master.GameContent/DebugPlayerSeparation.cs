// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugPlayerSeparation.cs
// Debug version of PlayerSeparation that removes None.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Debug version of PlayerSeparation that removes None.
    /// <remarks>Used on GameSettingsDebugControl.</remarks>
    /// </summary>
    public enum DebugPlayerSeparation {

        Close,
        Normal,
        Distant

    }
}

