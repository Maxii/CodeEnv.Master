// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICanNavigate.cs
// Interface for an item that can navigate, aka Ships and Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for an item that can navigate, aka Ships and Fleets.
    /// </summary>
    public interface ICanNavigate {

        Player Owner { get; }

        /// <summary>
        /// The range of the weapons of this ICanNavigate Item. If a fleetCmd, the value returned is the Unit's WeaponRange.
        /// </summary>
        RangeDistance WeaponsRange { get; }

    }
}

