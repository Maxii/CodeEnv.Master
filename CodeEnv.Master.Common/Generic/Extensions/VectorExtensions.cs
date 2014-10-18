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
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public static class VectorExtensions {

        public static void ValidateNormalized(this Vector3 v) {
            if (!v.IsSame(v.normalized)) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.NotNormalized.Inject(v, callingMethodName));
            }
        }

        /// <summary>
        /// Compares 2 vectors for equality.
        /// </summary>
        public static bool IsSame(this Vector3 source, Vector3 v) {
            return Mathfx.Approx(source, v, .0001F);    // 1M times less precise than Unity's built in == comparison
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

        public static Color ToUnityColor(this GameColor color) {
            switch (color) {
                case GameColor.Black:
                    return Color.black;
                case GameColor.Blue:
                    return Color.blue;
                case GameColor.Cyan:
                    return Color.cyan;
                case GameColor.Green:
                    return Color.green;
                case GameColor.Gray:
                    return Color.gray;
                case GameColor.Clear:
                    return Color.clear;
                case GameColor.Magenta:
                    return Color.magenta;
                case GameColor.Red:
                    return Color.red;
                case GameColor.White:
                    return Color.white;
                case GameColor.Yellow:
                    return Color.yellow;
                case GameColor.None:
                    return Color.white;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(color));
            }
        }

        public static void SetX(this Transform t, float x) {
            Vector3 newPosition = new Vector3(x, t.position.y, t.position.z);
            t.position = newPosition;
        }

        public static void SetY(this Transform t, float y) {
            Vector3 newPosition = new Vector3(t.position.x, y, t.position.z);
            t.position = newPosition;
        }

        public static void SetZ(this Transform t, float z) {
            Vector3 newPosition = new Vector3(t.position.x, t.position.y, z);
            t.position = newPosition;
        }

        public static Vector3 SetX(this Vector3 v, float x) {
            return new Vector3(x, v.y, v.z);
        }

        public static Vector3 SetY(this Vector3 v, float y) {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 SetZ(this Vector3 v, float z) {
            return new Vector3(v.x, v.y, z);
        }

    }
}

