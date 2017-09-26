// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Constants.cs
// Static Class of common constants. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Static Class of common constants. 
    /// </summary>
    public static class Constants {

        // Composite Formatting for Argument Index 0      see  http://msdn.microsoft.com/en-us/library/txafckwd.aspx 

        /// <summary>
        /// Three significant digits.
        /// </summary>
        public const string FormatNumber_Default = "{0:G3}";

        /// <summary>
        /// Minimum 1 digit.
        /// </summary>
        public const string FormatInt_1DMin = "{0:0}";
        /// <summary>
        /// Minimum 2 digits.
        /// </summary>
        public const string FormatInt_2DMin = "{0:00}";
        /// <summary>
        /// Zero decimal places, rounded.
        /// </summary>
        public const string FormatFloat_0Dp = "{0:0.}";
        /// <summary>
        /// One decimal place, rounded.
        /// </summary>
        public const string FormatFloat_1Dp = "{0:0.0}";
        /// <summary>
        /// Up to one decimal place, rounded.
        /// </summary>
        public const string FormatFloat_1DpMax = "{0:0.#}";
        /// <summary>
        /// Two decimal places, rounded.
        /// </summary>
        public const string FormatFloat_2Dp = "{0:0.00}"; // read as for Argument Index 0 : Leading/Trailing zeros = 0 . Decimal places = 2
        /// <summary>
        /// Up to two decimal places, rounded.
        /// </summary>
        public const string FormatFloat_2DpMax = "{0:0.##}";    // read as for Argument Index 0 : Leading/Trailing zeros = 0 . Decimal places = up to 2
        /// <summary>
        /// Three decimal places, rounded.
        /// </summary>
        public const string FormatFloat_3Dp = "{0:0.000}";
        /// <summary>
        /// Up to three decimal places, rounded.
        /// </summary>
        public const string FormatFloat_3DpMax = "{0:0.###}";
        /// <summary>
        /// Four decimal places, rounded.
        /// </summary>
        public const string FormatFloat_4Dp = "{0:0.0000}";
        /// <summary>
        /// Up to four decimal places, rounded.
        /// </summary>
        public const string FormatFloat_4DpMax = "{0:0.####}";

        /// <summary>
        /// Two decimal places, rounded.
        /// </summary>
        public const string FormatPercent_2Dp = "{0:P02}";
        /// <summary>
        /// One decimal place, rounded.
        /// </summary>
        public const string FormatPercent_1Dp = "{0:P01}";
        /// <summary>
        /// Zero decimal places, rounded.
        /// </summary>
        public const string FormatPercent_0Dp = "{0:P00}";

        // Common Numeric Formatting  see http://msdn.microsoft.com/en-us/library/dwhawy9k.aspx
        public const string FormatCurrency_0Dp = "C0";
        public const string FormatCurrency_2Dp = "C2";
        public const string PercentNumericFormat = "P00";

        // 3.25.16 Moved to GameTime
        //public const string GameCalenderDateFormat = "{0}.{1:D3}.{2:00.}";  //= "{0}.{1:D3}.{2:D2}";
        //public const string GamePeriodYearsFormat = "{0} years, {1:D3} days, {2:00.#} hours"; //= "{0} years, {1:D3} days, {2:D2} hours";
        //public const string GamePeriodNoYearsFormat = "{0:D3} days, {1:00.#} hours";  //= "{0:D3} days, {1:D2} hours";
        //public const string GamePeriodHoursOnlyFormat = "{0:00.#} hours"; //= "{0:D2} hours";

        // Common Strings
        public static string UserCurrentWorkingDirectoryPath { get { return System.Environment.CurrentDirectory; } }

        public static string NewLine { get { return System.Environment.NewLine; } } // "\n"
        public const string Empty = "";
        public const string Space = " ";
        public const string Period = ".";
        public const string Tab = "\t";
        public const string Comma = ",";
        public const string DoubleQuote = "\"";
        public const string SingleQuote = "'";
        public const string Underscore = "_";
        public const string Ellipsis = "...";
        public const string SemiColon = ";";
        public const string Colon = ":";
        public const string QuestionMark = "?";
        public const string PercentSign = "%";
        public const string LessThan = "<";
        public const string GreaterThan = ">";
        /// <summary>
        /// The forward slash, aka division sign.
        /// </summary>
        public const string ForwardSlash = "/";
        public const string BackSlash = "\\";

        // Switch Strings
        public const string GodMode = "/GodMode";

        // Common Characters     
        public static char FileSeparator { get { return Path.DirectorySeparatorChar; } }

        public static char PathSeparator { get { return Path.PathSeparator; } }
        public const char CommaDelimiter = ',';
        public const char SpaceDelimiter = ' ';
        public const char PeriodDelimiter = '.';

        // Numbers and algebraic signs
        public const string PositiveSign = "+";
        public const string NegativeSign = "-";

        public const int MinusOne = -1;
        public const int Zero = 0;
        public const int One = 1;

        public const long ZeroL = 0L;

        public const float ZeroF = 0.0F;
        public const float ZeroPercent = 0.0F;
        public const float OneF = 1.0F;
        public const float OneHundredPercent = 1.0F;
        public static float OneThird = 1F / 3F;
        public static float TwoThirds = 2F / 3F;

        public const int OneKilobyte = 1024;
        public const int OneMegabyte = 1048576;

        public const int DegreesPerRotation = 360;
        public const int DegreesPerOrbit = 360;

        // For monetary calculations
        public const int MoneyDecimalPlaces = 2;
        public const decimal ZeroCurrency = 0.00M;

        // Time conversion factors
        public const long NanosecondsPerMillisecond = 1000000L;
        public const long NanosecondsPerSecond = 1000000000L;
        public const long MicrosecondsPerSecond = 1000000L;
        public const long MillisecondsPerSecond = 1000L;
        public const int SecondsPerMinute = 60;
        //public const int MinutesPerHour = 60;
        //public const int HoursPerDay = 24;

        // Booleans
        public const bool Pass = true;
        public const bool Fail = false;

        public const bool Won = true;
        public const bool Lost = false;

        private static Vector3[] _normalizedCubeVertices;
        /// <summary>
        /// Array of the eight vertices of a cube at a normalized distance of 1 unit from the cube center.
        /// </summary>
        public static Vector3[] NormalizedCubeVertices {
            get {
                if (_normalizedCubeVertices.IsNullOrEmpty()) {
                    var normalizedCubeVertices = new List<Vector3>(8);
                    var pair = new float[] { -1F, 1F };
                    foreach (var x in pair) {
                        foreach (var y in pair) {
                            foreach (var z in pair) {
                                var normalizedBoxVertex = new Vector3(x, y, z).normalized;
                                normalizedCubeVertices.Add(normalizedBoxVertex);
                            }
                        }
                    }
                    _normalizedCubeVertices = normalizedCubeVertices.ToArray();
                }
                //D.Log("Normalized cube vertices: {0}.", _normalizedCubeVertices.Concatenate());
                return _normalizedCubeVertices;
            }
        }


    }
}

