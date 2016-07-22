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
        /// Determines whether the provided worldspace <c>point</c> is inside a sphere 
        /// defined by <c>radius</c> and <c>center</c>.
        /// </summary>
        /// <param name="center">The worldspace center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="point">The worldspace point to test.</param>
        /// <returns></returns>
        public static bool IsPointInsideSphere(Vector3 center, float radius, Vector3 point) {
            return Vector3.SqrMagnitude(point - center) < radius * radius;
        }

        /// <summary>
        /// Returns true if the line segment intersects the sphere.
        /// </summary>
        /// <param name="linePt1">One end of the segment.</param>
        /// <param name="linePt2">Other end of the segment.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool DoesLineSegmentIntersectSphere(Vector3 linePt1, Vector3 linePt2, Vector3 sphereCenter, float sphereRadius) {
            Vector3 closestPtOnLineSegmentToSphereCenter = Math3D.ProjectPointOnLineSegment(linePt1, linePt2, sphereCenter);
            return Vector3.SqrMagnitude(closestPtOnLineSegmentToSphereCenter - sphereCenter) <= sphereRadius * sphereRadius;
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
        /// <remarks>Replaces version which required both line points to be on the surface of the sphere.</remarks>
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

        /// <summary>
        /// Calculates and returns the vertices of the Icosahedron that surrounds an
        /// inscribed sphere (sphere touches center of each face of the Icosahedron) 
        /// defined by center and radius.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static Vector3[] CalcVerticesOfIcosahedronAroundInscribedSphere(Vector3 center, float radius) {
            float unusedEdgeLength, unusedMaxNodeDistance;
            return CalcVerticesOfIcosahedronAroundInscribedSphere(center, radius, out unusedEdgeLength, out unusedMaxNodeDistance);
        }

        /// <summary>
        /// Calculates and returns the vertices of the Icosahedron that surrounds an
        /// inscribed sphere (sphere touches center of each face of the Icosahedron) 
        /// defined by center and radius.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="edgeLength">The resulting length of the edge between neighboring vertices.</param>
        /// <returns></returns>
        public static Vector3[] CalcVerticesOfIcosahedronAroundInscribedSphere(Vector3 center, float radius, out float edgeLength) {
            float unusedMaxNodeDistance;
            return CalcVerticesOfIcosahedronAroundInscribedSphere(center, radius, out edgeLength, out unusedMaxNodeDistance);
        }

        /// <summary>
        /// Calculates and returns the vertices of the Icosahedron that surrounds an
        /// inscribed sphere (sphere touches center of each face of the Icosahedron) 
        /// defined by center and radius.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="edgeLength">The resulting length of the edge between neighboring vertices.</param>
        /// <param name="maxNodeDistance">The resulting maximum node distance allowed between any 2 nodes.</param>
        /// <returns></returns>
        public static Vector3[] CalcVerticesOfIcosahedronAroundInscribedSphere(Vector3 center, float radius, out float edgeLength, out float maxNodeDistance) {
            edgeLength = radius / 0.7557613141F;    // https://en.wikipedia.org/wiki/Regular_icosahedron#Dimensions
            Vector3[] verticesAroundOrigin = CalcVerticesOfIcosahedron(edgeLength, out maxNodeDistance);
            return new Vector3[] {
                center + verticesAroundOrigin[0],
                center + verticesAroundOrigin[1],
                center + verticesAroundOrigin[2],
                center + verticesAroundOrigin[3],
                center + verticesAroundOrigin[4],
                center + verticesAroundOrigin[5],
                center + verticesAroundOrigin[6],
                center + verticesAroundOrigin[7],
                center + verticesAroundOrigin[8],
                center + verticesAroundOrigin[9],
                center + verticesAroundOrigin[10],
                center + verticesAroundOrigin[11]
            };
        }

        private static IDictionary<float, Vector3[]> _icosahedronVerticesLookup = new Dictionary<float, Vector3[]>();

        /// <summary>
        /// Calculates the vertices of an Icosahedron with the designated edgeLength around the origin.
        /// <see cref="http://csharphelper.com/blog/2015/12/platonic-solids-part-6-the-icosahedron/" />
        /// </summary>
        /// <param name="edgeLength">The length of the edge between neighboring vertices.</param>
        /// <param name="minDistanceBetweenVertices">The minimum distance between vertices > edgeLength.
        /// Used to determine max allowable distance between AStar nodes.</param>
        /// <returns></returns>
        private static Vector3[] CalcVerticesOfIcosahedron(float edgeLength, out float minDistanceBetweenVertices) {
            Vector3[] vertices;
            if (!_icosahedronVerticesLookup.TryGetValue(edgeLength, out vertices)) {
                float t2 = (float)Math.PI / 10F;
                float t4 = (float)Math.PI / 5F;
                float r = (edgeLength / 2F) / Mathf.Sin(t4);
                float h = Mathf.Cos(t4) * r;
                float cx = r * Mathf.Sin(t2);
                float cz = r * Mathf.Cos(t2);
                float h1 = Mathf.Sqrt(edgeLength * edgeLength - r * r);
                float h2 = Mathf.Sqrt((h + r) * (h + r) - h * h);
                float y2 = (h2 - h1) / 2F;
                float y1 = y2 + h1;
                vertices = new Vector3[] {
                    new Vector3(0F, y1, 0F),
                    new Vector3(r, y2, 0),
                    new Vector3(cx, y2, cz),
                    new Vector3(-h, y2, edgeLength / 2F),
                    new Vector3(-h, y2, -edgeLength / 2F),
                    new Vector3(cx, y2, -cz),
                    new Vector3(-r, -y2, 0F),
                    new Vector3(-cx, -y2, -cz),
                    new Vector3(h, -y2, -edgeLength / 2F),
                    new Vector3(h, -y2, edgeLength / 2F),
                    new Vector3(-cx, -y2, cz),
                    new Vector3(0F, -y1, 0F)
                };
                _icosahedronVerticesLookup.Add(edgeLength, vertices);
                ValidateEdgeLengths(edgeLength, vertices);
            }
            minDistanceBetweenVertices = CalcMinDistanceBetweenVerticesGreaterThanEdgeLength(edgeLength, vertices);
            return vertices;
        }

        /// <summary>
        /// Calculates the lowest distance value between vertices that is greater than the value of edgeLength.
        /// </summary>
        /// <param name="edgeLength">The length of the edge between neighboring vertices.</param>
        /// <param name="vertices">The vertices.</param>
        /// <returns></returns>
        private static float CalcMinDistanceBetweenVerticesGreaterThanEdgeLength(float edgeLength, Vector3[] vertices) {
            IList<float> sqrDistances = new List<float>(102); // remove same vertices (12) and edges (3)
            float sqrDistance;
            float sqrdEdgeLengthThreshold = edgeLength * edgeLength + 1F;
            Vector3 vi, vj;
            for (int i = 0; i < 12; i++) {
                vi = vertices[i];
                for (int j = i; j < 12; j++) {
                    if (i == j) { continue; }
                    vj = vertices[j];
                    sqrDistance = Vector3.SqrMagnitude(vi - vj);
                    if (sqrDistance > sqrdEdgeLengthThreshold) {
                        //D.Log("Adding {0} as > {1}. Vertices: {2} & {3}.", sqrDistance, sqrdEdgeLengthThreshold, vi, vj);
                        sqrDistances.Add(sqrDistance);
                    }
                }
            }
            return Mathf.Sqrt(Mathf.Min(sqrDistances.ToArray()));
        }

        /// <summary>
        /// Validates the edge lengths of a Icosahedron using its provided vertices.
        /// <remarks>Simply validates the edges of 'a' as too much manual work to validate them all.</remarks>
        /// <see cref="http://csharphelper.com/blog/2015/12/platonic-solids-part-6-the-icosahedron/" for diagram./>
        /// </summary>
        /// <param name="edgeLength">The length of the edge between neighboring vertices.</param>
        /// <param name="vertices">The vertices.</param>
        private static void ValidateEdgeLengths(float edgeLength, Vector3[] vertices) {
            Vector3 a = vertices[0];
            float sqrdEdgeLength = edgeLength * edgeLength;
            Vector3[] aEdges = new Vector3[] {
                vertices[1] - a,    // ab
                vertices[2] - a,    // ac
                vertices[3] - a,    // ad
                vertices[4] - a,    // ae
                vertices[5] - a     // af
            };
            aEdges.ForAll<Vector3>(edge => {
                bool isEqual = Mathfx.Approx(Vector3.SqrMagnitude(edge), sqrdEdgeLength, 1F);
                D.Assert(isEqual, "{0} != {1}.", Vector3.SqrMagnitude(edge), sqrdEdgeLength);
            });
        }

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

