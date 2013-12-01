// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TaskManager.cs
// Singleton, lightweight task manager that lets you run a task either on the main thread (like a coroutine)
// or on a background thread. Derived from P31TaskManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// This is a lightweight task manager (derived from P31TaskManager) that lets you run a task either on the main thread (like a coroutine)
/// or on a background thread. The general idea is for simple tasks you can use the built in ActionTask which will tick any 
/// Func (function that returns a boolean) you pass in each frame until you return false indicating the task is complete. 
/// For more complex tasks you can subclass AbstractTask for main thread tasks and AbstractBackgroundTask for background tasks. 
/// All non-background tasks can be paused, unpaused or cancelled (background tasks cannot be paused).
///
///A task is defined as anything that requires work. It could be as simple as a method that needs to be called each frame (similar to Update
///but with the ability to pause/unpause/cancel) or as complex as something that needs to run on a background thread.
///
///Tasks can also be chained. Each task has a nextTask property that you can set with any other task. 
///This allows you to perform a series of work with each step needing the result of the previous task or to simply sequence any tasks.
/// For Usage:
/// <see cref="https://github.com/prime31/P31TaskManager/tree/master/Assets"/>
/// </summary>
public class TaskManager : MonoBehaviour, ITaskManager {

    private List<ATask> _taskList = new List<ATask>();
    private Queue<ATask> _completedTaskQueue = new Queue<ATask>();
    private bool _isRunningTasks = false;

    private List<ABackgroundTask> _backgroundTaskList = new List<ABackgroundTask>();
    private bool _isRunningBackgroundTasks = false;

    #region MonoBehaviour Singleton Pattern

    private static TaskManager _instance;
    public static TaskManager Instance {
        get {
            if (_instance == null) {
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(TaskManager);
                _instance = GameObject.FindObjectOfType(thisType) as TaskManager;
                if (_instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
                    D.Warn("No instance of {0} found, so a temporary one has been created. Called by {1}.", thisType.Name, callerIdMessage);

                    GameObject tempGO = new GameObject(thisType.Name, thisType);
                    _instance = tempGO.GetComponent<TaskManager>();
                    if (_instance == null) {
                        D.Error("Problem during the creation of {0}.", thisType.Name);
                    }
                }
                _instance.Initialize();
            }
            return _instance;
        }
    }

    void Awake() {
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            _instance = this as TaskManager;
            _instance.Initialize();
        }
    }

    // Make sure Instance isn't referenced anymore
    void OnApplicationQuit() {
        _instance = null;
    }
    #endregion

    private void Initialize() {
        // do any required initialization here as you would normally do in Awake()
        ATask.taskMgr = Instance;
    }

    /// <summary>
    /// adds a task to be run on the main thread
    /// </summary>
    public void AddTask(ATask task, params ATask[] otherTasks) {
        _taskList.Add(task);

        foreach (var t in otherTasks)
            _taskList.Add(t);

        // if our update loop isnt running start it up
        if (!_isRunningTasks)
            StartCoroutine(ProcessTasks());
    }

    /// <summary>
    /// adds a task to be run on a worker thread
    /// </summary>
    public void AddBackgroundTask(ABackgroundTask task) {
        _backgroundTaskList.Add(task);

        // if our update loop isnt running start it up
        if (!_isRunningBackgroundTasks)
            StartCoroutine(ProcessBackgroundTasks());
    }

    /// <summary>
    /// runs through all current tasks (when there are some) and calls their tick method
    /// </summary>
    private IEnumerator ProcessTasks() {
        _isRunningTasks = true;

        // keep the loop running as long as we have tasks to run
        while (_taskList.Count > 0) {
            foreach (var task in _taskList) {
                // if the task is not running, prepare it. taskStarted could set a task to running
                // so this needs to be the first item in the loop
                if (task.state == TaskState.NotRunning)
                    task.TaskStarted();

                // tick any tasks that need to run
                if (task.state == TaskState.Running)
                    task.Tick();

                // prepare to clear out any tasks that are completed or cancelled
                if (task.state == TaskState.Complete || task.state == TaskState.Canceled)
                    _completedTaskQueue.Enqueue(task);
            }

            // done running our tasks so lets clear out the completed queue now
            if (_completedTaskQueue.Count > 0) {
                foreach (var task in _completedTaskQueue) {
                    // we call taskCompleted here so that it can safely modify the task list
                    task.TaskCompleted();
                    _taskList.Remove(task);
                }
                _completedTaskQueue.Clear();
            }

            yield return null;
        }

        _isRunningTasks = false;
    }

    /// <summary>
    /// runs through all background tasks (when there are some) and manages their life cycle
    /// </summary>
    private IEnumerator ProcessBackgroundTasks() {
        _isRunningBackgroundTasks = true;
        var completedQueue = new Queue<ABackgroundTask>();

        // keep the loop running as long as we have tasks to run
        while (_backgroundTaskList.Count > 0) {
            foreach (var task in _backgroundTaskList) {
                // if the task is not running, prepare it. taskStarted could set a task to running
                // so this needs to be the first item in the loop
                if (task.state == TaskState.NotRunning)
                    task.TaskStarted();

                // tick any tasks that need to run. background tasks only get ticked once
                if (task.state == TaskState.Running) {
                    task.state = TaskState.InBackground;
                    System.Threading.ThreadPool.QueueUserWorkItem((obj) => {
                        task.Tick();
                    });
                }

                // prepare to clear out any tasks that are completed
                if (task.state == TaskState.Complete)
                    completedQueue.Enqueue(task);
            }

            // done running our tasks so lets clear out the completed queue now
            if (completedQueue.Count > 0) {
                foreach (var t in completedQueue) {
                    // we call taskCompleted here so that it can safely modify the task list
                    t.TaskCompleted();
                    _backgroundTaskList.Remove(t);
                }
                completedQueue.Clear();
            }

            yield return null;
        }

        _isRunningBackgroundTasks = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


