﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementView.cs
// A class for managing the UI of a Settlement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a Settlement.
/// </summary>
public class SettlementView : AFollowableView, ISettlementViewable {

    public new SettlementCmdPresenter Presenter {
        get { return base.Presenter as SettlementCmdPresenter; }
        protected set { base.Presenter = value; }
    }

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Indicates whether the item this view
    /// is associated with is detectable by the human player. 
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
    }

    public AudioClip dying;
    private AudioSource _audioSource;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        circleScaleFactor = 1.0F;
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * Radius);
        InitializeHighlighting();
    }

    protected override void InitializePresenter() {
        Presenter = new SettlementCmdPresenter(this);
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnInCameraLOSChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, IsDetectable);
        AssessHighlighting();
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        IsDetectable = PlayerIntelLevel != IntelLevel.Nil;
    }

    public override void AssessHighlighting() {
        if (!IsDetectable || !InCameraLOS) {
            ShowMesh(false);
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            ShowMesh(true);
            Highlight(Highlights.Focused);
            return;
        }
        ShowMesh(true);
        Highlight(Highlights.None);
    }

    private void ShowMesh(bool toShow) {
        if (toShow) {
            _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
            _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
            // TODO audio on goes here
        }
        else {
            _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
            _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
            // TODO audio off goes here
        }
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void InitializeHighlighting() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISettlementViewable Members

    public event Action onShowCompletion;

    public void ShowAttacking() {
        // TODO
    }

    public void ShowHit() {
        // TODO
    }

    public void ShowRepairing() {
        // TODO
    }

    public void ShowRefitting() {
        // TODO
    }

    public void StopShowing() {
        // TODO
    }

    public void ShowDying() {
        new Job(ShowingDying(), toStart: true);
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        Collider.enabled = false;
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;

        var sc = onShowCompletion;
        if (sc != null) {
            sc();
        }
    }

    #endregion

}

