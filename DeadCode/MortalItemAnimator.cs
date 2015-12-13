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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Animator for MortalItems.   IMPROVE
    /// </summary>
    [Obsolete]
    public class MortalItemAnimator : IAnimator {

        public event Action onAnimationFinished;

        protected Job _animationJob;
        protected Animation _animation;
        protected IAudioManager _audioMgr;  // IMPROVE static?

        public MortalItemAnimator(Animation animation) {
            _animation = animation;
            _animation.cullingType = AnimationCullingType.BasedOnRenderers;
            _animation.enabled = true;
            _audioMgr = References.SFXManager;
        }

        public void Start(EffectID animationID) {
            ShowAnimation(animationID);
        }

        public void Stop(EffectID animationID) {
            KillAnimationJob();
            //D.Warn("No Animation named {0} to stop.", animation.GetName());   // Commented out as most show Jobs not yet implemented
        }

        protected virtual void ShowAnimation(EffectID animationID) {
            KillAnimationJob();
            switch (animationID) {
                case EffectID.Dying:
                    ShowDying();
                    break;
                case EffectID.Hit:
                    ShowHit();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animationID));
            }
        }

        protected void KillAnimationJob() {
            if (_animationJob != null && _animationJob.IsRunning) {
                _animationJob.Kill();
            }
        }

        protected void OnAnimationFinished() {
            if (onAnimationFinished != null) {
                onAnimationFinished();
            }
        }

        private void ShowDying() {
            _animationJob = new Job(ShowingDying(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        private void ShowHit() {
            _animationJob = new Job(ShowingHit(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        private IEnumerator ShowingHit() {
            AudioClip hit = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Hit);
            if (hit != null) {
                _audioMgr.Play(hit);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        private IEnumerator ShowingDying() {
            AudioClip dying = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Dying);
            if (dying != null) {
                _audioMgr.Play(dying);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
            yield return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

