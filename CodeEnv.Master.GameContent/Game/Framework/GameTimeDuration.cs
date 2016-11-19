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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable data container struct that holds a duration of GameTime, a specific
    /// number of hours, days and years.
    /// <remarks>5.7.16: Now suitable for a dictionary key.</remarks>
    /// </summary>
    public struct GameTimeDuration : IEquatable<GameTimeDuration> {

        public const string FullFormat = "{0} years, {1} days, {2:0.0} hours"; //= "{0} years, {1:D3} days, {2:D2} hours";
        public const string NoYearsFormat = "{0} days, {1:0.0} hours";  //= "{0:D3} days, {1:D2} hours";
        public const string HoursOnlyFormat = "{0:0.0} hours"; //= "{0:D2} hours";

        public static readonly GameTimeDuration OneDay = new GameTimeDuration(hours: 0F, days: 1, years: 0);
        public static readonly GameTimeDuration OneYear = new GameTimeDuration(hours: 0F, days: 0, years: 1);

        // Bug: use of static constructor with struct causes intellisense for constructors to fail

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator <(GameTimeDuration left, GameTimeDuration right) {
            return left.TotalInHours < right.TotalInHours;
        }

        public static bool operator <=(GameTimeDuration left, GameTimeDuration right) {
            if (left < right) {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator >(GameTimeDuration left, GameTimeDuration right) {
            return left.TotalInHours > right.TotalInHours;
        }

        public static bool operator >=(GameTimeDuration left, GameTimeDuration right) {
            if (left > right) {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator ==(GameTimeDuration left, GameTimeDuration right) {
            return left.Equals(right);
        }

        public static bool operator !=(GameTimeDuration left, GameTimeDuration right) {
            return !left.Equals(right);
        }

        public static GameTimeDuration operator +(GameTimeDuration left, GameTimeDuration right) {
            float totalHours = left.TotalInHours + right.TotalInHours;
            return new GameTimeDuration(totalHours);
        }

        public static GameTimeDuration operator *(int scaler, GameTimeDuration right) {
            Utility.ValidateNotNegative(scaler);
            GameTimeDuration result = new GameTimeDuration();
            for (int i = 0; i < scaler; i++) {
                result += right;
            }
            return result;
        }

        public static GameTimeDuration operator *(GameTimeDuration left, int scaler) {
            return scaler * left;
        }

        // NOTE: no division or subtraction operators 

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
        public float Hours { get; private set; }

        /// <summary>
        /// The total duration in hours.
        /// </summary>
        public float TotalInHours { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration" /> class representing the hours provided.
        /// </summary>
        /// <param name="hours">The hours. Number of hours is unlimited.</param>
        public GameTimeDuration(float hours)
            : this() {
            Utility.ValidateNotNegative(hours);
            Initialize(GameTime.ConvertHoursValue(hours));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration" /> class representing the values provided.
        /// </summary>
        /// <param name="hours">The hours. Must be less than the number of hours in a day.</param>
        /// <param name="days">The days. Number of days is unlimited.</param>
        public GameTimeDuration(float hours, int days)
            : this() {
            Utility.ValidateForRange(hours, Constants.ZeroF, GameTime.HoursPerDay - UnityConstants.FloatEqualityPrecision);
            Utility.ValidateNotNegative(days);
            float totalHours = (days * GameTime.HoursPerDay) + GameTime.ConvertHoursValue(hours);
            Initialize(totalHours);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration" /> class representing the values provided.
        /// </summary>
        /// <param name="hours">The hours. Must be less than the number of hours in a day.</param>
        /// <param name="days">The days. Must be less than the number of days in a year.</param>
        /// <param name="years">The years. Unlimited.</param>
        public GameTimeDuration(float hours, int days, int years)
            : this() {
            Utility.ValidateForRange(hours, Constants.ZeroF, GameTime.HoursPerDay - UnityConstants.FloatEqualityPrecision);
            Utility.ValidateForRange(days, Constants.Zero, GameTime.DaysPerYear - 1);
            Utility.ValidateNotNegative(years);
            float totalHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + GameTime.ConvertHoursValue(hours);
            Initialize(totalHours);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration"/> class 
        /// starting now and ending on endDate.
        /// </summary>
        /// <param name="endDate">The end date.</param>
        public GameTimeDuration(GameDate endDate) : this(GameTime.Instance.CurrentDate, endDate) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimeDuration"/> class.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        public GameTimeDuration(GameDate startDate, GameDate endDate)
            : this() {
            GameUtility.ValidateForRange(startDate, GameTime.GameStartDate, GameTime.GameEndDate);
            GameUtility.ValidateForRange(endDate, startDate, GameTime.GameEndDate);
            float totalHours = endDate.TotalHoursSinceGameStart - startDate.TotalHoursSinceGameStart;
            Initialize(totalHours);
        }

        private void Initialize(float totalHours) {
            GameTime.ValidateHoursValue(totalHours);
            Hours = totalHours % GameTime.HoursPerDay;
            int days = Mathf.FloorToInt(totalHours / GameTime.HoursPerDay);
            Days = days % GameTime.DaysPerYear;
            Years = days / GameTime.DaysPerYear;
            TotalInHours = totalHours;
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
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;
                hash = hash * 31 + TotalInHours.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            if (Years == Constants.Zero) {
                if (Days == Constants.Zero) {
                    return HoursOnlyFormat.Inject(Hours);
                }
                return NoYearsFormat.Inject(Days, Hours);
            }
            return FullFormat.Inject(Years, Days, Hours);
        }

        #region IEquatable<GameTimeDuration> Members

        public bool Equals(GameTimeDuration other) {
            return TotalInHours == other.TotalInHours;  // Can't use Approx if comply with "If equal, HashCode must return same value"
        }

        #endregion

        #region Prior Implementation Archive

        //public static bool operator <(GameTimeDuration left, GameTimeDuration right) {
        //    if (left.Years < right.Years) { return true; }
        //    if (left.Years == right.Years) {
        //        if (left.Days < right.Days) { return true; }
        //        if (left.Days == right.Days) {
        //            if (left.Hours < right.Hours - GameTime.HoursEqualTolerance) {
        //                D.Assert(!left.Equals(right));
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public static bool operator <=(GameTimeDuration left, GameTimeDuration right) {
        //    if (left.Years < right.Years) { return true; }
        //    if (left.Years == right.Years) {
        //        if (left.Days < right.Days) { return true; }
        //        if (left.Days == right.Days) {
        //            if (left.Hours.IsLessThanOrEqualTo(right.Hours, GameTime.HoursEqualTolerance)) {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public static bool operator >(GameTimeDuration left, GameTimeDuration right) {
        //    if (left.Years > right.Years) { return true; }
        //    if (left.Years == right.Years) {
        //        if (left.Days > right.Days) { return true; }
        //        if (left.Days == right.Days) {
        //            if (left.Hours > right.Hours + GameTime.HoursEqualTolerance) {
        //                D.Assert(!left.Equals(right));
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public static bool operator >=(GameTimeDuration left, GameTimeDuration right) {
        //    if (left.Years > right.Years) { return true; }
        //    if (left.Years == right.Years) {
        //        if (left.Days > right.Days) { return true; }
        //        if (left.Days == right.Days) {
        //            if (left.Hours.IsGreaterThanOrEqualTo(right.Hours, GameTime.HoursEqualTolerance)) {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public static GameTimeDuration operator +(GameTimeDuration left, GameTimeDuration right) {
        //    int years = left.Years + right.Years;
        //    int days = left.Days + right.Days;
        //    if (days >= GameTime.DaysPerYear) {
        //        days = days % GameTime.DaysPerYear;
        //        years++;
        //    }
        //    float hours = left.Hours + right.Hours;
        //    if (hours.IsGreaterThanOrEqualTo(GameTime.HoursPerDay)) {
        //        hours = hours % GameTime.HoursPerDay;
        //        days++;
        //        if (days >= GameTime.DaysPerYear) {
        //            days = days % GameTime.DaysPerYear;
        //            years++;
        //        }
        //    }
        //    return new GameTimeDuration(hours, days, years);
        //}

        //public GameTimeDuration(float hours)
        //    : this() {
        //    Utility.ValidateNotNegative(hours);
        //    Hours = hours % GameTime.HoursPerDay;
        //    int days = Mathf.FloorToInt(hours / GameTime.HoursPerDay);
        //    Days = days % GameTime.DaysPerYear;
        //    Years = days / GameTime.DaysPerYear;
        //    TotalInHours = hours;
        //}

        //public GameTimeDuration(float hours, int days)
        //    : this() {
        //    Utility.ValidateForRange(hours, Constants.ZeroF, GameTime.HoursPerDay - Mathf.Epsilon);
        //    Utility.ValidateNotNegative(days);
        //    Hours = hours;
        //    Days = days % GameTime.DaysPerYear;
        //    Years = days / GameTime.DaysPerYear;
        //    TotalInHours = (days * GameTime.HoursPerDay) + hours;
        //}

        //public GameTimeDuration(float hours, int days, int years)
        //    : this() {
        //    Utility.ValidateForRange(hours, Constants.ZeroF, GameTime.HoursPerDay - Mathf.Epsilon);
        //    Utility.ValidateForRange(days, Constants.Zero, GameTime.DaysPerYear - 1);
        //    Utility.ValidateNotNegative(years);
        //    Hours = hours;
        //    Days = days;
        //    Years = years;
        //    TotalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        //}

        //public GameTimeDuration(GameDate startDate, GameDate endDate)
        //    : this() {
        //    GameUtility.ValidateForRange(startDate, GameDate.GameStartDate, GameDate.GameEndDate);
        //    GameUtility.ValidateForRange(endDate, GameDate.GameStartDate, GameDate.GameEndDate);

        //    int years = endDate.Year - startDate.Year;
        //    int days = endDate.DayOfYear - startDate.DayOfYear;
        //    if (days < 0) {
        //        years--;
        //        days = GameTime.DaysPerYear + days;
        //    }
        //    float hours = endDate.HourOfDay - startDate.HourOfDay;
        //    if (hours < 0F) {
        //        days--;
        //        hours = GameTime.HoursPerDay + hours;
        //        if (days < 0) {
        //            years--;
        //            days = GameTime.DaysPerYear + days;
        //        }
        //    }
        //    //Hours = hours;
        //    //Days = days;
        //    //Years = years;
        //    //TotalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        //    float totalHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        //    Initialize(totalHours);
        //}

        //public override int GetHashCode() {
        //    int hash = 17;
        //    hash = hash * 31 + Hours.GetHashCode(); // 31 = another prime number
        //    hash = hash * 31 + Days.GetHashCode();
        //    hash = hash * 31 + Years.GetHashCode();
        //    hash = hash * 31 + TotalInHours.GetHashCode();
        //    return hash;
        //}

        //public bool Equals(GameTimeDuration other) {
        //    return Mathfx.Approx(Hours, other.Hours, GameTime.HoursEqualTolerance) && Days == other.Days && Years == other.Years;
        //}

        #endregion

    }
}

