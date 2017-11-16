﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDirective.cs
// The directives that can be issued to a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a ship.
    /// </summary>
    public enum ShipDirective {

        None,

        /// <summary>
        /// Cancels any unit orders issued while in the CURRENT pause. Orders issued in a prior pause will not be canceled.
        /// <remarks>8.14.17 Must be issued by the User and only during pause.</remarks>
        /// <remarks>11.8.17 No requirements for Target.</remarks>
        /// </summary>
        Cancel,

        /// <summary>
        /// Ships can attempt to assume their station in the fleet formation at any time and any location. When this order is given, 
        /// the ship attempts to move to its station within the formation. Only Fleet Cmd or the Ship's Captain may order a ship to assume
        /// its station. Ships in orbit do not pay attention to formations. If this order is received 
        /// by a ship in orbit, the ship will immediately break orbit and move to their formation station.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        AssumeStation,

        /// <summary>
        /// Ships can assume a close orbit around any IShipCloseOrbitable target including Bases, Planets, Stars and the UniverseCenter.
        /// To be allowed to go into close orbit, the owner of the orbit target cannot be at war with the ship's owner. For a Base, the
        /// base owner cannot be an enemy which includes being in a state of ColdWar with the ship's owner.
        /// <remarks>4.5.17 Removed as not an issuable order by CmdStaff, AI or User as it makes no sense to close orbit something
        /// en-masse as a fleet. The state AssumingCloseOrbit is used but as a Call()ed state.</remarks>
        /// </summary>
        ////AssumeCloseOrbit,

        /// <summary>
        /// Ships can move to all INavigableTargets including Units (fleets and bases), Planetoids, Stars, Systems,
        /// Sectors, the UniverseCenter, Waypoints, OrbitSlots and FormationStations. Ownership is not a factor. 
        /// Only Fleet Cmd or the User may order a ship to move. When moving, ships automatically avoid obstacles 
        /// by adding in a detour waypoint to avoid the obstacle before resuming its move to its targeted destination.
        /// <remarks>11.8.17 Requires a non-null Target.</remarks>
        /// </summary>
        Move,

        /// <summary>
        /// Ships can attack Unit Elements (Ships and Facilities) and Planetoids owned by enemies of the ship's owner.
        /// Only Fleet Cmd may order a ship to attack.
        /// <remarks>11.8.17 Currently requires a non-null (Unit) Target. IMPROVE Element target should be contained in orders from FleetCmd.</remarks>
        /// </summary>
        Attack,

        /// <summary>
        /// Ships can repair where they are currently located, including in space and at planets and Bases they are allowed to orbit. 
        /// Only Fleet Cmd, the Ship's Captain or the User may order a ship to repair.
        /// <remarks>11.8.17 Target can be null. If null, the ship's FormationStation is used to repair in place.</remarks>
        /// </summary>
        Repair,

        /// <summary>
        /// Ships can refit at Bases owned by the ship's owner. Only Fleet Cmd or the User/PlayerAI may order a ship to refit.
        /// UNCLEAR should ships be able to refit at ally bases? What about when base is under attack?
        /// <remarks>11.8.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        Refit,

        /// <summary>
        /// Ships can leave one fleet and join another owned by the ship's owner. A Join order is only issuable
        /// by Fleet Cmd or the User/PlayerAI.
        /// <remarks>11.8.17 Requires a non-null (Fleet) Target.</remarks>
        /// </summary>
        Join,

        /// <summary>
        /// Ships can entrench where they are currently located, diverting engine power from creating movement into protecting itself.
        /// An entrench order can be issued by Fleet Cmd or the Ship's Captain.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Entrench,

        /// <summary>
        /// Ships disengage from active or potential combat with the enemy by redeploying to a more protected station 
        /// of the formation, if any are available. Ships whose CombatStance is Disengage will automatically be issued a
        /// Disengage order by the Captain when ordered to Attack. A Disengage order can be issued by the User, 
        /// Fleet Cmd or the Ship's Captain.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Disengage,

        /// <summary>
        /// The ship retreats away from the enemy at best speed. Only Fleet Cmd or the User may order the ship to retreat.
        /// <remarks>11.8.17 Target requirements not yet determined.</remarks>
        /// </summary>
        Retreat,

        /// <summary>
        /// Ships can explore Stars and Planets which involves going into orbit for a period to scan it. Only Fleet Cmd may
        /// order a ship to explore a star or planet. Ships may not 'explore' (orbit) a Star or Planet they are at war with.
        /// <remarks>11.8.17 Requires a non-null Target.</remarks>
        /// </summary>
        Explore,

        /// <summary>
        /// Ships can disband at Bases owned by the ship's owner. Ships can only be ordered to Disband by Fleet Cmd or
        /// the User/PlayerAi. When a ship is disbanded, the owner of the ship retains a percentage of the resources used to build it.
        /// <remarks>11.8.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        Disband,

        /// <summary>
        /// Ships can be scuttled anywhere. No resources are retained by the owner. Scuttle orders are only issuable by the User.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Scuttle,

        ChgOwner,

        StopAttack


    }
}

