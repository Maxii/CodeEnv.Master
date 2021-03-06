﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGenericSingleton.cs
// Abstract Generic Singleton Base Class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Abstract Generic Singleton Base Class. Derived classes must implement
    /// a Private Constructor and call Initialize() from it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AGenericSingleton<T> : APropertyChangeTracking where T : class {

        public virtual string DebugName { get { return typeof(T).Name; } }

        #region Singleton Pattern

        private static T _instance;

        /// <summary>Returns the singleton instance of the derived class.</summary>
        public static T Instance {
            get {
                //System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
                //D.Log("{0}.{1}() method called.".Inject(typeof(T).Name, stackFrame.GetMethod().Name));

                if (_instance == null) {
                    _instance = (T)Activator.CreateInstance(typeof(T), true);
                }
                return _instance;
            }
        }

        #endregion

        ///<summary>
        /// IMPORTANT: This must be called from the PRIVATE constructor in the derived class.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Clients should call this if and when they Dispose of T. 
        /// WARNING: T should be disposed of when it holds any reference to an object that does not persist across scenes.
        /// </summary>
        protected void CallOnDispose() {
            //D.Log("{0}.CallOnDispose() called.", GetType().Name);
            _instance = null;
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }

}

