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
    /// <remarks> Setting the conditional to the platform of choice will only compile the method for that platform.
    /// Alternatively, use the #defines at the top of this file</remarks>
    public static class D {

        private const string BoldFormat = "<b>{0}</b>";

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug log.
        /// </summary>
        /// <param name="obj">A System.Object to use ToString() or a composite message format string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Log(object obj, params object[] paramList) {
            string objText = obj as string;
            if (objText != null) {
                Debug.LogFormat(objText, paramList);
            }
            else {
                Debug.Log(obj);
            }
        }

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug log.
        /// </summary>
        /// <param name="obj">A System.Object to use ToString() or a composite message format string.</param>
        /// <param name="paramList">The paramaters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogBold(object obj, params object[] paramList) {
            string objText = obj as string;
            if (objText != null) {
                string boldObjText = BoldFormat.Inject(objText);
                Debug.LogFormat(boldObjText, paramList);
            }
            else {
                Debug.Log(obj);
            }
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug log.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Log(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                Log(formattedMsg, paramList);
            }
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug log.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogBold(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                LogBold(formattedMsg, paramList);
            }
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogContext(Object context, string formattedMsg, params object[] paramList) {
            Debug.LogFormat(context, formattedMsg, paramList);
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogContextBold(Object context, string formattedMsg, params object[] paramList) {
            string boldMsg = BoldFormat.Inject(formattedMsg);
            Debug.LogFormat(context, boldMsg, paramList);
        }


        /// <summary>
        /// If the condition is true, sends the specified msg to the Unity.Debug log.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogContext(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                LogContext(context, formattedMsg, paramList);
            }
        }

        /// <summary>
        /// If the condition is true, sends the specified msg to the Unity.Debug log.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogContextBold(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                LogContextBold(context, formattedMsg, paramList);
            }
        }

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug warning log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Warn(string formattedMsg, params object[] paramList) {
            Debug.LogWarningFormat(formattedMsg, paramList);
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug warning log.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        public static void Warn(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogWarningFormat(formattedMsg, paramList);
            }
        }

        /// <summary>
        /// Sends the specified message to the Unity.Debug warning log with a ping connection
        /// to the object that sent it.
        /// </summary>
        /// <param name="context">The object that sent the message.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void WarnContext(Object context, string formattedMsg, params object[] paramList) {
            Debug.LogWarningFormat(context, formattedMsg, paramList);
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the specified message to the Unity.Debug warning log
        /// with a ping connection to <c>context</c>.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The object that sent the message.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void WarnContext(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogWarningFormat(context, formattedMsg, paramList);
            }
        }

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug Error log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Error(string formattedMsg, params object[] paramList) {
            Debug.LogErrorFormat(formattedMsg, paramList);
        }

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug Error log and pings the context object.
        /// </summary>
        /// <param name="context">The context object.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The paramaters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void ErrorContext(Object context, string formattedMsg, params object[] paramList) {
            Debug.LogErrorFormat(context, formattedMsg, paramList);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context object.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (!condition) {
                Debug.LogErrorFormat(context, "Assert failed! " + formattedMsg, paramList);
                //Debug.Break();  // if in an infinite loop, error log never reaches console so console's ErrorPause doesn't engage
                throw new UnityException();
            }
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, string format, params object[] paramList) {
            if (!condition) {
                Debug.LogErrorFormat("Assert failed! " + format, paramList);
            }
        }

        /// <summary>
        /// Tests the specified condition and Logs an error if it fails.
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition) {
            if (!condition) {
                Debug.LogError("Assert failed!");
            }
        }

        /// <summary>
        /// Tests the specified condition and if it fails raises an exception that is gauranteed to
        /// stop the editor, even if in an infinite loop.
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <exception cref="UnityEngine.UnityException">Assert failed!  + format.Inject(paramList)</exception>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertException(bool condition) {
            if (!condition) {
                // Debug.Break() pauses editor play but only at the end of the frame. If infinite loop, no end of frame ever occurs
                throw new UnityException("Assert failed!");
            }
        }

        /// <summary>
        /// Tests the specified condition and if it fails raises an exception that is gauranteed to
        /// stop the editor, even if in an infinite loop.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        /// <exception cref="UnityEngine.UnityException">Assert failed!  + format.Inject(paramList)</exception>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertException(bool condition, string formattedMsg, params object[] paramList) {
            if (!condition) {
                // Debug.Break() pauses editor play but only at the end of the frame. If infinite loop, no end of frame ever occurs
                throw new UnityException("Assert failed! " + formattedMsg.Inject(paramList));
            }
        }

    }
}

