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
            UniverseSizeHelper values = UniverseSizeHelper.Instance;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return values.TinyRadius;
                case UniverseSize.Small:
                    return values.SmallRadius;
                case UniverseSize.Normal:
                    return values.NormalRadius;
                case UniverseSize.Large:
                    return values.LargeRadius;
                case UniverseSize.Enormous:
                    return values.EnormousRadius;
                case UniverseSize.Gigantic:
                    return values.GiganticRadius;
                //case UniverseSize.Random:
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }

        public static int DefaultAIPlayerCount(this UniverseSize universeSize) {
            UniverseSizeHelper values = UniverseSizeHelper.Instance;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return values.TinyDefaultAIPlayerCount;
                case UniverseSize.Small:
                    return values.SmallDefaultAIPlayerCount;
                case UniverseSize.Normal:
                    return values.NormalDefaultAIPlayerCount;
                case UniverseSize.Large:
                    return values.LargeDefaultAIPlayerCount;
                case UniverseSize.Enormous:
                    return values.EnormousDefaultAIPlayerCount;
                case UniverseSize.Gigantic:
                    return values.GiganticDefaultAIPlayerCount;
                //case UniverseSize.Random:
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }


        /// <summary>
        /// Parses UniverseSize.xml used to provide externalized values for the UniverseSize enum.
        /// </summary>
        public sealed class UniverseSizeHelper : AEnumValuesHelper<UniverseSizeHelper> {

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType {
                get {
                    return typeof(UniverseSize);
                }
            }

            #region Universe Radius

            private float _tinyRadius;
            public float TinyRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _tinyRadius;
                }
                private set { _tinyRadius = value; }
            }

            private float _smallRadius;
            public float SmallRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _smallRadius;
                }
                private set { _smallRadius = value; }
            }

            private float _normalRadius;
            public float NormalRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _normalRadius;
                }
                private set { _normalRadius = value; }
            }

            private float _largeRadius;
            public float LargeRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _largeRadius;
                }
                private set { _largeRadius = value; }
            }

            private float _enormousRadius;
            public float EnormousRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _enormousRadius;
                }
                private set { _enormousRadius = value; }
            }

            private float _giganticRadius;
            public float GiganticRadius {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _giganticRadius;
                }
                private set { _giganticRadius = value; }
            }

            #endregion

            #region Universe Default AI Player Count

            private int _tinyDefaultAIPlayerCount;
            public int TinyDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _tinyDefaultAIPlayerCount;
                }
                private set { _tinyDefaultAIPlayerCount = value; }
            }

            private int _smallDefaultAIPlayerCount;
            public int SmallDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _smallDefaultAIPlayerCount;
                }
                private set { _smallDefaultAIPlayerCount = value; }
            }

            private int _normalDefaultAIPlayerCount;
            public int NormalDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _normalDefaultAIPlayerCount;
                }
                private set { _normalDefaultAIPlayerCount = value; }
            }

            private int _largeDefaultAIPlayerCount;
            public int LargeDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _largeDefaultAIPlayerCount;
                }
                private set { _largeDefaultAIPlayerCount = value; }
            }

            private int _enormousDefaultAIPlayerCount;
            public int EnormousDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _enormousDefaultAIPlayerCount;
                }
                private set { _enormousDefaultAIPlayerCount = value; }
            }

            private int _giganticDefaultAIPlayerCount;
            public int GiganticDefaultAIPlayerCount {
                get {
                    if (!isPropertyValuesInitialized) {
                        InitializePropertyValues();
                    }
                    return _giganticDefaultAIPlayerCount;
                }
                private set { _giganticDefaultAIPlayerCount = value; }
            }

            #endregion

            private UniverseSizeHelper() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }
        }

    }
}

