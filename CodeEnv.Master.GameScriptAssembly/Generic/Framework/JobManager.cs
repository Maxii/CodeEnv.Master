// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: JobManager.cs
// Singleton. MonoBehaviour that creates, launches, executes and manages the lifecycle of Jobs.
// Derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Singleton. MonoBehaviour that creates, launches, executes and manages the lifecycle of Jobs.
/// Derived from P31 Job Manager.
/// </summary>
public class JobManager : AMonoSingleton<JobManager>, IJobManager, IJobRunner {

    private const string JobReuseOrCreationProfilerText = "Job Reuse or Creation";

    public override bool IsPersistentAcrossScenes { get { return true; } }

    private string DebugName { get { return GetType().Name; } }

    private bool IsGameRunning { get { return _gameMgr.IsRunning; } }

    /// <summary>
    /// Jobs that are available for reuse by using Job.Restart().
    /// </summary>
    private Stack<Job> _reusableJobCache;

    /// <summary>
    /// Jobs that have been started and are either
    /// 1) not yet completed, or
    /// 2) if completed, they have not yet been recycled.
    /// </summary>
    private HashSet<Job> _allExecutingJobs;

    /// <summary>
    /// Jobs that will automatically be Kill()ed on transition to a new scene.
    /// </summary>
    private HashSet<Job> _killableJobs;

