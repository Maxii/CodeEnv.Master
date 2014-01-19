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
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using System;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Data container class that holds the game date.
    /// </summary>
    public class GameDate : IGameDate, IEquatable<GameDate> {

        public enum PresetDateSelector {

            /// <summary>
            /// The game's starting date.
            /// </summary>
            Start,

            /// <summary>
            /// The current date in the game.
            /// </summary>
            Current
        }

        public static int DaysPerYear = GeneralSettings.Instance.DaysPerYear;
        public static float DaysPerSecond = GeneralSettings.Instance.DaysPerSecond;
        public static int StartingYear = GeneralSettings.Instance.StartingYear;

        public int DayOfYear { get; private set; }

        public int Year { get; private set; }

        public string FormattedDate {
            get {
                return Constants.GameDateFormat.Inject(Year, DayOfYear);
            }
        }

        /// <summary>
        /// Convenience constructor that initializes a new instance of the <see cref="GameDate" /> class
        /// set to Day 1 of the starting year.
        /// </summary>
        /// <param name="preset">The preset date selector.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public GameDate(PresetDateSelector preset) {
            switch (preset) {
                case PresetDateSelector.Start:
                    DayOfYear = Constants.One;
                    Year = StartingYear;
                    break;
                case PresetDateSelector.Current:
                    DayOfYear = GameTime.Date.DayOfYear;
                    Year = GameTime.Date.Year;
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(preset));
            }
        }

        public GameDate(int dayOfYear, int year) {
            Arguments.ValidateNotNegative(dayOfYear);
            DayOfYear = dayOfYear;
            Year = year;
        }

        internal void SyncDateToGameClock(float gameClock) {
            int elapsedDays = Mathf.FloorToInt(gameClock * DaysPerSecond);
            Year = StartingYear + Mathf.FloorToInt(elapsedDays / DaysPerYear);
            DayOfYear = 1 + (elapsedDays % DaysPerYear);
        }

        // Override object.Equals on reference types when you do not want your
        // reference type to obey reference semantics, as defined by System.Object.
        // Always override ValueType.Equals for your own Value Types.
        public override bool Equals(object right) {
            // TODO the containing class T must extend IEquatable<T>
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238 aka
            // "Rarely override the operator==() when you create reference types as
            // the .NET Framework classes expect it to follow reference semantics for
            // all reference types. Always override the == operator for your own
            // Value Types. See Effective C#, Item 6.

            // No need to check 'this' for null as the CLR throws an exception before
            // calling any instance method through a null reference.
            if (object.ReferenceEquals(right, null)) {
                return false;
            }

            if (object.ReferenceEquals(this, right)) {
                return true;
            }

            if (this.GetType() != right.GetType()) {
                return false;
            }

            // now call IEquatable's Equals
            return this.Equals(right as GameDate);
        }

        // Generally, do not override object.GetHashCode as object's version is reliable
        // although not efficient. You should override it IFF operator==() is redefined which
        // is rare. 
        // You should always override ValueType.GetHashCode and redefine ==() for your
        // value types. If the value type is used as a hash key, it must be immutable.
        // See Effective C# Item 7.
        public override int GetHashCode() {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }

        public override string ToString() {
            return FormattedDate;
        }

        #region IEquatable<Intel> Members

        public bool Equals(GameDate other) {
            // TODO add your equality test here. Call the base class Equals only if the
            // base class version is not provided by System.Object or System.ValueType
            // as all that occurs is either a check for reference equality or content equality.
            return DayOfYear == other.DayOfYear && Year == other.Year;
        }

        #endregion

    }

}

