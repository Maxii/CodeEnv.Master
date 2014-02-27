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
    public class GameTimePeriod {

        private static int _daysPerYear = GeneralSettings.Instance.DaysPerYear;
        private static int _hoursPerDay = GeneralSettings.Instance.HoursPerDay;

        private IGameDate _startDate;

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
        public int PeriodInDays { get; private set; }

        public string FormattedPeriod {
            get {
                if (Years == Constants.Zero) {
                    return Constants.GamePeriodNoYearsFormat.Inject(Days);
                }
                return Constants.GamePeriodYearsFormat.Inject(Years, Days);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimePeriod"/> class of
        /// Zero duration.
        /// </summary>
        public GameTimePeriod() : this(Constants.Zero, Constants.Zero) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimePeriod"/> class of the
        /// duration provided. Creates an artificial start date said duration before the current
        /// date.
        /// </summary>
        /// <param name="days">The days.</param>
        /// <param name="years">The years.</param>
        public GameTimePeriod(int days, int years) : this(Constants.Zero, days, years) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimePeriod" /> class of the
        /// duration provided.
        /// </summary>
        /// <param name="hours">The hours.</param>
        /// <param name="days">The days.</param>
        /// <param name="years">The years.</param>
        public GameTimePeriod(int hours, int days, int years) {
            Arguments.ValidateForRange(hours, Constants.Zero, _hoursPerDay - 1);
            Arguments.ValidateForRange(days, Constants.Zero, _daysPerYear - 1);
            Arguments.ValidateNotNegative(years);
            Hours = hours;
            Days = days;
            Years = years;
            PeriodInDays = years * _daysPerYear + days;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameTimePeriod"/> class.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        public GameTimePeriod(IGameDate startDate, IGameDate endDate) {
            _startDate = startDate;
            SetPeriodValues(startDate, endDate);
        }

        /// <summary>
        /// Updates the period to a value reflective of the supplied date. Useful
        /// when the period was created using IGameDates. Does nothing except issuing
        /// a warning if the period was created using days and years.
        /// </summary>
        /// <param name="currentDate">The current date.</param>
        public void UpdatePeriod(IGameDate currentDate) {
            if (_startDate != null) {
                SetPeriodValues(_startDate, currentDate);
            }
        }

        private void SetPeriodValues(IGameDate startDate, IGameDate endDate) {
            int years = endDate.Year - startDate.Year;
            int days = endDate.DayOfYear - startDate.DayOfYear;
            if (days < 0) {
                years--;
                days = _daysPerYear + days;
            }
            int hours = endDate.HourOfDay - startDate.HourOfDay;
            if (hours < 0) {
                days--;
                hours = _hoursPerDay + hours;
                if (days < 0) {
                    years--;
                    days = _daysPerYear + days;
                }
            }
            Years = years;
            Days = days;
            Hours = hours;
            PeriodInDays = years * _daysPerYear + days;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

