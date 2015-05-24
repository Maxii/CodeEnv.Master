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

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Parses GeneralSettings.xml providing externalized values to the Properties.
    /// </summary>
    public sealed class GeneralSettings : AValuesHelper<GeneralSettings> {

        #region Boolean

        #endregion

        #region Integer

        private int _gameStartYear;
        public int GameStartYear {
            get {
                CheckValuesInitialized();
                return _gameStartYear;
            }
            private set { _gameStartYear = value; }
        }

        private int _gameEndYear;
        public int GameEndYear {
            get {
                CheckValuesInitialized();
                return _gameEndYear;
            }
            private set { _gameEndYear = value; }
        }

        private int _daysPerYear;
        public int DaysPerYear {
            get {
                CheckValuesInitialized();
                return _daysPerYear;
            }
            private set { _daysPerYear = value; }
        }

        private int _hoursPerDay;
        public int HoursPerDay {
            get {
                CheckValuesInitialized();
                return _hoursPerDay;
            }
            private set { _hoursPerDay = value; }
        }


        #endregion

        #region Float

        private float _hoursPerSecond;
        /// <summary>
        /// The number of GameHours in a real-time Second at 
        /// a GameSpeedMultiplier of 1 (aka GameSpeed.Normal).
        /// </summary>
        public float HoursPerSecond {
            get {
                CheckValuesInitialized();
                return _hoursPerSecond;
            }
            private set { _hoursPerSecond = value; }
        }

        private float _injuredHealthThreshold;
        public float InjuredHealthThreshold {
            get {
                CheckValuesInitialized();
                return _injuredHealthThreshold;
            }
            private set { _injuredHealthThreshold = value; }
        }

        private float _criticalHealthThreshold;
        public float CriticalHealthThreshold {
            get {
                CheckValuesInitialized();
                return _criticalHealthThreshold;
            }
            private set { _criticalHealthThreshold = value; }
        }

        private float _contentApprovalThreshold;
        public float ContentApprovalThreshold {
            get {
                CheckValuesInitialized();
                return _contentApprovalThreshold;
            }
            private set { _contentApprovalThreshold = value; }
        }

        private float _unhappyApprovalThreshold;
        public float UnhappyApprovalThreshold {
            get {
                CheckValuesInitialized();
                return _unhappyApprovalThreshold;
            }
            private set { _unhappyApprovalThreshold = value; }
        }

        private float _revoltApprovalThreshold;
        public float RevoltApprovalThreshold {
            get {
                CheckValuesInitialized();
                return _revoltApprovalThreshold;
            }
            private set { _revoltApprovalThreshold = value; }
        }

        private float _hudRefreshRate;
        /// <summary>
        /// The base rate at which the HUD refreshes constantly changing data.
        /// UOM = seconds between each refresh.
        /// </summary>
        public float HudRefreshRate {
            get {
                CheckValuesInitialized();
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

