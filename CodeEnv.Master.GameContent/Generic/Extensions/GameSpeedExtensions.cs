// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpeedExtensions.cs
// Extension class providing values acquired externally from Xml for the GameClockSpeed enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Extension class providing values acquired externally from Xml for the GameClockSpeed enum.
    /// </summary>
    public static class GameSpeedExtensions {

        public static float SpeedMultiplier(this GameClockSpeed gameSpeed) {
            GameClockSpeedHelper values = GameClockSpeedHelper.Instance;
            switch (gameSpeed) {
                case GameClockSpeed.Slowest:
                    return values.SlowestMultiplier;
                case GameClockSpeed.Slow:
                    return values.SlowMultiplier;
                case GameClockSpeed.Normal:
                    return values.NormalMultiplier;
                case GameClockSpeed.Fast:
                    return values.FastMultiplier;
                case GameClockSpeed.Fastest:
                    return values.FastestMultiplier;
                case GameClockSpeed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameSpeed));
            }
        }

        /// <summary>
        /// Parses GameClockSpeed.xml used to provide externalized values for the GameClockSpeed enum.
        /// </summary>
        public sealed class GameClockSpeedHelper : AEnumValuesHelper<GameClockSpeedHelper> {

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
                private set { _slowestMultiplier = value; }
            }

            private float _slowMultiplier;
            public float SlowMultiplier {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _slowMultiplier;
                }
                private set { _slowMultiplier = value; }
            }

            private float _normalMultiplier;
            public float NormalMultiplier {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _normalMultiplier;
                }
                private set { _normalMultiplier = value; }
            }

            private float _fastMultiplier;
            public float FastMultiplier {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _fastMultiplier;
                }
                private set { _fastMultiplier = value; }
            }

            private float _fastestMultiplier;
            public float FastestMultiplier {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _fastestMultiplier;
                }
                private set { _fastestMultiplier = value; }
            }

            private GameClockSpeedHelper() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

    }
}

