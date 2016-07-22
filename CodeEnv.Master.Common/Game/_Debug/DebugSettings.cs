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
    public sealed class DebugSettings : AXmlPropertyReader<DebugSettings> {

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

        private bool _disableGui;
        public bool DisableGui {
            get {
                CheckValuesInitialized();
                return _disableGui;
            }
            private set { _disableGui = value; }
        }

        private bool _disableRepair;
        /// <summary>
        /// If <c>true</c> disables Repairing, overriding RepairAnyDamage.
        /// </summary>
        public bool DisableRepair {
            get {
                CheckValuesInitialized();
                return _disableRepair;
            }
            private set { _disableRepair = value; }
        }

        private bool _repairAnyDamage;
        /// <summary>
        /// If <c>true</c> any damage incurred initiates Repairing. Effectively
        /// ignores the damageThreshold provided to AssessNeedForRepair(damageThreshold).
        /// If DisableRepair is <c>true</c>, this setting is ignored.
        /// </summary>
        public bool RepairAnyDamage {
            get {
                CheckValuesInitialized();
                return _repairAnyDamage;
            }
            private set { _repairAnyDamage = value; }
        }

        private bool _disableRetreat;
        public bool DisableRetreat {
            get {
                CheckValuesInitialized();
                return _disableRetreat;
            }
            private set { _disableRetreat = value; }
        }

        private bool _allPlayersInvulnerable;
        public bool AllPlayersInvulnerable {
            get {
                CheckValuesInitialized();
                return _allPlayersInvulnerable;
            }
            private set { _allPlayersInvulnerable = value; }
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

        private bool _enableCombatResultLogging;

        /// <summary>
        /// Controls whether weapons will log CombatResults
        /// to the Console.
        /// </summary>
        public bool EnableCombatResultLogging {
            get {
                CheckValuesInitialized();
                return _enableCombatResultLogging;
            }
            private set { _enableCombatResultLogging = value; }
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

        private bool _allIntelCoverageComprehensive;
        /// <summary>
        /// Determines whether the initial IntelCoverage for each player about each Item
        /// in the Universe starts at its default or at Comprehensive. Starting at default
        /// means, in general, IntelCoverage of an item can improve. Starting at Comprehensive
        /// means no improvement possible.
        /// <remarks>If <c>true</c> every player knows everything about every item they detect. 
        /// It DOES NOT MEAN that they have detected everything or that players have met yet.
        /// Players meet when they first detect a HQ Element owned by another player.</remarks>
        /// </summary>
        public bool AllIntelCoverageComprehensive {
            get {
                CheckValuesInitialized();
                return _allIntelCoverageComprehensive;
            }
            private set { _allIntelCoverageComprehensive = value; }
        }

        private bool _enableStartupTimeout;
        public bool EnableStartupTimeout {
            get {
                CheckValuesInitialized();
                return _enableStartupTimeout;
            }
            private set { _enableStartupTimeout = value; }
        }

        private DebugSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

