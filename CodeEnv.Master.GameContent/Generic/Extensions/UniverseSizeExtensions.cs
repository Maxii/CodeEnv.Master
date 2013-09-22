// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSizeExtensions.cs
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
    public static class UniverseSizeExtensions {

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

            private UniverseSizeHelper() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }
        }

    }
}

