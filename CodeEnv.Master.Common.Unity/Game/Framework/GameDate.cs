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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using System;

    /// <summary>
    /// Data container class that holds the game date.
    /// </summary>
    public class GameDate : IGameDate {

        private static int daysPerYear = TempGameValues.DaysPerGameYear;
        private static float gameDaysPerSecond = TempGameValues.GameDaysPerSecond;
        private static int startingGameYear = TempGameValues.StartingGameYear;

        public int DayOfYear { get; private set; }

        public int Year { get; private set; }

        public string FormattedDate {
            get {
                return Constants.GameDateFormat.Inject(Year, DayOfYear);
            }
        }

        public GameDate(int dayOfYear, int year) {
            Arguments.ValidateNotNegative(dayOfYear);
            DayOfYear = dayOfYear;
            Year = year;
        }

        internal void SyncDateToGameClock(float gameClock) {
            int elapsedDays = Mathf.FloorToInt(gameClock * gameDaysPerSecond);
            Year = startingGameYear + Mathf.FloorToInt(elapsedDays / daysPerYear);
            DayOfYear = 1 + (elapsedDays % daysPerYear);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

