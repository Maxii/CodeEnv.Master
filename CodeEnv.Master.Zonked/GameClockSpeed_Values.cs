// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameClockSpeed_Values.cs
// Parses GameClockSpeed.xml used to provide externalized values for the GameClockSpeed enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Parses GameClockSpeed.xml used to provide externalized values for the GameClockSpeed enum.
    /// </summary>
    public sealed class GameClockSpeed_Values : AEnumXmlPropertyReader<GameClockSpeed_Values> {

        /// <summary>
        /// The type of the enum being supported by this class.
        /// </summary>
        protected override Type EnumType {
            get {
                return typeof(GameSpeed);
            }
        }

        private float _slowestMultiplier;
        public float SlowestMultiplier {
            get {
                if (!_isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _slowestMultiplier;
            }
            private set { _slowestMultiplier = value; }
        }

        private float _slowMultiplier;
        public float SlowMultiplier {
            get {
                if (!_isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _slowMultiplier;
            }
            private set { _slowMultiplier = value; }
        }

        private float _normalMultiplier;
        public float NormalMultiplier {
            get {
                if (!_isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _normalMultiplier;
            }
            private set { _normalMultiplier = value; }
        }

        private float _fastMultiplier;
        public float FastMultiplier {
            get {
                if (!_isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _fastMultiplier;
            }
            private set { _fastMultiplier = value; }
        }

        private float _fastestMultiplier;
        public float FastestMultiplier {
            get {
                if (!_isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _fastestMultiplier;
            }
            private set { _fastestMultiplier = value; }
        }

        private GameClockSpeed_Values() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


