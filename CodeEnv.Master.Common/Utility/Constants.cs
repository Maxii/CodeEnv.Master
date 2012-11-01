// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Constants.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Utility {

    using System;
    using System.IO;

    /// <summary>
    /// Static Class of common constants. 
    /// </summary>
    public static class Constants {


        // Common Formats. Usage: ONE_DP.format(variable)
        //public sealed static DecimalFormat NO_DP = new DecimalFormat("0");
        //public sealed static DecimalFormat ONE_DP = new DecimalFormat("0.#");
        //public sealed static DecimalFormat TWO_DP = new DecimalFormat("0.##");
        //public sealed static DecimalFormat FOUR_DP = new DecimalFormat("0.####");

        // Common Strings
        public const string UserCurrentWorkingDirectoryPath = System.Environment.CurrentDirectory;
        public const string NewLine = System.Environment.NewLine;
        public const string Space = " ";
        public const string Period = ".";
        public const string Tab = "\t";
        public const string Comma = ",";
        public const string DoubleQuote = "\"";
        public const string SingleQuote = "'";
        public const string UnderScore = "_";
        public const string Ellipsis = "...";

        // Common Characters        
        public const char FileSeparator = Path.DirectorySeparatorChar;
        public const char PathSeparator = Path.PathSeparator;
        public const char CommaDelimiter = ',';
        public const char SpaceDelimiter = ' ';
        public const char PeriodDelimiter = '.';

        // Numbers and algebraic signs
        public const string PositiveSign = "+";
        public const string NegativeSign = "-";

        public const int One = 1;

        public const float Zero = 0.0F;
        public const float ZeroPercent = 0.0F;
        public const float OneHundredPercent = 1.0F;

        public const int OneKilobyte = 1024;
        public const int OneMegabyte = 1048576;

        // For monetary calculations
        public const int MoneyDecimalPlaces = 2;
        public const decimal ZeroMoney = 0.00M;

        // Time conversion factors
        public const long NanosecondsPerMillisecond = 1000000L;
        public const long NanosecondsPerSecond = 1000000000L;
        public const long MicrosecondsPerSecond = 1000000L;
        public const long MillisecondsPerSecond = 1000L;
        public const int SecondsPerMinute = 60;
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;

        // Booleans
        public const bool Pass = true;
        public const bool Fail = false;

        public const bool Won = true;
        public const bool Lost = false;

    }
}

