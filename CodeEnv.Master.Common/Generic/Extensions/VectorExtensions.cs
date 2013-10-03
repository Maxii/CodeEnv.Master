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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
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
        public static bool IsSameDirection(this Vector3 source, Vector3 v) {
            return Vector3.Angle(source, v) < .01F;
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

        public static void SetX(this Transform transform, float x) {
            Vector3 newPosition = new Vector3(x, transform.position.y, transform.position.z);
            transform.position = newPosition;
        }

        public static void SetY(this Transform transform, float y) {
            Vector3 newPosition = new Vector3(transform.position.x, y, transform.position.z);
            transform.position = newPosition;
        }

        public static void SetZ(this Transform transform, float z) {
            Vector3 newPosition = new Vector3(transform.position.x, transform.position.y, z);
            transform.position = newPosition;
        }

    }
}

