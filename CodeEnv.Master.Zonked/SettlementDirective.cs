// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementDirective.cs
// The directives that can be issued to a Settlement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// The directives that can be issued to a Settlement.
    /// </summary>
    [Obsolete]
    public enum SettlementDirective {

        None,
        Attack,
        StopAttack,
        Repair,
        Refit,
        Disband

    }
}

