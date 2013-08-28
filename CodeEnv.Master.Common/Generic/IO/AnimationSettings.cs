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

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Parses AnimationSettings.xml providing externalized values to the Properties.
    /// </summary>
    public sealed class AnimationSettings : AValuesHelper<AnimationSettings> {

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

        private int _maxShipAnimateDistance;
        public int MaxShipAnimateDistance {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxShipAnimateDistance;
            }
            private set { _maxShipAnimateDistance = value; }
        }

        private int _maxShipShowDistance;
        public int MaxShipShowDistance {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _maxShipShowDistance;
            }
            private set { _maxShipShowDistance = value; }
        }

        private AnimationSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

