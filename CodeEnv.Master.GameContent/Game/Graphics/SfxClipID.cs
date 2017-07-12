// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SfxClipID.cs
// The ID for SFX AudioClips that may be specifically accessed rather than randomly by GroupID.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The ID for SFX AudioClips that may be specifically accessed rather than randomly
    /// by GroupID. Used to generate the string equivalent for SoundManagerPro.
    /// </summary>
    public enum SfxClipID {

        None,

        Select,

        UnSelect,

        Explosion1,

        Error,

        Swipe,

        Tap,

        OpenShut

    }
}

