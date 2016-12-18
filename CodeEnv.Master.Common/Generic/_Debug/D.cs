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

// 11.19.16 Defining these in this file does not control compilation. The key to compilation
// is whether these are defined from the perspective of the calling file (either in the file or in the project build config)
////#define DEBUG_WARN    // Show Warnings, Errors and Asserts only
////#define DEBUG_ERROR   // Show Errors and Asserts only
////#define DEBUG_LOG

#if DEBUG_ERROR
#define UNITY_ASSERTIONS
#endif

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Conditional Debug class for pre-compiled classes replacing UnityEngine.Debug.
    /// </summary>
    /// <remarks> Setting the conditional to the platform of choice will only compile the method for that platform.
    /// Alternatively, use the #defines at the top of this file</remarks>
    public static class D {

        private const string BoldFormat = "<b>{0}</b>";

        #region Logging

        /// <summary>
        /// Sends the specified formatted message to the Unity.Debug log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Log(string formattedMsg, params object[] paramList) {
            Debug.LogFormat(formattedMsg, paramList);
        }

        /// <summary>
        /// Sends the specified formatted message to the Unity.Debug log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void LogBold(string formattedMsg, params object[] paramList) {
            Debug.LogFormat(BoldFormat.Inject(formattedMsg), paramList);
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
            Debug.LogFormat(context, BoldFormat.Inject(formattedMsg), paramList);
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug log.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        //[System.Obsolete("Use external condition check then Log() instead.")]
        public static void Log(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogFormat(formattedMsg, paramList);
            }
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug log.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        //[System.Obsolete("Use external condition check then LogBold() instead.")]
        public static void LogBold(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogFormat(BoldFormat.Inject(formattedMsg), paramList);
            }
        }

        /// <summary>
        /// If the condition is true, sends the specified msg to the Unity.Debug log.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        //[System.Obsolete("Use external condition check then LogContext() instead.")]
        public static void LogContext(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogFormat(context, formattedMsg, paramList);
            }
        }

        /// <summary>
        /// If the condition is true, sends the specified msg to the Unity.Debug log.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Obsolete("Use external condition check then LogContextBold() instead.")]
        public static void LogContextBold(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogFormat(context, BoldFormat.Inject(formattedMsg), paramList);
            }
        }


        #region Obsolete

        /// <summary>
        /// Sends the specified message object (typically a composite message string) to the Unity.Debug log.
        /// </summary>
        /// <param name="obj">A System.Object to use ToString() or a composite message format string.</param>
        /// <param name="paramList">The parameters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        //[System.Obsolete("Use Log(formattedMsg, paramList instead.")]
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
        /// <param name="paramList">The parameters to insert into the composite message format string.</param>
        [System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Obsolete("Use LogBold(formattedMsg, paramList instead.")]
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

        #endregion

        #endregion

        #region Warnings

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug warning log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Warn(string formattedMsg, params object[] paramList) {
            Debug.LogWarningFormat(formattedMsg, paramList);
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

        #region Obsolete

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the composite message format string to the Unity.Debug warning log.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Obsolete("Warning: Allocates heap memory from formattedMsg and paramList. Use condition and D.Warn(formattedMsg, paramList) instead.")]
        public static void Warn(bool condition, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogWarningFormat(formattedMsg, paramList);
            }
        }

        /// <summary>
        /// If <c>condition</c> is <c>true</c>, sends the specified message to the Unity.Debug warning log
        /// with a ping connection to <c>context</c>.
        /// <remarks>Warning: Allocates memory on the heap whether condition is satisfied or not.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The object that sent the message.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Obsolete("Warning: Allocates heap memory from formattedMsg and paramList. Use condition and D.WarnContext(context, formattedMsg, paramList) instead.")]
        public static void WarnContext(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (condition) {
                Debug.LogWarningFormat(context, formattedMsg, paramList);
            }
        }

        #endregion

        #endregion

        #region Errors

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug Error log.
        /// </summary>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Error(string formattedMsg, params object[] paramList) {
            Debug.LogErrorFormat(formattedMsg, paramList);
        }

        /// <summary>
        /// Sends the composite message format string to the Unity.Debug Error log and pings the context object.
        /// </summary>
        /// <param name="context">The context object.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameters to insert into the composite message string.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void ErrorContext(Object context, string formattedMsg, params object[] paramList) {
            Debug.LogErrorFormat(context, formattedMsg, paramList);
        }

        #endregion

        #region Asserts

        #region Context Asserts

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context object.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, Object context) {
            Debug.Assert(condition, context);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// </summary>
        /// <param name="condition">if set to <c>true</c> [condition].</param>
        /// <param name="context">The context object.</param>
        /// <param name="msg">The text message.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, Object context, string msg) {
            Debug.Assert(condition, msg, context);
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
        [System.Obsolete("Warning: Allocates heap memory from formattedMsg and paramList. Use condition and D.ErrorContext(context, formattedMsg, paramList) instead.")]
        public static void Assert(bool condition, Object context, string formattedMsg, params object[] paramList) {
            if (!condition) {
                Debug.LogErrorFormat(context, "Assert failed! {0}".Inject(formattedMsg), paramList);
                //Debug.Break();  // if in an infinite loop, error log never reaches console so console's ErrorPause doesn't engage
                throw new UnityException();
            }
        }

        #endregion

        #region Condition Asserts

        /// <summary>
        /// Tests the specified condition and Logs an error if it fails.
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition) {
            UnityEngine.Assertions.Assert.IsTrue(condition);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// </remarks>
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="msg">The text message.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void Assert(bool condition, string msg) {
            UnityEngine.Assertions.Assert.IsTrue(condition, msg);
        }

        /// <summary>
        /// Tests the specified condition and logs the provided message as an Error if it fails.
        /// <remarks>This version makes a memory allocation.</remarks>
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Obsolete("Warning: Allocates heap memory from formattedMsg and paramList. Use condition and D.Error(formattedMsg, paramList) instead.")]
        public static void Assert(bool condition, string formattedMsg, params object[] paramList) {
            // UNCLEAR: While Debug.AssertFormat() makes no memory allocation, calling it via this method does
            Debug.AssertFormat(condition, formattedMsg, paramList);
        }

        #endregion

        #region Equality Asserts

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertApproxEqual(float expected, float actual) {
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected, actual);  // 11.19.16 Too precise at 0.00001F tolerance
            UnityEngine.Assertions.Assert.AreEqual(expected, actual, string.Empty, FloatEqualityComparer.Default);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertApproxEqual(float expected, float actual, string msg) {
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected, actual, msg);  // 11.19.16 Too precise at 0.00001F tolerance
            UnityEngine.Assertions.Assert.AreEqual(expected, actual, msg, FloatEqualityComparer.Default);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotApproxEqual(float expected, float actual) {
            //UnityEngine.Assertions.Assert.AreNotApproximatelyEqual(expected, actual);  // 11.19.16 Too precise at 0.00001F tolerance
            UnityEngine.Assertions.Assert.AreNotEqual(expected, actual, string.Empty, FloatEqualityComparer.Default);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotApproxEqual(float expected, float actual, string msg) {
            //UnityEngine.Assertions.Assert.AreNotApproximatelyEqual(expected, actual, msg);  // 11.19.16 Too precise at 0.00001F tolerance
            UnityEngine.Assertions.Assert.AreNotEqual(expected, actual, msg, FloatEqualityComparer.Default);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertEqual<T>(T expected, T actual) {
            UnityEngine.Assertions.Assert.AreEqual<T>(expected, actual);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertEqual<T>(T expected, T actual, string msg) {
            UnityEngine.Assertions.Assert.AreEqual<T>(expected, actual, msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotEqual<T>(T expected, T actual) {
            UnityEngine.Assertions.Assert.AreNotEqual<T>(expected, actual);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotEqual<T>(T expected, T actual, string msg) {
            UnityEngine.Assertions.Assert.AreNotEqual<T>(expected, actual, msg);
        }

        #endregion

        #region Null Asserts

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNull<T>(T value) where T : class {
            UnityEngine.Assertions.Assert.IsNull<T>(value);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNull<T>(T value, string msg) where T : class {
            UnityEngine.Assertions.Assert.IsNull<T>(value, msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNull(Object value) {
            UnityEngine.Assertions.Assert.IsNull(value);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNull(Object value, string msg) {
            UnityEngine.Assertions.Assert.IsNull(value, msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotNull<T>(T value) where T : class {
            UnityEngine.Assertions.Assert.IsNotNull<T>(value);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotNull<T>(T value, string msg) where T : class {
            UnityEngine.Assertions.Assert.IsNotNull<T>(value, msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotNull(Object value) {
            UnityEngine.Assertions.Assert.IsNotNull(value);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotNull(Object value, string msg) {
            UnityEngine.Assertions.Assert.IsNotNull(value, msg);
        }

        #endregion

        #region Default Asserts

        #region Custom struct Default Asserts

        // AssertDefault and AssertNotDefault for custom structures that implement IEquatable<T>.
        // Requires IEquatable<T> constraint to avoid boxing from Object.Equals which causes allocations on the heap

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotDefault<T>(T value) where T : struct, System.IEquatable<T> {
            UnityEngine.Assertions.Assert.IsFalse(value.Equals(default(T)));
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotDefault<T>(T value, string msg) where T : struct, System.IEquatable<T> {
            UnityEngine.Assertions.Assert.IsFalse(value.Equals(default(T)), msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertDefault<T>(T value) where T : struct, System.IEquatable<T> {
            UnityEngine.Assertions.Assert.IsTrue(value.Equals(default(T)));
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertDefault<T>(T value, string msg) where T : struct, System.IEquatable<T> {
            UnityEngine.Assertions.Assert.IsTrue(value.Equals(default(T)), msg);
        }

        #endregion

        #region Enum Default Asserts

        // AssertDefault and AssertNotDefault for Enums (and ints).
        // Requires enums be cast to int to avoid boxing from Object.Equals which causes allocations on the heap

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotDefault(int enumValue) {
            UnityEngine.Assertions.Assert.IsFalse(enumValue == Constants.Zero);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertNotDefault(int enumValue, string msg) {
            UnityEngine.Assertions.Assert.IsFalse(enumValue == Constants.Zero, msg);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertDefault(int enumValue) {
            UnityEngine.Assertions.Assert.IsTrue(enumValue == Constants.Zero);
        }

        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertDefault(int enumValue, string msg) {
            UnityEngine.Assertions.Assert.IsTrue(enumValue == Constants.Zero, msg);
        }

        #endregion

        #endregion

        #region Exception Asserts

        /// <summary>
        /// Tests the specified condition and if it fails raises an exception that is guaranteed to
        /// stop the editor, even if in an infinite loop.
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertException(bool condition) {
            UnityEngine.Assertions.Assert.raiseExceptions = true;
            UnityEngine.Assertions.Assert.IsTrue(condition);
            UnityEngine.Assertions.Assert.raiseExceptions = false;
        }

        /// <summary>
        /// Tests the specified condition and if it fails raises an exception that is guaranteed to
        /// stop the editor, even if in an infinite loop.
        /// <remarks>This version makes no memory allocation due to the magic of Unity's Assert.
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="msg">The text message.</param>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        public static void AssertException(bool condition, string msg) {
            UnityEngine.Assertions.Assert.raiseExceptions = true;
            UnityEngine.Assertions.Assert.IsTrue(condition, msg);
            UnityEngine.Assertions.Assert.raiseExceptions = false;
        }

        /// <summary>
        /// Tests the specified condition and if it fails raises an exception that is guaranteed to
        /// stop the editor, even if in an infinite loop.
        /// <remarks>Warning: object null conditional tests generate nullReferenceExceptions if object is used in paramList
        /// </remarks>
        /// </summary>
        /// <param name="condition">if <c>true</c> the assert passes.</param>
        /// <param name="formattedMsg">The formatted text message.</param>
        /// <param name="paramList">The parameter list.</param>
        /// <exception cref="UnityEngine.UnityException">Assert failed!  + format.Inject(paramList)</exception>
        [System.Diagnostics.Conditional("DEBUG_ERROR"), System.Diagnostics.Conditional("DEBUG_WARN"), System.Diagnostics.Conditional("DEBUG_LOG")]
        [System.Obsolete("Warning: Allocates heap memory from formattedMsg and paramList.")]
        public static void AssertException(bool condition, string formattedMsg, params object[] paramList) {
            if (!condition) {
                // Debug.Break() pauses editor play but only at the end of the frame. If infinite loop, no end of frame ever occurs
                throw new UnityException("Assert failed! {0}".Inject(formattedMsg).Inject(paramList));
            }
        }

        #endregion

        #endregion

    }
}

