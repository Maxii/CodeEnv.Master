// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityView.cs
// A class for managing the elements of a Facility's UI. 
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
/// A class for managing the elements of a Facility's UI. 
/// </summary>
public class FacilityView : AElementView {
    //public class FacilityView : AFocusableView, IFacilityViewable {

    public new FacilityPresenter Presenter {
        get { return base.Presenter as FacilityPresenter; }
        protected set { base.Presenter = value; }
    }

    //public AudioClip dying;
    //private AudioSource _audioSource;

    //private Color _originalMeshColor_Main;
    //private Color _originalMeshColor_Specular;
    //private Color _hiddenMeshColor;
    //private Renderer _renderer;

    //protected override void Awake() {
    //    base.Awake();
    //    _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
    //    circleScaleFactor = 1.0F;
    //    InitializeMesh();
    //}

    protected override void InitializePresenter() {
        Presenter = new FacilityPresenter(this);
    }



    //protected override void OnIsDiscernibleChanged() {
    //    base.OnIsDiscernibleChanged();
    //    ShowMesh(IsDiscernible);
    //}

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAttacked();
    }

    //private void InitializeMesh() {
    //    _renderer = gameObject.GetComponentInChildren<Renderer>();
    //    _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
    //    _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
    //    _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    //}

    //private void ShowMesh(bool toShow) {
    //    if (toShow) {
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
    //        // TODO audio on goes here
    //    }
    //    else {
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
    //        // TODO audio off goes here
    //    }
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region ICameraFocusable Members

    //protected override float CalcOptimalCameraViewingDistance() {
    //    return Radius * 2.4F;
    //}

    //#endregion

    //#region ICameraTargetable Members

    //public override bool IsEligible {
    //    get {
    //        return PlayerIntel.Source != IntelSource.None;
    //    }
    //}

    //protected override float CalcMinimumCameraViewingDistance() {
    //    return Radius * 2.0F;
    //}

    //#endregion


    //#region IFacilityViewable Members

    //public event Action onShowCompletion;

    //public void ShowAttacking() {
    //    // TODO
    //}

    //public void ShowHit() {
    //    // TODO
    //}

    //public void ShowRepairing() {
    //    // TODO
    //}

    //public void ShowRefitting() {
    //    // TODO
    //}

    //public void StopShowing() {
    //    // TODO
    //}

    //public void ShowDying() {
    //    new Job(ShowingDying(), toStart: true);
    //}

    //private IEnumerator ShowingDying() {
    //    if (dying != null) {
    //        _audioSource.PlayOneShot(dying);
    //    }
    //    _collider.enabled = false;
    //    //animation.Stop();
    //    //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
    //    yield return null;

    //    var sc = onShowCompletion;
    //    if (sc != null) {
    //        sc();
    //    }
    //}

    //#endregion

}

