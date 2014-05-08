// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGameDate.cs
// Restricted access Interface for GameDate.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Restricted access Interface for GameDate.
    /// </summary>
    [Obsolete]
    public interface IGameDate {

        int Year { get; }

        int DayOfYear { get; }

        int HourOfDay { get; }

        string FormattedDate { get; }

        string ToString();

    }
}

