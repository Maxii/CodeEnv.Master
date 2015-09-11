// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpeedExtensions.cs
// Extension methods for GameClockSpeed values.
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
    /// Extension methods for GameClockSpeed values.
    /// </summary>
    public static class GameSpeedExtensions {

        private static GameSpeedXmlPropertyReader _xmlReader = GameSpeedXmlPropertyReader.Instance;

        public static float SpeedMultiplier(this GameSpeed gameSpeed) {
            switch (gameSpeed) {
                case GameSpeed.Slowest:
                    return _xmlReader.SlowestMultiplier;
                case GameSpeed.Slow:
                    return _xmlReader.SlowMultiplier;
                case GameSpeed.Normal:
                    return _xmlReader.NormalMultiplier;
                case GameSpeed.Fast:
                    return _xmlReader.FastMultiplier;
                case GameSpeed.Fastest:
                    return _xmlReader.FastestMultiplier;
                case GameSpeed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameSpeed));
            }
        }

        #region Nested Classes

        /// <summary>
        /// Parses GameSpeed.xml used to provide externalized values for the GameSpeed enum.
        /// </summary>
        private sealed class GameSpeedXmlPropertyReader : AEnumXmlPropertyReader<GameSpeedXmlPropertyReader> {

            private float _slowestMultiplier;
            public float SlowestMultiplier {
                get {
                    CheckValuesInitialized();
                    return _slowestMultiplier;
                }
                private set { _slowestMultiplier = value; }
            }

            private float _slowMultiplier;
            public float SlowMultiplier {
                get {
                    CheckValuesInitialized();
                    return _slowMultiplier;
                }
                private set { _slowMultiplier = value; }
            }

            private float _normalMultiplier;
            public float NormalMultiplier {
                get {
                    CheckValuesInitialized();
                    return _normalMultiplier;
                }
                private set { _normalMultiplier = value; }
            }

            private float _fastMultiplier;
            public float FastMultiplier {
                get {
                    CheckValuesInitialized();
                    return _fastMultiplier;
                }
                private set { _fastMultiplier = value; }
            }

            private float _fastestMultiplier;
            public float FastestMultiplier {
                get {
                    CheckValuesInitialized();
                    return _fastestMultiplier;
                }
                private set { _fastestMultiplier = value; }
            }

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(GameSpeed); } }

            private GameSpeedXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}

