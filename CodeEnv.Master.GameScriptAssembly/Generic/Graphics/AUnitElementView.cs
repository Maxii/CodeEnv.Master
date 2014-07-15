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
public abstract class AUnitElementView : AMortalItemView, IElementViewable, ICameraFollowable, IGuiTrackable {

    public new AUnitElementPresenter Presenter {
        get { return base.Presenter as AUnitElementPresenter; }
        protected set { base.Presenter = value; }
    }

    /// <summary>
    /// The Collider encompassing the bounds of this element that intercepts input events for this view. 
    /// This collider also detects collisions with other operating objects in the universe and therefore
    /// should NOT be disabled when it is undiscernible.
    /// </summary>
    protected override Collider Collider { get { return base.Collider; } }

    public AudioClip cmdHit;
    public AudioClip attacking;
    public AudioClip repairing;
    public AudioClip refitting;
    public AudioClip disbanding;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor;
    private Renderer _renderer;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 1.0F;
        InitializeMesh();
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowMesh(IsDiscernible);
    }

    #region Mouse Events

    protected override void OnLeftDoubleClick() {
        base.OnLeftDoubleClick();
        SelectCommand();
    }

    #endregion

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

    protected override float CalcOptimalCameraViewingDistance() {
        return Radius * 2.4F;
    }

    protected override float CalcMinimumCameraViewingDistance() {
        return Radius * 2.0F;
    }

    #region Animations

    protected override void ShowCmdHit() {
        base.ShowCmdHit();
        _showingJob = new Job(ShowingCmdHit(), toStart: true);
    }

    protected override void ShowAttacking() {
        base.ShowAttacking();
        _showingJob = new Job(ShowingAttacking(), toStart: true);
    }

    // these run continuously until they are stopped via StopAnimation() 
    protected override void ShowRepairing() {
        base.ShowRepairing();
        _showingJob = new Job(ShowingRepairing(), toStart: true);
    }

    protected override void ShowRefitting() {
        base.ShowRefitting();
        _showingJob = new Job(ShowingRefitting(), toStart: true);
    }

    protected override void ShowDisbanding() {
        base.ShowDisbanding();
        _showingJob = new Job(ShowingDisbanding(), toStart: true);
    }

    private IEnumerator ShowingCmdHit() {
        if (cmdHit != null) {
            _audioSource.PlayOneShot(cmdHit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingAttacking() {
        if (attacking != null) {
            _audioSource.PlayOneShot(attacking);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingRefitting() {
        if (refitting != null) {
            _audioSource.PlayOneShot(refitting);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingDisbanding() {
        if (disbanding != null) {
            _audioSource.PlayOneShot(disbanding);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingRepairing() {
        if (repairing != null) {
            _audioSource.PlayOneShot(repairing);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
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

    #region IGuiTrackable Members

    public Vector3 LeftExtent {
        get { return new Vector3(-Collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF); }
    }

    public Vector3 RightExtent {
        get { return new Vector3(Collider.bounds.extents.x, Constants.ZeroF, Constants.ZeroF); }
    }

    public Vector3 UpperExtent {
        get { return new Vector3(Constants.ZeroF, Collider.bounds.extents.y, Constants.ZeroF); }
    }

    public Vector3 LowerExtent {
        get { return new Vector3(Constants.ZeroF, -Collider.bounds.extents.y, Constants.ZeroF); }
    }

    public Transform Transform { get { return _transform; } }

    #endregion
}

