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

namespace CodeEnv.Master.CommonUnity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEditor;
    using CodeEnv.Master.Resources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class GameObjectExtensions {

        /// <summary>
        /// Defensive GameObject.GetComponent alternative. // TODO one for Transform?, ComponentFromChildren?
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The GameObject obstensibly containing the Component.</param>
        /// <returns></returns>
        public static T GetSafeComponent<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponent<T>();
            if (component == null) {
                Debug.LogError(ErrorMessages.ComponentNotFound.Inject(typeof(T), go));
            }
            return component;
        }

    }
}


