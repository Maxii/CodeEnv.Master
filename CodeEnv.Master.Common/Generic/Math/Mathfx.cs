// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Mathfx.cs
// Like Unity's Mathf, more methods like Lerp for smoothing.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Like Unity's Mathf, more methods like Lerp for smoothing.
    /// <see cref="http://wiki.unity3d.com/index.php/Mathfx"/>
    /// </summary>
    public static class Mathfx {

        /// <summary>
        /// Interpolates while easing in and out at the limits.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Hermite(float start, float end, float value) {
            return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
        }

        /// <summary>
        /// Short for 'sinusoidal interpolation', interpolatex while easing around the end, when value is near one.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Sinerp(float start, float end, float value) {
            return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
        }

        /// <summary>
        /// Similar to Sinerp, except it eases in, when value is near zero, instead of easing out (and uses cosine instead of sine).
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Coserp(float start, float end, float value) {
            return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
        }

        /// <summary>
        ///Short for 'boing-like interpolation', this method will first overshoot, then waver back and forth
        ///around the end value before coming to a rest.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Berp(float start, float end, float value) {
            value = Mathf.Clamp01(value);
            value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
            return start + (end - start) * value;
        }

        /// <summary>
        ///  Works like Lerp, but has ease-in and ease-out of the values.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns></returns>
        public static float SmoothStep(float x, float min, float max) {
            x = Mathf.Clamp(x, min, max);
            float v1 = (x - min) / (max - min);
            float v2 = (x - min) / (max - min);
            return -2 * v1 * v1 * v1 + 3 * v2 * v2;
        }

        /// <summary>
        /// Short for 'linearly interpolate', this method is equivalent to Unity's Mathf.Lerp, included for comparison.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Lerp(float start, float end, float value) {
            return ((1.0f - value) * start) + (value * end);
        }

        /// <summary>
        /// Will return the nearest point on a 'virtual' line to a point. The line is treated as having infinite magnitude
        /// so the nearest point on this 'virtual' line can be way beyond lineEnd. Useful for making an object follow a track.
        /// </summary>
        /// <param name="lineStart">The line start.</param>
        /// <param name="lineEnd">The line end.</param>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static Vector3 NearestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
            float closestPoint = Vector3.Dot((point - lineStart), lineDirection); //WASTE OF CPU POWER - This is always ONE -- Vector3.Dot(lineDirection,lineDirection);
            return lineStart + (closestPoint * lineDirection);
        }

        /// <summary>
        /// Works like NearestPoint except the line is NOT treated as infinite. The point returned is the 
        /// nearest point on the line that ends at lineEnd.
        /// </summary>
        /// <param name="lineStart">The line start.</param>
        /// <param name="lineEnd">The line end.</param>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static Vector3 NearestPointStrict(Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            Vector3 fullDirection = lineEnd - lineStart;
            Vector3 lineDirection = Vector3.Normalize(fullDirection);
            float closestPoint = Vector3.Dot((point - lineStart), lineDirection); //WASTE OF CPU POWER - This is always ONE -- Vector3.Dot(lineDirection,lineDirection);
            return lineStart + (Mathf.Clamp(closestPoint, 0.0f, Vector3.Magnitude(fullDirection)) * lineDirection);
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
        ///Returns a value between 0 and 1 that can be used to easily make bouncing GUI items (a la OS X's Dock)
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns></returns>
        public static float Bounce(float x) {
            return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
        }

        /// <summary>
        ///  Tests if value is within acceptable acceptableRange of the targetValue. Useful in dealing with floating point inprecision
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="acceptableRange">The acceptableRange to either side of the targetValue.</param>
        /// <returns></returns>
        public static bool Approx(float value, float targetValue, float acceptableRange) {
            return ((Mathf.Abs(value - targetValue) < acceptableRange));
        }


        /// <summary>
        ///  Tests if a Vector3 is within acceptableRange of another Vector3. Useful in dealing with floating point inprecision.
        /// Compares the square of the distance separating the two vectors to the square of the acceptableRange as this 
        /// avoids calculating a square root which is much slower than squaring the acceptableRange.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetValue">The targetValue.</param>
        /// <param name="acceptableRange">The acceptableRange.</param>
        /// <returns></returns>
        public static bool Approx(Vector3 vector, Vector3 targetVector, float acceptableRange) {
            return ((vector - targetVector).sqrMagnitude < acceptableRange * acceptableRange);
        }

        /// <summary>
        /// Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
        /// This is useful when interpolating eulerAngles and the object
        /// crosses the 0/360 boundary.  The standard Lerp function causes the object
        /// to rotate in the wrong direction and looks stupid. Clerp fixes that.Clerps the specified start.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static float Clerp(float start, float end, float value) {
            float min = 0.0f;
            float max = 360.0f;
            float half = Mathf.Abs((max - min) / 2.0f);//half the distance between min and max
            float retval = 0.0f;
            float diff = 0.0f;

            if ((end - start) < -half) {
                diff = ((max - start) + end) * value;
                retval = start + diff;
            }
            else if ((end - start) > half) {
                diff = -((max - end) + start) * value;
                retval = start + diff;
            }
            else
                retval = start + (end - start) * value;

            // Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
            return retval;
        }

        /// <summary>
        /// Returns a collection of Vector3 points (y = 0) that are uniformly distributed around a circle in the xz plane.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <returns></returns>
        public static Vector3[] UniformPointsOnCircle(float radius, int numberOfPoints) {
            Vector3[] points = new Vector3[numberOfPoints];
            float twoPi = (float)(2F * Math.PI);
            float startAngleInRadians = UnityEngine.Random.Range(0F, twoPi);
            for (int i = 0; i < numberOfPoints; i++) {
                float x = radius * Mathf.Cos((i * twoPi / (float)numberOfPoints) + startAngleInRadians);
                float z = radius * Mathf.Sin((i * twoPi / (float)numberOfPoints) + startAngleInRadians);
                points[i] = new Vector3(x, 0F, z);
            }
            return points;
        }
    }
}


