// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AnimationSettings.cs
// Parses AnimationSettings.xml providing externalized values to the Properties.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Parses AnimationSettings.xml providing externalized values to the Properties.
    /// </summary>
    public sealed class AnimationSettings : AValuesHelper<AnimationSettings> {

        private int _cameraDistanceThresholdFactor_3dAnimationsTo3dDisplayMode;
        /// <summary>
        /// The factor to use in generating the distance (in sectors) from the camera where a View
        /// makes the Display mode transition from showing a 3D representation of
        /// the object with Animations running to a 3D representation without animations.
        /// </summary>
        public int CameraDistanceThresholdFactor_3dAnimationsTo3dDisplayMode {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _cameraDistanceThresholdFactor_3dAnimationsTo3dDisplayMode;
            }
            private set { _cameraDistanceThresholdFactor_3dAnimationsTo3dDisplayMode = value; }
        }


        private int _cameraDistanceThresholdFactor_3dTo2dDisplayMode;
        /// <summary>
        /// The factor to use in generating the distance (in sectors) from the camera where a View
        /// makes the Display mode transition from showing a 3D representation of
        /// the object to a 2D representation.
        /// </summary>
        public int CameraDistanceThresholdFactor_3dTo2dDisplayMode {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _cameraDistanceThresholdFactor_3dTo2dDisplayMode;
            }
            private set { _cameraDistanceThresholdFactor_3dTo2dDisplayMode = value; }
        }


        private int _cameraDistanceThresholdFactor_2dToNoneDisplayMode;
        /// <summary>
        /// The factor to use in generating the distance (in sectors) from the camera where a View
        /// makes the Display mode transition from showing a 2D representation of
        /// the object to not showing it at all.
        /// </summary>
        public int CameraDistanceThresholdFactor_2dToNoneDisplayMode {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _cameraDistanceThresholdFactor_2dToNoneDisplayMode;
            }
            private set { _cameraDistanceThresholdFactor_2dToNoneDisplayMode = value; }
        }


        private AnimationSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

