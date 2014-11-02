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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a ship.
    /// </summary>
    public enum ShipDirective {

        None,

        AssumeStation,

        //AssumeOrbit,

        Move,

        Attack,

        StopAttack,

        Repair,

        Entrench,

        // Refit, Disband,  Join and SelfDestruct can also be issued by the Player

        Refit,

        Disband,

        Join,

        SelfDestruct

    }
}

