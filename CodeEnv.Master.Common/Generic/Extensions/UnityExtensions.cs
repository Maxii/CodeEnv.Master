// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityExtensions.cs
// Unity GameObject, Transform, Component and Collider extensions
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Unity GameObject, Transform, Component and Collider extensions
    /// </summary>
    public static class UnityExtensions {

        #region GetMonoBehaviour... Extensions

        /// <summary>
        /// Returns the MonoBehaviour of Type T in the GameObject or any of its parents.
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static T GetSafeMonoBehaviourComponentInParents<T>(this GameObject go, bool excludeSelf = false) where T : MonoBehaviour {
            Transform parent = excludeSelf ? go.transform.parent : go.transform;
            while (parent != null) {
                T component = parent.gameObject.GetComponent<T>();
                if (component != null) {
                    return component;
                }
                parent = parent.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            return null;
        }

        /// <summary>
        /// Returns the Component of Type T in the GameObject or any of its parents.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static T GetComponentInParents<T>(this GameObject go, bool excludeSelf = false) where T : Component {
            Transform parent = excludeSelf ? go.transform.parent : go.transform;
            while (parent != null) {
                T component = parent.gameObject.GetComponent<T>();
                if (component != null) {
                    return component;
                }
                parent = parent.parent;
            }
            return null;
        }

        /// <summary>
        /// Gets the single component of Type T in  immediate children. Does not include components
        /// of type T in the gameobject itself. Throws an exception if more than one exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The source gameobject.</param>
        /// <returns>The component of type T or null if none exists.</returns>
        public static T GetComponentInImmediateChildren<T>(this GameObject go) where T : Component {
            T[] components = go.GetComponentsInChildren<T>();
            return components.Single(c => c.transform.parent == go.transform);
        }

        /// <summary>
        /// Gets the components of Type T in  immediate children. Does not include components
        /// of type T in the gameobject itself. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The source gameobject.</param>
        /// <returns>The components of type T or an empty array if none exists.</returns>
        public static T[] GetComponentsInImmediateChildren<T>(this GameObject go) where T : Component {
            T[] components = go.GetComponentsInChildren<T>();
            return components.Where(c => c.transform.parent == go.transform).ToArray();
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
        /// Returns the MonoBehaviour of Type T in the GameObject or any of its children using depth first search.
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
        /// Returns all MonoBehaviours of Type T in the GameObject or any of its children.
        /// Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T">Must be a MonoBehaviour.</typeparam>
        /// <param name="go">The go.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns>
        /// An array of components of type T. Can be empty.
        /// </returns>
        public static T[] GetSafeMonoBehaviourComponentsInChildren<T>(this GameObject go, bool includeInactive = false) where T : MonoBehaviour {
            T[] components = go.GetComponentsInChildren<T>(includeInactive);
            if (components.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return components;
        }

        #endregion

        /// <summary>
        /// Finds the first child of this Transform that also is a MonoBehaviour of Type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">The transform.</param>
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

        /// <summary>
        /// Gets the first transform that contains an interface of Type I in this transform or any of its parents.
        /// Logs a warning if the transform cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="t">The transform.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static Transform GetSafeTransformWithInterfaceInParents<I>(this Transform t, bool excludeSelf = false) where I : class {
            Transform parent = excludeSelf ? t.parent : t;
            while (parent != null) {
                I component = parent.gameObject.GetComponent(typeof(I)) as I;
                if (component != null) {
                    return parent;
                }
                parent = parent.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, t.name);
            return null;
        }

        /// <summary>
        /// Gets the first interface of Type I in this transform or any of its parents.
        /// Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="t">The transform.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static I GetSafeInterfaceInParents<I>(this Transform t, bool excludeSelf = false) where I : class {
            Transform parent = excludeSelf ? t.parent : t;
            while (parent != null) {
                I component = parent.GetComponent(typeof(I)) as I;
                if (component != null) {
                    return component;
                }
                parent = parent.parent;
            }
            D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, t.name);
            return null;
        }

        /// <summary>
        /// Gets the first interface of Type I in this gameobject or any of its parents.
        /// Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameobject.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static I GetSafeInterfaceInParents<I>(this GameObject go, bool excludeSelf = false) where I : class {
            return go.transform.GetSafeInterfaceInParents<I>(excludeSelf);
        }


        /// <summary>
        /// Gets the interface of type I found in the Transform's peer components.
        /// </summary>
        /// <typeparam name="I">The Interface type.</typeparam>
        /// <param name="t">The Transform</param>
        /// <returns>The class of type I found, if any. Can be null.</returns>
        public static I GetInterface<I>(this Transform t) where I : class {
            return t.GetComponent(typeof(I)) as I;
        }

        /// <summary>
        ///  Gets the interface of type I found in the gameObject's components.
        ///  Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameobject.</param>
        /// <returns></returns>
        public static I GetSafeInterface<I>(this GameObject go) where I : class {
            I i = go.transform.GetInterface<I>();
            if (i == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return i;

        }

        /// <summary>
        /// Gets the first interface of type I found in the GameObject's components or its children.
        /// Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameobject.</param>
        /// <returns></returns>
        public static I GetSafeInterfaceInChildren<I>(this GameObject go) where I : class {
            I i = go.transform.GetInterfaceInChildren<I>();
            if (i == null) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return i;
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

        /// <summary>
        /// Gets the first transform that contains an interface of Type I in this transform's peer components or any of the gameObject's children.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="transform">The transform.</param>
        /// <returns></returns>
        public static Transform GetTransformWithInterfaceInChildren<I>(this Transform transform) where I : class {
            Transform[] transforms = transform.GetComponentsInChildren<Transform>();
            return transforms.First<Transform>(t => t.GetInterface<I>() != null);
        }

        /// <summary>
        /// Returns all Interfaces of Type I in the GameObject or any of its children.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameObject.</param>
        /// <returns></returns>
        public static I[] GetInterfacesInChildren<I>(this GameObject go) where I : class {
            // GetComponent(typeof(I)) works, but GetComponents(typeof(I)) does not
            return go.GetComponentsInChildren<Component>().OfType<I>().ToArray();
        }

        /// <summary>
        /// Returns all Interfaces of Type I in the GameObject or any of its children.
        /// Logs a warning if an interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The go.</param>
        /// <returns></returns>
        public static I[] GetSafeInterfacesInChildren<I>(this GameObject go) where I : class {
            I[] interfaces = go.GetInterfacesInChildren<I>();
            if (interfaces.Length == 0) {
                D.Warn(ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return interfaces;
        }

        /// <summary>
        /// Gets all transforms that contain the interface of Type I in its peer components or any of the gameObject's children.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="transform">The transform.</param>
        /// <returns></returns>
        public static Transform[] GetTransformsWithInterfaceInChildren<I>(this Transform transform) where I : class {
            Transform[] transforms = transform.GetComponentsInChildren<Transform>();
            return transforms.Where<Transform>(t => t.GetInterface<I>() != null).ToArray<Transform>();
        }

        #endregion

        #region DistanceToCamera Extensions

        public static float DistanceToCamera(this Vector3 point) {
            Transform cameraTransform = Camera.main.transform;
            Plane cameraPlane = new Plane(cameraTransform.forward, cameraTransform.position);
            float distanceToCamera = cameraPlane.GetDistanceToPoint(point);
            return distanceToCamera;
        }

        public static float DistanceToCamera(this GameObject go) {
            return go.transform.DistanceToCamera();
        }

        public static float DistanceToCamera(this Transform t) {
            return t.position.DistanceToCamera();
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
        /// on the screen. WARNING: Do not use to scale an object
        /// </summary>
        /// <param name="col">The collider.</param>
        /// <returns>The diameter of the collider on the screen. Can be zero.</returns>
        public static float OnScreenDiameter(this Collider col) {
            Vector3 colliderPosition = col.transform.position;
            D.Log("ColliderPosition = {0}.".Inject(colliderPosition));
            if (!UnityUtility.IsVisibleAt(colliderPosition)) {
                return Constants.ZeroF;
            }
            float colliderDiameter = col.bounds.extents.magnitude;
            D.Log("ColliderDiameter = {0}.".Inject(colliderDiameter));
            float distanceFromCamera = Vector3.Distance(colliderPosition, Camera.main.transform.position);
            D.Log("DistanceFromCamera = {0}.".Inject(distanceFromCamera));
            float angularSize = (colliderDiameter / distanceFromCamera) * Mathf.Rad2Deg;
            D.Log("AngularSize = {0}.".Inject(angularSize));
            float pixelSize = ((angularSize * Screen.height) / Camera.main.fieldOfView);
            D.Log("PixelSize = {0}.".Inject(pixelSize));
            return pixelSize;
        }

        /// <summary>
        /// Determines whether this renderer is in the line of sight of (and therefore rendered by) the provided camera.
        /// Does not take into account layer-specific farClipPlanes and my approach to a workaround is not reliable.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="camera">The camera.</param>
        /// <returns></returns>
        [Obsolete]
        public static bool InLineOfSightOf(this Renderer renderer, Camera camera) {
            Plane[] frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            bool inDefaultFrustrum = GeometryUtility.TestPlanesAABB(frustrumPlanes, renderer.bounds);
            if (inDefaultFrustrum) {
                int layer = renderer.gameObject.layer;
                float layerCullingDistance = Camera.main.layerCullDistances[layer];
                if (layerCullingDistance != Constants.ZeroF) {
                    float sqrDistanceToRenderer = Vector3.SqrMagnitude(renderer.transform.position - camera.transform.position);
                    if (sqrDistanceToRenderer > Mathf.Pow(layerCullingDistance, 2F)) {
                        // outside layer farClipPlane
                        return false;
                    }
                }
            }
            return inDefaultFrustrum;
        }

    }
}


