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

#define DEBUG_LEVEL_LOG
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
        private int daysPerYear = TempGameValues.DaysPerGameYear;
        private float gameDaysPerSecond = TempGameValues.GameDaysPerSecond;
        private int startingGameYear = TempGameValues.StartingGameYear;

        public int DayOfYear { get; internal set; }

        public int Year { get; internal set; }

        public string FormattedDate {
            get {
                return Constants.GameDateFormat.Inject(Year, DayOfYear);
            }
        }

        internal void SyncDateToGameClock(float gameClock) {
            int elapsedDays = Mathf.FloorToInt(gameClock * gameDaysPerSecond);
            Year = startingGameYear + Mathf.FloorToInt(elapsedDays / daysPerYear);
            DayOfYear = 1 + (elapsedDays % daysPerYear);
        }

        [Obsolete]
        internal GameDate Clone() {
            GameDate clone = new GameDate();
            clone.DayOfYear = this.DayOfYear;
            clone.Year = this.Year;
            return clone;
        }
    }

}

