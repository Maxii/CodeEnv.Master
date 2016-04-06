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
    /// <remarks>WARNING: Not suitable for a dictionary key.</remarks>
    /// </summary>
    public struct GameDate : IEquatable<GameDate> {

        public const string CalenderDateFormat = "{0}.{1:D3}.{2:00.}";  //= "{0}.{1:D3}.{2:D2}";
        public const string FullDateFormat = "{0}.{1:D3}.{2:00.0}";  //= "{0}.{1:D3}.{2:D2}";

        public static GameDate GameStartDate = new GameDate(Constants.ZeroF, Constants.Zero, GameTime.GameStartYear);    // 2700.000.00
        public static GameDate GameEndDate = new GameDate(GameTime.HoursPerDay - Mathf.Epsilon, GameTime.DaysPerYear - 1, GameTime.GameEndYear);    // 8999.099.19

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator <(GameDate left, GameDate right) {
            return left.TotalHoursSinceGameStart < right.TotalHoursSinceGameStart - GameTime.HoursEqualTolerance;
        }

        public static bool operator <=(GameDate left, GameDate right) {
            if (left < right) {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator >(GameDate left, GameDate right) {
            return left.TotalHoursSinceGameStart > right.TotalHoursSinceGameStart + GameTime.HoursEqualTolerance;
        }

        public static bool operator >=(GameDate left, GameDate right) {
            if (left > right) {
                return true;
            }
            return left.Equals(right);
        }

        public static bool operator ==(GameDate left, GameDate right) {
            return left.Equals(right);
        }

        public static bool operator !=(GameDate left, GameDate right) {
            return !left.Equals(right);
        }

        public static GameTimeDuration operator -(GameDate left, GameDate right) {
            D.Assert(left >= right);
            return new GameTimeDuration(right, left);
        }

        #endregion

        public float HourOfDay { get; private set; }
        public int DayOfYear { get; private set; }
        public int Year { get; private set; }

        /// <summary>
        /// Returns a string formatted for use by the calender.
        /// </summary>
        public string CalenderFormattedDate { get { return CalenderDateFormat.Inject(Year, DayOfYear, Mathf.Floor(HourOfDay)); } }

        /// <summary>
        /// The total number of hours this GameDate represents since the game was started.
        /// <remarks>This approach using total hours is necessary as 2700.001.99.99 should equal 2700.002.00.01 as it is only .02 hours apart.
        /// Comparing Year, DayOfYear and HourOfDay does not accomplish that.</remarks>
        /// </summary>
        public float TotalHoursSinceGameStart { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct whose values
        /// are set to the future that is <c>timeFromCurrentDate</c> from the CurrentDate.
        /// </summary>
        /// <param name="timeFromCurrentDate">The time from current date.</param>
        public GameDate(GameTimeDuration timeFromCurrentDate)
            : this() {
            GameDate currentDate = GameTime.Instance.CurrentDate;
            float totalHoursSinceGameStart = currentDate.TotalHoursSinceGameStart;
            totalHoursSinceGameStart += timeFromCurrentDate.TotalInHours;
            Initialize(totalHoursSinceGameStart);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct.
        /// </summary>
        /// <param name="dayOfYear">The day of year.</param>
        /// <param name="year">The year.</param>
        public GameDate(int dayOfYear, int year) : this(Constants.ZeroF, dayOfYear, year) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct.
        /// </summary>
        /// <param name="hourOfDay">The hour of day.</param>
        /// <param name="dayOfYear">The day of year.</param>
        /// <param name="year">The year.</param>
        public GameDate(float hourOfDay, int dayOfYear, int year)
            : this() {
            Utility.ValidateForRange(hourOfDay, Constants.ZeroF, GameTime.HoursPerDay - Mathf.Epsilon);
            Utility.ValidateForRange(dayOfYear, Constants.Zero, GameTime.DaysPerYear - 1);
            Utility.ValidateForRange(year, GameTime.GameStartYear, GameTime.GameEndYear);

            float totalHoursSinceGameStart = (year - GameTime.GameStartYear) * GameTime.DaysPerYear * GameTime.HoursPerDay;
            totalHoursSinceGameStart += dayOfYear * GameTime.HoursPerDay;
            totalHoursSinceGameStart += hourOfDay;
            Initialize(totalHoursSinceGameStart);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDate"/> struct synched to the provided elapsed time in seconds.
        /// To be used only by GameTime.
        /// </summary>
        /// <param name="gameClockInSecs">The game clock in seconds.</param>
        internal GameDate(float gameClockInSecs)
            : this() {
            float totalHoursSinceGameStart = gameClockInSecs * GameTime.HoursPerSecond;
            Initialize(totalHoursSinceGameStart);
        }

        private void Initialize(float totalHoursSinceGameStart) {
            TotalHoursSinceGameStart = totalHoursSinceGameStart;
            int elapsedDays = Mathf.FloorToInt(totalHoursSinceGameStart / GameTime.HoursPerDay);
            int hoursPerYear = GameTime.DaysPerYear * GameTime.HoursPerDay;
            Year = GameTime.GameStartYear + Mathf.FloorToInt(totalHoursSinceGameStart / hoursPerYear);
            DayOfYear = elapsedDays % GameTime.DaysPerYear;
            HourOfDay = totalHoursSinceGameStart % GameTime.HoursPerDay;
        }

        /// <summary>
        /// Returns <c>true</c> if this GameDate is equal to other GameDate for use
        /// by the Calender.
        /// </summary>
        /// <param name="other">The other GameDate.</param>
        /// <returns></returns>
        public bool CalenderEquals(GameDate other) {
            return HoursEqualForCalender(other.HourOfDay) && DayOfYear == other.DayOfYear && Year == other.Year;
        }

        private bool HoursEqualForCalender(float otherHourOfDay) {
            return Mathf.FloorToInt(HourOfDay) == Mathf.FloorToInt(otherHourOfDay);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is GameDate)) { return false; }
            return Equals((GameDate)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;
            // Rule: If two things are equal then they MUST return the same value for GetHashCode()
            // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
            // I don't know how to do this when Hours uses Approx so I'll gaurantee it and live with conflicts
            return hash;
        }

        #endregion

        public override string ToString() {
            return FullDateFormat.Inject(Year, DayOfYear, HourOfDay);
        }

        #region IEquatable<GameDate> Members

        public bool Equals(GameDate other) {
            return Mathfx.Approx(TotalHoursSinceGameStart, other.TotalHoursSinceGameStart, GameTime.HoursEqualTolerance);
        }

        #endregion

        #region Prior Implementation Archive

        //public static bool operator <(GameDate left, GameDate right) {
        //    if (left.Year < right.Year) { return true; }
        //    if (left.Year == right.Year) {
        //        if (left.DayOfYear < right.DayOfYear) { return true; }
        //        if (left.DayOfYear == right.DayOfYear) {
        //            if (left.HourOfDay < right.HourOfDay - GameTime.HoursEqualTolerance) {
        //                D.Assert(!left.Equals(right));
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public static bool operator >(GameDate left, GameDate right) {
        //    if (left.Year > right.Year) { return true; }
        //    if (left.Year == right.Year) {
        //        if (left.DayOfYear > right.DayOfYear) { return true; }
        //        if (left.DayOfYear == right.DayOfYear) {
        //            if (left.HourOfDay > right.HourOfDay + GameTime.HoursEqualTolerance) {
        //                D.Assert(!left.Equals(right));
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //public GameDate(GameTimeDuration timeFromCurrentDate)
        //    : this() {
        //    GameDate currentDate = GameTime.Instance.CurrentDate;
        //    int futureYear = currentDate.Year + timeFromCurrentDate.Years;
        //    int futureDay = currentDate.DayOfYear + timeFromCurrentDate.Days;
        //    if (futureDay >= GameTime.DaysPerYear) {
        //        futureYear++;
        //        futureDay = futureDay % GameTime.DaysPerYear;
        //    }
        //    float futureHour = currentDate.HourOfDay + timeFromCurrentDate.Hours;
        //    if (futureHour >= GameTime.HoursPerDay) {
        //        futureDay++;
        //        futureHour = futureHour % GameTime.HoursPerDay;
        //        if (futureDay == GameTime.DaysPerYear) {
        //            futureYear++;
        //            futureDay = Constants.Zero;
        //        }
        //    }
        //    HourOfDay = futureHour;
        //    DayOfYear = futureDay;
        //    Year = futureYear;
        //}

        //public override int GetHashCode() {
        //    int hash = 17;
        //    hash = hash * 31 + HourOfDay.GetHashCode(); // 31 = another prime number
        //    hash = hash * 31 + DayOfYear.GetHashCode();
        //    hash = hash * 31 + Year.GetHashCode();
        //    return hash;
        //}

        //public bool Equals(GameDate other) {
        //    return Mathfx.Approx(HourOfDay, other.HourOfDay, GameTime.HoursEqualTolerance) && DayOfYear == other.DayOfYear && Year == other.Year;
        //}

        #endregion

    }

}

