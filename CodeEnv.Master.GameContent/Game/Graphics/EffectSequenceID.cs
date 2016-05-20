// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EffectSequenceID.cs
//  Enum specifying the ID of the EffectSequence to show.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum specifying the ID of the EffectSequence to show.
    /// <remarks>Identifies one or more effects that should play in sequence.
    /// The effects themselves are identified by EffectID.</remarks>
    /// </summary>
    public enum EffectSequenceID {

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

