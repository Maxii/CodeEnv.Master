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
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for managing an Element's UI. 
/// </summary>
public abstract class AUnitElementView : AMortalItemView, IElementViewable, ICameraFollowable {

    public new AUnitElementPresenter Presenter {
        get { return base.Presenter as AUnitElementPresenter; }
        protected set { base.Presenter = value; }
    }

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

    protected override void OnLeftDoubleClick() {
        base.OnLeftDoubleClick();
        SelectCommand();
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

    protected override float CalcMinimumCameraViewingDistance() {
        return Radius * 2.0F;
    }

    #endregion

    #region IElementViewable Members

    // the following must return onShowCompletion when finished to inform 
    // ElementItem when it is OK to progress to the next state
    public void ShowAttacking() {
        // TODO
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

