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

// default namespace


/// <summary>
/// Restricted access Interface for GameDate.
/// </summary>
public interface IGameDate {

    int Year { get; }

    int DayOfYear { get; }

    int HourOfDay { get; }

    string FormattedDate { get; }

    string ToString();

}

