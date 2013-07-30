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

#define DEBUG_WARN
#define DEBUG_ERROR
#define DEBUG_LOG

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Parses DebugSettings.xml used to provide externalized values to DebugSettings.cs Properties.
    /// </summary>
    public sealed class DebugSettings : AValuesHelper<DebugSettings> {

        private bool _enableFpsReadout;
        public bool EnableFpsReadout {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _enableFpsReadout;
            }
            private set { _enableFpsReadout = value; }
        }

        private bool _unlockAllItems;
        public bool UnlockAllItems {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _unlockAllItems;
            }
            private set { _unlockAllItems = value; }
        }

        private bool _disableEnemies;
        public bool DisableEnemies {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _disableEnemies;
            }
            private set { _disableEnemies = value; }
        }

        private bool _disableGui;
        public bool DisableGui {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _disableGui;
            }
            private set { _disableGui = value; }
        }

        private bool _makePlayerInvincible;
        public bool MakePlayerInvincible {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _makePlayerInvincible;
            }
            private set { _makePlayerInvincible = value; }
        }

        private bool _disableAllGameplay;
        public bool DisableAllGameplay {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _disableAllGameplay;
            }
            private set { _disableAllGameplay = value; }
        }

        private DebugSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

