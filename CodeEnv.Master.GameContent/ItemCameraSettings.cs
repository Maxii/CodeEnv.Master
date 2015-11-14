// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemCameraSettings.cs
// Camera settings for an Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Camera settings for an Item.
    /// </summary>
    public struct ItemCameraSettings {

        public float ItemRadius { get; private set; }

        public float MinimumViewingDistance { get { return ItemRadius * _minViewDistanceMultiplier; } }

        public float NearClipPlaneDistance { get { return MinimumViewingDistance - ItemRadius; } }

        public float OptimalViewingDistance { get { return ItemRadius * _optViewDistanceMultiplier; } }

        public float FieldOfView { get; private set; }

        public float FollowDistanceDampener { get; private set; }

        public float FollowRotationDampener { get; private set; }

        private float _minViewDistanceMultiplier;

        private float _optViewDistanceMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemCameraSettings"/> struct for a ICameraFocusable Item.
        /// </summary>
        /// <param name="itemRadius">The item radius.</param>
        /// <param name="minViewDistanceMultiplier">The minimum view distance multiplier.</param>
        /// <param name="optViewDistanceMultiplier">The opt view distance multiplier.</param>
        /// <param name="fov">The fov.</param>
        public ItemCameraSettings(float itemRadius, float minViewDistanceMultiplier, float optViewDistanceMultiplier, float fov)
            : this(itemRadius, minViewDistanceMultiplier, optViewDistanceMultiplier, fov, Constants.ZeroF, Constants.ZeroF) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemCameraSettings"/> struct for a ICameraFollowable Item.
        /// </summary>
        /// <param name="itemRadius">The item radius.</param>
        /// <param name="minViewDistanceMultiplier">The minimum view distance multiplier.</param>
        /// <param name="optViewDistanceMultiplier">The opt view distance multiplier.</param>
        /// <param name="fov">The fov.</param>
        /// <param name="followDistanceDampener">The follow distance dampener.</param>
        /// <param name="followRotationDampener">The follow rotation dampener.</param>
        public ItemCameraSettings(float itemRadius, float minViewDistanceMultiplier, float optViewDistanceMultiplier, float fov, float followDistanceDampener, float followRotationDampener)
            : this() {
            ItemRadius = itemRadius;
            _minViewDistanceMultiplier = minViewDistanceMultiplier;
            _optViewDistanceMultiplier = optViewDistanceMultiplier;
            FieldOfView = fov;
            FollowDistanceDampener = followDistanceDampener;
            FollowRotationDampener = followRotationDampener;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

