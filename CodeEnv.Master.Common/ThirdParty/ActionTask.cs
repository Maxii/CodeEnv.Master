// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActionTask.cs
// A task that executes a simple action that returns false when completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// A task that executes a simple action that returns false when completed.
    /// The action is actually implemented as a Func&lt;bool&gt;.   For Usage:
    /// <see cref="https://github.com/prime31/P31TaskManager/tree/master/Assets"/>
    /// </summary>
    public class ActionTask : ATask {

        private Func<bool> _action;

        /// <summary>
        /// Creates and starts a task with no onCompletion handler.
        /// </summary>
        /// <param name="action">The action to execute, returning false when complete.</param>
        /// <returns></returns>
        public static ActionTask CreateAndStartTask(Func<bool> action) {
            return CreateAndStartTask(action, null);
        }

        /// <summary>
        /// Creates and starts a task with the designated onCompletion handler.
        /// </summary>
        /// <param name="action">The action to execute, returning false when complete.</param>
        /// <param name="onCompletion">The completion handler.</param>
        /// <returns></returns>
        public static ActionTask CreateAndStartTask(Func<bool> action, Action<ATask> onCompletion) {
            var actionTask = new ActionTask(action);
            actionTask.onCompletion = onCompletion;
            taskMgr.AddTask(actionTask);
            return actionTask;
        }

        public ActionTask(Func<bool> action) {
            _action = action;
        }

        public override void Tick() {
            if (!_action())
                state = TaskState.Complete;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

