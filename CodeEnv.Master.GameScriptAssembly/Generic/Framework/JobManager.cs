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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. MonoBehaviour that creates, launches, executes and manages the lifecycle of Jobs.
/// Derived from P31 Job Manager.
/// </summary>
public class JobManager : AMonoSingleton<JobManager>, IJobManager, IJobRunner {

    public override bool IsPersistentAcrossScenes { get { return true; } }

    private bool IsGameRunning { get { return _gameMgr.IsRunning; } }

    /// <summary>
    /// All the Jobs currently running.
    /// </summary>
    private List<Job> _allRunningJobs;

    /// <summary>
    /// Running Jobs that will automatically be Kill()ed when IsRunning changes to false.
    /// </summary>
    private List<Job> _jobsToKillWhenGameNoLongerRunning;

    /// <summary>
    /// Running Jobs that are allowed to change pause state when the game changes pause state.
    /// <remarks>Most running Jobs are in this list. Those that are not are generally jobs that 
    /// 1) allow interaction with the camera/mouse when paused, or 
    /// 2) execute behavior that would look unnatural if stopped in mid motion.
    /// Examples of Item 1 include Highlight interaction and HUD response to the mouse.
    /// Examples of 2 include eye candy animations like rotation or beam operation and GUI fades when 
    /// opening or closing menus.
    /// </summary>
    private List<Job> _pausableJobs;
    private GameTime _gameTime;
    private bool _isGamePaused;
    private IGameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        Job.JobRunner = Instance;
        References.JobManager = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameTime = GameTime.Instance;
        _gameMgr = References.GameManager;
        _allRunningJobs = new List<Job>(500);
        _jobsToKillWhenGameNoLongerRunning = new List<Job>(500);
        _pausableJobs = new List<Job>(500);
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsRunning, IsRunningPropChangedHandler));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void IsRunningPropChangedHandler() {
        HandleIsRunningChanged();
    }

    private void HandleIsRunningChanged() {
        //D.Log("{0} received IsRunning = {1} event.", GetType().Name, IsGameRunning);
        if (IsGameRunning) {
            D.Log("{0}: {1} Jobs running when IsRunning -> true. Jobs: {2}.", GetType().Name, _allRunningJobs.Count, _allRunningJobs.Select(job => job.JobName).Concatenate());
        }
        else {
            KillAllKillableJobs();
        }
    }

    private void IsPausedPropChangedHandler() {
        _isGamePaused = _gameMgr.IsPaused;
        ChangePauseStateOfJobs(_isGamePaused);
        string pauseStateMsg = _isGamePaused ? "paused" : "unpaused";
        D.Log("{0} {1} {2} Jobs. Frame: {3}, Jobs: {4}.", GetType().Name, pauseStateMsg, _pausableJobs.Count, Time.frameCount, _pausableJobs.Select(job => job.JobName).Concatenate());
    }

    #endregion

    /// <summary>
    /// Removes any completed (no longer running) jobs from the collections of Jobs.
    /// <remarks>Jobs that are 'killed' by the Job client will automatically be removed here.</remarks>
    /// </summary>
    private void RemoveCompletedJobs() {
        // http://stackoverflow.com/questions/17233558/remove-add-items-to-from-a-list-while-iterating-it
        _allRunningJobs.RemoveAll(job => !job.IsRunning);
        _jobsToKillWhenGameNoLongerRunning.RemoveAll(job => !job.IsRunning);
        _pausableJobs.RemoveAll(job => !job.IsRunning);
    }

    /// <summary>
    /// Kills all the Jobs designated as killable.
    /// </summary>
    private void KillAllKillableJobs() {
        D.Assert(!IsGameRunning);
        //D.Log("{0}.KillAllKillableJobs() called in Frame {1}.", GetType().Name, Time.frameCount);
        _jobsToKillWhenGameNoLongerRunning.ForAll(job => job.Kill());
        // 8.11.16 IMPROVE Rarely there could be jobs that continue to run and complete even when IsRunning becomes false.
        // To my knowledge, the only current candidates would be GuiWindow Fade jobs that may not yet have completed their 
        // fade. Currently they are not auto killable as GuiWindows like the Tooltip window need to be operational in the Lobby.
        // Right now, I don't offer a StartJob method that allows a Job to start when not running, yet also allows the Job to
        // be auto killed when IsRunning becomes false. This can be changed of course.
        IEnumerable<Job> runningJobs;
        if ((runningJobs = _allRunningJobs.Where(job => job.IsRunning)).Any()) {
            string runningJobNames = runningJobs.Select(job => job.JobName).Concatenate();
            D.Warn("{0} has found {1} Jobs that continue to run when Game not running. They are: {2}.", GetType().Name, runningJobs.Count(), runningJobNames);
        }
        D.Log("{0}: {1} Jobs killed. Jobs: {2}.", GetType().Name, _jobsToKillWhenGameNoLongerRunning.Count, _jobsToKillWhenGameNoLongerRunning.Select(job => job.JobName).Concatenate());
        // Note: when jobs are killed, they remove themselves from the lists the next frame. I see no reason to wait
        RemoveCompletedJobs();
    }

    /// <summary>
    /// Changes the pause state of all pausable Jobs.
    /// </summary>
    private void ChangePauseStateOfJobs(bool toPause) {
        //D.Log("{0}.ChangePauseStateOfJobs({1}) called in Frame {2}.", GetType().Name, toPause, Time.frameCount);
        _pausableJobs.ForAll(job => job.IsPaused = toPause);
    }

    /// <summary>
    /// Adds a running job to the lists of tracked jobs.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="isPausable">if set to <c>true</c> the job is pausable and should have its 
    /// paused state changed when the game changes its paused state.</param>
    /// <param name="isAutoKillable">if set to <c>true</c> this job should be killed when the game instance
    /// is no longer running, aka GameMgr.IsRunning changed to false.</param>
    private void AddRunningJob(Job job, bool isPausable, bool isAutoKillable = true) {
        D.Assert(job.IsRunning);
        _allRunningJobs.Add(job);
        if (isAutoKillable) {
            _jobsToKillWhenGameNoLongerRunning.Add(job);
        }
        if (isPausable) {
            _pausableJobs.Add(job);
        }
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
        Profiler.BeginSample("Job creation allocation");
        var job = new Job(coroutine, jobName, toStart: true, jobCompleted: (jobWasKilled) => {
            if (jobCompleted != null) {
                jobCompleted(jobWasKilled);
            }
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable: false, isAutoKillable: false);
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
        Profiler.BeginSample("Job creation allocation");
        var job = new Job(coroutine, jobName, toStart: true, jobCompleted: (jobWasKilled) => {
            if (jobCompleted != null) {
                jobCompleted(jobWasKilled);
            }
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
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
        Profiler.BeginSample("Job creation allocation");
        var job = new Job(WaitForSeconds(seconds), jobName, toStart: true, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            RemoveCompletedJobs();
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
        Profiler.BeginSample("Job creation allocation");
        var job = new Job(RepeatingWaitForSeconds(initialWait, recurringWait, waitMilestone), jobName, toStart: true, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled);
            RemoveCompletedJobs();
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
        Profiler.BeginSample("Job creation allocation");
        WaitForHours waitYieldInstruction = new WaitForHours(hours);
        var job = new Job(WaitForHours(waitYieldInstruction), jobName, waitYieldInstruction, toStart: true, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
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
        Profiler.BeginSample("Job creation allocation");
        WaitForHours waitYieldInstruction = new WaitForHours(durationReference);
        var job = new Job(RepeatingWaitForHours(waitYieldInstruction, waitMilestone), jobName, waitYieldInstruction, toStart: true, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
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
        Profiler.BeginSample("Job creation allocation");
        WaitForHours waitYieldInstruction = new WaitForHours(duration);
        var job = new Job(RepeatingWaitForHours(waitYieldInstruction, waitMilestone), jobName, waitYieldInstruction, toStart: true, jobCompleted: (jobWasKilled) => {
            D.Assert(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
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
        Profiler.BeginSample("Job creation allocation");
        WaitForDate waitYieldInstruction = new WaitForDate(futureDate);
        var job = new Job(WaitForDate(waitYieldInstruction), jobName, waitYieldInstruction, toStart: true, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable: true);
        if (_isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
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
        Profiler.BeginSample("Job creation allocation");
        MyWaitWhile waitWhileYI = new MyWaitWhile(waitWhileCondition);
        var job = new Job(WaitWhileCondition(waitWhileYI), jobName, waitWhileYI, toStart: true, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
            job.IsPaused = true;
        }
        return job;
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
        D.Assert(!particleSystem.loop);
        if (includeChildren && particleSystem.transform.childCount > Constants.Zero) {
            var childParticleSystems = particleSystem.gameObject.GetComponentsInChildren<ParticleSystem>().Except(particleSystem);
            childParticleSystems.ForAll(cps => D.Assert(!cps.loop));
        }
        Profiler.BeginSample("Job creation allocation");
        var job = new Job(WaitForParticleSystemCompletion(particleSystem, includeChildren), jobName, toStart: true, jobCompleted: (jobWasKilled) => {
            waitFinished(jobWasKilled);
            RemoveCompletedJobs();
        });
        Profiler.EndSample();
        AddRunningJob(job, isPausable);
        if (isPausable && _isGamePaused) {
            D.Log("{0} has paused {1} immediately after starting it.", GetType().Name, jobName);
            job.IsPaused = true;
        }
        return job;
    }

    private void ValidateGameIsRunning(string jobName) {
        D.Assert(IsGameRunning, jobName);
    }

    [Obsolete]  // 8.15.16 Keeping Jobs from starting when paused is no longer reqd as most JobStart methods here check for the game
    // being paused if they are pausable
    private void ValidateGameNotPaused(string jobName) {
        D.Assert(!_isGamePaused, jobName);
    }

    #region Coroutines

    private IEnumerator WaitForSeconds(float seconds) {
        yield return Yielders.GetWaitForSeconds(seconds);
    }

    private IEnumerator RepeatingWaitForSeconds(float initialWait, float recurringWait, Action waitMilestone) {
        yield return Yielders.GetWaitForSeconds(initialWait);
        waitMilestone();
        while (true) {
            yield return Yielders.GetWaitForSeconds(recurringWait);
            waitMilestone();
        }
    }

    private IEnumerator WaitForHours(WaitForHours waitYI) {
        yield return waitYI;
    }

    private IEnumerator RepeatingWaitForHours(WaitForHours waitYI, Action waitMilestone) {
        while (true) {
            yield return waitYI;
            waitMilestone();
        }
    }

    private IEnumerator WaitForDate(WaitForDate waitYI) {
        yield return waitYI;
    }

    private IEnumerator WaitWhileCondition(MyWaitWhile waitWhileYI) {
        yield return waitWhileYI;
    }
    private IEnumerator WaitWhileCondition(Func<bool> waitWhileCondition) {
        yield return new WaitWhile(waitWhileCondition);
    }

    private IEnumerator WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren) {
        while (particleSystem != null && particleSystem.IsAlive(includeChildren)) {
            yield return null;
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        Unsubscribe();
        if (_allRunningJobs.Any()) {
            // GameMgr.IsRunning should be set to false when the application is quiting so only a few, if any, Jobs should be remaining
            _allRunningJobs.ForAll(job => job.Kill());
        }
        Job.JobRunner = null;
        References.JobManager = null;
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


