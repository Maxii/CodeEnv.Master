// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameDate.cs
// Immutable, data container structure that holds the game date.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable, data container structure that holds the game date.
    /// </summary>
    public struct GameDate : IEquatable<GameDate> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator <(GameDate left, GameDate right) {
            if (left.year < right.year) { return true; }
            if (left.year == right.year) {
                if (left.dayOfYear < right.dayOfYear) { return true; }
                if (left.dayOfYear == right.dayOfYear) {
                    if (left.hourOfDay < right.hourOfDay) { return true; }
                }
            }
            return false;
        }

        public static bool operator <=(GameDate left, GameDate right) {
            if (left.year < right.year) { return true; }
            if (left.year == right.year) {
                if (left.dayOfYear < right.dayOfYear) { return true; }
                if (left.dayOfYear == right.dayOfYear) {
                    if (left.hourOfDay < right.hourOfDay) { return true; }
                    if (left.hourOfDay == right.hourOfDay) { return true; }
                }
            }
            return false;
        }

        public static bool operator >(GameDate left, GameDate right) {
            if (left.year > right.year) { return true; }
            if (left.year == right.year) {
                if (left.dayOfYear > right.dayOfYear) { return true; }
                if (left.dayOfYear == right.dayOfYear) {
                    if (left.hourOfDay > right.hourOfDay) { return true; }
                }
            }
            return false;
        }

        public static bool operator >=(GameDate left, GameDate right) {
            if (left.year > right.year) { return true; }
            if (left.year == right.year) {
                if (left.dayOfYear > right.dayOfYear) { return true; }
                if (left.dayOfYear == right.dayOfYear) {
                    if (left.hourOfDay > right.hourOfDay) { return true; }
                    if (left.hourOfDay == right.hourOfDay) { return true; }
                }
            }
            return false;
        }

        public static bool operator ==(GameDate left, GameDate right) {
            return left.Equals(right);
        }

        public static bool operator !=(GameDate left, GameDate right) {
            return !left.Equals(right);
        }

        #endregion

        public static GameDate GameStartDate = new GameDate(Constants.Zero, Constants.Zero, GameTime.GameStartYear);    // 2700.000.00
        public static GameDate GameEndDate = new GameDate(GameTime.HoursPerDay - 1, GameTime.DaysPerYear - 1, GameTime.GameEndYear);    // 8999.099.19

        // Bug: use of static constructor with struct causes intellisense for constructors to fail

        public readonly int hourOfDay;
        public readonly int dayOfYear;
        public readonly int year;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct whose values
        /// are set to the future that is <c>timeFromCurrentDate</c> from the CurrentDate.
        /// </summary>
        /// <param name="timeFromCurrentDate">The time from current date. Cannot be the default (0.0.0).</param>
        public GameDate(GameTimeDuration timeFromCurrentDate) {
            // If timeFromCurrentDate is the default (0.0.0), this constructs a GameDate that is identical to CurrentDate. 
            // As CurrentDate has already changed, the next time onCurrentDateChanged will be raised will be CurrentDate + 1 hour. 
            // Therefore this constructed GameDate can never be matched to a date delivered by onCurrentDateChanged
            D.Assert(timeFromCurrentDate != default(GameTimeDuration));

            GameDate currentDate = GameTime.CurrentDate;
            int futureYear = currentDate.year + timeFromCurrentDate.years;
            int futureDay = currentDate.dayOfYear + timeFromCurrentDate.days;
            if (futureDay >= GameTime.DaysPerYear) {
                futureYear++;
                futureDay = futureDay % GameTime.DaysPerYear;
            }
            int futureHour = currentDate.hourOfDay + timeFromCurrentDate.hours;
            if (futureHour >= GameTime.HoursPerDay) {
                futureDay++;
                futureHour = futureHour % GameTime.HoursPerDay;
                if (futureDay == GameTime.DaysPerYear) {
                    futureYear++;
                    futureDay = 0;
                }
            }
            hourOfDay = futureHour;
            dayOfYear = futureDay;
            year = futureYear;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct.
        /// </summary>
        /// <param name="dayOfYear">The day of year.</param>
        /// <param name="year">The year.</param>
        public GameDate(int dayOfYear, int year) : this(Constants.Zero, dayOfYear, year) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct.
        /// </summary>
        /// <param name="hourOfDay">The hour of day.</param>
        /// <param name="dayOfYear">The day of year.</param>
        /// <param name="year">The year.</param>
        public GameDate(int hourOfDay, int dayOfYear, int year) {
            Arguments.ValidateForRange(hourOfDay, Constants.Zero, GameTime.HoursPerDay - 1);
            Arguments.ValidateForRange(dayOfYear, Constants.Zero, GameTime.DaysPerYear - 1);  // UNCLEAR is this range correct?
            Arguments.ValidateForRange(year, GameTime.GameStartYear, GameTime.GameEndYear);
            this.hourOfDay = hourOfDay;
            this.dayOfYear = dayOfYear;
            this.year = year;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct synched to the gameClock value provided.
        /// </summary>
        /// <param name="gameClock">The game clock.</param>
        internal GameDate(float gameClock) {
            int elapsedHours = Mathf.FloorToInt(gameClock * GameTime.HoursPerSecond);
            int elapsedDays = elapsedHours / GameTime.HoursPerDay;
            int hoursPerYear = GameTime.DaysPerYear * GameTime.HoursPerDay;
            year = GameTime.GameStartYear + Mathf.FloorToInt(elapsedHours / hoursPerYear);
            dayOfYear = elapsedDays % GameTime.DaysPerYear;
            hourOfDay = elapsedHours % GameTime.HoursPerDay;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is GameDate)) { return false; }
            return Equals((GameDate)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <see cref="Page 254, C# 4.0 in a Nutshell."/>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + hourOfDay.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + dayOfYear.GetHashCode();
            hash = hash * 31 + year.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return Constants.GameDateFormat.Inject(year, dayOfYear, hourOfDay);
        }

        #region IEquatable<GameDate> Members

        public bool Equals(GameDate other) {
            return hourOfDay == other.hourOfDay && dayOfYear == other.dayOfYear && year == other.year;
        }

        #endregion

    }

}

