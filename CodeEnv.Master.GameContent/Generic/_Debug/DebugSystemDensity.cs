// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemDensity.cs
// Debug version of SystemDensity that removes None.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Debug version of SystemDensity that removes None.
    /// </summary>
    public enum DebugSystemDensity {

        Sparse,
        Low,
        Normal,
        High,
        Dense

    }
}

