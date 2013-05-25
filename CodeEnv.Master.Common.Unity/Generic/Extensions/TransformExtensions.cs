// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TransformExtensions.cs
// Unity Transform extensions for more convenient syntax.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class TransformExtensions {

        public static void SetX(this Transform transform, float x) {
            Vector3 newPosition = new Vector3(x, transform.position.y, transform.position.z);
            transform.position = newPosition;
        }

        public static void SetY(this Transform transform, float y) {
            Vector3 newPosition = new Vector3(transform.position.x, y, transform.position.z);
            transform.position = newPosition;
        }

        public static void SetZ(this Transform transform, float z) {
            Vector3 newPosition = new Vector3(transform.position.x, transform.position.y, z);
            transform.position = newPosition;
        }

        /// <summary>
        /// Finds the first child of this Transform that also is a MonoBehaviour of Type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">The t.</param>
        /// <returns>The child t or null if no child of Type T exists.</returns>
        public static Transform FindChild<T>(this Transform transform) where T : MonoBehaviour {
            T mono = transform.GetComponentInChildren<T>();
            if (mono == null || mono.transform == transform) {
                return null;
            }
            return mono.transform;
        }

        /// <summary>
        /// Gets an interface of type I found in the Transform's peer components.
        /// </summary>
        /// <typeparam name="I">The Interface type.</typeparam>
        /// <param name="t">The Transform</param>
        /// <returns>The class of type I found, if any. Can be null.</returns>
        public static I GetInterface<I>(this Transform t) where I : class {
            return t.GetComponent(typeof(I)) as I;
        }

        /// <summary>
        /// Gets the first interface of type I found in the Transform's peer components or the gameobject's children.
        /// </summary>
        /// <typeparam name="I">The Interface type.</typeparam>
        /// <param name="t">The game object</param>
        /// <returns>The class of type I found, if any. Can be null.</returns>
        public static I GetInterfaceInChildren<I>(this Transform t) where I : class {
            return t.GetComponentInChildren(typeof(I)) as I;
        }

    }
}


