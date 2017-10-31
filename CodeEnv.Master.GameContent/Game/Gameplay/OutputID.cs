// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OutputID.cs
// Unique identifier for an output - aka food, prod'n, income, expense, netIncome, science or culture.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Unique identifier for an output - aka food, prod'n, income, expense, netIncome, science or culture.
    /// </summary>
    public enum OutputID {

        None = 0,

        [EnumAttribute("F")]
        Food = 1,

        [EnumAttribute("P")]
        Production = 2,

        [EnumAttribute("In")]
        Income = 3,

        [EnumAttribute("Ex")]
        Expense = 4,

        [EnumAttribute("Net")]
        NetIncome = 5,

        [EnumAttribute("Sc")]
        Science = 6,

        [EnumAttribute("Cu")]
        Culture = 7

    }
}

