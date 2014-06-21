// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameTimeDuration.cs
// Immutable data container struct that holds a duration of GameTime, a specific
// number of hours, days and years.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable data container struct that holds a duration of GameTime, a specific
    /// number of hours, days and years.
    /// </summary>
    public struct GameTimeDuration : IEquatable<GameTimeDuration> {

        public static GameTimeDuration OneDay = new GameTimeDuration(days: 1, years: 0);
        public static GameTimeDuration OneYear = new GameTimeDuration(days: 0, years: 1);

        // Bug: use of static constructor with struct causes intellisense for constructors to fail

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator <(GameTimeDuration left, GameTimeDuration right) {
            if (left.Years < right.Years) { return true; }
            if (left.Years == right.Years) {
                if (left.Days < right.Days) { return true; }
                if (left.Days == right.Days) {
                    if (left.Hours < right.Hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator <=(GameTimeDuration left, GameTimeDuration right) {
            if (left.Years < right.Years) { return true; }
            if (left.Years == right.Years) {
                if (left.Days < right.Days) { return true; }
                if (left.Days == right.Days) {
                    if (left.Hours < right.Hours) { return true; }
                    if (left.Hours == right.Hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator >(GameTimeDuration left, GameTimeDuration right) {
            if (left.Years > right.Years) { return true; }
            if (left.Years == right.Years) {
                if (left.Days > right.Days) { return true; }
                if (left.Days == right.Days) {
                    if (left.Hours > right.Hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator >=(GameTimeDuration left, GameTimeDuration right) {
            if (left.Years > right.Years) { return true; }
            if (left.Years == right.Years) {
                if (left.Days > right.Days) { return true; }
                if (left.Days == right.Days) {
                    if (left.Hours > right.Hours) { return true; }
                    if (left.Hours == right.Hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator ==(GameTimeDuration left, GameTimeDuration right) {
            return left.Equals(right);
        }

        public static bool operator !=(GameTimeDuration left, GameTimeDuration right) {
            return !left.Equals(right);
        }

        #endregion

        /// <summary>
        /// The years setting of this duration. Note: The total duration is acquired using totalInHours.
        /// </summary>
        public int Years { get; private set; }

        /// <summary>
        /// The days setting of this duration. Note: The total duration is acquired using totalInHours.
        /// </summary>
        public int Days { get; private set; }

        /// <summary>
        /// The hours setting of this duration. Note: The total duration is acquired using totalInHours.
        /// </summary>
        public int Hours { get; private set; }

        /// <summary>
        /// The total duration in hours.
        /// </summary>
        public int TotalInHours { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration" /> class representing the values provided. 
        /// </summary>
        /// <param name="days">The days.</param>
        /// <param name="years">The years.</param>
        public GameTimeDuration(int days, int years) : this(Constants.Zero, days, years) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration" /> class representing the values provided.
        /// </summary>
        /// <param name="hours">The hours.</param>
        /// <param name="days">The days.</param>
        /// <param name="years">The years.</param>
        public GameTimeDuration(int hours, int days, int years)
            : this() {
            Arguments.ValidateForRange(hours, Constants.Zero, GameTime.HoursPerDay - 1);
            Arguments.ValidateForRange(days, Constants.Zero, GameTime.DaysPerYear - 1);
            Arguments.ValidateNotNegative(years);
            Hours = hours;
            Days = days;
            Years = years;
            TotalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration"/> class 
        /// starting now and ending on endDate.
        /// </summary>
        /// <param name="endDate">The end date.</param>
        public GameTimeDuration(GameDate endDate) : this(GameTime.CurrentDate, endDate) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration"/> class.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        public GameTimeDuration(GameDate startDate, GameDate endDate)
            : this() {
            GameUtility.ValidateForRange(startDate, GameDate.GameStartDate, GameDate.GameEndDate);
            GameUtility.ValidateForRange(endDate, GameDate.GameStartDate, GameDate.GameEndDate);
            //D.Assert(startDate != endDate);   // a GameTimeDuration of zero should be legal

            int years = endDate.Year - startDate.Year;
            int days = endDate.DayOfYear - startDate.DayOfYear;
            if (days < 0) {
                years--;
                days = GameTime.DaysPerYear + days;
            }
            int hours = endDate.HourOfDay - startDate.HourOfDay;
            if (hours < 0) {
                days--;
                hours = GameTime.HoursPerDay + hours;
                if (days < 0) {
                    years--;
                    days = GameTime.DaysPerYear + days;
                }
            }
            Hours = hours;
            Days = days;
            Years = years;
            TotalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is GameTimeDuration)) { return false; }
            return Equals((GameTimeDuration)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + Hours.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Days.GetHashCode();
            hash = hash * 31 + Years.GetHashCode();
            hash = hash * 31 + TotalInHours.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            if (Years == Constants.Zero) {
                if (Days == Constants.Zero) {
                    return Constants.GamePeriodHoursOnlyFormat.Inject(Hours);
                }
                return Constants.GamePeriodNoYearsFormat.Inject(Days, Hours);
            }
            return Constants.GamePeriodYearsFormat.Inject(Years, Days, Hours);
        }

        #region IEquatable<GameTimeDuration> Members

        public bool Equals(GameTimeDuration other) {
            return Hours == other.Hours && Days == other.Days && Years == other.Years && TotalInHours == other.TotalInHours;
        }

        #endregion

    }
}

