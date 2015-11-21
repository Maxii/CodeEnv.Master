// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFollowableStat.cs
// Camera settings for ICameraFollowable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera settings for ICameraFollowable Items.
    /// </summary>
    public class CameraFollowableStat : CameraFocusableStat {

        public float FollowDistanceDampener { get; private set; }

        public float FollowRotationDampener { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraFollowableStat"/> class.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistance">The opt view distance.</param>
        /// <param name="fov">The fov.</param>
        /// <param name="followDistanceDampener">The follow distance dampener. Default is 3F.</param>
        /// <param name="followRotationDampener">The follow rotation dampener. Default is 1F.</param>
        public CameraFollowableStat(float minViewDistance, float optViewDistance, float fov, float followDistanceDampener = 3F, float followRotationDampener = 1F)
            : base(minViewDistance, optViewDistance, fov) {
            FollowDistanceDampener = followDistanceDampener;
            FollowRotationDampener = followRotationDampener;
            Validate();
        }

        private void Validate() {
            D.Assert(FollowDistanceDampener > Constants.OneF);
            D.Assert(FollowRotationDampener > Constants.ZeroF);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

