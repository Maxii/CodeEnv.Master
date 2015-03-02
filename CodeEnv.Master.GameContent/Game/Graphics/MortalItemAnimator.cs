// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MortalItemAnimator.cs
// Animator for MortalItems.
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
/// Animator for MortalItems.   IMPROVE
/// </summary>
public class MortalItemAnimator : IAnimator {

    public event Action onAnimationFinished;

    protected Job _animationJob;
    protected Animation _animation;
    protected AudioSource _audioSource;

    public MortalItemAnimator(Animation animation, AudioSource audioSource) {
        _animation = animation;
        _animation.cullingType = AnimationCullingType.BasedOnRenderers;
        _animation.enabled = true;
        _audioSource = audioSource;
    }

    public void Start(MortalAnimations animationID) {
        ShowAnimation(animationID);
    }

    public void Stop(MortalAnimations animationID) {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
            return;
        }
        //D.Warn("No Animation named {0} to stop.", animation.GetName());   // Commented out as most show Jobs not yet implemented
    }

    protected virtual void ShowAnimation(MortalAnimations animationID) {
        switch (animationID) {
            case MortalAnimations.Dying:
                ShowDying();
                break;
            case MortalAnimations.Hit:
                ShowHit();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animationID));
        }
    }

    protected void OnAnimationFinished() {
        if (onAnimationFinished != null) {
            onAnimationFinished();
        }
    }

    // these must call OnShowCompletion when finished
    private void ShowDying() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingDying(), toStart: true, onJobComplete: (wasKilled) => {
            OnAnimationFinished();
        });
    }

    // these run until finished with no requirement to call OnShowCompletion
    private void ShowHit() {
        if (_animationJob != null && _animationJob.IsRunning) {
            _animationJob.Kill();
        }
        _animationJob = new Job(ShowingHit(), toStart: true);
    }

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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

