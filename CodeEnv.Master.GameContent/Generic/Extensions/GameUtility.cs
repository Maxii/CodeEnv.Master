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
    using UnityEngine;

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
        /// Changes a GameColor value into a 6 digit Hex RGB string, ignoring the alpha channel.
        /// </summary>
        /// <param name="color">The GameColor.</param>
        /// <returns></returns>
        public static string ColorToHex(GameColor color) {
            return ColorToHex(color.ToUnityColor());
        }

        /// <summary>
        /// Changes a Unity Color value into a 6 digit Hex RGB string, ignoring the alpha channel.
        /// <remarks>Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.</remarks>
        /// </summary>
        /// <param name="color">The Unity Color.</param>
        /// <returns></returns>
        public static string ColorToHex(Color32 color) {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        /// <summary>
        /// Changes the first 6 digits of a hex value string into a Unity Color value.
        /// <remarks>Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.</remarks>
        /// </summary>
        /// <param name="hex">The hex value string.</param>
        /// <returns></returns>
        public static Color HexToColor(string hex) {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        /// <summary>
        /// Waits the designated number of hours, then executes the provided delegate.
        /// Automatically accounts for Pausing and GameSpeed changes.
        /// Usage:
        /// WaitForHours(hours, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="hours">The hours to wait.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForHours(float hours, Action<bool> onWaitFinished) {
            return new WaitJob(WaitForHours(hours), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
            });
        }

        private static IEnumerator WaitForHours(float hours) {
            var gameTime = GameTime.Instance;
            var allowedSeconds = hours / gameTime.GameSpeedAdjustedHoursPerSecond;
            float elapsedSeconds = Constants.ZeroF;
            while (elapsedSeconds < allowedSeconds) {
                elapsedSeconds += gameTime.DeltaTimeOrPaused;
                allowedSeconds = hours / gameTime.GameSpeedAdjustedHoursPerSecond;
                yield return null;
            }
        }
        //private static IEnumerator WaitForHours(float hours) {
        //    var gameTime = GameTime.Instance;
        //    var allowedSeconds = hours / GameTime.HoursPerSecond;
        //    float elapsedSeconds = Constants.ZeroF;
        //    while (elapsedSeconds < allowedSeconds) {
        //        elapsedSeconds += gameTime.GameSpeedAdjustedDeltaTimeOrPaused;
        //        yield return null;
        //    }
        //}

        /// <summary>
        /// Waits the designated number of hours, then executes the provided delegate.
        /// As this method converts hours to a date, it automatically adjusts for Pauses and
        /// GameSpeed changes.
        /// Usage:
        /// WaitForHours(hours, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="hours">The hours to wait. A minimum of 1 but max is unlimited.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForHoursFromCurrentDate(int hours, Action<bool> onWaitFinished) {
            D.Assert(hours >= Constants.One);
            GameDate futureDate = new GameDate(new GameTimeDuration(hours));
            return WaitForDate(futureDate, onWaitFinished);
        }

        /// <summary>
        /// Waits for the designated GameDate, then executes the provided delegate. As this method 
        /// uses a date, it automatically adjusts for Pauses and GameSpeed changes.

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
        /// WARNING: the code in this location will execute immediately after the Job starts.
        /// </summary>
        /// <param name="futureDate">The date.</param>
        /// <returns></returns>
        private static IEnumerator WaitForDate(GameDate futureDate) {
            if (futureDate <= GameTime.Instance.CurrentDate) {
                // IMPROVE current date can exceed a future date of hours when game speed high?
                D.Warn("Future date {0} should be > Current date {1}.", futureDate, GameTime.Instance.CurrentDate);
            }
            while (futureDate > GameTime.Instance.CurrentDate) {
                yield return null;
            }
        }

        /// <summary>
        /// Waits while a condition exists, then executes the onWaitFinished delegate.
        /// </summary>
        /// <param name="waitWhileCondition">The condition that continues the wait.</param>
        /// <param name="onWaitFinished">The delegate to execute when the wait is finished.
        /// The signature is onWaitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static WaitJob WaitWhileCondition(Reference<bool> waitWhileCondition, Action<bool> onWaitFinished) {
            var waitJob = new WaitJob(WaitWhileCondition(waitWhileCondition), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
            });
            return waitJob;
        }

        private static IEnumerator WaitWhileCondition(Reference<bool> waitWhileCondition) {
            while (waitWhileCondition.Value) {
                yield return null;
            }
        }

        /// <summary>
        /// Waits for a particle system to complete, then executes the onWaitFinished delegate.
        /// Warning: If any members of this particle system are set to loop, this method will fail.
        /// </summary>
        /// <param name="particleSystem">The particle system.</param>
        /// <param name="includeChildren">if set to <c>true</c> [include children].</param>
        /// <param name="onWaitFinished">The delegate to execute when the wait is finished. 
        /// The signature is onWaitFinished(jobWasKilled).</param>
        /// <returns></returns>
        public static WaitJob WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren, Action<bool> onWaitFinished) {
            D.Assert(!particleSystem.loop);
            if (includeChildren && particleSystem.transform.childCount > Constants.Zero) {
                var childParticleSystems = particleSystem.gameObject.GetComponentsInChildren<ParticleSystem>().Except(particleSystem);
                childParticleSystems.ForAll(cps => D.Assert(!cps.loop));
            }
            var waitJob = new WaitJob(WaitForParticleSystemCompletion(particleSystem, includeChildren), toStart: true, onJobComplete: (wasKilled) => {
                onWaitFinished(wasKilled);
            });
            return waitJob;
        }

        private static IEnumerator WaitForParticleSystemCompletion(ParticleSystem particleSystem, bool includeChildren) {
            while (particleSystem != null && particleSystem.IsAlive(includeChildren)) {
                yield return null;
            }
        }


    }
}

