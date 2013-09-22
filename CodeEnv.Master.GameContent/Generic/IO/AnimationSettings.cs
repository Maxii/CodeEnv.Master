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

        private int _maxCelestialObjectAnimateDistanceFactor;
        public int MaxCelestialObjectAnimateDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxCelestialObjectAnimateDistanceFactor;
            }
            private set { _maxCelestialObjectAnimateDistanceFactor = value; }
        }

        private int _maxShipAnimateDistanceFactor;
        public int MaxShipAnimateDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxShipAnimateDistanceFactor;
            }
            private set { _maxShipAnimateDistanceFactor = value; }
        }


        private int _maxSystemAnimateDistance;
        public int MaxSystemAnimateDistance {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxSystemAnimateDistance;
            }
            private set { _maxSystemAnimateDistance = value; }
        }


        private int _maxShipShowDistanceFactor;
        public int MaxShipShowDistanceFactor {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxShipShowDistanceFactor;
            }
            private set { _maxShipShowDistanceFactor = value; }
        }

        private AnimationSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

