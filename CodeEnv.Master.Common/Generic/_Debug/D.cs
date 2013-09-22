// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: D.cs
// Conditional Debug class replacing UnityEngine.Debug.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN    // Show Warnings, Errors and Asserts only
#define DEBUG_ERROR   // Show Errors and Asserts only
#define DEBUG_LOG

namespace CodeEnv.Master.Common {

    using UnityEngine;

    /// <summary>
    /// Conditional Debug class for pre-compiled classes replacing UnityEngine.Debug.
    /// </summary>
    /// <remarks> Setting the conditional to the platform of choice will only compile the method for that platform
    /// Alternatively, use the #defines at the top of this file</remarks>
    public static class D {

        /// <summary>
        /// Sends the specified message to the Unity.Debug log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="context">The object that sent the message.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogContext(string message, Object context) {
            Debug.Log(message, context);
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug log.
        /// </summary>
        /// <remarks>Use this when including MyObject.ToString() which contains {} that string.Format() doesn't like.</remarks>
        /// <param name="message">The string message.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Log(string message) {
            Debug.Log(message);
        }

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug
        /// log.
        /// </summary>
        /// <param name="format">The message object, typically a composite message string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Log(object format, params object[] paramList) {
            if (format is string) {
                Debug.Log(string.Format(format as string, paramList));
            }
            else {
                Debug.Log(format);
            }
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug warning log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="context">The object that sent the message.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void WarnContext(string message, Object context) {
            Debug.LogWarning(message, context);
        }


        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug
        /// warning log.
        /// </summary>
        /// <param name="message">The message object, typically a composite message string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Warn(object format, params object[] paramList) {
            if (format is string) {
                Debug.LogWarning(string.Format(format as string, paramList));
            }
            else {
                Debug.LogWarning(format);
            }
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug error log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="context">The object that sent the message.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void ErrorContext(string message, Object context) {
            Debug.LogError(message, context);
        }

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug
        /// Error log.
        /// </summary>
        /// <param name="message">The message object, typically a composite message string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Error(object format, params object[] paramList) {
            if (format is string) {
                Debug.LogError(string.Format(format as string, paramList));
            }
            else {
                Debug.LogError(format);
            }
        }

        /// <summary>
        /// Tests the specified condition and immediately pauses on failure.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, Object context = null) {
            Assert(condition, string.Empty, true, context);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails. Does not pause
        /// on failure.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="assertString">The message to log as an Error on failure.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, string assertString, Object context = null) {
            Assert(condition, assertString, false, context);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails, with an option for
        /// the Editor to pause on failure.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="assertString">The message to log as an Error on failure.</param>
        /// <param name="pauseOnFail">if set to <c>true</c>, the UnityEditor will [pause on fail].</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, string assertString, bool pauseOnFail, Object context = null) {
            if (!condition) {
                if (context != null) {
                    Debug.LogError("Assert failed! " + assertString, context);
                }
                else {
                    Debug.LogError("Assert failed! " + assertString);
                }
                if (pauseOnFail) {
                    Debug.Break();
                }
            }
        }
    }
}

