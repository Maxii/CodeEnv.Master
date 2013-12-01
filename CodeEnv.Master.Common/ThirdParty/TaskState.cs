// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TaskState.cs
// The state of an ATask.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// The state of an ATask.
    /// </summary>
    public enum TaskState {

        None,

        NotRunning,

        Running,

        InBackground,

        Paused,

        Canceled,

        Complete

    }
}

