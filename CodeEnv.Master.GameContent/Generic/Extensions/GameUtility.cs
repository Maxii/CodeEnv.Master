// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameUtility.cs
// Collection of tools and utilities specific to the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;
    using System.Diagnostics;
    using System.Collections;

    /// <summary>
    /// Collection of tools and utilities specific to the game.
    /// </summary>
    public static class GameUtility {

        /// <summary>
        /// Derives the enum value of type E from within the provided name. Case insensitive.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static E DeriveEnumFromName<E>(string name) where E : struct {
            D.Assert(typeof(E).IsEnum, "{0} must be an enumerated type.".Inject(typeof(E).Name));
            Arguments.ValidateForContent(name);
            return Enums<E>.GetValues().Single(e => name.Trim().ToLower().Contains(e.ToString().ToLower()));
        }

        /// <summary>
        /// Validates the provided GameDate is within the designated range, inclusive.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <param name="earliest">The earliest acceptable date.</param>
        /// <param name="latest">The latest acceptable date.</param>
        /// <exception cref="IllegalArgumentException"></exception>
        public static void ValidateForRange(GameDate date, GameDate earliest, GameDate latest) {
            if (latest <= earliest || date < earliest || date > latest) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(date, earliest, latest, callingMethodName));
            }
        }

        /// <summary>
        /// Waits for the designated GameDate, then executes the provided delegate.
        /// Usage:
        /// WaitForDate(futureDate, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="futureDate">The future date.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForDate(GameDate futureDate, Action<bool> onWaitFinished) {
            return new WaitJob(WaitForDate(futureDate), toStart: true, onJobComplete: onWaitFinished);
        }

        /// <summary>
        /// Waits for the designated GameDate. Usage:
        /// new Job(GameUtility.WaitForDate(futureDate), toStart: true, onJobCompletion: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: the code in this location will execute immediately after the Job starts
        /// </summary>
        /// <param name="futureDate">The date.</param>
        /// <returns></returns>
        private static IEnumerator WaitForDate(GameDate futureDate) {
            D.Assert(futureDate > GameTime.CurrentDate);
            while (futureDate > GameTime.CurrentDate) {
                yield return null;
            }
        }

    }
}

