// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public abstract class AMortalItemDisplayManager : ADiscernibleItemDisplayManager {

    protected new AMortalItem Item { get { return base.Item as AMortalItem; } }

    protected AudioSource _audioSource;
    protected Job _showingJob;


    public AMortalItemDisplayManager(AMortalItem item)
        : base(item) {
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(item.gameObject);
    }

    protected void OnShowCompletion() {
        Item.OnShowCompletion();
    }

    // these must call OnShowCompletion when finished
    private void ShowDying() {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingDying(), toStart: true, onJobComplete: (wasKilled) => {
            OnShowCompletion();
        });
    }

    // these run until finished with no requirement to call OnShowCompletion
    private void ShowHit() {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingHit(), toStart: true);
    }

    protected virtual void ShowCmdHit() { }

    protected virtual void ShowAttacking() { }

    // these run continuously until they are stopped via StopAnimation() 
    protected virtual void ShowRepairing() { }

    protected virtual void ShowRefitting() { }

    protected virtual void ShowDisbanding() { }

    private IEnumerator ShowingHit() {
        AudioClip hit = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Hit);
        if (hit != null) {
            _audioSource.PlayOneShot(hit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingDying() {
        AudioClip dying = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Dying);
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;
    }

    public void ShowAnimation(MortalAnimations animation) {
        switch (animation) {
            case MortalAnimations.Dying:
                ShowDying();
                break;
            case MortalAnimations.Hit:
                ShowHit();
                break;
            case MortalAnimations.Attacking:
                ShowAttacking();
                break;
            case MortalAnimations.CmdHit:
                ShowCmdHit();
                break;
            case MortalAnimations.Disbanding:
                ShowDisbanding();
                break;
            case MortalAnimations.Refitting:
                ShowRefitting();
                break;
            case MortalAnimations.Repairing:
                ShowRepairing();
                break;
            case MortalAnimations.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animation));
        }
    }

    public void StopAnimation(MortalAnimations animation) {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
            return;
        }
        //D.Warn("No Animation named {0} to stop.", animation.GetName());   // Commented out as most show Jobs not yet implemented
    }

}

