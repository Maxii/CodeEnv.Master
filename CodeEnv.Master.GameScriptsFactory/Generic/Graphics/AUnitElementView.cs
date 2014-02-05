// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementView.cs
// Abstract base class for managing an Element's UI. 
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
/// Abstract base class for managing an Element's UI. 
/// </summary>
public abstract class AUnitElementView : AFocusableItemView, IElementViewable, ICameraFollowable {

    public new AUnitElementPresenter Presenter {
        get { return base.Presenter as AUnitElementPresenter; }
        protected set { base.Presenter = value; }
    }

    public AudioClip dying;
    private AudioSource _audioSource;
    protected Job _showingJob;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    protected override void Awake() {
        base.Awake();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
        circleScaleFactor = 1.0F;
        InitializeMesh();
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowMesh(IsDiscernible);
    }

    private void SelectCommand() {
        Presenter.IsCommandSelected = true;
    }

    private void InitializeMesh() {
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _hiddenMeshColor = GameColor.Clear.ToUnityColor();
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

    #region Mouse Events

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

    protected virtual void OnLeftDoubleClick() {
        SelectCommand();
    }

    #endregion

    #region ICameraFollowable Members

    // TODO Settlement Facilities should be followable as they orbit, but Starbase Facilities?

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

    #region ICameraFocusable Members

    protected override float CalcOptimalCameraViewingDistance() {
        return Radius * 2.4F;
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return PlayerIntel.CurrentCoverage != IntelCoverage.None;
        }
    }

    protected override float CalcMinimumCameraViewingDistance() {
        return Radius * 2.0F;
    }

    #endregion

    #region IElementViewable Members

    public event Action onShowCompletion;

    /// <summary>
    /// Safely invokes the onShowCompletion event.
    /// </summary>
    protected void OnShowCompletion() {
        var temp = onShowCompletion;
        if (temp != null) {
            temp();
        }
    }

    // the following must return onShowCompletion when finished to inform 
    // ElementItem when it is OK to progress to the next state

    public void ShowHit() {
        // TODO
        OnShowCompletion();
    }

    public void ShowCmdHit() {
        // TODO
        OnShowCompletion();
    }

    public void ShowAttacking() {
        // TODO
        OnShowCompletion();
    }

    public void ShowDying() {
        //_showingJob = new Job(ShowingDying(), toStart: true); // Coroutines don't show the right method name when logged using stacktrace
        OnShowCompletion();
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

    // these run continuously until they are stopped via StopShowing() when
    // ElementItem state changes from the state that started them
    public void ShowRepairing() {
        throw new NotImplementedException();
    }

    public void ShowRefitting() {
        throw new NotImplementedException();
    }

    public void StopShowing() {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
    }

    #endregion

}

