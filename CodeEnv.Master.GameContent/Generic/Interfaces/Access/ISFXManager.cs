// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISFXManager.cs
// Interface for access to Game SoundEffects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for access to Game SoundEffects.
    /// </summary>
    public interface ISFXManager {

        /// <summary>
        /// Plays the specified 2D AudioClip from Vector3.zero.
        /// Warning: If the AudioClip is encoded 3D, the user may not hear it
        /// as the location it is play from is Vector3.zero.
        /// </summary>
        /// <param name="clipID">The ID for the 2D AudioClip.</param>
        /// <returns></returns>
        AudioSource PlaySFX(SfxClipID clipID);

        /// <summary>
        /// Plays the specified 3D AudioClip on the designated GameObject.
        /// </summary>
        /// <param name="go">The GameObject to play on.</param>
        /// <param name="clipID">The ID for the 3D AudioClip.</param>
        /// <param name="toLoop">if set to <c>true</c> [to loop].</param>
        /// <returns></returns>
        AudioSource PlaySFX(GameObject go, SfxClipID clipID, bool toLoop = false);

        /// <summary>
        /// Plays a random 3D AudioClip acquired from the specified Group on the designated GameObject.
        /// </summary>
        /// <param name="go">The GameObject to play on.</param>
        /// <param name="groupID">The ID for the SFXGroup.</param>
        /// <param name="toLoop">if set to <c>true</c> [to loop].</param>
        /// <returns></returns>
        AudioSource PlaySFX(GameObject go, SfxGroupID groupID, bool toLoop = false);

        /// <summary>
        /// Plays the specified 3D AudioClip on the designated GameObject IFF 
        /// the number of clip instances currently playing with <c>capID</c> does
        /// not exceed the cap amount associated with the clip.
        /// </summary>
        /// <param name="go">The GameObject to play on.</param>
        /// <param name="clipID">The ID of the AudioClip.</param>
        /// <param name="capID">The ID for the cap.</param>
        /// <returns></returns>
        AudioSource PlayCappedSFX(GameObject go, SfxClipID clipID, SfxCapID capID);

    }
}

