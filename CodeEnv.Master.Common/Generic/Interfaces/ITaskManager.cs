// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITaskManager.cs
//  Interface for TaskManager (monoBehaviour) that allows all ATask-derived
// tasks to reside in the Common or GameContent assemblies.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface for TaskManager (monoBehaviour) that allows all ATask-derived
    /// tasks to reside in the Common or GameContent assemblies.
    /// </summary>
    public interface ITaskManager {

        void AddTask(ATask task, params ATask[] otherTasks);

    }
}

