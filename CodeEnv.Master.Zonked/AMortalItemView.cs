// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemView.cs
// Abstract base class for managing the elements of an object that is both Mortal and Focusable.
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
///  Abstract base class for managing the elements of an object that is both Mortal and Focusable.
/// </summary>
[Obsolete]
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

    protected override void OnClick() {
        base.OnClick();
        if (IsDiscernible) {
            if (GameInputHelper.IsLeftMouseButton()) {
                KeyCode notUsed;
                if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    OnAltLeftClick();
                }
                else {
                    OnLeftClick();
                }
            }
        }
    }

    protected virtual void OnLeftClick() { }

    protected virtual void OnAltLeftClick() { }

    void OnDoubleClick() {
        if (IsDiscernible && GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    /// <summary>
    /// Safely invokes the onShowCompletion event.
    /// </summary>
    protected void OnShowCompletion() {
        var temp = onShowCompletion;
        if (temp != null) {
            temp();
        }
    }

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return PlayerIntel.CurrentCoverage != IntelCoverage.None;
        }
    }

    #endregion

    #region IMortalViewable Members

    public event Action onShowCompletion;

    public void ShowDying() {
        _showingJob = new Job(ShowingDying(), toStart: true);
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

    #endregion

}

