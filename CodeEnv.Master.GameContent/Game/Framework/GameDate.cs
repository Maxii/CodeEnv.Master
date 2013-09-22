// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameDate.cs
// Data container class that holds the game date.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using System;

    /// <summary>
    /// Data container class that holds the game date.
    /// </summary>
    public class GameDate : IGameDate {

        private static int _daysPerYear = GeneralSettings.Instance.DaysPerYear;
        private static float _daysPerSecond = GeneralSettings.Instance.DaysPerSecond;
        private static int _startingYear = GeneralSettings.Instance.StartingYear;

        public int DayOfYear { get; private set; }

        public int Year { get; private set; }

        public string FormattedDate {
            get {
                return Constants.GameDateFormat.Inject(Year, DayOfYear);
            }
        }

        /// <summary>
        /// Convenience constructor that initializes a new instance of the <see cref="GameDate"/> class
        /// set to Day 1 of the starting year.
        /// </summary>
        public GameDate()
            : this(Constants.One, _startingYear) {
        }

        public GameDate(int dayOfYear, int year) {
            Arguments.ValidateNotNegative(dayOfYear);
            DayOfYear = dayOfYear;
            Year = year;
        }

        internal void SyncDateToGameClock(float gameClock) {
            int elapsedDays = Mathf.FloorToInt(gameClock * _daysPerSecond);
            Year = _startingYear + Mathf.FloorToInt(elapsedDays / _daysPerYear);
            DayOfYear = 1 + (elapsedDays % _daysPerYear);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

