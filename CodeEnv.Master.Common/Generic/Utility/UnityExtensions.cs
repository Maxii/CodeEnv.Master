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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Unity GameObject, Transform, Component and Collider extensions
    /// </summary>
    public static class UnityExtensions {

        #region Get Safe/Single Component Extensions

        /// <summary>
        /// Gets the component of Type T in this gameObject. Logs a warning if the component cannot be found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The GameObject ostensibly containing the Component.</param>
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
        /// of type T in the gameObject itself. Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The source gameObject.</param>
        /// <param name="includeInactive">if set to <c>true</c> [include inactive].</param>
        /// <returns> The component of type T. </returns>
        public static T GetSingleComponentInImmediateChildren<T>(this GameObject go, bool includeInactive = false) where T : Component {
            var components = go.GetComponentsInChildren<T>(includeInactive).Where(c => c.transform.parent == go.transform);
            return components.Single();
        }

        /// <summary>
        /// Gets the components of Type T in  immediate children. Does not include components
        /// of type T in the gameObject itself.
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
        /// Gets the single interface of Type I in the GameObject or its parents.
        /// Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The go.</param>
        /// <param name="excludeSelf">if set to <c>true</c> [exclude self].</param>
        /// <returns></returns>
        public static I GetSingleInterfaceInParents<I>(this GameObject go, bool excludeSelf = false) where I : class {
            var interfaces = go.GetComponentsInParent<I>();
            //D.Log("Found {0} interfaces of type {1}.", interfaces.Count(), typeof(I).Name);
            if (excludeSelf) {
                interfaces = interfaces.Where(i => (i as Component).gameObject != go).ToArray();
            }
            return interfaces.Single();
        }

        /// <summary>
        ///  Gets the interface of type I found in the gameObject's components.
        ///  Logs a warning if the interface cannot be found.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The gameObject.</param>
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
        /// <returns></returns>
        public static I GetSingleInterfaceInChildren<I>(this GameObject go, bool excludeSelf = false) where I : class {
            var interfaces = go.GetComponentsInChildren<I>();
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
        /// <returns>        /// An array of interfaces of type I. Can be empty. </returns>
        public static I[] GetSafeInterfacesInChildren<I>(this GameObject go, bool excludeSelf = false) where I : class {
            I[] interfaces = go.GetComponentsInChildren<I>();
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
        /// of type I in the gameObject itself. Throws an exception if none are found or more than one exists.
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="go">The source gameObject.</param>
        /// <returns> The interface of type I. </returns>
        public static I GetSingleInterfaceInImmediateChildren<I>(this GameObject go) where I : class {
            var interfaces = go.GetComponentsInChildren<I>().Where(i => (i as Component).transform.parent == go.transform);
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

        #endregion

        #region Vector Extensions

        public static void ValidateNormalized(this Vector3 v) {
            if (!v.IsSameAs(v.normalized)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.NotNormalized.Inject(v, callingMethodName));
            }
        }

        /// <summary>
        /// Efficient way of comparing two vectors for equality.
        /// </summary>
        public static bool IsSameAs(this Vector3 source, Vector3 v) {
            return Mathfx.Approx(source, v, UnityConstants.FloatEqualityPrecision);
        }

        /// <summary>
        /// Compares the direction of 2 vectors for equality, ignoring their magnitude.
        /// </summary>
        /// <param name="sourceDir">The source direction.</param>
        /// <param name="dir">The direction to compare the source to.</param>
        /// <param name="allowedDeviation">The allowed deviation in degrees. Cannot be more precise
        /// than UnityConstants.AngleEqualityPrecision due to Unity floating point precision.</param>
        /// <returns></returns>
        public static bool IsSameDirection(this Vector3 sourceDir, Vector3 dir, float allowedDeviation = UnityConstants.AngleEqualityPrecision) {
            return UnityUtility.AreDirectionsWithinTolerance(sourceDir.normalized, dir.normalized, allowedDeviation);
        }

        /// <summary>
        /// Returns a more precise version of Vector3.ToString().
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static string ToPreciseString(this Vector3 source) {
            return source.ToString("G4");
        }

        /// <summary>
        /// Returns a more precise version of Vector2.ToString().
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static string ToPreciseString(this Vector2 source) {
            return source.ToString("G4");
        }

        /// <summary>
        /// Gets the vectors between <c>fromLocation</c> and all <c>toLocations</c>.
        /// </summary>
        /// <param name="fromLocation">From location.</param>
        /// <param name="toLocations">To locations.</param>
        /// <returns></returns>
        public static IEnumerable<Vector3> GetVectorsTo(this Vector3 fromLocation, IEnumerable<Vector3> toLocations) {
            D.Assert(!toLocations.IsNullOrEmpty());
            int length = toLocations.Count();
            var vectorsToOtherLocations = new List<Vector3>(length);
            foreach (var loc in toLocations) {
                vectorsToOtherLocations.Add(loc - fromLocation);
            }
            return vectorsToOtherLocations;
        }

        /// <summary>
        /// Finds the average vector between fromLocation and all toLocations.
        /// </summary>
        /// <param name="fromLocation"> Source location.</param>
        /// <param name="toLocations">The other locations.</param>
        /// <returns></returns>
        public static Vector3 FindMeanVectorTo(this Vector3 fromLocation, IEnumerable<Vector3> toLocations) {
            var vectorsToLocations = fromLocation.GetVectorsTo(toLocations);
            return MyMath.Mean(vectorsToLocations);
        }

        /// <summary>
        /// Finds the normalized average direction vector between fromLocation and all toLocations.
        /// </summary>
        /// <param name="fromLocation">Source location.</param>
        /// <param name="toLocations">The provided locations.</param>
        /// <returns></returns>
        public static Vector3 FindMeanDirectionTo(this Vector3 fromLocation, IEnumerable<Vector3> toLocations) {
            return fromLocation.FindMeanVectorTo(toLocations).normalized;
        }

        /// <summary>
        /// Finds the mean distance between fromLocation and all toLocations.
        /// </summary>
        /// <param name="fromLocation">Source location.</param>
        /// <param name="toLocations">The other locations.</param>
        /// <returns></returns>
        public static float FindMeanDistanceTo(this Vector3 fromLocation, IEnumerable<Vector3> toLocations) {
            return fromLocation.FindMeanVectorTo(toLocations).magnitude;
        }

        /// <summary>
        /// Finds the maximum distance between fromLocation and all toLocations.
        /// </summary>
        /// <param name="fromLocation">Source location.</param>
        /// <param name="toLocations">The other locations.</param>
        /// <returns></returns>
        public static float FindMaxDistanceTo(this Vector3 fromLocation, IEnumerable<Vector3> toLocations) {
            var vectorsToLocations = fromLocation.GetVectorsTo(toLocations);
            return Mathf.Sqrt(Mathf.Max(vectorsToLocations.Select(v => v.sqrMagnitude).ToArray()));
        }

        /// <summary>
        /// Sets the x value of this transform's world position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="x">The x.</param>
        public static void SetWorldPositionX(this Transform t, float x) {
            Vector3 newPosition = new Vector3(x, t.position.y, t.position.z);
            t.position = newPosition;
        }

        /// <summary>
        /// Sets the x value of this transform's world position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="y">The y.</param>
        public static void SetWorldPositionY(this Transform t, float y) {
            Vector3 newPosition = new Vector3(t.position.x, y, t.position.z);
            t.position = newPosition;
        }

        /// <summary>
        /// Sets the x value of this transform's world position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="z">The z.</param>
        public static void SetWorldPositionZ(this Transform t, float z) {
            Vector3 newPosition = new Vector3(t.position.x, t.position.y, z);
            t.position = newPosition;
        }

        /// <summary>
        /// Sets the x value of this transform's local position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="x">The x.</param>
        public static void SetLocalPositionX(this Transform t, float x) {
            Vector3 newPosition = new Vector3(x, t.localPosition.y, t.localPosition.z);
            t.localPosition = newPosition;
        }

        /// <summary>
        /// Sets the x value of this transform's local position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="y">The y.</param>
        public static void SetLocalPositionY(this Transform t, float y) {
            Vector3 newPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
            t.localPosition = newPosition;
        }

        /// <summary>
        /// Sets the x value of this transform's local position.
        /// </summary>
        /// <param name="t">The transform.</param>
        /// <param name="z">The z.</param>
        public static void SetLocalPositionZ(this Transform t, float z) {
            Vector3 newPosition = new Vector3(t.localPosition.x, t.localPosition.y, z);
            t.localPosition = newPosition;
        }

        public static Vector3 SetX(this Vector3 source, float x) {
            return new Vector3(x, source.y, source.z);
        }

        public static Vector3 SetY(this Vector3 source, float y) {
            return new Vector3(source.x, y, source.z);
        }

        public static Vector3 SetZ(this Vector3 source, float z) {
            return new Vector3(source.x, source.y, z);
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
            //D.Log("ColliderPosition = {0}.".Inject(colliderPosition));
            if (!UnityUtility.IsWithinCameraViewport(colliderPosition)) {
                return Constants.ZeroF;
            }
            float colliderDiameter = col.bounds.extents.magnitude;
            //D.Log("ColliderDiameter = {0}.".Inject(colliderDiameter));
            float distanceFromCamera = Vector3.Distance(colliderPosition, Camera.main.transform.position);
            //D.Log("DistanceFromCamera = {0}.".Inject(distanceFromCamera));
            float angularSize = (colliderDiameter / distanceFromCamera) * Mathf.Rad2Deg;
            //D.Log("AngularSize = {0}.".Inject(angularSize));
            float pixelSize = ((angularSize * Screen.height) / Camera.main.fieldOfView);
            //D.Log("PixelSize = {0}.".Inject(pixelSize));
            return pixelSize;
        }

        /// <summary>
        /// Determines whether this renderer is in the line of sight of (and therefore rendered by) the provided camera.
        /// WARNING: Does not take into account layer-specific farClipPlanes and my approach to a workaround is not reliable.
        ///   - it thinks the camera sees the renderer as it doesn't account for layer-specific clipPlane
        ///   - unsuccessfully used Vector3.Distance() to compare to layer farClipPlane distance
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
        /// <param name="allowedDeviation">The allowed deviation in degrees. Cannot be more precise
        /// than UnityConstants.AngleEqualityPrecision due to Unity floating point precision.</param>
        /// <returns></returns>
        public static bool IsSame(this Quaternion sourceRotation, Quaternion otherRotation, float allowedDeviation = UnityConstants.AngleEqualityPrecision) {
            //var actualDeviation = Quaternion.Angle(__FixQuaternion(sourceRotation), __FixQuaternion(otherRotation));
            D.Warn(allowedDeviation < UnityConstants.AngleEqualityPrecision, "Angle Deviation precision {0} cannot be < {1}.", allowedDeviation, UnityConstants.AngleEqualityPrecision);
            allowedDeviation = Mathf.Clamp(allowedDeviation, UnityConstants.AngleEqualityPrecision, 180F);
            var actualDeviation = Quaternion.Angle(sourceRotation, otherRotation);
            var isSame = actualDeviation <= allowedDeviation;
            //D.Log("IsSame result = {0}, remainingAngle: {1}, allowedDeviation: {2}.", isSame, actualDeviation, allowedDeviation);
            return isSame;
        }

        // see http://answers.unity3d.com/questions/1036566/quaternionangle-is-inaccurate.html#answer-1162822
        [Obsolete]
        private static Quaternion __FixQuaternion(Quaternion q) {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            q.x /= mag;
            q.y /= mag;
            q.z /= mag;
            q.w /= mag;
            return q;
        }
    }
}


