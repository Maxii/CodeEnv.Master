// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFocusableStat.cs
// Camera stat for ICameraFocusable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera stat for ICameraFocusable Items.
    /// </summary>
    public class CameraFocusableStat : ACameraItemStat {

        public float OptimalViewingDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraFocusableStat" />.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistance">The opt view distance.</param>
        /// <param name="fov">The fov.</param>
        public CameraFocusableStat(float minViewDistance, float optViewDistance, float fov)
            : base(minViewDistance, fov) {
            D.Assert(optViewDistance >= MinimumViewingDistance);
            OptimalViewingDistance = optViewDistance;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

