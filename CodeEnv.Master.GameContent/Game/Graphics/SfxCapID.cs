// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SfxCapID.cs
// The ID for a collection of SFX AudioClips that should use the same cap amount.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The ID for a collection of SFX AudioClips that should use the same cap amount.
    /// A cap amount is the number of instances of clips common to a GroupID
    /// or CapID that are allowed to play concurrently.
    /// Used to generate the string equivalent for SoundManagerPro.
    /// </summary>
    public enum SfxCapID {

        None,

        Explosions,

        Impacts,

        Operations

    }
}

