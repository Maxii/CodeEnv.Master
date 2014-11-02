// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseDirective.cs
// The directives that can be issued to a Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// The directives that can be issued to a Starbase.
    /// </summary>
    [Obsolete]
    public enum StarbaseDirective {

        None,
        Attack,
        StopAttack,
        Repair,
        Refit,
        Disband

    }
}

