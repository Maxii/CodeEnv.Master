// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OutputsID.cs
// Unique identifier for an output - aka food, prod'n, income, expense, netIncome, science or culture.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for an output - aka food, prod'n, income, expense, netIncome, science or culture.
    /// </summary>
    public enum OutputsID {

        None = 0,

        Food = 1,

        Prodn = 2,

        Income = 3,

        Expense = 4,

        NetIncome = 5,

        Science = 6,

        Culture = 7

    }
}

