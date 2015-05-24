// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GraphicsSettings.cs
// Parses GraphicsSettings.xml providing externalized values to the Properties.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Parses GraphicsSettings.xml providing externalized values to the Properties.
    /// </summary>
    public sealed class GraphicsSettings : AValuesHelper<GraphicsSettings> {

        private int _shipLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int ShipLayerCullingDistanceFactor {
            get {
                CheckValuesInitialized();
                return _shipLayerCullingDistanceFactor;
            }
            private set { _shipLayerCullingDistanceFactor = value; }
        }

        private int _facilityLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int FacilityLayerCullingDistanceFactor {
            get {
                CheckValuesInitialized();
                return _facilityLayerCullingDistanceFactor;
            }
            private set { _facilityLayerCullingDistanceFactor = value; }
        }

        private int _planetoidLayerCullingDistanceFactor;
        /// <summary>
        /// The multiplication factor to use in generating the farClipPlane distance for the named layer.
        /// </summary>
        public int PlanetoidLayerCullingDistanceFactor {
            get {
                CheckValuesInitialized();
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
                CheckValuesInitialized();
                return _starLayerCullingDistanceFactor;
            }
            private set { _starLayerCullingDistanceFactor = value; }
        }

        private GraphicsSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

