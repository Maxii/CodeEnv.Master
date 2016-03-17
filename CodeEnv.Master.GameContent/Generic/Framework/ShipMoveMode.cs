// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipMoveMode.cs
// The mode a ship is to move in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The mode a ship is to move in.
    /// </summary>
    public enum ShipMoveMode {

        None,

        /// <summary>
        /// The ship moves without any attention to fleet coordination. This means:
        /// 1) departing as soon as the ship is on heading, 2) ignoring any
        /// fleet formation restrictions, and 3) moving at any speed it is individually capable of.
        /// </summary>
        ShipSpecific,

        /// <summary>
        /// The ship move is part of a larger fleet move so attention is paid to fleet coordination. This means:
        /// 1) waiting for the fleet to align on heading before initiating the move, 2) trying to move
        /// in formation with the fleet (-> respecting its _fstOffset), and 3) moving at speeds
        /// that the whole fleet can maintain.
        /// </summary>
        FleetWide

    }
}

