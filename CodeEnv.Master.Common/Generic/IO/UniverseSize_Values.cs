// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSize_Values.cs
// Parses UniverseSize.xml used to provide externalized values for the UniverseSize enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Parses UniverseSize.xml used to provide externalized values for the UniverseSize enum.
    /// </summary>
    public sealed class UniverseSize_Values : AEnumValues<UniverseSize_Values> {

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
            set { _tinyRadius = value; }
        }

        private float _smallRadius;
        public float SmallRadius {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _smallRadius;
            }
            set { _smallRadius = value; }
        }

        private float _normalRadius;
        public float NormalRadius {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _normalRadius;
            }
            set { _normalRadius = value; }
        }

        private float _largeRadius;
        public float LargeRadius {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _largeRadius;
            }
            set { _largeRadius = value; }
        }

        private float _enormousRadius;
        public float EnormousRadius {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _enormousRadius;
            }
            set { _enormousRadius = value; }
        }

        private float _giganticRadius;
        public float GiganticRadius {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _giganticRadius;
            }
            set { _giganticRadius = value; }
        }

        private UniverseSize_Values() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

