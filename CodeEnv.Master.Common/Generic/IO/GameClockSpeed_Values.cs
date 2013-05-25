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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Parses GameClockSpeed.xml used to provide externalized values for the GameClockSpeed enum.
    /// </summary>
    public sealed class GameClockSpeed_Values : AEnumValues<GameClockSpeed_Values> {

        /// <summary>
        /// The type of the enum being supported by this class.
        /// </summary>
        protected override Type EnumType {
            get {
                return typeof(GameClockSpeed);
            }
        }

        private float _slowestMultiplier;
        public float SlowestMultiplier {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _slowestMultiplier;
            }
            set { _slowestMultiplier = value; }
        }

        private float _slowMultiplier;
        public float SlowMultiplier {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _slowMultiplier;
            }
            set { _slowMultiplier = value; }
        }

        private float _normalMultiplier;
        public float NormalMultiplier {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _normalMultiplier;
            }
            set { _normalMultiplier = value; }
        }

        private float _fastMultiplier;
        public float FastMultiplier {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _fastMultiplier;
            }
            set { _fastMultiplier = value; }
        }

        private float _fastestMultiplier;
        public float FastestMultiplier {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _fastestMultiplier;
            }
            set { _fastestMultiplier = value; }
        }

        private GameClockSpeed_Values() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


