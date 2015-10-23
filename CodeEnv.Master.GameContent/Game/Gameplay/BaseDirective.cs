// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseDirective.cs
// The directives that can be issued to a Base.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a Base.
    /// </summary>
    public enum BaseDirective {

        None,
        Attack,
        StopAttack,
        Repair,
        Refit,
        Disband,
        Scuttle

    }
}

