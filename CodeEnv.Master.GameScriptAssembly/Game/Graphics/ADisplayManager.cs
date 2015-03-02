// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADisplayManager.cs
// Abstract base class for DisplayManagers.
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
/// Abstract base class for DisplayManagers.
/// </summary>
public abstract class ADisplayManager : APropertyChangeTracking, IDisposable {

    private bool _inCameraLOS = true;
    /// <summary>
    /// Indicates whether this item is within a Camera's Line Of Sight.
    /// Note: All items start out thinking they are in a camera's LOS. This is so IsDiscernible will properly operate
    /// during the period when a item's visual members have not yet been initialized. If and when they are
    /// initialized, the item will be notified by their CameraLosChangedListener of their actual InCameraLOS state.
    /// </summary>
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        protected set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS"); }
    }

    private bool _isDisplayEnabled;
    /// <summary>
    /// Flag controlling whether this DisplayManager is allowed to display material
    /// to the screen. This flag DOESNOT affect the operation of InCameraLOS.
    /// </summary>
    public bool IsDisplayEnabled {
        get { return _isDisplayEnabled; }
        set { SetProperty<bool>(ref _isDisplayEnabled, value, "IsDisplayEnabled", OnIsDisplayEnabledChanged); }
    }

    protected bool _isPrimaryMeshShowing;
    protected bool _isPrimaryMeshInCameraLOS = true;
    protected MeshRenderer _primaryMeshRenderer;

    public ADisplayManager(GameObject itemGO) {
        Initialize(itemGO);
    }

    private void Initialize(GameObject itemGO) {
        _primaryMeshRenderer = InitializePrimaryMesh(itemGO);
        _primaryMeshRenderer.enabled = true;

        var meshCameraLosChgdListener = _primaryMeshRenderer.gameObject.GetSafeInterface<ICameraLosChangedListener>();
        meshCameraLosChgdListener.onCameraLosChanged += (go, isMeshInCameraLOS) => OnPrimaryMeshInCameraLOSChanged(isMeshInCameraLOS);
        meshCameraLosChgdListener.enabled = true;

        InitializeSecondaryMeshes(itemGO);
        InitializeOther(itemGO);
        //AssessComponentsToShow(); no need to call here as IsDisplayEnabled is called immediately after initialization
    }

    protected abstract MeshRenderer InitializePrimaryMesh(GameObject itemGO);

    protected virtual void InitializeSecondaryMeshes(GameObject itemGO) { }

    protected virtual void InitializeOther(GameObject itemGO) { }

    protected virtual void OnIsDisplayEnabledChanged() {
        //D.Log("{0}.IsDisplayEnabled changed to {1}.", GetType().Name, IsDisplayEnabled);
        AssessComponentsToShow();
    }

    private void ShowPrimaryMesh(bool toShow) {
        // can't disable meshRenderer as lose OnMeshInCameraLOSChanged events
        if (_isPrimaryMeshShowing == toShow) {
            //D.Log("{0} recording duplicate call to ShowMesh({1}).", GetType().Name, toShow);
            return;
        }
        if (toShow) {
            ShowPrimaryMesh();
        }
        else {
            HidePrimaryMesh();
        }
        _isPrimaryMeshShowing = toShow;
    }

    /// <summary>
    /// Shows the primary mesh. This base implementation does nothing as all primary MeshRenderers are
    /// enabled from the beginning because they must feed OnVisible events to this displayMgr. Derived
    /// classes should enable any other renderers and/or components that should show or operate when the
    /// primary mesh is showing.
    /// </summary>
    protected virtual void ShowPrimaryMesh() { }

    /// <summary>
    /// Hides the primary mesh. This base implementation does nothing as primary MeshRenderers cannot
    /// be disabled because they must feed OnVisible events to this displayMgr. Derived classes should disable
    /// any other renderers and/or components that should hide or not operate when the primary mesh is hidden.
    /// </summary>
    protected virtual void HidePrimaryMesh() { }

    protected virtual void OnPrimaryMeshInCameraLOSChanged(bool isPrimaryMeshInCameraLOS) {
        //D.Log("{0}.OnPrimaryMeshInCameraLOSChanged({1}) called.", GetType().Name, isPrimaryMeshInCameraLOS);
        _isPrimaryMeshInCameraLOS = isPrimaryMeshInCameraLOS;
        AssessInCameraLOSState();
        AssessComponentsToShow();
    }

    protected virtual void AssessComponentsToShow() {
        bool toShow = IsDisplayEnabled && _isPrimaryMeshInCameraLOS;
        ShowPrimaryMesh(toShow);
    }

    protected virtual void AssessInCameraLOSState() {
        InCameraLOS = _isPrimaryMeshInCameraLOS;
    }

}

