// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSizeExtensions.cs
// Extension methods for UniverseSize and UniverseSizeGuiSelection values.
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
    /// Extension methods for UniverseSize and UniverseSizeGuiSelection values.
    /// </summary>
    public static class UniverseSizeExtensions {

        private static UniverseSizeXmlPropertyReader _xmlReader = UniverseSizeXmlPropertyReader.Instance;

        /// <summary>
        /// Converts this UniverseSizeGuiSelection value to a UniverseSize value.
        /// </summary>
        /// <param name="universeSizeSelection">The universe size GUI selection.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static UniverseSize Convert(this UniverseSizeGuiSelection universeSizeSelection) {
            switch (universeSizeSelection) {
                case UniverseSizeGuiSelection.Random:
                    return Enums<UniverseSize>.GetRandom(excludeDefault: true);
                case UniverseSizeGuiSelection.Tiny:
                    return UniverseSize.Tiny;
                case UniverseSizeGuiSelection.Small:
                    return UniverseSize.Small;
                case UniverseSizeGuiSelection.Normal:
                    return UniverseSize.Normal;
                case UniverseSizeGuiSelection.Large:
                    return UniverseSize.Large;
                case UniverseSizeGuiSelection.Enormous:
                    return UniverseSize.Enormous;
                case UniverseSizeGuiSelection.Gigantic:
                    return UniverseSize.Gigantic;
                case UniverseSizeGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSizeSelection));
            }
        }

        public static float Radius(this UniverseSize universeSize) {
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return _xmlReader.TinyRadius;
                case UniverseSize.Small:
                    return _xmlReader.SmallRadius;
                case UniverseSize.Normal:
                    return _xmlReader.NormalRadius;
                case UniverseSize.Large:
                    return _xmlReader.LargeRadius;
                case UniverseSize.Enormous:
                    return _xmlReader.EnormousRadius;
                case UniverseSize.Gigantic:
                    return _xmlReader.GiganticRadius;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }

        public static int DefaultPlayerCount(this UniverseSize universeSize) {
            int defaultPlayerCount;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    defaultPlayerCount = _xmlReader.TinyDefaultPlayerCount;
                    break;
                case UniverseSize.Small:
                    defaultPlayerCount = _xmlReader.SmallDefaultPlayerCount;
                    break;
                case UniverseSize.Normal:
                    defaultPlayerCount = _xmlReader.NormalDefaultPlayerCount;
                    break;
                case UniverseSize.Large:
                    defaultPlayerCount = _xmlReader.LargeDefaultPlayerCount;
                    break;
                case UniverseSize.Enormous:
                    defaultPlayerCount = _xmlReader.EnormousDefaultPlayerCount;
                    break;
                case UniverseSize.Gigantic:
                    defaultPlayerCount = _xmlReader.GiganticDefaultPlayerCount;
                    break;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
            return defaultPlayerCount;
        }


        #region Nested Classes

        /// <summary>
        /// Parses UniverseSize.xml used to provide externalized values for the UniverseSize enum.
        /// </summary>
        private sealed class UniverseSizeXmlPropertyReader : AEnumXmlPropertyReader<UniverseSizeXmlPropertyReader> {

            #region Universe Radius

            private float _tinyRadius;
            public float TinyRadius {
                get {
                    CheckValuesInitialized();
                    return _tinyRadius;
                }
                private set { _tinyRadius = value; }
            }

            private float _smallRadius;
            public float SmallRadius {
                get {
                    CheckValuesInitialized();
                    return _smallRadius;
                }
                private set { _smallRadius = value; }
            }

            private float _normalRadius;
            public float NormalRadius {
                get {
                    CheckValuesInitialized();
                    return _normalRadius;
                }
                private set { _normalRadius = value; }
            }

            private float _largeRadius;
            public float LargeRadius {
                get {
                    CheckValuesInitialized();
                    return _largeRadius;
                }
                private set { _largeRadius = value; }
            }

            private float _enormousRadius;
            public float EnormousRadius {
                get {
                    CheckValuesInitialized();
                    return _enormousRadius;
                }
                private set { _enormousRadius = value; }
            }

            private float _giganticRadius;
            public float GiganticRadius {
                get {
                    CheckValuesInitialized();
                    return _giganticRadius;
                }
                private set { _giganticRadius = value; }
            }

            #endregion

            #region Universe Default Player Count

            private int _tinyDefaultPlayerCount;
            public int TinyDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _tinyDefaultPlayerCount;
                }
                private set { _tinyDefaultPlayerCount = value; }
            }

            private int _smallDefaultPlayerCount;
            public int SmallDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _smallDefaultPlayerCount;
                }
                private set { _smallDefaultPlayerCount = value; }
            }

            private int _normalDefaultPlayerCount;
            public int NormalDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _normalDefaultPlayerCount;
                }
                private set { _normalDefaultPlayerCount = value; }
            }

            private int _largeDefaultPlayerCount;
            public int LargeDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _largeDefaultPlayerCount;
                }
                private set { _largeDefaultPlayerCount = value; }
            }

            private int _enormousDefaultPlayerCount;
            public int EnormousDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _enormousDefaultPlayerCount;
                }
                private set { _enormousDefaultPlayerCount = value; }
            }

            private int _giganticDefaultPlayerCount;
            public int GiganticDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _giganticDefaultPlayerCount;
                }
                private set { _giganticDefaultPlayerCount = value; }
            }

            #endregion

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(UniverseSize); } }

            private UniverseSizeXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }
        }

        #endregion

    }
}

