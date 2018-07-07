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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// My Math utilities so I don't have to add to Mathfx or Math3D.
    /// </summary>
    public static class MyMath {

        public static float SqrtOfTwo = Mathf.Sqrt(2.0F);

        public static float SqrtOfThree = Mathf.Sqrt(3.0F);

        /// <summary>
        /// The cube face normals in order:
        /// right, left, top, bottom, front, back.
        /// <remarks>Cube must be axis aligned.</remarks>
        /// </summary>
        public static Vector3[] CubeFaceNormals = {
            new Vector3(1F, 0F, 0F),
            new Vector3(-1F, 0F, 0F),
            new Vector3(0F, 1F, 0F),
            new Vector3(0F, -1F, 0F),
            new Vector3(0F, 0F, 1F),
            new Vector3(0F, 0F, -1F)
        };

        /// <summary>
        /// Chooses the point from candidatePoints that is furthest from all existingPoints, aka chooses the point
        /// that has the largest minimum distance to any existing point.
        /// </summary>
        /// <param name="candidatePoints">The candidate points.</param>
        /// <param name="existingPoints">The existing points.</param>
        /// <returns></returns>
        public static Vector3 ChooseFurthestFrom(IEnumerable<Vector3> candidatePoints, IEnumerable<Vector3> existingPoints) {
            Utility.ValidateNotNullOrEmpty(candidatePoints);
            Utility.ValidateNotNullOrEmpty(existingPoints);
            // for each candidate cell, find min distance to existing cells and record it
            // then chosen cell should be that cell whose min to any existing cell is > any other
            IDictionary<float, Vector3> pointByMinDistanceToExistingPointsLookup = new Dictionary<float, Vector3>();
            foreach (var candidatePoint in candidatePoints) {
                Vector3 minDistancePoint = default(Vector3);
                float minSqrDistanceToExistingPoint = float.MaxValue;
                foreach (var existingPoint in existingPoints) {
                    float sqrDistanceToExistingPoint = Vector3.SqrMagnitude(candidatePoint - existingPoint);
                    if (sqrDistanceToExistingPoint < minSqrDistanceToExistingPoint) {
                        minSqrDistanceToExistingPoint = sqrDistanceToExistingPoint;
                        minDistancePoint = candidatePoint;
                    }
                }
                if (!pointByMinDistanceToExistingPointsLookup.ContainsKey(minSqrDistanceToExistingPoint)) {
                    pointByMinDistanceToExistingPointsLookup.Add(minSqrDistanceToExistingPoint, minDistancePoint);
                }
            }

            var largestMinSqrDistance = pointByMinDistanceToExistingPointsLookup.Keys.Max();
            return pointByMinDistanceToExistingPointsLookup[largestMinSqrDistance];
        }

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
        /// Returns an array of Vector3 local positions (y = 0F) that are uniformly distributed in a circle in the XZ plane.
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
        /// Determines whether one sphere is completely contained by another.
        /// </summary>
        /// <param name="containedCenter">The contained center.</param>
        /// <param name="containedRadius">The contained radius.</param>
        /// <param name="containingCenter">The containing center.</param>
        /// <param name="containingRadius">The containing radius.</param>
        /// <returns></returns>
        public static bool IsSphereCompletelyContainedWithinSphere(Vector3 containedCenter, float containedRadius, Vector3 containingCenter, float containingRadius) {
            // isContained = containingRadius >= distanceBetweenCenters + containedRadius ->
            // isContained = containingRadius - containedRadius >= distanceBetweenCenters
            float sqrDistanceBetweenCenters = Vector3.SqrMagnitude(containingCenter - containedCenter);
            float containingRadiusSqrd = containingRadius * containingRadius;
            float containedRadiusSqrd = containedRadius * containedRadius;
            return containingRadiusSqrd - containedRadiusSqrd >= sqrDistanceBetweenCenters;
        }

        /// <summary>
        /// Calculates the location in world space of 8 vertices of a cube surrounding a point.
        /// The distance from this 'center' point to any side of the cube is faceDistance.
        /// <remarks>Index order returned: left/bottom/back, left/bottom/fwd, left/top/back, left/top/fwd,
        /// right/bottom/back, right/bottom/fwd, right/top/back, right/top/fwd.</remarks>
        /// </summary>
        /// <param name="cubeCenter">The point.</param>
        /// <param name="faceDistance">The distance from point to any face of the cube.</param>
        /// <returns></returns>
        public static IList<Vector3> CalcCubeVertices(Vector3 cubeCenter, float faceDistance) {
            IList<Vector3> vertices = new List<Vector3>(8);
            var xPair = new float[2] { cubeCenter.x - faceDistance, cubeCenter.x + faceDistance };
            var yPair = new float[2] { cubeCenter.y - faceDistance, cubeCenter.y + faceDistance };
            var zPair = new float[2] { cubeCenter.z - faceDistance, cubeCenter.z + faceDistance };
            foreach (var x in xPair) {
                foreach (var y in yPair) {
                    foreach (var z in zPair) {
                        Vector3 cubeVertex = new Vector3(x, y, z);
                        vertices.Add(cubeVertex);
                    }
                }
            }
            return vertices;
        }

        /// <summary>
        /// Returns the center point of each of the 6 faces of a cube around a central point.
        /// </summary>
        /// <param name="cubeCenter">The center point.</param>
        /// <param name="faceDistance">The distance of all faces from the center.</param>
        /// <returns></returns>
        public static Vector3[] CalcCubeFaceCenters(Vector3 cubeCenter, float faceDistance) {
            return new Vector3[] {
                cubeCenter + CubeFaceNormals[0] * faceDistance,
                cubeCenter + CubeFaceNormals[1] * faceDistance,
                cubeCenter + CubeFaceNormals[2] * faceDistance,
                cubeCenter + CubeFaceNormals[3] * faceDistance,
                cubeCenter + CubeFaceNormals[4] * faceDistance,
                cubeCenter + CubeFaceNormals[5] * faceDistance
            };
        }

        /// <summary>
        /// Returns the center point of each of the 6 faces of a cube along with the normal for each face.
        /// </summary>
        /// <param name="cubeCenter">The center point.</param>
        /// <param name="faceDistance">The distance of all faces from the center.</param>
        /// <param name="faceNormals">The face normals.</param>
        /// <returns></returns>
        public static Vector3[] CalcCubeFaces(Vector3 cubeCenter, float faceDistance, out Vector3[] faceNormals) {
            faceNormals = new Vector3[] {
                CubeFaceNormals[0],
                CubeFaceNormals[1],
                CubeFaceNormals[2],
                CubeFaceNormals[3],
                CubeFaceNormals[4],
                CubeFaceNormals[5]
            };
            CubeFaceNormals[0].ValidateNormalized();

            return CalcCubeFaceCenters(cubeCenter, faceDistance);
        }

        /// <summary>
        /// Calculates the vertices of a (circumscribed) cube surrounding an inscribed sphere with the provided radius and center point.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static List<Vector3> CalcVerticesOfCubeSurroundingInscribedSphere(Vector3 center, float radius) {
            float unusedVertexDistance;
            return CalcVerticesOfCubeSurroundingInscribedSphere(center, radius, out unusedVertexDistance);
        }

        /// <summary>
        /// Calculates the vertices of a (circumscribed) cube surrounding an inscribed sphere with the provided radius and center point.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="vertexDistance">The resulting distance from center to each vertex.</param>
        /// <returns></returns>
        public static List<Vector3> CalcVerticesOfCubeSurroundingInscribedSphere(Vector3 center, float radius, out float vertexDistance) {
            List<Vector3> vertices = new List<Vector3>(8);
            var normalizedVertices = Constants.NormalizedCubeVertices;
            vertexDistance = radius * SqrtOfThree;  // https://en.wikipedia.org/wiki/Cube
            foreach (var normalizedVertex in normalizedVertices) {
                vertices.Add(center + normalizedVertex * vertexDistance);
            }
            //D.Log("Center = {0}, Radius = {1}, Vertices = {2}.", center, radius, vertices.Concatenate());
            return vertices;
        }

        /// <summary>
        /// Calculates the vertices of an inscribed cube inside a sphere with the provided radius and center point.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static List<Vector3> CalcVerticesOfInscribedCubeInsideSphere(Vector3 center, float radius) {
            List<Vector3> vertices = new List<Vector3>(8);
            var normalizedVertices = Constants.NormalizedCubeVertices;
            foreach (var normalizedVertex in normalizedVertices) {
                vertices.Add(center + (normalizedVertex * radius));
            }
            //D.Log("Center = {0}, Radius = {1}, Vertices = {2}.", center, radius, vertices.Concatenate());
            return vertices;
        }

        /// <summary>
        /// Determines whether the provided worldspace <c>point</c> is on (surface) or inside a sphere 
        /// defined by <c>radius</c> and <c>center</c>.
        /// </summary>
        /// <param name="center">The worldspace center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="point">The worldspace point to test.</param>
        /// <returns></returns>
        public static bool IsPointOnOrInsideSphere(Vector3 center, float radius, Vector3 point) {
            return Vector3.SqrMagnitude(point - center) <= radius * radius;
        }

        /// <summary>
        /// Returns true if the line segment intersects the sphere.
        /// <remarks>Test Suite:
        /// D.Assert(MyMath.DoesLineSegmentIntersectSphere(new Vector3(1F, 0F, 0F), Vector3.one, Vector3.zero, 1F));    // tangent
        /// D.Assert(!MyMath.DoesLineSegmentIntersectSphere(Vector3.one, 2 * Vector3.one, Vector3.zero, 1F)); // non-intersecting line segment
        /// D.Assert(MyMath.DoesLineSegmentIntersectSphere(Vector3.zero, Vector3.one, Vector3.zero, 1F));    // intersecting one inside
        /// D.Assert(MyMath.DoesLineSegmentIntersectSphere(new Vector3(-0.5F, 0F, 0F), new Vector3(0.5F, 0F, 0F), Vector3.zero, 1F));    // intersecting all inside
        /// D.Assert(MyMath.DoesLineSegmentIntersectSphere(new Vector3(-1.5F, 0F, 0F), new Vector3(1.5F, 0F, 0F), Vector3.zero, 1F));    // intersecting none inside
        /// </remarks>
        /// </summary>
        /// <param name="linePt1">One end of the segment.</param>
        /// <param name="linePt2">Other end of the segment.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool DoesLineSegmentIntersectSphere(Vector3 linePt1, Vector3 linePt2, Vector3 sphereCenter, float sphereRadius) {
            Vector3 closestPtOnLineSegmentToSphereCenter = Math3D.ProjectPointOnLineSegment(linePt1, linePt2, sphereCenter);
            return IsPointOnOrInsideSphere(sphereCenter, sphereRadius, closestPtOnLineSegmentToSphereCenter);
        }

        public static bool IsSphereCompletelyContainedWithinCube(Vector3 cubeCenter, float faceDistance, Vector3 sphereCenter, float sphereRadius) {
            Vector3[] faceNormals;
            var cubeFaceCenters = CalcCubeFaces(cubeCenter, faceDistance, out faceNormals);

            for (int i = 0; i < 6; i++) {
                Vector3 cubeFaceCenter = cubeFaceCenters[i];
                Vector3 cubeFaceNormal = faceNormals[i];
                if (!IsSphereInsidePlane(cubeFaceNormal, cubeFaceCenter, sphereCenter, sphereRadius)) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsPointOnOrInsideCube(Vector3 cubeCenter, float faceDistance, Vector3 point) {
            var cubeVertices = CalcCubeVertices(cubeCenter, faceDistance);
            var minVertex = cubeVertices[0];    // left/bottom/back
            var maxVertex = cubeVertices[7];    // right/top/front
            return point.x >= minVertex.x && point.x <= maxVertex.x && point.y >= minVertex.y && point.y <= maxVertex.y
                && point.z >= minVertex.z && point.z <= maxVertex.z;
        }

        /// <summary>
        /// Determines whether a sphere is inside a plane.
        /// <remarks>Returns true if the entire sphere is located on the opposite side of the plane indicated
        /// by the plane's normal, aka behind the plane.</remarks>
        /// <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/>
        /// </summary>
        /// <param name="planeNormal">The plane normal.</param>
        /// <param name="planePoint">The plane point.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool IsSphereInsidePlane(Vector3 planeNormal, Vector3 planePoint, Vector3 sphereCenter, float sphereRadius) {
            return -Math3D.SignedDistancePlanePoint(planeNormal, planePoint, sphereCenter) > sphereRadius;
        }

        /// <summary>
        /// Determines whether a sphere is outside a plane.
        /// <remarks>Returns true if the entire sphere is located on the side of the plane indicated
        /// by the plane's normal, aka in front of the plane.</remarks>
        /// <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/>
        /// </summary>
        /// <param name="planeNormal">The plane normal.</param>
        /// <param name="planePoint">The plane point.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool IsSphereOutsidePlane(Vector3 planeNormal, Vector3 planePoint, Vector3 sphereCenter, float sphereRadius) {
            return Math3D.SignedDistancePlanePoint(planeNormal, planePoint, sphereCenter) > sphereRadius;
        }

        /// <summary>
        /// Determines whether a sphere intersects a plane.
        /// <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/>
        /// </summary>
        /// <param name="planeNormal">The plane normal.</param>
        /// <param name="planePoint">The plane point.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool IsSphereIntersectingPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 sphereCenter, float sphereRadius) {
            return Mathf.Abs(Math3D.SignedDistancePlanePoint(planeNormal, planePoint, sphereCenter)) <= sphereRadius;
        }

        /// <summary>
        /// Determines whether a sphere intersects an axis aligned cube.
        /// <remarks>Can be reworked to also apply to non-axis aligned boxes.
        /// <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/></remarks>
        /// </summary>
        /// <param name="cubeCenter">The cube center.</param>
        /// <param name="faceDistance">The face distance.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool IsSphereIntersectingCube(Vector3 cubeCenter, float faceDistance, Vector3 sphereCenter, float sphereRadius) {
            // right, left, top, bottom, front, back
            Vector3[] cubeFaceNormals;
            Vector3[] cubeFaceCenters = CalcCubeFaces(cubeCenter, faceDistance, out cubeFaceNormals);

            Vector3 leftFaceNormal = cubeFaceNormals[1];
            Vector3 leftFaceCenter = cubeFaceCenters[1];
            Vector3 rightFaceNormal = cubeFaceNormals[0];
            Vector3 rightFaceCenter = cubeFaceCenters[0];
            Vector3 frontFaceNormal = cubeFaceNormals[4];
            Vector3 frontFaceCenter = cubeFaceCenters[4];
            Vector3 backFaceNormal = cubeFaceNormals[5];
            Vector3 backFaceCenter = cubeFaceCenters[5];
            Vector3 topFaceNormal = cubeFaceNormals[2];
            Vector3 topFaceCenter = cubeFaceCenters[2];
            Vector3 bottomFaceNormal = cubeFaceNormals[3];
            Vector3 bottomFaceCenter = cubeFaceCenters[3];

            bool outLeft = IsSphereOutsidePlane(leftFaceNormal, leftFaceCenter, sphereCenter, sphereRadius); // completely outside left face
            bool outRight = IsSphereOutsidePlane(rightFaceNormal, rightFaceCenter, sphereCenter, sphereRadius);
            bool outFront = IsSphereOutsidePlane(frontFaceNormal, frontFaceCenter, sphereCenter, sphereRadius);
            bool outBack = IsSphereOutsidePlane(backFaceNormal, backFaceCenter, sphereCenter, sphereRadius);
            bool outTop = IsSphereOutsidePlane(topFaceNormal, topFaceCenter, sphereCenter, sphereRadius);
            bool outBottom = IsSphereOutsidePlane(bottomFaceNormal, bottomFaceCenter, sphereCenter, sphereRadius);

            if (IsSphereIntersectingPlane(topFaceNormal, topFaceCenter, sphereCenter, sphereRadius) && !outLeft && !outRight && !outFront && !outBack) {
                return true;
            }

            if (IsSphereIntersectingPlane(bottomFaceNormal, bottomFaceCenter, sphereCenter, sphereRadius) && !outLeft && !outRight && !outFront && !outBack) {
                return true;
            }

            if (IsSphereIntersectingPlane(leftFaceNormal, leftFaceCenter, sphereCenter, sphereRadius) && !outTop && !outBottom && !outFront && !outBack) {
                return true;
            }

            if (IsSphereIntersectingPlane(rightFaceNormal, rightFaceCenter, sphereCenter, sphereRadius) && !outTop && !outBottom && !outFront && !outBack) {
                return true;
            }

            if (IsSphereIntersectingPlane(frontFaceNormal, frontFaceCenter, sphereCenter, sphereRadius) && !outTop && !outBottom && !outLeft && !outRight) {
                return true;
            }

            if (IsSphereIntersectingPlane(backFaceNormal, backFaceCenter, sphereCenter, sphereRadius) && !outTop && !outBottom && !outLeft && !outRight) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a sphere is completely outside an axis aligned cube.
        /// <remarks>Can be reworked to also apply to non-axis aligned boxes.
        /// <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/></remarks>
        /// </summary>
        /// <param name="cubeCenter">The cube center.</param>
        /// <param name="faceDistance">The face distance.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static bool IsSphereOutsideCube(Vector3 cubeCenter, float faceDistance, Vector3 sphereCenter, float sphereRadius) {
            return !IsSphereCompletelyContainedWithinCube(cubeCenter, faceDistance, sphereCenter, sphereRadius)
                && !IsSphereIntersectingCube(cubeCenter, faceDistance, sphereCenter, sphereRadius);
        }

        [Obsolete("Not currently used")]
        private static Vector3[][] CalcCubeEdges(Vector3 center, float faceDistance) {
            var vertices = CalcCubeVertices(center, faceDistance);
            // Vertex index order returned: left/bottom/back, left/bottom/fwd, left/top/back, left/top/fwd,
            // right/bottom/back, right/bottom/fwd, right/top/back, right/top/fwd
            Vector3[][] edges = {
                new Vector3[] { vertices[0], vertices[1] },
                new Vector3[] { vertices[0], vertices[4] },
                new Vector3[] { vertices[0], vertices[2] },
                new Vector3[] { vertices[5], vertices[1] },
                new Vector3[] { vertices[5], vertices[4] },
                new Vector3[] { vertices[5], vertices[7] },
                new Vector3[] { vertices[3], vertices[7] },
                new Vector3[] { vertices[3], vertices[2] },
                new Vector3[] { vertices[3], vertices[1] },
                new Vector3[] { vertices[6], vertices[2] },
                new Vector3[] { vertices[6], vertices[7] },
                new Vector3[] { vertices[6], vertices[4] }
            };
            return edges;
        }

        /// <summary>
        /// Returns <c>true</c> if any part of the cube defined by the 2 provided opposing vertexes 
        /// intersects the sphere defined by its center and radius.
        /// <remarks>The cube must be axis aligned and both the cube and sphere solid.</remarks>
        /// <remarks>The requirement to be solid means if either shape is completely contained by the other, 
        /// they will intersect. This is not a boundary intersection algorithm.</remarks>
        /// </summary>
        /// <see cref="https://stackoverflow.com/questions/4578967/cube-sphere-intersection-test"/>
        /// <remarks>For potentially erroneous <see cref="http://theorangeduck.com/page/correct-box-sphere-intersection"/></remarks>
        /// <param name="aCubeCornerVertex">a cube corner vertex.</param>
        /// <param name="aCubeCornerOpposingVertex">a cube corner opposing vertex.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        [Obsolete("Not tested and potentially erroneous. Use IsSphereIntersectingCube")]
        public static bool DoesCubeIntersectSphere(Vector3 aCubeCornerVertex, Vector3 aCubeCornerOpposingVertex, Vector3 sphereCenter, float sphereRadius) {
            float sphereRadiusSqrd = sphereRadius * sphereRadius;
            // assume C1 and C2 are element-wise sorted, if not, do that now ???
            if (sphereCenter.x < aCubeCornerVertex.x) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.x - aCubeCornerVertex.x, 2F);
            }
            else if (sphereCenter.x > aCubeCornerOpposingVertex.x) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.x - aCubeCornerOpposingVertex.x, 2F);
            }

            if (sphereCenter.y < aCubeCornerVertex.y) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.y - aCubeCornerVertex.y, 2F);
            }
            else if (sphereCenter.y > aCubeCornerOpposingVertex.y) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.y - aCubeCornerOpposingVertex.y, 2F);
            }
            if (sphereCenter.z < aCubeCornerVertex.z) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.z - aCubeCornerVertex.z, 2F);
            }
            else if (sphereCenter.z > aCubeCornerOpposingVertex.z) {
                sphereRadiusSqrd -= Mathf.Pow(sphereCenter.z - aCubeCornerOpposingVertex.z, 2F);
            }
            return sphereRadiusSqrd > 0F;
        }

        /// <summary>
        /// Returns true if the line defined by linePt and lineDirection intersects the sphere
        /// defined by center and radius.
        /// <see cref="https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection"/> 
        /// </summary>
        /// <param name="center">The sphere center.</param>
        /// <param name="radius">The sphere radius.</param>
        /// <param name="linePt">The line point origin</param>
        /// <param name="lineDirection">The line direction.</param>
        /// <returns></returns>
        [Obsolete("Use DoesLineSegmentIntersectSphere")]    // 1.24.17 I'm not clear how reliable DoesLineIntersectSphere is
        public static bool DoesLineIntersectSphere(Vector3 center, float radius, Vector3 linePt, Vector3 lineDirection) {
            lineDirection.ValidateNormalized();
            Vector3 centerToLinePoint = linePt - center;
            float dot = Vector3.Dot(lineDirection, centerToLinePoint);
            return (dot * dot) - Vector3.SqrMagnitude(centerToLinePoint) + (radius * radius) >= Constants.ZeroF;
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
            D.AssertNotEqual(point, sphereCenter);
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
            var linePtOnSphereToCenterDistance = Vector3.Distance(endLinePtOnSphere, sphereCenter);
            // 11.10.16 tolerance .01F -> .0001F // 1.23.17 tolerance .0001F -> .001F
            D.Assert(Mathfx.Approx(sphereRadius, linePtOnSphereToCenterDistance, .001F), "{0} should equal {1}.".Inject(linePtOnSphereToCenterDistance, sphereRadius));

            Vector3 midPtOfLineInsideSphere = Mathfx.NearestPoint(startLinePt, endLinePtOnSphere, sphereCenter);
            if (midPtOfLineInsideSphere != sphereCenter) {
                return FindClosestPointOnSphereTo(midPtOfLineInsideSphere, sphereCenter, sphereRadius);
            }
            // any plane containing the intersecting line will do as its normal will always be orthogonal to the intersecting line
            D.Log("Line goes directly through SphereCenter, so using random plane to generate normal.");
            Vector3 thirdPtDefiningPlane = sphereCenter + UnityEngine.Random.onUnitSphere;
            int count = 0;
            while (IsPointOnLine(startLinePt, endLinePtOnSphere, thirdPtDefiningPlane)) {
                D.Assert(count++ < 100, "Too many iterations");
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
            return CalcVerticesOfIcosahedronSurroundingInscribedSphere(center, radius, out unusedEdgeLength, out unusedMaxNodeDistance);
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
        public static Vector3[] CalcVerticesOfIcosahedronSurroundingInscribedSphere(Vector3 center, float radius, out float edgeLength) {
            float unusedMaxNodeDistance;
            return CalcVerticesOfIcosahedronSurroundingInscribedSphere(center, radius, out edgeLength, out unusedMaxNodeDistance);
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
        public static Vector3[] CalcVerticesOfIcosahedronSurroundingInscribedSphere(Vector3 center, float radius, out float edgeLength, out float maxNodeDistance) {
            edgeLength = radius / 0.7557613141F;    // https://en.wikipedia.org/wiki/Regular_icosahedron#Dimensions
            Vector3[] verticesAroundOrigin = GetVerticesOfIcosahedron(edgeLength, out maxNodeDistance);
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

        private static IDictionary<float, Vector3[]> _icosahedronVerticesLookup = new Dictionary<float, Vector3[]>(FloatEqualityComparer.Default);

        /// <summary>
        /// Calculates the vertices of an Icosahedron with the designated edgeLength around the origin.
        /// <see cref="http://csharphelper.com/blog/2015/12/platonic-solids-part-6-the-icosahedron/" />
        /// </summary>
        /// <param name="edgeLength">The length of the edge between neighboring vertices.</param>
        /// <param name="minDistanceBetweenVertices">The minimum distance between vertices > edgeLength.
        /// Used to determine max allowable distance between AStar nodes.</param>
        /// <returns></returns>
        private static Vector3[] GetVerticesOfIcosahedron(float edgeLength, out float minDistanceBetweenVertices) {
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
            Vector3 iVertex, jVertex;
            for (int i = 0; i < 12; i++) {
                iVertex = vertices[i];
                for (int j = i; j < 12; j++) {
                    if (i == j) { continue; }
                    jVertex = vertices[j];
                    sqrDistance = Vector3.SqrMagnitude(iVertex - jVertex);
                    if (sqrDistance > sqrdEdgeLengthThreshold) {
                        //D.Log("Adding {0} as > {1}. Vertices: {2} & {3}.", sqrDistance, sqrdEdgeLengthThreshold, iVertex, jVertex);
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
                vertices[1] - a,    // a-b
                vertices[2] - a,    // a-c
                vertices[3] - a,    // a-d
                vertices[4] - a,    // a-e
                vertices[5] - a     // a-f
            };
            aEdges.ForAll<Vector3>(edge => {
                bool isEqual = Mathfx.Approx(Vector3.SqrMagnitude(edge), sqrdEdgeLength, 1F);
                if (!isEqual) {
                    D.Error("{0} should equal {1}.", Vector3.SqrMagnitude(edge), sqrdEdgeLength);
                }
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

        public static int Factorial(int i) {
            if (i <= Constants.One) {
                return Constants.One;

            }
            return i * Factorial(i - 1);
        }

    }
}

