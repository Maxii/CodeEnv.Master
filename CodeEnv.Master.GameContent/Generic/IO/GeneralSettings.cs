// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GeneralSettings.cs
// Parses GeneralSettings.xml providing externalized values to the Properties.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Parses GeneralSettings.xml providing externalized values to the Properties.
    /// </summary>
    public sealed class GeneralSettings : AValuesHelper<GeneralSettings> {

        #region Boolean

        #endregion

        #region Integer

        private int _startingYear;
        public int StartingYear {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _startingYear;
            }
            private set { _startingYear = value; }
        }

        private int _daysPerYear;
        public int DaysPerYear {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _daysPerYear;
            }
            private set { _daysPerYear = value; }
        }

        #endregion

        #region Float

        private float _daysPerSecond;
        public float DaysPerSecond {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _daysPerSecond;
            }
            private set { _daysPerSecond = value; }
        }

        private float _injuredHealthThreshold;
        public float InjuredHealthThreshold {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _injuredHealthThreshold;
            }
            private set { _injuredHealthThreshold = value; }
        }

        private float _criticalHealthThreshold;
        public float CriticalHealthThreshold {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _criticalHealthThreshold;
            }
            private set { _criticalHealthThreshold = value; }
        }

        private float _hudRefreshRate;
        /// <summary>
        /// The base rate at which the HUD refreshes constantly changing data.
        /// UOM = seconds between each refresh.
        /// </summary>
        public float HudRefreshRate {
            get {
                if (!isPropertyValuesInitialized) {
                    InitializePropertyValues();
                }
                return _hudRefreshRate;
            }
            private set { _hudRefreshRate = value; }
        }

        #endregion


        private GeneralSettings() {
            Initialize();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

