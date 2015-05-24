// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EffectID.cs
//  Enum specifying the ID of the Effect to show.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum specifying the ID of the Effect to show.
    /// </summary>
    public enum EffectID {

        None,

        Attacking,

        Hit,

        CmdHit,

        Refitting,

        Repairing,

        Disbanding,

        Dying

    }
}

