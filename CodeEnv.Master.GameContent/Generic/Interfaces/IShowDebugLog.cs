// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShowDebugLog.cs
// Interface for objects that support the ShowDebugLog control.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for objects that support the ShowDebugLog control.
    /// </summary>
    public interface IShowDebugLog {

        bool ShowDebugLog { get; set; }

    }
}

