// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSettings.cs
// Parses DebugSettings.xml used to provide externalized values to DebugSettings.cs Properties.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Parses DebugSettings.xml used to provide externalized values to DebugSettings.cs Properties.
    /// </summary>
    public sealed class DebugSettings : AValuesHelper<DebugSettings> {

        private bool _enableFpsReadout;
        /// <summary>
        /// Gets a value indicating whether the Frames per 
        /// Second Readout display will show on the screen.
        /// </summary>
        public bool EnableFpsReadout {
            get {
                CheckValuesInitialized();
                return _enableFpsReadout;
            }
            private set { _enableFpsReadout = value; }
        }

        private bool _unlockAllItems;
        public bool UnlockAllItems {
            get {
                CheckValuesInitialized();
                return _unlockAllItems;
            }
            private set { _unlockAllItems = value; }
        }

        private bool _disableEnemies;
        public bool DisableEnemies {
            get {
                CheckValuesInitialized();
                return _disableEnemies;
            }
            private set { _disableEnemies = value; }
        }

        private bool _disableGui;
        public bool DisableGui {
            get {
                CheckValuesInitialized();
                return _disableGui;
            }
            private set { _disableGui = value; }
        }

        private bool _allPlayersInvulnerable;
        public bool AllPlayersInvulnerable {
            get {
                CheckValuesInitialized();
                return _allPlayersInvulnerable;
            }
            private set { _allPlayersInvulnerable = value; }
        }

        private bool _disableAllGameplay;
        public bool DisableAllGameplay {
            get {
                CheckValuesInitialized();
                return _disableAllGameplay;
            }
            private set { _disableAllGameplay = value; }
        }

        private bool _forceFpsToTarget;
        /// <summary>
        /// Forces the game to run at the target FPS. Other QualitySettings
        /// remain the same.
        /// </summary>
        /// <value>
        /// <c>true</c> if [restrict FPS automatic target]; otherwise, <c>false</c>.
        /// </value>
        public bool ForceFpsToTarget {
            get {
                CheckValuesInitialized();
                return _forceFpsToTarget;
            }
            private set { _forceFpsToTarget = value; }
        }

        private bool _enableEventLogging;
        /// <summary>
        /// Controls whether LogEvent() will print debug messages
        /// for MonoBehaviour methods containing LogEvent();.
        /// </summary>
        public bool EnableEventLogging {
            get {
                CheckValuesInitialized();
                return _enableEventLogging;
            }
            private set { _enableEventLogging = value; }
        }

        private bool _enableVerboseDebugLog;
        /// <summary>
        /// Debug readouts (console, etc.) will be comprehensive
        /// in their display of data. e.g. ObjectAnalyzer.ToString().
        /// </summary>
        public bool EnableVerboseDebugLog {
            get {
                CheckValuesInitialized();
                return _enableVerboseDebugLog;
            }
            private set { _enableVerboseDebugLog = value; }
        }

        private bool _enableShipVelocityRays;
        public bool EnableShipVelocityRays {
            get {
                CheckValuesInitialized();
                return _enableShipVelocityRays;
            }
            private set { _enableShipVelocityRays = value; }
        }

        private bool _enableFleetVelocityRays;
        public bool EnableFleetVelocityRays {
            get {
                CheckValuesInitialized();
                return _enableFleetVelocityRays;
            }
            private set { _enableFleetVelocityRays = value; }
        }

        private bool _enableFleetCourseDisplay;
        public bool EnableFleetCourseDisplay {
            get {
                CheckValuesInitialized();
                return _enableFleetCourseDisplay;
            }
            private set { _enableFleetCourseDisplay = value; }
        }

        private bool _enableShipCourseDisplay;
        public bool EnableShipCourseDisplay {
            get {
                CheckValuesInitialized();
                return _enableShipCourseDisplay;
            }
            private set { _enableShipCourseDisplay = value; }
        }

        //private bool _stopShipMovement;
        //public bool StopShipMovement {
        //    get {
        //        if (!isPropertyValuesInitialized) {
        //            InitializePropertyValues();
        //        }
        //        return _stopShipMovement;
        //    }
        //    private set { _stopShipMovement = value; }
        //}

        private bool _allowEnemyOrders;
        public bool AllowEnemyOrders {
            get {
                CheckValuesInitialized();
                return _allowEnemyOrders;
            }
            private set { _allowEnemyOrders = value; }
        }

        private bool _allIntelCoverageComprehensive;
        public bool AllIntelCoverageComprehensive {
            get {
                CheckValuesInitialized();
                return _allIntelCoverageComprehensive;
            }
            private set { _allIntelCoverageComprehensive = value; }
        }

        private DebugSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

