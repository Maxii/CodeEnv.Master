// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameObjectExtensions.cs
// Unity GameObject, Transform, Component and Collider extensions
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Unity GameObject, Transform, Component and Collider extensions
    /// </summary>
    public static class GameObjectExtensions {

        #region GetSafeMonoBehaviour... Extensions

        public static T GetSafeMonoBehaviourComponentInParents<T>(this GameObject go) where T : MonoBehaviour {
            Transform parent = go.transform.parent;
            while (parent != null) {
                T component = parent.gameObject.GetComponent<T>();
                if (component != null) {
                    return component;
                }
                parent = parent.gameObject.transform.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            return null;
        }

        /// <summary>
        /// Defensive GameObject.GetComponent&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="t">The GameObject obstensibly containing the Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeMonoBehaviourComponent<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponent<T>();
            if (component == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return component;
        }

        /// <summary>
        /// Defensive GameObject.GetComponents&lt;&gt;() alternative for acquiring MonoBehaviours. Only
        /// active MonoBehaviours are returned. Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="t">The GameObject obstensibly containing the MonoBehaviour Components.</param>
        /// <returns>An array of components of type T. Can be empty.</returns>
        public static T[] GetSafeMonoBehaviourComponents<T>(this GameObject go) where T : MonoBehaviour {
            T[] components = go.GetComponents<T>();
            if (components.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return components;
        }

        /// <summary>
        /// Defensive GameObject.GetComponentInChildren&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="t">The GameObject obstensibly containing the MonoBehaviour Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeMonoBehaviourComponentInChildren<T>(this GameObject go) where T : MonoBehaviour {
            T component = go.GetComponentInChildren<T>();
            if (component == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return component;
        }

        /// <summary>
        /// Defensive GameObject.GetComponentsInChildren&lt;&gt;() alternative for acquiring MonoBehaviours. 
        /// Only active MonoBehaviours are returned. Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="t">The GameObject obstensibly containing the MonoBehaviour Components.</param>
        /// <returns>An array of components of type T. Can be empty.</returns>
        public static T[] GetSafeMonoBehaviourComponentsInChildren<T>(this GameObject go, bool includeInactive = false) where T : MonoBehaviour {
            T[] components = go.GetComponentsInChildren<T>(includeInactive);
            if (components.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return components;
        }

        #endregion


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
        public static Transform FindSafeChild<T>(this Transform t) where T : MonoBehaviour {
            T mono = t.GetComponentInChildren<T>();
            if (mono == null || mono.transform == t) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, t.name);
                return null;
            }
            return mono.transform;
        }

        #region GetInterface... Extensions

        public static Transform GetSafeTransformWithInterfaceInParents<I>(this Transform t) where I : class {
            Transform parent = t.parent;
            while (parent != null) {
                I component = parent.gameObject.GetComponent(typeof(I)) as I;
                if (component != null) {
                    return parent;
                }
                parent = parent.gameObject.transform.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, t.name);
            return null;
        }

        public static I GetSafeInterfaceInParents<I>(this Transform t) where I : class {
            Transform parent = t.parent;
            while (parent != null) {
                I component = parent.GetComponent(typeof(I)) as I;
                if (component != null) {
                    return component;
                }
                parent = parent.gameObject.transform.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, t.name);
            return null;
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

        public static Transform GetTransformWithInterfaceInChildren<I>(this Transform transform) where I : class {
            Transform[] transforms = transform.GetComponentsInChildren<Transform>();
            return transforms.First<Transform>(t => t.GetInterface<I>() != null);
        }

        public static Transform[] GetTransformsWithInterfaceInChildren<I>(this Transform transform) where I : class {
            Transform[] transforms = transform.GetComponentsInChildren<Transform>();
            return transforms.Where<Transform>(t => t.GetInterface<I>() != null).ToArray<Transform>();
        }

        #endregion

        #region DistanceToCamera Extensions
        public static float DistanceToCamera(this GameObject go) {
            return go.transform.DistanceToCamera();
        }

        public static float DistanceToCamera(this Transform t) {
            Transform cameraTransform = Camera.main.transform;
            Plane cameraPlane = new Plane(cameraTransform.forward, cameraTransform.position);
            float distanceToCamera = cameraPlane.GetDistanceToPoint(t.position);
            return distanceToCamera;
            //return Vector3.Distance(Camera.main.transform.position, t.position);
        }

        public static int DistanceToCameraInt(this GameObject go) {
            return go.transform.DistanceToCameraInt();
        }

        public static int DistanceToCameraInt(this Transform t) {
            return (int)Math.Round(t.DistanceToCamera());
        }

        #endregion

        /// <summary>
        /// Gets the current OnScreen diameter in pixels of this collider. Can be Zero if not
        /// on the screen. WARNING: Donot use to scale an object
        /// </summary>
        /// <param name="col">The collider.</param>
        /// <returns>The diameter of the collider on the screen. Can be zero.</returns>
        public static float OnScreenDiameter(this Collider col) {
            Vector3 colliderPosition = col.transform.position;
            Debug.Log("ColliderPosition = {0}.".Inject(colliderPosition));
            if (!UnityUtility.IsVisibleAt(colliderPosition)) {
                return Constants.ZeroF;
            }
            float colliderDiameter = col.bounds.extents.magnitude;
            Debug.Log("ColliderDiameter = {0}.".Inject(colliderDiameter));
            float distanceFromCamera = Vector3.Distance(colliderPosition, Camera.main.transform.position);
            Debug.Log("DistanceFromCamera = {0}.".Inject(distanceFromCamera));
            float angularSize = (colliderDiameter / distanceFromCamera) * Mathf.Rad2Deg;
            Debug.Log("AngularSize = {0}.".Inject(angularSize));
            float pixelSize = ((angularSize * Screen.height) / Camera.main.fieldOfView);
            Debug.Log("PixelSize = {0}.".Inject(pixelSize));
            return pixelSize;
        }

    }
}


