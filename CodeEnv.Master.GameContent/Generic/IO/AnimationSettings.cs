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

        private int _shipLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int ShipLayerCullingDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _shipLayerCullingDistanceFactor;
            }
            private set { _shipLayerCullingDistanceFactor = value; }
        }

        private int _starBaseSettlementLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int StarBaseSettlementLayerCullingDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _starBaseSettlementLayerCullingDistanceFactor;
            }
            private set { _starBaseSettlementLayerCullingDistanceFactor = value; }
        }

        private int _planetoidLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int PlanetoidLayerCullingDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _planetoidLayerCullingDistanceFactor;
            }
            private set { _planetoidLayerCullingDistanceFactor = value; }
        }

        private int _starLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int StarLayerCullingDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _starLayerCullingDistanceFactor;
            }
            private set { _starLayerCullingDistanceFactor = value; }
        }


        private AnimationSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

