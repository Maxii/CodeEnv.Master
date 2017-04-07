// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetDirective.cs
//  The directives that can be issued to a fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  The directives that can be issued to a fleet.
    /// </summary>
    public enum FleetDirective {

        None,

        /// <summary>
        /// Fleets can move to most INavigableTargets including other Units (fleets and bases), Planetoids, Stars, Systems,
        /// Sectors and the UniverseCenter. Diplomatic state with the owner, if any, is not a factor. The speed of the move
        /// is chosen by Fleet Cmd with the exception of FullSpeedMove below. If the target of the move is IShipOrbitable,
        /// then the fleet will go into high orbit until it receives another order.
        /// If a system or sector is designated as the move target and the fleet is currently outside of the system/sector, 
        /// the fleet will move to just outside the radius of the system/sector. If the fleet is currently inside the 
        /// system/sector, the fleet will move to the closest patrol point within the system/sector. 
        /// </summary>
        Move,

        /// <summary>
        /// Fleets can move to most INavigableTargets including other Units (fleets and bases), Planetoids, Stars, Systems,
        /// Sectors and the UniverseCenter. Diplomatic state with the owner, if any, is not a factor. The move will occur at FullSpeed.
        /// If the target of the move is IShipOrbitable, then the fleet will go into high orbit until it receives another order.
        /// If a system or sector is designated as the move target and the fleet is currently outside of the system/sector, 
        /// the fleet will move to just outside the radius of the system/sector. If the fleet is currently inside the 
        /// system/sector, the fleet will move to the closest patrol point within the system/sector. 
        /// </summary>
        FullSpeedMove,

        /// <summary>
        /// Fleets can go into close orbit around Bases, Stars, Planets and the UniverseCenter. 
        /// Diplomatic state with the owner, if any, cannot be at war, and with Bases cannot be an enemy.
        /// </summary>
        //[System.Obsolete]
        //CloseOrbit,

        /// <summary>
        /// Fleets can patrol any IPatrollable target including Bases, Systems, Sectors and the UniverseCenter. 
        /// Diplomatic state with the owner, if any, cannot be an enemy. When a fleet is ordered to patrol the 
        /// fleet will move to the target's closest patrol station and initiate a patrol pattern encompassing 
        /// the other patrol stations of the target. 
        /// </summary>
        Patrol,

        /// <summary>
        /// Fleets can guard any IGuardable target including Bases, Systems, Sectors and the UniverseCenter. 
        /// Diplomatic state with the owner, if any, cannot be an enemy. When a fleet is ordered to guard a target, the fleet
        /// will move to the target's closest guard station and reside there waiting for an enemy to be detected.
        /// </summary>
        Guard,

        /// <summary>
        /// Fleets can explore Systems, Sectors and the UniverseCenter. Planets and the Star are explored by ships executing
        /// a Explore order from the fleet. This occurs automatically when a fleet is ordered to explore a system. 
        /// Diplomatic state with the owner, if any, cannot be at war.
        /// </summary>
        Explore,

        /// <summary>
        /// Fleets can attack Units (Fleets and Bases), Planetoids and Systems owned by enemies of the fleet owner.
        /// If an enemy System is designated as the attack target, the fleet attacks the Settlement. Diplomatic state 
        /// with the owner must be an enemy.
        /// </summary>
        Attack,

        /// <summary>
        /// Fleets can refit at Bases owned by the Fleet owner.
        /// UNCLEAR should fleets be able to refit at ally bases? What about when base is under attack?
        /// </summary>
        Refit,

        /// <summary>
        /// Fleets can repair at Bases owned by the Fleet owner.
        /// UNCLEAR where else can repairs take place and to what degree?
        /// </summary>
        Repair,

        /// <summary>
        /// Fleets can disband at Bases owned by the Fleet owner.
        /// When a fleet is disbanded, the owner of the fleet retains a percentage of the resources used to build it.
        /// </summary>
        Disband,

        /// <summary>
        /// Fleets can assume their chosen formation at any time and any location although they must be stationary??? to
        /// do so. When this order is given, the ships of the fleet attempt to move to their stations within the formation. 
        /// When ships have idle time, they automatically move to their formation station if the FleetCmd is stationary
        /// and the ship is not in orbit. If this order is received by a ship in orbit, it will immediately break orbit 
        /// and move to their formation station.
        /// </summary>
        AssumeFormation,

        Regroup,

        /// <summary>
        /// Fleets can join other fleets owned by the Fleet owner.
        /// </summary>
        Join,

        /// <summary>
        /// Fleets can execute a covered retreat. Healthier ships in the fleet stay in 
        /// line and provide cover fire to keep the enemy from pursuing while seriously damaged ships retreat. 
        /// </summary>
        Withdraw,

        /// <summary>
        /// Each ship in the fleet retreats at best speed.
        /// </summary>
        Retreat,

        StopAttack,

        /// <summary>
        /// Fleets can be scuttled anywhere. No resources are retained by the owner. Scuttle orders are only 
        /// issuable by the User???
        /// </summary>
        Scuttle,

        /// <summary>
        /// Fleet's can be instructed to assign a new ship as the HQElement.
        /// <remarks>Implemented directly by changing the Cmd's HQElement.</remarks>
        /// </summary>
        ChangeHQ

        /************************************ Possible Directives *****************************************/
        /***************************************************************************************************
         * Entrench - would issue ShipDirective.Entrench to all ships in Fleet. 6.28.16 Not planned
         * 
         * 
         * 
         ***************************************************************************************************/

    }
}

