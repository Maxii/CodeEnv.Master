// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementItemAnimationController.cs
//  Animator for ElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Animator for ElementItems.
/// </summary>
public class ElementItemAnimator : MortalItemAnimator {

    public ElementItemAnimator(Animation animation, AudioSource audioSource) : base(animation, audioSource) { }

    protected override void ShowAnimation(MortalAnimations animationID) {
        switch (animationID) {
            case MortalAnimations.Dying:
                base.ShowAnimation(animationID);
                break;
            case MortalAnimations.Hit:
                base.ShowAnimation(animationID);
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animationID));
        }
    }

    // these run until finished with no requirement to call OnShowCompletion
    protected void ShowCmdHit() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingCmdHit(), toStart: true);
    }

    protected void ShowAttacking() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingAttacking(), toStart: true);
    }

    // these run continuously until they are stopped via StopAnimation() 
    protected void ShowRepairing() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingRepairing(), toStart: true);
    }

    protected void ShowRefitting() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingRefitting(), toStart: true);
    }

    protected void ShowDisbanding() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingDisbanding(), toStart: true);
    }

    private IEnumerator ShowingCmdHit() {
        AudioClip cmdHit = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.CmdHit);
        if (cmdHit != null) {
            _audioSource.PlayOneShot(cmdHit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingAttacking() {
        AudioClip attacking = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Attacking);
        if (attacking != null) {
            _audioSource.PlayOneShot(attacking);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRefitting() {
        AudioClip refitting = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Refitting);
        if (refitting != null) {
            _audioSource.PlayOneShot(refitting);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not useOnShowCompletion
    }

    private IEnumerator ShowingDisbanding() {
        AudioClip disbanding = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Disbanding);
        if (disbanding != null) {
            _audioSource.PlayOneShot(disbanding);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRepairing() {
        AudioClip repairing = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Repairing);
        if (repairing != null) {
            _audioSource.PlayOneShot(repairing);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

