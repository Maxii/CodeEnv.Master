// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATask.cs
//  A base class for a Task that does work when TaskManager calls its Tick method.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// A base class for a Task that does work when TaskManager calls its Tick method. For Usage:
    /// <see cref="https://github.com/prime31/P31TaskManager/tree/master/Assets"/>
    /// </summary>
    public abstract class ATask {

        public static ITaskManager taskMgr;

        public TaskState state; // the tasks current state
        public float delay; // delay when starting the task in seconds
        public ATask nextTask;

        public object userInfo; // random bucket for data associated with the task
        public Action<ATask> onCompletion;


        private float _elapsedTime; // timer used internally for NOTHING RIGHT NOW!
        public float elapsedTime { get { return _elapsedTime; } }

        /// <summary>
        /// subclasses should override this and set state to Complete when done
        /// </summary>
        public abstract void Tick();

        /// <summary>
        /// reset all state before we start the task
        /// </summary>
        public virtual void ResetState() {
            _elapsedTime = 0;
            state = TaskState.NotRunning;
        }

        /// <summary>
        /// called when the task is started. allows setup/cleanup to occur and delays to be used
        /// </summary>
        public virtual void TaskStarted() {
            ResetState();

            // if we are delayed then set ourself as paused then unpause after the delay
            if (delay > 0) {
                state = TaskState.Paused;

                var delayInMilliseconds = (int)(delay * 1000);
                new System.Threading.Timer(obj => {
                    lock (this) {
                        state = TaskState.Running;
                    }
                }, null, delayInMilliseconds, System.Threading.Timeout.Infinite);
            }
            else {
                // start immediately
                state = TaskState.Running;
            }
        }


        /// <summary>
        /// called when the task is completed
        /// </summary>
        public void TaskCompleted() {
            // fire off our completion handler if we have one
            if (onCompletion != null)
                onCompletion(this);

            // if we have a next task to run and we were not cancelled, start it
            if (nextTask != null && state != TaskState.Canceled)
                taskMgr.AddTask(nextTask);
        }

        /// <summary>
        /// cancelling a task stops it immediately and causes its nextTask to not be executed
        /// </summary>
        public void Cancel() {
            state = TaskState.Canceled;
        }

        public void Pause() {
            state = TaskState.Paused;
        }

        public void Unpause() {
            state = TaskState.Running;
        }

    }
}

