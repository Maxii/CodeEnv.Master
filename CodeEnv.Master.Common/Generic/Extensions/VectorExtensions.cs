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
        /// <param name="degreeTolerance">The tolerance of the comparison in degrees.
        /// Default if not specified is the FloatEqualityPrecision of the game, aka 0.0001F.</param>
        /// <returns></returns>
        public static bool IsSameDirection(this Vector3 source, Vector3 v, float degreeTolerance = UnityConstants.FloatEqualityPrecision) {
            float unused;
            return UnityUtility.AreDirectionsWithinTolerance(source, v, degreeTolerance, out unused);
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

    }
}

