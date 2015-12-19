// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACameraItemStat.cs
// Abstract base stat for Items that can interact with the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base stat for Items that can interact with the camera.
    /// </summary>
    public abstract class ACameraItemStat {

        public float MinimumViewingDistance { get; private set; }

        public float FieldOfView { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ACameraItemStat" />.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="fov">The fov.</param>
        public ACameraItemStat(float minViewDistance, float fov) {
            Arguments.ValidateForRange(minViewDistance, Constants.ZeroF, Mathf.Infinity);
            Arguments.ValidateForRange(fov, Constants.ZeroF, 180F);
            MinimumViewingDistance = minViewDistance;
            FieldOfView = fov;
        }

    }
}

