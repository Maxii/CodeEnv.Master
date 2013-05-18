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

//#define DEBUG_LEVEL_LOG     // Show all messages
#define DEBUG_LEVEL_WARN    // Show Warnings, Errors and Asserts only
#define DEBUG_LEVEL_ERROR   // Show Errors and Asserts only
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
        /// Sends the specified message to the Unity.Debug log.
        /// </summary>
        /// <remarks>Use this when including MyObject.ToString() which contains {} that string.Format() doesn't like.</remarks>
        /// <param name="message">The string message.</param>
        //[System.Diagnostics.Conditional("DEBUG_LEVEL_LOG")]
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
        //[System.Diagnostics.Conditional("DEBUG_LEVEL_LOG")]
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
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug
        /// warning log.
        /// </summary>
        /// <param name="message">The message object, typically a composite message string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_WARN")]
        public static void Warn(object format, params object[] paramList) {
            if (format is string) {
                Debug.LogWarning(string.Format(format as string, paramList));
            }
            else {
                Debug.LogWarning(format);
            }
        }

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug
        /// Error log.
        /// </summary>
        /// <param name="message">The message object, typically a composite message string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_WARN")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_ERROR")]
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
        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_WARN")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_ERROR")]
        public static void Assert(bool condition) {
            Assert(condition, string.Empty, true);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails. Does not pause
        /// on failure.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="assertString">The message to log as an Error on failure.</param>
        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_WARN")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_ERROR")]
        public static void Assert(bool condition, string assertString) {
            Assert(condition, assertString, false);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails, with an option for
        /// the Editor to pause on failure.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="assertString">The message to log as an Error on failure.</param>
        /// <param name="pauseOnFail">if set to <c>true</c>, the UnityEditor will [pause on fail].</param>
        //[System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_WARN")]
        [System.Diagnostics.Conditional("DEBUG_LEVEL_ERROR")]
        public static void Assert(bool condition, string assertString, bool pauseOnFail) {
            if (!condition) {
                Debug.LogError("Assert failed! " + assertString);
                if (pauseOnFail) {
                    Debug.Break();
                }
            }
        }
    }
}

