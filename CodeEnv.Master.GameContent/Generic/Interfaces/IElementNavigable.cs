﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementNavigable.cs
// INavigable destination that can be navigated to by any UnitElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// INavigable destination that can be navigated to by any UnitElement.
    /// <remarks>All INavigable destinations are also IElementNavigable including all AItems,
    /// Sector, Stationary/MovableLocs, FormationStation, CloseOrbitSimulator, etc.</remarks>
    /// <remarks>Ship's are the only element that can practically use all of these destinations
    /// due to their speed, but technically, a Facility could also, just at a much lower speed.</remarks>
    /// <remarks>4.3.17 Created to allow Facilities to receive orders with an IElementNavigable target,
    /// ala the way Ship uses IShipNavigable as its orders target.</remarks>
    /// </summary>
    public interface IElementNavigable : INavigable {



    }
}

