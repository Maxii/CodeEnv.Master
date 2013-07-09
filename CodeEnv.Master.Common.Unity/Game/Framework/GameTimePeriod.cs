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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container class that holds a duration of GameTime.
    /// </summary>
    public class GameTimePeriod {

        private static int daysPerYear = TempGameValues.DaysPerGameYear;

        private IGameDate _startDate;

        public int Years { get; private set; }

        public int Days { get; private set; }

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
        public GameTimePeriod(int days, int years) {
            Arguments.ValidateForRange(days, Constants.Zero, 99);
            Arguments.ValidateNotNegative(years);
            Days = days;
            Years = years;
            PeriodInDays = years * daysPerYear + days;
            GenerateArtificialStartDate(days, years);
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

        public void UpdatePeriod(IGameDate currentDate) {
            SetPeriodValues(_startDate, currentDate);
        }

        private void SetPeriodValues(IGameDate startDate, IGameDate endDate) {
            int years = endDate.Year - startDate.Year;
            int days = endDate.DayOfYear - startDate.DayOfYear;
            if (days < 0) {
                years--;
                days = daysPerYear + days;
            }
            Years = years;
            Days = days;
            PeriodInDays = years * daysPerYear + days;
        }

        private void GenerateArtificialStartDate(int days, int years) {
            IGameDate currentDate = GameTime.Date;
            int startYear = currentDate.Year - years;
            int startDay = currentDate.DayOfYear - days;
            if (startDay < 0) {
                startYear--;
                startDay = daysPerYear + startDay;
            }
            _startDate = new GameDate(startDay, startYear);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

