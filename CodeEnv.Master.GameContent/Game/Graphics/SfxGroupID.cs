// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SfxGroupID.cs
// The ID for a Group of SFX AudioClips. Typically used to gain
// access to a randomly selected AudioClip from the Group.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The ID for a Group of SFX AudioClips. Typically used to gain
    /// access to a randomly selected AudioClip from the Group.
    /// Used to generate the string equivalent for SoundManagerPro.
    /// </summary>
    public enum SfxGroupID {

        None,

        Explosions,

        ProjectileImpacts,

        BeamOperations


    }
}

