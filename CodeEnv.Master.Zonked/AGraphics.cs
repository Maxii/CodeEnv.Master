// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGraphics.cs
// Abstract base class supporting graphics optimization for Ships, Fleets and Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class supporting graphics optimization for Ships, Fleets and Systems.
/// </summary>
[System.Obsolete]
public abstract class AGraphics : AMonoBase, ICameraLOSChangedClient {

    public enum Highlights {

        None = -1,
        /// <summary>
        /// The item is the focus.
        /// </summary>
        Focused = 0,
        /// <summary>
        /// The item is selected..
        /// </summary>
        Selected = 1,
        /// <summary>
        /// The item is highlighted for other reasons. This is
        /// typically used on a fleet's ships when the fleet is selected.
        /// </summary>
        General = 2,
        /// <summary>
        /// The item is both selected and the focus.
        /// </summary>
        SelectedAndFocus = 3,
        /// <summary>
        /// The item is both the focus and generally highlighted.
        /// </summary>
        FocusAndGeneral = 4

    }

    /// <summary>
    /// The transform to use when determining distance to the camera.
    /// </summary>
    protected Transform Target { get; set; }

    public int maxAnimateDistance;

    /// <summary>
    /// The components to disable when not visible or detectable.
    /// </summary>
    protected Component[] disableComponentOnNotDiscernible;

    /// <summary>
    /// The game objects to disable when not visible or detectable.
    /// </summary>
    protected GameObject[] disableGameObjectOnNotDiscernible;

    /// <summary>
    /// The components to disable when invisible or too far/close to the camera.
    /// </summary>
    protected Component[] disableComponentOnCameraDistance;

    /// <summary>
    /// The game objects to disable when invisible or too far/close to the camera.
    /// </summary>
    protected GameObject[] disableGameObjectOnCameraDistance;

    private IList<Transform> _visibleMeshes = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Seldom;
    }

    protected override void Start() {
        base.Start();
        Arguments.ValidateNotNull(Target);
        RegisterComponentsToDisable();
    }

    /// <summary>
    /// Optional ability to register components and gameobjects to disable programatically.
    /// </summary>
    protected abstract void RegisterComponentsToDisable();

    public abstract void AssessHighlighting();

    protected virtual void Update() {
        if (ToUpdate()) {
            OnToUpdate();
        }
    }

    protected virtual void OnToUpdate() {
        EnableBasedOnDistanceToCamera();
    }

    protected virtual void OnIsVisibleChanged() {
        EnableBasedOnDiscernible(InCameraLOS);
        EnableBasedOnDistanceToCamera(InCameraLOS);
        AssessHighlighting();
    }

    protected void EnableBasedOnDiscernible(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        D.Log("{0}.EnableBasedOnDiscernible() called. IsDiscernible = {1}.", this.GetType().Name, condition);
        if (!disableComponentOnNotDiscernible.IsNullOrEmpty()) {

            disableComponentOnNotDiscernible.Where(c => c is Behaviour).ForAll(c => (c as Behaviour).enabled = condition);
            disableComponentOnNotDiscernible.Where(c => c is Renderer).ForAll(c => (c as Renderer).enabled = condition);
            disableComponentOnNotDiscernible.Where(c => c is Collider).ForAll(c => (c as Collider).enabled = condition);
        }
        if (!disableGameObjectOnNotDiscernible.IsNullOrEmpty()) {

            disableGameObjectOnNotDiscernible.ForAll(go => go.SetActive(condition));
        }
    }

    /// <summary>
    /// Controls enabled state of components based on the Target's distance from the camera plane.
    /// </summary>
    /// <returns>The Target's distance to the camera. Will be zero if not visible.</returns>
    protected virtual int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = Constants.Zero;
        if (maxAnimateDistance == Constants.Zero) {
            D.Warn("{0}.maxAnimateDistance is 0 on {1}.", this.GetType().Name, gameObject.name);
        }

        bool toEnable = false;
        if (condition) {
            distanceToCamera = Target.DistanceToCameraInt();
            //D.Log("CameraPlane distance to {2} = {0}, CameraTransformPosition distance = {1}.", distanceToCamera, Vector3.Distance(Camera.main.transform.position, Target.position), Target.name);
            //D.Log("{0}.EnableBasedOnDistanceToCamera() called. Distance = {1}.", this.GetType().Name, distanceToCamera);
            if (distanceToCamera < maxAnimateDistance) {
                toEnable = true;
            }
        }
        EnableComponents(toEnable);
        return distanceToCamera;
    }

    private bool _isPreviouslyEnabled = true;   // assumes all components and game objects start enabled
    private void EnableComponents(bool toEnable) {
        if (_isPreviouslyEnabled != toEnable) {
            //if (disableComponentOnCameraDistance.Length != Constants.Zero) {
            if (!disableComponentOnCameraDistance.IsNullOrEmpty()) {

                disableComponentOnCameraDistance.Where(c => c is Behaviour).ForAll(c => (c as Behaviour).enabled = toEnable);
                disableComponentOnCameraDistance.Where(c => c is Renderer).ForAll(c => (c as Renderer).enabled = toEnable);
                disableComponentOnCameraDistance.Where(c => c is Collider).ForAll(c => (c as Collider).enabled = toEnable);
            }
            //if (disableGameObjectOnCameraDistance.Length != Constants.Zero) {
            if (!disableGameObjectOnCameraDistance.IsNullOrEmpty()) {

                disableGameObjectOnCameraDistance.ForAll(go => go.SetActive(toEnable));
            }
            _isPreviouslyEnabled = toEnable;
        }
    }

    private void OnMeshNotifingCameraLOSChanged(Transform sender, bool isVisible) {
        if (isVisible) {
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
            if (!_visibleMeshes.Contains(sender)) {
                _visibleMeshes.Add(sender);
            }
        }
        else {
            _visibleMeshes.Remove(sender);
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
        }

        if (InCameraLOS == (_visibleMeshes.Count == 0)) {
            // visibility state of this object should now change
            InCameraLOS = !InCameraLOS;
            D.Log("{0} isVisible changed to {1}.", gameObject.name, InCameraLOS);
        }
    }

    #region ICameraLOSChangedClient Members

    private bool _inCameraLOS = true; // everyone starts out thinking they are visible as it controls the enabled/activated state of key components
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        private set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnIsVisibleChanged); }
    }

    public void NotifyCameraLOSChanged(Transform sender, bool inLOS) {
        OnMeshNotifingCameraLOSChanged(sender, inLOS);
    }

    #endregion

}

