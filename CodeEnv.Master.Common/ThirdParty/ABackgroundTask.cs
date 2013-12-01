// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ABackgroundTask.cs
// A base class for a Task that does work in a background worker thread 
// when TaskManager calls its Tick method.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// A base class for a Task that does work in a background worker thread 
    /// when TaskManager calls its Tick method.   For Usage:
    /// <see cref="https://github.com/prime31/P31TaskManager/tree/master/Assets"/>
    /// </summary>
    public abstract class ABackgroundTask : ATask {

    }
}

