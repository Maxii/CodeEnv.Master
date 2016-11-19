// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemCameraStat.cs
// Abstract base stat for Items that can interact with the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base stat for Items that can interact with the camera.
    /// </summary>
    public abstract class AItemCameraStat {

        public float MinimumViewingDistance { get; private set; }

        public float FieldOfView { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemCameraStat" />.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="fov">The field of view.</param>
        public AItemCameraStat(float minViewDistance, float fov) {
            Utility.ValidateForRange(minViewDistance, Constants.ZeroF, Mathf.Infinity);
            Utility.ValidateForRange(fov, Constants.ZeroF, 180F);
            MinimumViewingDistance = minViewDistance;
            FieldOfView = fov;
        }

    }
}

