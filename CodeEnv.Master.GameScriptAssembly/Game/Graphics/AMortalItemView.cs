﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract class managing the UI View for a mortal object.
///  </summary>
public abstract class AMortalItemView : AFocusableItemView, IMortalViewable {

    public new AMortalItemPresenter Presenter {
        get { return base.Presenter as AMortalItemPresenter; }
        protected set { base.Presenter = value; }
    }

    public AudioClip dying;
    private AudioSource _audioSource;
    protected Job _showingJob;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
    }

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAttacked();
    }

    #endregion

    /// <summary>
    /// Safely invokes the onShowCompletion event.
    /// </summary>
    protected void OnShowCompletion() {
        var temp = onShowCompletion;
        if (temp != null) {
            temp();
        }
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        _collider.enabled = false;
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;
        OnShowCompletion();
    }

    #region ICameraTargetable Members

    public override bool IsEligible {
        get { return PlayerIntel.CurrentCoverage != IntelCoverage.None; }
    }

    #endregion

    #region IMortalViewable Members

    public event Action onShowCompletion;

    public void ShowDying() {
        _showingJob = new Job(ShowingDying(), toStart: true);
    }

    public override void AssessDiscernability() {
        IsDiscernible = InCameraLOS && PlayerIntel.CurrentCoverage != IntelCoverage.None && Presenter.IsAlive;
    }

    #endregion

}

