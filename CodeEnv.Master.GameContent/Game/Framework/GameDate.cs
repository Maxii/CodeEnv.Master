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

        public static int DaysPerYear = GeneralSettings.Instance.DaysPerYear;
        public static float DaysPerSecond = GeneralSettings.Instance.DaysPerSecond;
        public static int StartingYear = GeneralSettings.Instance.StartingYear;

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
            : this(Constants.One, StartingYear) {
        }

        public GameDate(int dayOfYear, int year) {
            Arguments.ValidateNotNegative(dayOfYear);
            DayOfYear = dayOfYear;
            Year = year;
        }

        internal void SyncDateToGameClock(float gameClock) {
            int elapsedDays = Mathf.FloorToInt(gameClock * DaysPerSecond);
            Year = StartingYear + Mathf.FloorToInt(elapsedDays / DaysPerYear);
            DayOfYear = 1 + (elapsedDays % DaysPerYear);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

