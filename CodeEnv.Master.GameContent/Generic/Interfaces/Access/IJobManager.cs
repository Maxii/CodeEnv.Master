// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IJobManager.cs
// Interface to the MonoBehaviour that creates, launches and manages the lifecycle of Jobs.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using Common;
    using UnityEngine;

    /// <summary>
    /// Interface to the MonoBehaviour that creates, launches and manages the lifecycle of Jobs.
    /// </summary>
    public interface IJobManager {

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
        Job StartNonGameplayJob(IEnumerator coroutine, string jobName, Action<bool> jobCompleted = null);

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
        Job StartGameplayJob(IEnumerator coroutine, string jobName, bool isPausable, Action<bool> jobCompleted = null);

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
        Job WaitForGameplaySeconds(float seconds, string jobName, Action<bool> waitFinished);

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
        Job RecurringWaitForGameplaySeconds(float initialWait, float recurringWait, string jobName, Action waitMilestone);

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
        Job WaitForHours(float hours, string jobName, Action<bool> waitFinished);

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
        Job __WaitForHours(float hours, string jobName, Reference<bool> isInterveningJobRef, Action<bool> waitFinished);

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
        Job RecurringWaitForHours(Reference<GameTimeDuration> durationReference, string jobName, Action waitMilestone);

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
        Job RecurringWaitForHours(GameTimeDuration duration, string jobName, Action waitMilestone);

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
        Job RecurringWaitForHours(float hours, string jobName, Action waitMilestone);

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
        Job WaitForDate(GameDate futureDate, string jobName, Action<bool> waitFinished);

        /// <summary>
        /// Waits until waitWhileCondition returns <c>false</c> during GamePlay, then executes the provided delegate.
        /// </summary>
        /// <param name="waitWhileCondition">The <c>true</c> condition that continues the wait.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="isPausable">if set to <c>true</c> the paused state of the job tracks that of the game.</param>
        /// <param name="waitFinished">The delegate to execute when the wait is finished.
        /// The signature is waitFinished(jobWasKilled).</param>
        /// <returns>A reference to the Job so it can be killed before it finishes, if needed.</returns>
        Job WaitWhile(Func<bool> waitWhileCondition, string jobName, bool isPausable, Action<bool> waitFinished);

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
        Job WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren, string jobName, bool isPausable, Action<bool> waitFinished);

    }
}

