// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUGWARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Abstract Generic Singleton Base Class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AGenericSingleton<T> where T : class {

        #region Singleton Pattern

        private static T _instance;

        /// <summary>Returns the singleton instance of the derived class.</summary>
        public static T Instance {
            get {
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

    }

}

