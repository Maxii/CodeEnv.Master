// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SFXManager.cs
// Singleton controlling Game SoundEffects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton controlling Game SoundEffects.
/// </summary>
public class SFXManager : AMonoSingleton<SFXManager>, ISFXManager {

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SFXManager = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        // TODO
    }

    #endregion

    /// <summary>
    /// Plays the specified 2D AudioClip from Vector3.zero.
    /// Warning: If the AudioClip is encoded 3D, the user may not hear it
    /// as the location it is play from is Vector3.zero.
    /// </summary>
    /// <param name="clipID">The ID for the 2D AudioClip.</param>
    /// <returns></returns>
    public AudioSource PlaySFX(SfxClipID clipID) {
        // TODO verify clip is coded as 2D - need AudioImporter???
        return SoundManager.PlaySFX(clipID.GetValueName());
    }

    /// <summary>
    /// Plays the specified 3D AudioClip on the designated GameObject.
    /// </summary>
    /// <param name="go">The GameObject to play on.</param>
    /// <param name="clipID">The ID for the 3D AudioClip.</param>
    /// <param name="toLoop">if set to <c>true</c> [to loop].</param>
    /// <returns></returns>
    public AudioSource PlaySFX(GameObject go, SfxClipID clipID, bool toLoop = false) {
        // TODO verify clip is coded as 3D - need AudioImporter???
        return SoundManager.PlaySFX(go, clipID.GetValueName(), toLoop);
    }

    /// <summary>
    /// Plays a random 3D AudioClip acquired from the specified Group on the designated GameObject.
    /// </summary>
    /// <param name="go">The GameObject to play on.</param>
    /// <param name="groupID">The ID for the SFXGroup.</param>
    /// <param name="toLoop">if set to <c>true</c> [to loop].</param>
    /// <returns></returns>
    public AudioSource PlaySFX(GameObject go, SfxGroupID groupID, bool toLoop = false) {
        // TODO verify clip is coded as 3D - need AudioImporter???
        var clip = SoundManager.LoadFromGroup(groupID.GetValueName());
        return SoundManager.PlaySFX(go, clip, toLoop);
    }

    /// <summary>
    /// Plays the specified 3D AudioClip on the designated GameObject IFF 
    /// the number of clip instances currently playing with <c>capID</c> does
    /// not exceed the cap amount associated with the clip.
    /// </summary>
    /// <param name="go">The GameObject to play on.</param>
    /// <param name="clipID">The ID of the AudioClip.</param>
    /// <param name="capID">The ID for the cap.</param>
    /// <returns></returns>
    public AudioSource PlayCappedSFX(GameObject go, SfxClipID clipID, SfxCapID capID) {
        // TODO verify clip is coded as 3D - need AudioImporter???
        var aS = go.AddMissingComponent<AudioSource>();
        return SoundManager.PlayCappedSFX(aS, clipID.GetValueName(), capID.GetValueName());
    }

    protected override void ExecutePriorToDestroy() {
        base.ExecutePriorToDestroy();
        // TODO tasks to execute before this extra copy of this persistent singleton is destroyed. Default does nothing
    }

    #region Cleanup

    protected override void Cleanup() {
        // TODO
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

