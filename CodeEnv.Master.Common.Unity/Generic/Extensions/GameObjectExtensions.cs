// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameObjectExtensions.cs
// TODO - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LEVEL_LOG
//#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
//

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class GameObjectExtensions {

        /// <summary>
        /// Defensive GameObject.GetComponent&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeMonoBehaviourComponent<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponent<T>();
            if (component == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T), go);
            }
            return component;
        }

        /// <summary>
        /// Defensive GameObject.GetComponents&lt;&gt;() alternative for acquiring MonoBehaviours. Only
        /// active MonoBehaviours are returned. Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the MonoBehaviour Components.</param>
        /// <returns>An array of components of type T. Can be empty.</returns>
        public static T[] GetSafeMonoBehaviourComponents<T>(this GameObject go) where T : MonoBehaviour {
            T[] components = go.GetComponents<T>();
            if (components.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T), go);
            }
            return components;
        }

        /// <summary>
        /// Defensive GameObject.GetComponentInChildren&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the MonoBehaviour Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeMonoBehaviourComponentInChildren<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponentInChildren<T>();
            if (component == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T), go);
            }
            return component;
        }

        /// <summary>
        /// Defensive GameObject.GetComponentsInChildren&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Only active MonoBehaviours are returned. Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the MonoBehaviour Components.</param>
        /// <returns>An array of components of type T. Can be empty.</returns>
        public static T[] GetSafeMonoBehaviourComponentsInChildren<T>(this GameObject go, bool includeInactive = false) where T : MonoBehaviour {
            T[] components = go.GetComponentsInChildren<T>(includeInactive);
            if (components.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T), go);
            }
            return components;
        }

    }
}


