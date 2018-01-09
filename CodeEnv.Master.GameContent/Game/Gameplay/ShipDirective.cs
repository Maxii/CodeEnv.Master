// --------------------------------------------------------------------------------------------------------------------
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
        /// <remarks>12.12.17 Requires a null target.</remarks>
        /// </summary>
        Cancel,

        /// <summary>
        /// Ships 'manage' their own initial construction in a Base Hanger. When they receive this order, they are of course already
        /// instantiated and operational and have an existing Construction present (with no progress having occurred yet) in the 
        /// Base's ConstructionManager.
        /// <remarks>12.1.17 Requires a null Target.</remarks>
        /// </summary>
        Construct,

        /// <summary>
        /// Ships can attempt to assume their station in the fleet formation at any time and any location. When this order is given, 
        /// the ship attempts to move to its station within the formation. Only Fleet Cmd or the Ship's Captain may order a ship to assume
        /// its station. If this order is received by a ship in orbit, the ship will break orbit and move to their formation station. 
        /// <remarks> 12.17.17 If the ship is the Flagship and in close orbit, it will move to the closest local assembly station so that 
        /// other ships assuming station (if any) will not find their station within the item being close orbited.</remarks>
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
        /// Ships can repair where they are currently located, including in place on their formation station, at planets and Bases 
        /// they are allowed to orbit, and within a base's hanger.
        /// <remarks>12.13.17 Requires a non-null Target which can be a Base, Planet or the ship's own FleetCmd. If the target
        /// is the ship's own FleetCmd, the ship's FormationStation will be used to repair in place.</remarks>
        /// </summary>
        Repair,

        /// <summary>
        /// Instructs the ship to refit in the hanger of the target base. If already located in the hanger of the target
        /// base, the ship will initiate refitting. If not, it will attempt to transfer from its existing fleet and enter the
        /// target base's hanger and then initiate refitting.
        /// <remarks>UNCLEAR should ships be able to refit at ally bases? What about when base is under attack?</remarks>
        /// <remarks>11.8.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        Refit,

        /// <summary>
        /// A convenience 'short cut' directive for the user that implements the Fleet.JoinFleet directive for a single ship.
        /// It causes a separate 'transfer' fleet to be formed (if needed) and instructs that fleet to
        /// join a designated fleet owned by the same owner. Only available from a ship UserCtxMenu.
        /// <remarks>12.30.17 Requires a non-null (Fleet) Target.</remarks>
        /// </summary>
        JoinFleet,

        /// <summary>
        /// A convenience 'short cut' directive for the user that implements the Fleet.JoinHanger directive for a single ship.
        /// It causes a separate 'transfer' fleet to be formed (if needed) and instructs that fleet to
        /// join a designated Base Hanger owned by the same owner. Only available from a ship UserCtxMenu.
        /// <remarks>12.30.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        JoinHanger,

        /// <summary>
        /// Instructs the ship to attempt to transfer from its existing fleet and enter the hanger of the target base. 
        /// <remarks>12.31.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        EnterHanger,

        /// <summary>
        /// Ships can entrench on their FormationStation, diverting engine power from creating movement into protecting itself.
        /// An entrench order will cause the ship to relocate to its FormationStation (if not already there) and then entrench. 
        /// While entrenched, it will automatically repair itself if damaged.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Entrench,

        /// <summary>
        /// Ships disengage from active or potential combat with the enemy by attempting to redeploy to a more protected station 
        /// of the formation if any are available. Ships that are disengaged will automatically repair if needed while they are disengaged.
        /// Ships whose CombatStance is Disengage will automatically be issued a Disengage order by the Captain when ordered to Attack. 
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Disengage,

        /// <summary>
        /// The ship retreats away from the enemy at best speed. 
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
        /// Instructs the ship to disband in the hanger of the target base. If already located in the hanger of the target
        /// base, the ship will initiate disbanding. If not, it will attempt to transfer from its existing fleet and enter the
        /// target base's hanger and then initiate disbanding.
        /// When a ship is disbanded, the owner of the ship retains a percentage of the resources used to build it.
        /// <remarks>12.13.17 Requires a non-null (Base) Target.</remarks>
        /// </summary>
        Disband,

        /// <summary>
        /// Ships can be scuttled anywhere. No resources are retained by the owner.
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Scuttle,

        __ChgOwner,


    }
}

