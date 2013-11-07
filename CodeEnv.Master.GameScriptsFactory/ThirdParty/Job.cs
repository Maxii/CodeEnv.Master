// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Job.cs
// Interruptable Coroutine container that is run from Coroutine Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
/// <summary>
/// Interruptable Coroutine container that is run from Coroutine Manager.
/// </summary>
public class Job {

    /// <summary>
    /// Action delegate executed when the job is completed. Contains a
    /// boolean indicating whether the job was killed or completed normally.
    /// </summary>
    public event Action<bool> onJobComplete;

    public bool IsRunning { get; private set; }

    public bool IsPaused { get; private set; }

    private IEnumerator _coroutine;
    private bool _jobWasKilled;
    private Stack<Job> _childJobStack;


    #region constructors

    public Job(IEnumerator coroutine, bool toStart = false, Action<bool> onJobComplete = null) {
        _coroutine = coroutine;
        this.onJobComplete = onJobComplete;
        if (toStart) { Start(); }
    }

    #endregion

    private IEnumerator Run() {
        // null out the first run through in case we start paused
        yield return null;

        while (IsRunning) {
            if (IsPaused) {
                yield return null;
            }
            else {
                // run the next iteration and stop if we are done
                if (_coroutine.MoveNext()) {
                    yield return _coroutine.Current;
                }
                else {
                    // run our child jobs if we have any
                    if (_childJobStack != null && _childJobStack.Count > 0) {
                        Job childJob = _childJobStack.Pop();
                        _coroutine = childJob._coroutine;
                    }
                    else
                        IsRunning = false;
                }
            }
        }

        // fire off a complete event
        if (onJobComplete != null)
            onJobComplete(_jobWasKilled);
    }

    #region public API

    public Job CreateAndAddChildJob(IEnumerator coroutine) {
        var j = new Job(coroutine, false);
        AddChildJob(j);
        return j;
    }

    public void AddChildJob(Job childJob) {
        if (_childJobStack == null)
            _childJobStack = new Stack<Job>();
        _childJobStack.Push(childJob);
    }

    public void RemoveChildJob(Job childJob) {
        if (_childJobStack.Contains(childJob)) {
            var childStack = new Stack<Job>(_childJobStack.Count - 1);
            var allCurrentChildren = _childJobStack.ToArray();
            System.Array.Reverse(allCurrentChildren);

            for (var i = 0; i < allCurrentChildren.Length; i++) {
                var j = allCurrentChildren[i];
                if (j != childJob)
                    childStack.Push(j);
            }

            // assign the new stack
            _childJobStack = childStack;
        }
    }

    public void Start() {
        IsRunning = true;
        CoroutineManager.Instance.StartCoroutine(Run());
    }

    public IEnumerator StartAsCoroutine() {
        IsRunning = true;
        yield return CoroutineManager.Instance.StartCoroutine(Run());
    }

    public void Pause() {
        IsPaused = true;
    }

    public void Unpause() {
        IsPaused = false;
    }

    /// <summary>
    /// Stops this Job if running, along with all child jobs waiting.
    /// </summary>
    public void Kill() {
        _jobWasKilled = true;
        IsRunning = false;
        IsPaused = false;
    }

    // IMPROVE This and KillInDays needs to use GameTime which includes gamespeed and pausing
    public void Kill(float delayInSeconds) {
        var delay = (int)(delayInSeconds * 1000);
        new System.Threading.Timer(obj => {
            lock (this) {
                Kill();
            }
        }, null, delay, System.Threading.Timeout.Infinite);
    }

    //public void KillInDays(float delayInDays) {
    //    float daysAdjustmentFactor =  1F / (GameDate.DaysPerSecond * GameTime.Instance.GameSpeed.SpeedMultiplier());
    //    var delay = (int)(delayInDays * 1000 * daysAdjustmentFactor);
    //    new System.Threading.Timer(obj => {
    //        lock (this) {
    //            Kill();
    //        }
    //    }, null, delay, System.Threading.Timeout.Infinite);
    //}


    #endregion
}
