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

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEditor;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class GameObjectExtensions {

        /// <summary>
        /// Defensive GameObject.GetComponent<>() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeMonoBehaviourComponent<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponent<T>();
            if (component == null) {
                Debug.LogWarning(ErrorMessages.ComponentNotFound.Inject(typeof(T), go));
            }
            return component;
        }

        /// <summary>
        /// Defensive GameObject.GetComponentInChildren<>() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the MonoBehaviour Component.</param>
        /// <returns>The component of type T or null if not found.</returns>

        public static T GetSafeMonoBehaviourComponentInChildren<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponentInChildren<T>();
            if (component == null) {
                Debug.LogWarning(ErrorMessages.ComponentNotFound.Inject(typeof(T), go));
            }
            return component;
        }

    }
}


