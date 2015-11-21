// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFocusableStat.cs
// Camera settings for ICameraFocusable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Camera settings for ICameraFocusable Items.
    /// </summary>
    public class CameraFocusableStat {

        public float MinimumViewingDistance { get; private set; }

        public virtual float OptimalViewingDistance { get; private set; }

        public float FieldOfView { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraFocusableStat" />.
        /// </summary>
        /// <param name="minViewDistance">The minimum view distance.</param>
        /// <param name="optViewDistance">The opt view distance.</param>
        /// <param name="fov">The fov.</param>
        public CameraFocusableStat(float minViewDistance, float optViewDistance, float fov) {
            MinimumViewingDistance = minViewDistance;
            OptimalViewingDistance = optViewDistance;
            FieldOfView = fov;
            Validate();
        }

        private void Validate() {
            D.Assert(MinimumViewingDistance > Constants.ZeroF);
            ValidateOptimalViewingDistance();
            D.Assert(FieldOfView > Constants.ZeroF);
        }

        protected virtual void ValidateOptimalViewingDistance() {   // virtual to allow CameraFleetCmdStat to override
            D.Assert(OptimalViewingDistance > MinimumViewingDistance);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

