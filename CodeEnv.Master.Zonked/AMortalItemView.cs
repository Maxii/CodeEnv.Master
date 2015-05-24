// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemView.cs
// Abstract class managing the UI View for a mortal object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
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
///  Abstract class managing the UI View for a mortal object.
///  </summary>
public abstract class AMortalItemView : AFocusableItemView, IMortalViewable {

    #region Debug

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines showup as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    public override void LogEvent() {
        if (DebugSettings.Instance.EnableEventLogging) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackFrame(1);
            string name = Presenter != null ? Presenter.Model.FullName : _transform.name + "(from transform)";
            Debug.Log("{0}.{1}.{2}() called.".Inject(name, GetType().Name, stackFrame.GetMethod().Name));
        }
    }

    #endregion

    public new AMortalItemPresenter Presenter {
        get { return base.Presenter as AMortalItemPresenter; }
        protected set { base.Presenter = value; }
    }

    public AudioClip dying;
    public AudioClip hit;
    protected AudioSource _audioSource;
    protected Job _showingJob;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        LogEvent();
    }

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAttacked();
    }

    #endregion

    #region Animations

    // these must return onShowCompletion when finished
    private void ShowDying() {
        LogEvent();
        _showingJob = new Job(ShowingDying(), toStart: true);
    }

    // these run until finished with no requirement to return onShowCompletion
    private void ShowHit() {
        LogEvent();
        _showingJob = new Job(ShowingHit(), toStart: true);
    }

    protected virtual void ShowCmdHit() { LogEvent(); }

    protected virtual void ShowAttacking() { LogEvent(); }

    // these run continuously until they are stopped via StopAnimation() 
    protected virtual void ShowRepairing() { LogEvent(); }

    protected virtual void ShowRefitting() { LogEvent(); }

    protected virtual void ShowDisbanding() { LogEvent(); }

    private IEnumerator ShowingHit() {
        if (hit != null) {
            _audioSource.PlayOneShot(hit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;
        OnShowCompletion();
    }

    protected void OnShowCompletion() {
        if (onShowCompletion != null) {
            onShowCompletion();
        }
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsCameraTargetEligible {
        get { return PlayerIntel.CurrentCoverage != IntelCoverage.None; }
    }

    #endregion

    #region IMortalViewable Members

    public event Action onShowCompletion;

    public void ShowAnimation(EffectID animation) {
        switch (animation) {
            case EffectID.Dying:
                ShowDying();
                break;
            case EffectID.Hit:
                ShowHit();
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animation));
        }
    }

    public void StopAnimation(EffectID animation) {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
    }

    public virtual void OnDeath() { }

    #endregion

}

