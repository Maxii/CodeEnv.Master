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

        #region Get Safe/Single Component Extensions

        /// <summary>
        /// Gets the component of Type T in this gameobject. Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The GameObject obstensibly containing the Component.</param>
        /// <returns>The component of type T or null if not found.</returns>
        public static T GetSafeComponent<T>(this GameObject go) where T : Component {
            T component = go.GetComponent<T>();
            if (component == null) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return component;
        }

        /// <summary>
        /// Returns all Components of Type T in the GameObject or any of its children.
        /// Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns>        /// An array of components of type T. Can be empty. </returns>
        public static T[] GetSafeComponentsInChildren<T>(this GameObject go, bool excludeSelf = false, bool includeInactive = false) where T : Component {
            T[] components = go.GetComponentsInChildren<T>(includeInactive);
            if (excludeSelf) {
                components = components.Where(c => c.gameObject != go).ToArray();
            }
            if (components.Length == Constants.Zero) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return components;
        }

        /// <summary>
        /// Returns all Components of Type T in the GameObject's immediate children, not
        /// including the gameObject itself. Logs a warning if a component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns>        /// An array of components of type T. Can be empty. </returns>
        public static T[] GetSafeComponentsInImmediateChildren<T>(this GameObject go, bool includeInactive = false) where T : Component {
            var components = go.GetSafeComponentsInChildren<T>(excludeSelf: true, includeInactive: includeInactive);
            components = components.Where(c => c.transform.parent == go.transform).ToArray();
            if (components.Length == Constants.Zero) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return components;
        }

        /// <summary>
        /// Gets the single component of Type T in the GameObject or its children.
        /// Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns></returns>
        public static T GetSingleComponentInChildren<T>(this GameObject go, bool excludeSelf = false, bool includeInactive = false) where T : Component {
            var components = go.GetComponentsInChildren<T>(includeInactive);
            if (excludeSelf) {
                components = components.Where(c => c.gameObject != go).ToArray();
            }
            return components.Single();
        }

        /// <summary>
        /// Gets the single component of Type T in the GameObject or its parents.
        /// Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns></returns>
        public static T GetSingleComponentInParents<T>(this GameObject go, bool excludeSelf = false, bool includeInactive = false) where T : Component {
            var components = go.GetComponentsInParent<T>(includeInactive);
            if (excludeSelf) {
                components = components.Where(c => c.gameObject != go).ToArray();
            }
            return components.Single();
        }

        /// <summary>
        /// Gets the first component found of Type T in the GameObject or its parents. 
        /// Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static T GetSafeFirstComponentInParents<T>(this GameObject go, bool excludeSelf = false) where T : Component {
            GameObject startSearchGo = excludeSelf ? go.transform.parent.gameObject : go;
            T component = startSearchGo.GetComponentInParent<T>();
            if (component == null) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(T).Name, go.name);
            }
            return component;
        }

        /// <summary>
        /// Gets the single component of Type T in immediate children. Does not include components
        /// of type T in the gameobject itself. Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The source gameobject.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns> The component of type T. </returns>
        public static T GetSingleComponentInImmediateChildren<T>(this GameObject go, bool includeInactive = false) where T : Component {
            var components = go.GetComponentsInChildren<T>(includeInactive).Where(c => c.transform.parent == go.transform);
            return components.Single();
        }

        /// <summary>
        /// Gets the components of Type T in  immediate children. Does not include components
        /// of type T in the gameobject itself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The source gameObject.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns>
        /// The components of type T or an empty array if none exists.
        /// </returns>
        public static T[] GetComponentsInImmediateChildren<T>(this GameObject go, bool includeInactive = false) where T : Component {
            T[] components = go.GetComponentsInChildren<T>(includeInactive);
            return components.Where(c => c.transform.parent == go.transform).ToArray();
        }

        #endregion

        #region Get Safe/Single Interface Extensions

        /// <summary>
        /// Gets the first Interface found of Type I in the GameObject or its parents. 
        /// Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static I GetSafeFirstInterfaceInParents<I>(this GameObject go, bool excludeSelf = false) where I : class {
            Transform parent = excludeSelf ? go.transform.parent : go.transform;
            I i = parent.GetComponentInParent<I>();
            if (i == null) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return i;
        }

        /// <summary>
        ///  Gets the interface of type I found in the gameObject's components.
        ///  Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameobject.</param>
        /// <returns></returns>
        public static I GetSafeInterface<I>(this GameObject go) where I : class {
            I i = go.GetComponent<I>();
            if (i == null) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return i;
        }

        /// <summary>
        /// Gets the single interface of Type I in the GameObject or its children.
        /// Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns></returns>
        public static I GetSingleInterfaceInChildren<I>(this GameObject go, bool excludeSelf = false, bool includeInactive = false) where I : class {
            var interfaces = go.GetComponentsInChildren<I>(includeInactive);
            //D.Log("Found {0} interfaces of type {1}.", interfaces.Count(), typeof(I).Name);
            if (excludeSelf) {
                interfaces = interfaces.Where(i => (i as Component).gameObject != go).ToArray();
            }
            return interfaces.Single();
        }

        /// <summary>
        /// Returns all Interfaces of Type I in the GameObject or any of its children.
        /// Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns>        /// An array of interfaces of type I. Can be empty. </returns>
        public static I[] GetSafeInterfacesInChildren<I>(this GameObject go, bool excludeSelf = false, bool includeInactive = false) where I : class {
            I[] interfaces = go.GetComponentsInChildren<I>(includeInactive);
            if (excludeSelf) {
                interfaces = interfaces.Where(i => (i as Component).gameObject != go).ToArray();
            }
            if (interfaces.Length == Constants.Zero) {
                D.WarnContext(go, ErrorMessages.ComponentNotFound, typeof(I).Name, go.name);
            }
            return interfaces;
        }


        /// <summary>
        /// Gets the single interface of Type I in immediate children. Does not include interfaces
        /// of type I in the gameobject itself. Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The source gameobject.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns> The interface of type I. </returns>
        public static I GetSingleInterfaceInImmediateChildren<I>(this GameObject go, bool includeInactive = false) where I : class {
            var interfaces = go.GetComponentsInChildren<I>(includeInactive).Where(i => (i as Component).transform.parent == go.transform);
            return interfaces.Single();
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
        /// on the screen. WARNING: Do not use to scale an object.
        /// </summary>
        /// <param name="col">The collider.</param>
        /// <returns>The diameter of the collider on the screen. Can be zero.</returns>
        public static float OnScreenDiameter(this Collider col) {
            Vector3 colliderPosition = col.transform.position;
            D.Log("ColliderPosition = {0}.".Inject(colliderPosition));
            if (!UnityUtility.IsWithinCameraViewport(colliderPosition)) {
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
        /// WARNING: Does not take into account layer-specific farClipPlanes and my approach to a workaround is not reliable.
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

        /// <summary>
        /// Sets the alpha value of the provided material.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="value">The value.</param>
        public static void SetAlpha(this Material material, float value) {
            Color color = material.color;
            color.a = value;
            material.color = color;
        }

        /// <summary>
        /// Adds the designated component if it is missing. If one is already present, returns the existing component.
        /// </summary>
        /// <typeparam name="C"></typeparam>
        /// <param name="go">The go.</param>
        /// <returns></returns>
        public static C AddMissingComponent<C>(this GameObject go) where C : Component {
            var c = go.GetComponent<C>();
            if (c == null) {
                c = go.AddComponent<C>();
            }
            return c;
        }

        /// <summary>
        /// Determines whether the two rotations are the same within the allowedDeviation in degrees.
        /// </summary>
        /// <param name="sourceRotation">The source rotation.</param>
        /// <param name="otherRotation">The other rotation.</param>
        /// <param name="allowedDeviation">The allowed deviation in degrees.</param>
        /// <returns></returns>
        public static bool IsSame(this Quaternion sourceRotation, Quaternion otherRotation, float allowedDeviation = UnityConstants.FloatEqualityPrecision) {
            return Quaternion.Angle(sourceRotation, otherRotation) <= allowedDeviation;
        }

    }
}


