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

        public static GameTimeDuration OneYear;
        public static GameTimeDuration OneDay;

        static GameTimeDuration() {
            OneDay = new GameTimeDuration(days: 1, years: 0);
            OneYear = new GameTimeDuration(days: 0, years: 1);
        }

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator <(GameTimeDuration left, GameTimeDuration right) {
            if (left.years < right.years) { return true; }
            if (left.years == right.years) {
                if (left.days < right.days) { return true; }
                if (left.days == right.days) {
                    if (left.hours < right.hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator <=(GameTimeDuration left, GameTimeDuration right) {
            if (left.years < right.years) { return true; }
            if (left.years == right.years) {
                if (left.days < right.days) { return true; }
                if (left.days == right.days) {
                    if (left.hours < right.hours) { return true; }
                    if (left.hours == right.hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator >(GameTimeDuration left, GameTimeDuration right) {
            if (left.years > right.years) { return true; }
            if (left.years == right.years) {
                if (left.days > right.days) { return true; }
                if (left.days == right.days) {
                    if (left.hours > right.hours) { return true; }
                }
            }
            return false;
        }

        public static bool operator >=(GameTimeDuration left, GameTimeDuration right) {
            if (left.years > right.years) { return true; }
            if (left.years == right.years) {
                if (left.days > right.days) { return true; }
                if (left.days == right.days) {
                    if (left.hours > right.hours) { return true; }
                    if (left.hours == right.hours) { return true; }
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
        public readonly int years;

        /// <summary>
        /// The days setting of this duration. Note: The total duration is acquired using totalInHours.
        /// </summary>
        public readonly int days;

        /// <summary>
        /// The hours setting of this duration. Note: The total duration is acquired using totalInHours.
        /// </summary>
        public readonly int hours;

        /// <summary>
        /// The total duration in hours.
        /// </summary>
        public readonly int totalInHours;

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
        public GameTimeDuration(int hours, int days, int years) {
            Arguments.ValidateForRange(hours, Constants.Zero, GameTime.HoursPerDay - 1);
            Arguments.ValidateForRange(days, Constants.Zero, GameTime.DaysPerYear - 1);
            Arguments.ValidateNotNegative(years);
            this.hours = hours;
            this.days = days;
            this.years = years;
            totalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
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
        public GameTimeDuration(GameDate startDate, GameDate endDate) {
            GameUtility.ValidateForRange(startDate, GameDate.GameStartDate, GameDate.GameEndDate);
            GameUtility.ValidateForRange(endDate, GameDate.GameStartDate, GameDate.GameEndDate);
            D.Assert(startDate != endDate);

            int years = endDate.year - startDate.year;
            int days = endDate.dayOfYear - startDate.dayOfYear;
            if (days < 0) {
                years--;
                days = GameTime.DaysPerYear + days;
            }
            int hours = endDate.hourOfDay - startDate.hourOfDay;
            if (hours < 0) {
                days--;
                hours = GameTime.HoursPerDay + hours;
                if (days < 0) {
                    years--;
                    days = GameTime.DaysPerYear + days;
                }
            }
            this.hours = hours;
            this.days = days;
            this.years = years;
            totalInHours = (years * GameTime.DaysPerYear + days) * GameTime.HoursPerDay + hours;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is GameTimeDuration)) { return false; }
            return Equals((GameTimeDuration)obj);
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
            hash = hash * 31 + hours.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + days.GetHashCode();
            hash = hash * 31 + years.GetHashCode();
            hash = hash * 31 + totalInHours.GetHashCode();
            return hash;
        }

        #endregion


        public override string ToString() {
            if (years == Constants.Zero) {
                if (days == Constants.Zero) {
                    return Constants.GamePeriodHoursOnlyFormat.Inject(hours);
                }
                return Constants.GamePeriodNoYearsFormat.Inject(days, hours);
            }
            return Constants.GamePeriodYearsFormat.Inject(years, days, hours);
        }

        #region IEquatable<GameTimeDuration> Members

        public bool Equals(GameTimeDuration other) {
            return hours == other.hours && days == other.days && years == other.years && totalInHours == other.totalInHours;
        }

        #endregion


    }
}

