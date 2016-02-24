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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  The directives that can be issued to a fleet.
    /// </summary>
    public enum FleetDirective {

        None,

        /// <summary>
        /// Fleets can move to any INavigableTarget including other Units (fleets and bases), Planetoids, Stars, Systems,
        /// Sectors and the UniverseCenter. Ownership is not a factor. If a system or sector is designated as the move target 
        /// and the fleet is currently outside of the system or sector, the fleet will move to just outside the radius of 
        /// the system or sector. If the fleet is currently inside of the system or sector, the fleet will move to the 
        /// closest patrol point within the system or sector.
        /// </summary>
        Move,

        /// <summary>
        /// Fleets can patrol any IPatrollable target. This includes Systems, Sectors and Stars. Ownership is not a factor. 
        /// They can also patrol the UniverseCenter. If a system, sector or UniverseCenter is designated as the patrol 
        /// target the fleet will move to the target's closest patrol point and initiate a patrol pattern encompassing the 
        /// other patrol points in the area. If a Star is designated as the patrol target, the fleet will patrol the System.
        /// </summary>
        Patrol,

        /// <summary>
        /// Fleets can guard Units (Fleets and Bases), Planetoids, Stars, Systems and Sectors not owned by enemies 
        /// of the fleet owner. They can also guard the UniverseCenter. If a system or sector is designated as the 
        /// guard target the fleet will move to the closest patrol point within the system or sector to guard. 
        /// If a Star is designated as the guard target, the fleet will guard the System.
        /// </summary>
        Guard,

        /// <summary>
        /// Fleets can attack Units (Fleets and Bases), Planetoids and Systems owned by enemies of the fleet owner.
        /// If an enemy System is designated as the attack target, the fleet attacks the Settlement.
        /// </summary>
        Attack,

        /// <summary>
        /// Fleets can refit at Bases owned by the Fleet owner.
        /// If a System is designated as the refit target, the fleet is refit at the settlement.
        /// UNCLEAR should fleets be able to refit at ally bases?
        /// </summary>
        Refit,

        /// <summary>
        /// Fleets can repair at Bases owned by the Fleet owner.
        /// If a System is designated as the repair target, the fleet is repaired at the settlement.
        /// UNCLEAR where else can repairs take place and to what degree?
        /// </summary>
        Repair,

        /// <summary>
        /// Fleets can disband at Bases owned by the Fleet owner.
        /// If a System is designated as the disband target, the fleet is disbanded at the settlement.
        /// When a fleet is disbanded, the owner of the fleet retains a percentage of the resources used to build it.
        /// </summary>
        Disband,

        /// <summary>
        /// Fleets can assume their chosen formation at any time and any location although they must be stationary to
        /// do so. When this order is given, the ships of the fleet attempt to move to their stations within the formation. 
        /// When ships have idle time, they automatically move to their formation station if the FleetCmd is stationary 
        /// and the ship is not in orbit. Ships in orbit donot pay attention to formations. If this order is received 
        /// by a ship in orbit, it will immediately break orbit and move to their formation station.
        /// </summary>
        AssumeFormation,

        /// <summary>
        /// Fleets can join other fleets owned by the Fleet owner.
        /// </summary>
        Join,

        Retreat,
        StopAttack,
        Explore,

        /// <summary>
        /// Fleets can be scuttled anywhere. No resources are retained by the owner.
        /// </summary>
        Scuttle

    }
}

