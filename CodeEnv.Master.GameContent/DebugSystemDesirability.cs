// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemDesirability.cs
// Debug version of SystemDesirability that removes None.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Debug version of SystemDesirability that removes None.
    /// <remarks>Used on GameSettingsDebugControl.</remarks>
    /// </summary>
    public enum DebugSystemDesirability {

        Desirable,
        Normal,
        Challenged

    }
}

