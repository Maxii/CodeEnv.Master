// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyMath.cs
// My Math utilities so I don't have to add to Mathfx or Math3D.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// My Math utilities so I don't have to add to Mathfx or Math3D.
    /// </summary>
    public static class MyMath {

        /// <summary>
        /// Returns the percentage distance along the line where the nearest point on the line is located.
        /// 1.0 = 100%. The value can be greater than 1.0 if point is beyond lineEnd.
        /// </summary>
        /// <param name="lineStart">The line start.</param>
        /// <param name="lineEnd">The line end.</param>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static float NearestPointFactor(Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineMagnitude = lineDirection.magnitude;
            lineDirection /= lineMagnitude;    // normalized direction

            float closestPoint = Vector3.Dot((point - lineStart), lineDirection); //Vector3.Dot(lineDirection,lineDirection);
            return closestPoint / lineMagnitude;
        }

        /// <summary>
        /// Returns an array of Vector3 local positions (y = 0F) that are uniformly distributed in a circle in the xz plane.
        /// </summary>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <returns></returns>
        public static Vector3[] UniformPointsOnCircle(float radius, int numberOfPoints) {
            Vector3[] points = new Vector3[numberOfPoints];
            float twoPi = (float)(2F * Math.PI);
            float startAngleInRadians = UnityEngine.Random.Range(0F, twoPi);
            for (int i = 0; i < numberOfPoints; i++) {
                float x = radius * Mathf.Cos((i * twoPi / (float)numberOfPoints) + startAngleInRadians);
                float z = radius * Mathf.Sin((i * twoPi / (float)numberOfPoints) + startAngleInRadians);
                points[i] = new Vector3(x, Constants.ZeroF, z);
            }
            return points;
        }

        /// <summary>
        /// Returns <c>true</c> if the specified point is on or within 
        /// the specified sphere, otherwise <c>false</c>.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <returns></returns>
        public static bool IsPointContainedInSphere(Vector3 point, float sphereRadius, Vector3 sphereCenter) {
            float sqrDistanceToPointFromCenter = Vector3.SqrMagnitude(point - sphereCenter);
            return sqrDistanceToPointFromCenter <= sphereRadius * sphereRadius;
        }

        /// <summary>
        /// Returns <c>true</c> if the two specified spheres intersect, otherwise <c>false</c>.
        /// </summary>
        /// <param name="aCenter">The center of Sphere A.</param>
        /// <param name="aRadius">The radius of Sphere A.</param>
        /// <param name="bCenter">The center of Sphere B.</param>
        /// <param name="bRadius">The radius of Sphere B.</param>
        /// <returns></returns>
        public static bool DoSpheresIntersect(Vector3 aCenter, float aRadius, Vector3 bCenter, float bRadius) {
            float sqrDistanceBetweenCenters = Vector3.SqrMagnitude(aCenter - bCenter);
            float combinedRadiusDistance = aRadius + bRadius;
            return sqrDistanceBetweenCenters <= combinedRadiusDistance * combinedRadiusDistance;
        }

        /// <summary>
        /// Calculates the location in world space of 8 vertices of a box surrounding a point.
        /// The minimum distance from this 'center' point to any side of the box is distance.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="distance">The minimum distance to the side of the box.</param>
        /// <returns></returns>
        public static IList<Vector3> CalcBoxVerticesAroundPoint(Vector3 point, float distance) {
            IList<Vector3> vertices = new List<Vector3>(8);
            var xPair = new float[2] { point.x - distance, point.x + distance };
            var yPair = new float[2] { point.y - distance, point.y + distance };
            var zPair = new float[2] { point.z - distance, point.z + distance };
            foreach (var x in xPair) {
                foreach (var y in yPair) {
                    foreach (var z in zPair) {
                        Vector3 gridBoxVertex = new Vector3(x, y, z);
                        vertices.Add(gridBoxVertex);
                    }
                }
            }
            return vertices;
        }

        /// <summary>
        /// Calculates the vertices of an inscribed box inside a sphere with 
        /// the provided radius and center point.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static IList<Vector3> CalcVerticesOfInscribedBoxInsideSphere(Vector3 center, float radius) {
            IList<Vector3> vertices = new List<Vector3>(8);
            IList<Vector3> normalizedVertices = Constants.NormalizedBoxVertices;
            foreach (var normalizedVertex in normalizedVertices) {
                vertices.Add(center + normalizedVertex * radius);
            }
            //D.Log("Center = {0}, Radius = {1}, Vertices = {2}.", center, radius, vertices.Concatenate());
            return vertices;
        }

        /// <summary>
        /// Determines whether the provided worldspace <c>point</c> is on or inside a sphere 
        /// defined by <c>radius</c> and <c>center</c>.
        /// </summary>
        /// <param name="center">The worldspace center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="point">The worldspace point to test.</param>
        /// <returns></returns>
        public static bool IsPointInsideSphere(Vector3 center, float radius, Vector3 point) {
            return Vector3.SqrMagnitude(point - center) <= radius * radius;
        }

        /// <summary>
        /// Determines whether <c>point</c> is on the infinite line defined by <c>linePtA</c> and <c>linePtB</c>.
        /// <see cref="http://stackoverflow.com/questions/7050186/find-if-point-lays-on-line-segment"/>
        /// </summary>
        /// <param name="linePtA">A point on a line.</param>
        /// <param name="linePtB">Another point on the same line.</param>
        /// <param name="point">The point in question.</param>
        /// <returns></returns>
        public static bool IsPointOnLine(Vector3 linePtA, Vector3 linePtB, Vector3 point) {
            float xValue = (point.x - linePtA.x) / (linePtB.x - linePtA.x);
            float yValue = (point.y - linePtA.y) / (linePtB.y - linePtA.y);
            float zValue = (point.z - linePtA.z) / (linePtB.z - linePtA.z);
            return Mathf.Approximately(xValue, yValue) && Mathf.Approximately(xValue, zValue) && Mathf.Approximately(yValue, zValue);
        }

        /// <summary>
        /// Finds the closest location on the surface of a sphere to the provided point. 
        /// Throws an error if <c>point</c> and <c>sphereCenter</c> are at the same location.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static Vector3 FindClosestPointOnSphereTo(Vector3 point, Vector3 sphereCenter, float sphereRadius) {
            D.Assert(point != sphereCenter);
            return sphereCenter + ((point - sphereCenter).normalized * sphereRadius);
        }

        /// <summary>
        /// Finds the closest point on sphere surface orthogonal to intersecting line.
        /// <remarks>Replaces archived version below which required both line points to be on the surface of the sphere.</remarks>
        /// </summary>
        /// <param name="startLinePt">The start point of the line in world space.</param>
        /// <param name="endLinePtOnSphere">The end point of the line in world space. Must be located on the surface of the sphere.</param>
        /// <param name="sphereCenter">The sphere center in world space.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static Vector3 FindClosestPointOnSphereOrthogonalToIntersectingLine(Vector3 startLinePt, Vector3 endLinePtOnSphere, Vector3 sphereCenter, float sphereRadius) {
            var linePtOnSphereToCenterSqrd = Vector3.SqrMagnitude(endLinePtOnSphere - sphereCenter);
            D.Assert(Mathfx.Approx(linePtOnSphereToCenterSqrd, Mathf.Pow(sphereRadius, 2F), .01F), "{0} should equal {1}.".Inject(Mathf.Sqrt(linePtOnSphereToCenterSqrd), sphereRadius));

            Vector3 midPtOfLineInsideSphere = Mathfx.NearestPoint(startLinePt, endLinePtOnSphere, sphereCenter);
            if (midPtOfLineInsideSphere != sphereCenter) {
                return FindClosestPointOnSphereTo(midPtOfLineInsideSphere, sphereCenter, sphereRadius);
            }
            // any plane containing the intersecting line will do as its normal will always be orthogonal to the intersecting line
            D.Log("Line goes directly through SphereCenter, so using random plane to generate normal.");
            Vector3 thirdPtDefiningPlane = sphereCenter + UnityEngine.Random.onUnitSphere;
            int count = 0;
            while (IsPointOnLine(startLinePt, endLinePtOnSphere, thirdPtDefiningPlane)) {
                D.Assert(count++ < 100);
                thirdPtDefiningPlane = sphereCenter + UnityEngine.Random.onUnitSphere;
            }
            Plane aPlaneContainingLine = new Plane(startLinePt, endLinePtOnSphere, thirdPtDefiningPlane);
            return sphereCenter + aPlaneContainingLine.normal * sphereRadius;
        }

        #region FindClosestPointOnSphereOrthogonalToIntersectingLine Archive

        /// <summary>
        /// Finds the closest point on sphere surface orthogonal to intersecting line.
        /// </summary>
        /// <param name="intersectPtA">First intersection point between the line and the sphere's surface.</param>
        /// <param name="intersectPtB">Second intersection point between the line and the sphere's surface.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        //public static Vector3 FindClosestPointOnSphereOrthogonalToIntersectingLine(Vector3 intersectPtA, Vector3 intersectPtB, Vector3 sphereCenter, float sphereRadius) {
        //    var aToCenterSqrd = Vector3.SqrMagnitude(intersectPtA - sphereCenter);
        //    var bToCenterSqrd = Vector3.SqrMagnitude(intersectPtB - sphereCenter);
        //    D.Assert(Mathfx.Approx(aToCenterSqrd, Mathf.Pow(sphereRadius, 2F), .01F), "{0} should equal {1}.".Inject(Mathf.Sqrt(aToCenterSqrd), sphereRadius));
        //    D.Assert(Mathfx.Approx(bToCenterSqrd, Mathf.Pow(sphereRadius, 2F), .01F), "{0} should equal {1}.".Inject(Mathf.Sqrt(bToCenterSqrd), sphereRadius));

        //    Vector3 lineMidPt = intersectPtA + (intersectPtB - intersectPtA) / 2F;
        //    if (lineMidPt != sphereCenter) {
        //        return FindClosestPointOnSphereTo(lineMidPt, sphereCenter, sphereRadius);
        //    }
        //    // any plane containing the intersecting line will do as its normal will always be orthogonal to the intersecting line
        //    D.Warn("LineMidPoint and SphereCenter are the same. Using random plane to generate normal.");
        //    Vector3 thirdPtDefiningPlane = sphereCenter + UnityEngine.Random.onUnitSphere;
        //    int count = 0;
        //    while (IsPointOnLine(intersectPtA, intersectPtB, thirdPtDefiningPlane)) {
        //        D.Assert(count++ < 100);
        //        thirdPtDefiningPlane = sphereCenter + UnityEngine.Random.onUnitSphere;
        //    }
        //    Plane aPlaneContainingLine = new Plane(intersectPtA, intersectPtB, thirdPtDefiningPlane);
        //    return sphereCenter + aPlaneContainingLine.normal * sphereRadius;
        //}

        #endregion

        /// <summary>
        /// Rounds each value in this Vector3 to the float equivalent of the closest integer.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static Vector3 RoundPoint(Vector3 point) {
            return RoundPoint(point, Vector3.one);
        }

        /// <summary>
        /// Rounds each value in this Vector3 to the float equivalent of the closest integer
        /// multiple of multi.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="multi">The multi.</param>
        /// <returns></returns>
        public static Vector3 RoundPoint(Vector3 point, Vector3 multi) {
            for (int i = 0; i < 3; i++) {
                point[i] = Utility.RoundMultiple(point[i], multi[i]);
            }
            return point;
        }

        /// <summary>
        /// Derives the average of the supplied vectors.
        /// </summary>
        /// <param name="vectors">The vectors.</param>
        /// <returns></returns>
        public static Vector3 Mean(IEnumerable<Vector3> vectors) {
            int length = vectors.Count();
            if (length == Constants.Zero) {
                return Vector3.zero;
            }
            float x = Constants.ZeroF, y = Constants.ZeroF, z = Constants.ZeroF;
            foreach (var v in vectors) {
                x += v.x;
                y += v.y;
                z += v.z;
            }
            return new Vector3(x / length, y / length, z / length);
        }

    }
}

