﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FollowableItemCameraStat.cs
// Camera stat for ICameraFollowable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera stat for ICameraFollowable Items.
    /// </summary>
    public class FollowableItemCameraStat : FocusableItemCameraStat {

        public float FollowDistanceDamper { get; private set; }

        public float FollowRotationDamper { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FollowableItemCameraStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistance">The opt view distance.</param>
        /// <param name="fov">The field of view.</param>
        /// <param name="followDistanceDamper">The follow distance damper. Default is 3F.</param>
        /// <param name="followRotationDamper">The follow rotation damper. Default is 1F.</param>
        public FollowableItemCameraStat(float minViewDistance, float optViewDistance, float fov, float followDistanceDamper = 3F,
            float followRotationDamper = 1F)
            : base(minViewDistance, optViewDistance, fov) {
            D.Assert(followDistanceDamper > Constants.OneF);
            D.Assert(followRotationDamper > Constants.ZeroF);
            FollowDistanceDamper = followDistanceDamper;
            FollowRotationDamper = followRotationDamper;
        }

    }
}

