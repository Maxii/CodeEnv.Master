// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipTransitBanned.cs
// Interface for Items that implement a surrounding zone where ships cannot transit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that implement a surrounding zone where ships cannot transit.
    /// </summary>
    public interface IShipTransitBanned {

        /// <summary>
        /// The radius of the Ship TransitBan around this Item.
        /// </summary>
        float ShipTransitBanRadius { get; }

        string FullName { get; }

    }
}

