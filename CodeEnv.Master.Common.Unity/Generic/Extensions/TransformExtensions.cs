// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TransformExtensions.cs
// Unity Transform extensions for more convenient syntax.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// TODO 
    /// </summary>
    public static class TransformExtensions {

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


