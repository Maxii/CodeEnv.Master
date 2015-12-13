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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Animator for ElementItems.
    /// </summary>
    [Obsolete]
    public class ElementItemAnimator : MortalItemAnimator {

        public ElementItemAnimator(Animation animation) : base(animation) { }

        protected override void ShowAnimation(EffectID animationID) {
            KillAnimationJob();
            switch (animationID) {
                case EffectID.Dying:
                    base.ShowAnimation(animationID);
                    break;
                case EffectID.Hit:
                    base.ShowAnimation(animationID);
                    break;
                case EffectID.Attacking:
                    ShowAttacking();
                    break;
                case EffectID.CmdHit:
                    ShowCmdHit();
                    break;
                case EffectID.Disbanding:
                    ShowDisbanding();
                    break;
                case EffectID.Refitting:
                    ShowRefitting();
                    break;
                case EffectID.Repairing:
                    ShowRepairing();
                    break;
                case EffectID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animationID));
            }
        }

        protected void ShowCmdHit() {
            _animationJob = new Job(ShowingCmdHit(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        protected void ShowAttacking() {
            _animationJob = new Job(ShowingAttacking(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        protected void ShowRepairing() {
            _animationJob = new Job(ShowingRepairing(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        protected void ShowRefitting() {
            _animationJob = new Job(ShowingRefitting(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        protected void ShowDisbanding() {
            _animationJob = new Job(ShowingDisbanding(), toStart: true, onJobComplete: (wasKilled) => {
                OnAnimationFinished();
            });
        }

        private IEnumerator ShowingCmdHit() {
            AudioClip cmdHit = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.CmdHit);
            if (cmdHit != null) {
                _audioMgr.Play(cmdHit);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        private IEnumerator ShowingAttacking() {
            AudioClip attacking = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Attacking);
            if (attacking != null) {
                _audioMgr.Play(attacking);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        private IEnumerator ShowingRefitting() {
            AudioClip refitting = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Refitting);
            if (refitting != null) {
                _audioMgr.Play(refitting);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        private IEnumerator ShowingDisbanding() {
            AudioClip disbanding = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Disbanding);
            if (disbanding != null) {
                _audioMgr.Play(disbanding);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        private IEnumerator ShowingRepairing() {
            AudioClip repairing = UnityUtility.__GetAudioClip(UnityUtility.AudioClipID.Repairing);
            if (repairing != null) {
                _audioMgr.Play(repairing);
            }
            //animation.Stop();
            //yield return UnityUtility.PlayAnimation(animation, "hit");  
            yield return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

