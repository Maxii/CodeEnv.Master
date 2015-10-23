// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDirective.cs
// The directives that can be issued to a facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a facility.
    /// </summary>
    public enum FacilityDirective {

        None,

        Attack,

        StopAttack,

        Repair,

        // Refit, Disband and SelfDestruct can also be issued by the Player

        Refit,

        Disband,

        Scuttle

    }
}

