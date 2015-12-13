// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameTimePeriod.cs
// Data container class that holds a duration of GameTime.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container class that holds a duration of GameTime.
    /// </summary>
    [Obsolete]
    public class GameTimePeriod {

        //public static GameTimePeriod OneYear { get; private set; }
        //public static GameTimePeriod OneDay { get; private set; }

        //public static GameTimePeriod() {
        //    OneYear = new GameTimePeriod(days: 0, years: 1);
        //    OneDay = new GameTimePeriod(days: 1, years: 0);
        //}

        private GameDate _startDate;
        private GameDate _endDate;

        /// <summary>
        /// The Year value for this period. Note: The total duration of the
        /// period is acquired using PeriodInDays.
        /// </summary>
        public int Years { get; private set; }

        /// <summary>
        /// The day value for this period. Note: The total duration of the
        /// period is acquired using PeriodInDays.
        /// </summary>
        public int Days { get; private set; }

        /// <summary>
        /// The hours value for this period. Note: The total duration of the
        /// period is acquired using PeriodInDays.
        /// </summary>
        public int Hours { get; private set; }

        /// <summary>
        /// The total duration of the period in Days.
        /// </summary>
        //public int PeriodInDays { get; private set; }

        public string FormattedPeriod {
            get {
                if (Years == Constants.Zero) {
                    if (Days == Constants.Zero) {
                        return Constants.GamePeriodHoursOnlyFormat.Inject(Hours);
                    }
                    return Constants.GamePeriodNoYearsFormat.Inject(Days, Hours);
                }
                return Constants.GamePeriodYearsFormat.Inject(Years, Days, Hours);

                //if (Years == Constants.Zero) {
                //    return Constants.GamePeriodNoYearsFormat.Inject(Days);
                //}
                //return Constants.GamePeriodYearsFormat.Inject(Years, Days);
            }
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="GameTimePeriod"/> class of the
        ///// duration provided. Creates an artificial start date said duration before the current
        ///// date.
        ///// </summary>
        ///// <param name="days">The days.</param>
        ///// <param name="years">The years.</param>
        //public GameTimePeriod(int days, int years) : this(Constants.Zero, days, years) { }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="GameTimePeriod" /> class of the
        ///// duration provided.
        ///// </summary>
        ///// <param name="hours">The hours.</param>
        ///// <param name="days">The days.</param>
        ///// <param name="years">The years.</param>
        //public GameTimePeriod(int hours, int days, int years) {
        //    Arguments.ValidateForRange(hours, Constants.Zero, GameDate.HoursPerDay - 1);
        //    Arguments.ValidateForRange(days, Constants.Zero, GameDate.DaysPerYear - 1);
        //    Arguments.ValidateNotNegative(years);
        //    Hours = hours;
        //    Days = days;
        //    Years = years;
        //    PeriodInDays = years * GameDate.DaysPerYear + days;
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimePeriod"/> class.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        public GameTimePeriod(GameDate startDate, GameDate endDate) {
            GameUtility.ValidateForRange(startDate, GameDate.GameStartDate, GameDate.GameEndDate);
            GameUtility.ValidateForRange(endDate, GameDate.GameStartDate, GameDate.GameEndDate);
            D.Assert(startDate != endDate);
            //_startDate = startDate;
            SetPeriodValues(startDate, endDate);
        }

        ///// <summary>
        ///// Updates the period to a value reflective of the supplied date. Useful
        ///// when the period was created using GameDates. Does nothing except issuing
        ///// a warning if the period was created using days and years.
        ///// </summary>
        ///// <param name="currentDate">The current date.</param>
        //public void UpdatePeriod(GameDate currentDate) {
        //    if (_startDate != null) {
        //        SetPeriodValues(_startDate, currentDate);
        //    }
        //}

        private void SetPeriodValues(GameDate startDate, GameDate endDate) {
            _startDate = startDate;
            _endDate = endDate;
            int years = endDate.Year - startDate.Year;
            int days = endDate.DayOfYear - startDate.DayOfYear;
            if (days < 0) {
                years--;
                days = GameDate.DaysPerYear + days;
            }
            int hours = endDate.HourOfDay - startDate.HourOfDay;
            if (hours < 0) {
                days--;
                hours = GameDate.HoursPerDay + hours;
                if (days < 0) {
                    years--;
                    days = GameDate.DaysPerYear + days;
                }
            }
            Years = years;
            Days = days;
            Hours = hours;
            //PeriodInDays = years * GameDate.DaysPerYear + days;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

