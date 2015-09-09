// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VectorExtensions.cs
// Extensions for Vector2, Vector3 and Vector4 (Unity.Color).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public static class VectorExtensions {

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
        /// <param name="source">The source direction.</param>
        /// <param name="v">The direction to compare the source to.</param>
        /// <param name="degreeTolerance">The tolerance of the comparison in degrees.</param>
        /// <returns></returns>
        public static bool IsSameDirection(this Vector3 source, Vector3 v, float degreeTolerance) {
            float angle = Vector3.Angle(source, v);
            D.Log("Angle = {0} degrees.", angle);
            return angle < degreeTolerance;
        }

        /// <summary>
        /// Finds the average vector between myLoc and all otherLocations.
        /// </summary>
        /// <param name="myLoc">My loc.</param>
        /// <param name="otherLocations">The other locations.</param>
        /// <returns></returns>
        public static Vector3 FindMean(this Vector3 myLoc, IEnumerable<Vector3> otherLocations) {
            int length = otherLocations.Count();
            var vectorsToOtherLocations = new List<Vector3>(length);
            foreach (var loc in otherLocations) {
                vectorsToOtherLocations.Add(loc - myLoc);
            }
            return UnityUtility.Mean(vectorsToOtherLocations);
        }

        /// <summary>
        /// Finds the normalized average direction from myLoc toward the otherLocations.
        /// </summary>
        /// <param name="myLoc">My location.</param>
        /// <param name="otherLocations">The provided locations.</param>
        /// <returns></returns>
        public static Vector3 FindMeanDirection(this Vector3 myLoc, IEnumerable<Vector3> otherLocations) {
            return myLoc.FindMean(otherLocations).normalized;
        }

        /// <summary>
        /// Finds the mean distance between myLoc and all otherLocations.
        /// </summary>
        /// <param name="myLoc">My loc.</param>
        /// <param name="otherLocations">The other locations.</param>
        /// <returns></returns>
        public static float FindMeanDistance(this Vector3 myLoc, IEnumerable<Vector3> otherLocations) {
            return myLoc.FindMean(otherLocations).magnitude;
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

    }
}