    /// <summary>
    /// Running Jobs that are allowed to change pause state when the game changes pause state.
    /// <remarks>Most running Jobs are in this list. Those that are not are generally jobs that 
    /// 1) allow interaction with the camera/mouse when paused, or 
    /// 2) execute behavior that would look unnatural if stopped in mid motion.
    /// Examples of Item 1 include Highlight interaction and HUD response to the mouse.
    /// Examples of 2 include eye candy animations like rotation or beam operation and GUI fades when 
    /// opening or closing menus.
    /// </summary>
    private HashSet<Job> _pausableJobs;
    private GameTime _gameTime;
    private bool _isGamePaused;
    private IGameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        Job.JobRunner = Instance;
        GameReferences.JobManager = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        Subscribe();
        PreloadJobs(preloadQty: 20);
        //__TestCoroutineExecutionSequencing();
    }

    private void InitializeValuesAndReferences() {
        _gameTime = GameTime.Instance;
        _gameMgr = GameReferences.GameManager;
        _allExecutingJobs = new HashSet<Job>();
        _killableJobs = new HashSet<Job>();
        _pausableJobs = new HashSet<Job>();
        _reusableJobCache = new Stack<Job>(1000);
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
        _gameMgr.sceneLoading += SceneLoadingEventHandler;
    }

    /// <summary>
    /// Preloads the JobManager with a quantity of Jobs determined by GameSettings in preparation for a new Game instance.
    /// </summary>
    /// <param name="gameSettings">The game settings.</param>
    public void PreloadJobsDeterminedBy(GameSettings gameSettings) {
        int preloadQty = __CalcPreloadQty(gameSettings);
        RecycleAllCompletedJobs();

        // 3.8.17 Removed all Asserts for expected JobCounts as I want to be able to call this when I want to

        int reusableJobCount = _reusableJobCache.Count + _allExecutingJobs.Count;
        preloadQty -= reusableJobCount;
        if (preloadQty < Constants.Zero) {
            preloadQty = Constants.Zero;
        }

        PreloadJobs(preloadQty);
        D.Log("{0} is pre-loading Jobs having added {1} to the {2} already available.", DebugName, preloadQty, reusableJobCount);
    }

    private void PreloadJobs(int preloadQty) {
        for (int i = 0; i < preloadQty; i++) {
            _reusableJobCache.Push(new Job());
        }
        __cumPreloadQty += preloadQty;
    }

    #region Event and Property Change Handlers

    private void SceneLoadingEventHandler(object sender, EventArgs e) {
        HandleSceneLoading();
    }

    private void IsPausedPropChangedHandler() {
        _isGamePaused = _gameMgr.IsPaused;
        ChangePauseStateOfJobs(_isGamePaused);
        //string pauseStateMsg = _isGamePaused ? "paused" : "unpaused";
        //D.Log("{0} {1} {2} Jobs. Frame: {3}, Jobs: {4}.", DebugName, pauseStateMsg, _pausableJobs.Count, Time.frameCount, _pausableJobs.Select(job => job.JobName).Concatenate());
    }

    #endregion

    /// <summary>
    /// Called when a scene has begun loading.
    /// </summary>
    private void HandleSceneLoading() {
        KillAllKillableJobs();
        __ReportJobStatistics();
    }

    /// <summary>
    /// Initiates execution of a Job. The Job used will either be from the reusable cache of Jobs or a new Job if none are available.
    /// </summary>
    /// <param name="coroutine">The coroutine.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="customYI">The custom yield instruction. Can be null.</param>
    /// <param name="jobCompleted">The delegate fired on Job Completion. Can be null.</param>
    /// <returns></returns>
    private Job RunJob(IEnumerator coroutine, string jobName, APausableKillableYieldInstruction customYI, Action<bool> jobCompleted) {
        Job job;
        if (_reusableJobCache.Count > Constants.Zero) {
            job = _reusableJobCache.Pop();
            //D.Log("{0} is about to reuse a recycled Job to make Job {1}.", DebugName, jobName);
            job.Restart(coroutine, jobName, customYI, jobCompleted);
            __reusedJobNames.Add(jobName);
        }
        else {
            //D.Log("{0} is about to create new Job {1} as no recycled Jobs are available.", DebugName, jobName);
            job = new Job(coroutine, jobName, customYI, jobCompleted);
            __cumNewJobCount++;
        }
        return job;
    }

    /// <summary>
    /// Adds a running job to the lists of tracked jobs.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="isPausable">if set to <c>true</c> the job is pausable and should have its 
    /// paused state changed when the game changes its paused state.</param>
    /// <param name="isKillable">if set to <c>true</c> this job should be killed when the previous scene
    /// stops running and a new scene begins loading, aka when GameMgr.sceneLoading fires.</param>
    private void AddRunningJob(Job job, bool isPausable, bool isKillable = true) {
        D.Assert(job.IsRunning);
        //D.Log("{0} is adding {1} to _allExecutingJobs.", DebugName, job.JobName);
        _allExecutingJobs.Add(job);

        int executingJobCount = _allExecutingJobs.Count;
        if (executingJobCount > __peakJobUseCount) {
            __peakJobUseCount = executingJobCount;
        }

        if (isKillable) {
            _killableJobs.Add(job);
        }
        if (isPausable) {
            _pausableJobs.Add(job);
        }
    }

    #region Recycling

    /// <summary>
    /// The number of completed Jobs to allow to accumulate before initiating a recycle.
    /// </summary>
    private const int CompletedJobRecycleThreshold = 10;

    /// <summary>
    /// The buffer used to temporarily store completed jobs. Avoids excessive heap memory allocations.
    /// </summary>
    private IList<Job> _completedJobsBuffer = new List<Job>(CompletedJobRecycleThreshold);

    private int _completedJobCount;

    /// <summary>
    /// Called when a Job completes, this method accumulates completed jobs
    /// until the CompletedJobRecycleThreshold is reached, then recycles them.
    /// </summary>
    /// <returns></returns>
    private bool TryRecycleCompletedJobs(string jobName) {
        _completedJobCount++;
        //D.Log("{0}.TryRecycleCompletedJobs() called by {1}. {2} Jobs now awaiting recycle.", DebugName, jobName, _completedJobCount);
        if (_completedJobCount == CompletedJobRecycleThreshold) {
            _completedJobCount = 0;
            D.Assert(_completedJobsBuffer.Count == Constants.Zero);

            RecycleAllCompletedJobs();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Recycles all the jobs that have completed.
    /// </summary>
    private void RecycleAllCompletedJobs() {
        //D.Log("{0}.RecycleAllCompletedJobs() called in Frame {1}.", DebugName, Time.frameCount);
        foreach (Job job in _allExecutingJobs) {
            if (job.IsCompleted) {
                _completedJobsBuffer.Add(job);
            }
        }
        foreach (Job completedJob in _completedJobsBuffer) {
            Recycle(completedJob);
        }
        _completedJobsBuffer.Clear();
    }

    /// <summary>
    /// Recycles the provided job into the reusable Job cache.
    /// </summary>
    private void Recycle(Job job) {
        D.Assert(job.IsCompleted);
        D.Assert(!job.IsRunning);
        bool isRemoved = _allExecutingJobs.Remove(job);
        D.Assert(isRemoved);

        _killableJobs.Remove(job);
        _pausableJobs.Remove(job);
        _reusableJobCache.Push(job);

        job.OnRecycled();
        //D.Log("{0} has recycled a Job for future reuse.", DebugName);
        //D.Log("Remaining Jobs in _allExecutingJobs after recycle = {0}.", _allExecutingJobs.Select(j => j.JobName).Concatenate());
    }

    #endregion

    /// <summary>
    /// Kills all the Jobs designated as killable.
    /// </summary>
    private void KillAllKillableJobs() {
        D.Assert(!IsGameRunning);
        //D.Log("{0}.KillAllKillableJobs() called in Frame {1}.", DebugName, Time.frameCount);
        var runningJobsToKill = _killableJobs.Where(job => job.IsRunning).ToArray();   // ToArray reqd to avoid lazy evaluation gotcha!
        runningJobsToKill.ForAll(job => job.Kill());
        // 12.8.16 There are jobs that should continue to execute even after IsRunning becomes false. These jobs are launched
        // with StartNonGameplayJob() and are not designated as killable. The only current candidates are GameMgr's 
        // GameStateProgressReadinessCheck job and GuiWindow Fade jobs. GuiWindows like the Tooltip window need to be operational 
        // in the Lobby.

        //D.Log("{0}: {1} Jobs killed: {2}.", DebugName, runningJobsToKill.Count(), runningJobsToKill.Select(job => job.JobName).Concatenate());
        // Note: when jobs are killed, they attempt to remove themselves from the lists the next frame when the jobCompleted delegate fires. 
        // Trying to remove them now won't work as the criteria for removal is Job.IsCompleted which is set just before the delegate fires.
    }

    /// <summary>
    /// Changes the pause state of all pausable Jobs.
    /// </summary>
    /// <param name="toPause">if set to <c>true</c> [to pause].</param>
    private void ChangePauseStateOfJobs(bool toPause) {
        //D.Log("{0}.ChangePauseStateOfJobs({1}) called in Frame {2}.", DebugName, toPause, Time.frameCount);
        _pausableJobs.ForAll(job => {
            if (!job.IsCompleted) {
                // 12.13.16 Avoids Job.IsPaused = toPause Assert failure when job.IsCompleted (-> _isPaused reset to false) 
                // but not yet removed from _pausableJobs. Jobs can be killed (and therefore completed) while paused so when
                // trying to unpause them after game is paused, it trips the 'already unpaused' Assert
                job.IsPaused = toPause;
            }
        });
    }

    /// <summary>
    /// Starts a Job to execute <c>coroutine</c> then executes the provided delegate when the coroutine completes.
    /// This version is intended for Jobs that startup the game, shutdown the game, or change game scenes. 
    /// As such these Jobs are not pausable, have no concept of game speed and won't be killed when IsRunning changes to false.
    /// Usage:
    /// StartNonGameplayJob(coroutine, jobName, jobCompleted: (jobWasKilled) =&gt; {
    /// Code to execute after the Job has completed;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// </summary>
    /// <param name="coroutine">The coroutine to execute.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="jobCompleted">The delegate to execute once the Job completes. The
    /// signature is jobCompleted(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job StartNonGameplayJob(IEnumerator coroutine, string jobName, Action<bool> jobCompleted = null) {

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(coroutine, jobName, null, jobCompleted: (jobWasKilled) => {
            if (jobCompleted != null) {
                jobCompleted(jobWasKilled);
            }
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: false, isKillable: false);
        return job;
    }

    /// <summary>
    /// Starts a Job to execute <c>coroutine</c> during GamePlay then executes the provided delegate when the coroutine completes.
    /// This version is intended for Jobs that run while the game is being played, not for
    /// Jobs that startup the game, shutdown the game, or change game scenes. The job will be killed
    /// when the game's IsRunning state changes to false. If isPaused is <c>true</c> the job
    /// automatically pauses and unpauses when the game's IsPaused state changes, including when started while paused.  
    /// It will throw an error if started when the game is not running.
    /// Warning: Does NOT account for GameSpeed changes.
    /// Usage:
    /// StartGameplayJob(coroutine, jobName, isPausable, jobCompleted: (jobWasKilled) =&gt; {
    /// Code to execute after the Job has completed;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// </summary>
    /// <param name="coroutine">The coroutine to execute.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="isPausable">if set to <c>true</c> the paused state of the job tracks that of the game.</param>
    /// <param name="jobCompleted">The delegate to execute once the Job completes. The
    /// signature is jobCompleted(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job StartGameplayJob(IEnumerator coroutine, string jobName, bool isPausable, Action<bool> jobCompleted = null) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(coroutine, jobName, null, jobCompleted: (jobWasKilled) => {
            if (jobCompleted != null) {
                jobCompleted(jobWasKilled);
            }
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Waits the designated number of seconds during GamePlay, then executes the provided delegate.
    /// Warning: Does not pause and does not account for GameSpeed changes.
    /// Usage:
    /// WaitForGameplaySeconds(seconds, jobName, waitFinished: (jobWasKilled) =&gt; {
    /// Code to execute after the wait;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// </summary>
    /// <param name="seconds">The seconds to wait.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitFinished">The delegate to execute once the wait is finished. The
    /// signature is waitFinished(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job WaitForGameplaySeconds(float seconds, string jobName, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(WaitForSeconds(seconds), jobName, null, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: false);
        return job;
    }

    /// <summary>
    /// Waits the designated number of seconds during GamePlay, then executes the provided waitMilestone delegate and repeats.
    /// Warning: Does not pause and does not account for GameSpeed changes.
    /// Usage:
    /// RecurringWaitForGameplaySeconds(initialWait, recurringWait, jobName, waitMilestone: () =&gt; {
    /// Code to execute after the wait;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// <remarks>WARNING: The return Job must be killed by the client to end the recurring wait.</remarks>
    /// </summary>
    /// <param name="initialWait">The initial wait in seconds.</param>
    /// <param name="recurringWait">The recurring wait in seconds.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitMilestone">The delegate to execute when the waitMilestone occurs. The
    /// signature is waitMilestone().</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job RecurringWaitForGameplaySeconds(float initialWait, float recurringWait, string jobName, Action waitMilestone) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(RepeatingWaitForSeconds(initialWait, recurringWait, waitMilestone), jobName, null, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled, jobName);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: false);
        return job;
    }

    /// <summary>
    /// Waits the designated number of hours during GamePlay, then executes the provided delegate.
    /// Automatically accounts for Pause and GameSpeed changes.
    /// Usage:
    /// WaitForHours(hours, jobName, waitFinished: (jobWasKilled) =&gt; {
    /// Code to execute after the wait;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// </summary>
    /// <param name="hours">The hours to wait.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitFinished">The delegate to execute once the wait is finished. The
    /// signature is waitFinished(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job WaitForHours(float hours, string jobName, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        WaitForHours waitYieldInstruction = new WaitForHours(hours);
        Job job = RunJob(WaitForHours(waitYieldInstruction), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Debug version that waits the designated number of hours during GamePlay, then executes the provided delegate.
    /// Automatically accounts for Pause and GameSpeed changes. Used with workaround to accommodate
    /// the 1 frame gap between when the coroutine finishes (or is killed) and when waitFinished fires.
    /// Usage:
    /// WaitForHours(hours, jobName, isInterveningJobRef, waitFinished: (jobWasKilled) =&gt; {
    /// Code to execute after the wait;
    /// });
    /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitFinished delegate.
    /// </summary>
    /// <param name="hours">The hours to wait.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="isInterveningJobRef">Reference&lt;bool&gt; to allow the WaitForHours coroutine to toggle it to false 
    /// when it starts running.</param>
    /// <param name="waitFinished">The delegate to execute once the wait is finished. The
    /// signature is waitFinished(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    [Obsolete]
    public Job __WaitForHours(float hours, string jobName, Reference<bool> isInterveningJobRef, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        WaitForHours waitYieldInstruction = new WaitForHours(hours);
        Job job = RunJob(__WaitForHours(waitYieldInstruction, isInterveningJobRef), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Waits the designated (but variable) duration during GamePlay, then executes the provided waitMilestone delegate and repeats.
    /// Automatically accounts for Pause and GameSpeed changes.
    /// Usage:
    /// RecurringWaitForHours(durationReference, jobName, waitMilestone () =&gt; {
    /// Code to execute after each wait;
    /// });
    /// <remarks>WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitMilestone delegate.</remarks>
    /// <remarks>WARNING: The return Job must be killed by the client to end the recurring wait.</remarks>
    /// </summary>
    /// <param name="durationReference">The duration reference that tracks duration value changes.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitMilestone">The delegate to execute when the waitMilestone occurs. The
    /// signature is waitMilestone().</param>
    /// <returns>
    /// A reference to the Job so it can be killed.
    /// </returns>
    public Job RecurringWaitForHours(Reference<GameTimeDuration> durationReference, string jobName, Action waitMilestone) {
        ValidateGameIsRunning(jobName);
        //D.Log("Launching new RecurringWaitForHours(Ref) Job named {0}. Hours = {1}.", jobName, durationReference.Value.TotalInHours);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        RecurringWaitForHours waitYieldInstruction = new RecurringWaitForHours(durationReference);
        Job job = RunJob(RecurringWaitForHours(waitYieldInstruction, waitMilestone), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled, jobName);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Waits the designated duration during GamePlay, then executes the provided waitMilestone delegate and repeats.
    /// Automatically accounts for Pause and GameSpeed changes.
    /// Usage:
    /// RecurringWaitForHours(hours, jobName, waitMilestone =&gt; {
    /// Code to execute after each wait;
    /// });
    /// <remarks>WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitMilestone delegate.</remarks><remarks>WARNING: The return Job must be killed by the client to end the recurring wait.</remarks>
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitMilestone">The delegate to execute when the waitMilestone occurs. The
    /// signature is waitMilestone().</param>
    /// <returns>
    /// A reference to the Job so it can be killed.
    /// </returns>
    public Job RecurringWaitForHours(GameTimeDuration duration, string jobName, Action waitMilestone) {
        ValidateGameIsRunning(jobName);
        //D.Log("Launching new RecurringWaitForHours Job named {0} Hours = {1}.", jobName, duration.TotalInHours);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        RecurringWaitForHours waitYieldInstruction = new RecurringWaitForHours(duration);
        Job job = RunJob(RecurringWaitForHours(waitYieldInstruction, waitMilestone), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled, jobName);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Waits the designated number of hours during GamePlay, then executes the provided waitMilestone delegate and repeats.
    /// Automatically accounts for Pause and GameSpeed changes.
    /// Usage:
    /// RecurringWaitForHours(hours, jobName, waitMilestone =&gt; {
    /// Code to execute after each wait;
    /// });
    /// <remarks>WARNING: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the waitMilestone delegate.</remarks><remarks>WARNING: The return Job must be killed by the client to end the recurring wait.</remarks>
    /// </summary>
    /// <param name="hours">The hours to wait.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitMilestone">The delegate to execute when the waitMilestone occurs. The
    /// signature is waitMilestone().</param>
    /// <returns>
    /// A reference to the Job so it can be killed.
    /// </returns>
    public Job RecurringWaitForHours(float hours, string jobName, Action waitMilestone) {
        return RecurringWaitForHours(new GameTimeDuration(hours), jobName, waitMilestone);
    }

    /// <summary>
    /// Waits for the designated GameDate during GamePlay, then executes the provided delegate.
    /// Automatically accounts for Pause and GameSpeed changes.
    /// <remarks>3.10.17 Usage largely replaced by more efficient DateMinder implementation.</remarks>
    /// Usage:
    /// WaitForDate(futureDate, jobName, onWaitFinished: (jobWasKilled) =&gt; {
    /// Code to execute after the wait;
    /// });
    /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
    /// immediately return which means the code you have following it will execute
    /// before the code assigned to the onWaitFinished delegate.
    /// </summary>
    /// <param name="futureDate">The future date.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="waitFinished">The delegate to execute once the wait is finished. The
    /// signature is waitFinished(jobWasKilled).</param>
    /// <returns>
    /// A reference to the Job so it can be killed before it finishes, if needed.
    /// </returns>
    public Job WaitForDate(GameDate futureDate, string jobName, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);
        if (futureDate < _gameTime.CurrentDate) {
            D.Warn("Future date {0} < Current date {1}.", futureDate, _gameTime.CurrentDate);
        }

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        WaitForDate waitYieldInstruction = new WaitForDate(futureDate);
        Job job = RunJob(WaitForDate(waitYieldInstruction), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Waits until waitWhileCondition returns <c>false</c> during GamePlay, then executes the provided delegate.
    /// </summary>
    /// <param name="waitWhileCondition">The <c>true</c> condition that continues the wait.</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="isPausable">if set to <c>true</c> the paused state of the job tracks that of the game.</param>
    /// <param name="waitFinished">The delegate to execute when the wait is finished.
    /// The signature is waitFinished(jobWasKilled).</param>
    /// <returns>A reference to the Job so it can be killed before it finishes, if needed.</returns>
    public Job WaitWhile(Func<bool> waitWhileCondition, string jobName, bool isPausable, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        MyWaitWhile waitYieldInstruction = new MyWaitWhile(waitWhileCondition);
        Job job = RunJob(WaitWhileCondition(waitYieldInstruction), jobName, waitYieldInstruction, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    /// <summary>
    /// Makes a one-time execution of the delegate during FixedUpdate.
    /// <remarks>12.17.16 I was thinking I would use this to manipulate values like velocity, rotation, etc in a Rigidbody,
    /// but according to Eric5h5 below, it's not necessary.</remarks>
    /// <see cref="http://answers.unity3d.com/questions/270552/not-sure-if-this-should-go-in-update-or-fixedupdat.html"/>
    /// </summary>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="isPausable">if set to <c>true</c> [is pausable].</param>
    /// <param name="execInFixedUpdate">The execute in fixed update.</param>
    public void OneTimeExecutionInFixedUpdate(string jobName, bool isPausable, Action execInFixedUpdate) {
        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(OneTimeWaitForFixedUpdate(), jobName, null, jobCompleted: (jobWasKilled) => {
            if (!jobWasKilled) {
                execInFixedUpdate();
            }
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", DebugName, jobName);
            job.IsPaused = true;
        }
    }

    /// <summary>
    /// Waits for a particle system to complete during GamePlay, then executes the onWaitFinished delegate.
    /// Warning: If any members of this particle system are set to loop, this method will fail.
    /// </summary>
    /// <param name="particleSystem">The particle system.</param>
    /// <param name="includeChildren">if set to <c>true</c> [include children].</param>
    /// <param name="jobName">Name of the job.</param>
    /// <param name="isPausable">if set to <c>true</c> the paused state of the job tracks that of the game.</param>
    /// <param name="waitFinished">The delegate to execute when the wait is finished.
    /// The signature is waitFinished(jobWasKilled).</param>
    /// <returns></returns>
    public Job WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren, string jobName, bool isPausable, Action<bool> waitFinished) {
        ValidateGameIsRunning(jobName);
        D.Assert(!particleSystem.main.loop); //D.Assert(!particleSystem.loop); Deprecated in Unity 5.5
        if (includeChildren && particleSystem.transform.childCount > Constants.Zero) {
            var childParticleSystems = particleSystem.gameObject.GetComponentsInChildren<ParticleSystem>().Except(particleSystem);
            childParticleSystems.ForAll(cps => {
                var childMainModule = cps.main;
                D.Assert(!childMainModule.loop);    // childParticleSystems.ForAll(cps => D.Assert(!cps.loop)); Deprecated in Unity 5.5
            });
        }

        Profiler.BeginSample(JobReuseOrCreationProfilerText, gameObject);
        Job job = RunJob(WaitForParticleSystemCompletion(particleSystem, includeChildren), jobName, null, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            TryRecycleCompletedJobs(jobName);
        });
        Profiler.EndSample();

        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            //D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    private void ValidateGameIsRunning(string jobName) {
        D.Assert(IsGameRunning, jobName);
    }

    #region Coroutines

    private IEnumerator WaitForSeconds(float seconds) {
        yield return Yielders.GetWaitForSeconds(seconds);
    }

    private IEnumerator RepeatingWaitForSeconds(float initialWait, float recurringWait, Action waitMilestone) {
        yield return Yielders.GetWaitForSeconds(initialWait);
        //D.Log("RepeatingWaitForSeconds Initial Milestone with Wait = {0:0.##} is firing on frame {1}.", initialWait, Time.frameCount);
        waitMilestone();
        while (true) {
            yield return Yielders.GetWaitForSeconds(recurringWait);
            //D.Log("RepeatingWaitForSeconds Milestone with Wait = {0:0.##} is firing on frame {1}.", recurringWait, Time.frameCount);
            waitMilestone();
        }
    }

    private IEnumerator WaitForHours(WaitForHours waitYI) {
        yield return waitYI;
    }

    [Obsolete]
    private IEnumerator __WaitForHours(WaitForHours waitYI, Reference<bool> isInterveningJobRef) {
        isInterveningJobRef.Value = false;
        yield return waitYI;
    }

    private IEnumerator RecurringWaitForHours(WaitForHours waitYI, Action waitMilestone) {
        while (true) {
            yield return waitYI;
            // 3.15.17 UNCLEAR is waitYI.IsKilled needed to keep last milestone after killed from firing
            //D.Log("RepeatingWaitForHours Milestone with Wait = {0:0.#} fired on frame {1}.", waitYI.DurationInHours, Time.frameCount);
            waitMilestone();
        }
    }

    private IEnumerator WaitForDate(WaitForDate waitYI) {
        yield return waitYI;
    }

    private IEnumerator WaitWhileCondition(MyWaitWhile waitWhileYI) {
        yield return waitWhileYI;
    }

    private IEnumerator OneTimeWaitForFixedUpdate() {
        yield return Yielders.WaitForFixedUpdate;
    }

    private IEnumerator WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren) {
        while (particleSystem != null && particleSystem.IsAlive(includeChildren)) {
            yield return null;
        }
    }

    #endregion

    #region Cleanup

    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        __ReportJobStatistics();
    }

    protected override void Cleanup() {
        Unsubscribe();
        _allExecutingJobs.ForAll(job => job.Dispose());
        _reusableJobCache.ForAll(job => job.Dispose());
        Job.JobRunner = null;
        GameReferences.JobManager = null;
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
        _gameMgr.sceneLoading -= SceneLoadingEventHandler;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private int __CalcPreloadQty(GameSettings gameSettings) {
        int fleetQty = DebugUtility.__GetApproxFleetQty(gameSettings);
        int baseQty = DebugUtility.__GetApproxBaseQty(gameSettings);
        int maxFacilities = baseQty * TempGameValues.MaxFacilitiesPerBase;
        int maxShips = fleetQty * TempGameValues.MaxShipsPerFleet;
        return Mathf.RoundToInt(fleetQty * 1F + maxShips * 1F + baseQty * 0.5F + maxFacilities * 0.5F);
    }

    /// <summary>
    /// The cumulative number of Jobs that have been preloaded.
    /// </summary>
    private int __cumPreloadQty;

    private IList<string> __reusedJobNames = new List<string>(1000);

    private int __peakJobUseCount;

    /// <summary>
    /// The cumulative number of Jobs that had to be created during this Game Instance.
    /// If a Job was newly created, there were no available recycled Jobs to reuse at the time.
    /// </summary>
    private int __cumNewJobCount;

    private void __ReportJobStatistics() {
        Debug.LogFormat("{0} Job use statistics: {1} reuses, {2} peak use, {3} new Jobs created beyond Preload Qty {4}.",
            DebugName, __reusedJobNames.Count, __peakJobUseCount, __cumNewJobCount, __cumPreloadQty);
        // EditorLog lists ALL the names which can easily >> 10000! Takes a long time for Editor to log them and shutdown
        //Debug.LogFormat("{0} Reused Job Names: {1}.", DebugName, __reusedJobNames.Concatenate());
    }

    private void __WarnOfJobsRunning(string whenMsg) {
        IEnumerable<Job> remainingRunningJobs;
        if ((remainingRunningJobs = _allExecutingJobs.Where(job => job.IsRunning)).Any()) {
            var runningJobNamesExceptProgressCheckJob = remainingRunningJobs.Select(job => job.JobName).Except(TempGameValues.__GameMgrProgressCheckJobName);
            if (runningJobNamesExceptProgressCheckJob.Any()) {
                string warningJobNames = runningJobNamesExceptProgressCheckJob.Concatenate();
                D.Warn("{0} has found {1} Jobs that are running when {2}. {3}.", DebugName, remainingRunningJobs.Count(), whenMsg, runningJobNamesExceptProgressCheckJob);
            }
        }
    }

    private void __TestCoroutineExecutionSequencing() {
        __LaunchPrintJob("A");
        __LaunchPrintJob("B");    // 12.11.16 Testing shows sequence is always A, B
    }

    private void __LaunchPrintJob(string msg) {
        new Job(Print(msg), "Job");
    }

    private IEnumerator Print(string msg) {
        while (true) {
            D.Log(msg);
            yield return null;
        }
    }

    #endregion

}


