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
public abstract class AGraphics : AMonoBehaviourBase, INotifyVisibilityChanged {

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
    /// The components to disable ONLY when invisible.
    /// </summary>
    protected Component[] disableComponentOnInvisible;

    /// <summary>
    /// The game objects to disable ONLY when invisible.
    /// </summary>
    protected GameObject[] disableGameObjectOnInvisible;

    /// <summary>
    /// The components to disable when invisible AND based on distance from camera.
    /// </summary>
    protected Component[] disableComponentOnCameraDistance;

    /// <summary>
    /// The game objects to disable when invisible AND based on distance from camera.
    /// </summary>
    protected GameObject[] disableGameObjectOnCameraDistance;

    private IList<Transform> _visibleMeshes = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter

    protected override void Awake() {
        base.Awake();
        _isVisible = true;
        UpdateRate = FrameUpdateFrequency.Seldom;
    }

    protected override void Start() {
        base.Start();
        Arguments.ValidateNotNull(Target);
        RegisterComponentsToDisable();
        //if (disableComponentOnCameraDistance.Length == Constants.Zero && disableGameObjectOnCameraDistance.Length == Constants.Zero &&
        //    disableComponentOnInvisible.Length == Constants.Zero && disableGameObjectOnInvisible.Length == Constants.Zero) {
        //    RegisterComponentsToDisable();
        //}
    }

    /// <summary>
    /// Optional ability to register components and gameobjects to disable programatically.
    /// Automatically called if the public arrays are empty.
    /// </summary>
    protected abstract void RegisterComponentsToDisable();

    protected virtual void Update() {
        if (ToUpdate()) {
            OnToUpdate();
        }
    }

    protected virtual void OnToUpdate() {
        EnableBasedOnDistanceToCamera();
    }

    protected virtual void OnIsVisibleChanged() {
        EnableBasedOnVisibility();
        EnableBasedOnDistanceToCamera();
    }

    private void EnableBasedOnVisibility() {
        D.Log("{0}.EnableBasedOnVisibility() called. IsVisible = {1}.", this.GetType().Name, IsVisible);
        //if (disableComponentOnInvisible.Length != Constants.Zero) {
        if (!disableComponentOnInvisible.IsNullOrEmpty()) {

            disableComponentOnInvisible.Where(c => c is Behaviour).ForAll(c => (c as Behaviour).enabled = IsVisible);
            disableComponentOnInvisible.Where(c => c is Renderer).ForAll(c => (c as Renderer).enabled = IsVisible);
            disableComponentOnInvisible.Where(c => c is Collider).ForAll(c => (c as Collider).enabled = IsVisible);
        }
        //if (disableGameObjectOnInvisible.Length != Constants.Zero) {
        if (!disableGameObjectOnInvisible.IsNullOrEmpty()) {

            disableGameObjectOnInvisible.ForAll(go => go.SetActive(IsVisible));
        }
    }

    /// <summary>
    /// Controls enabled state of components based on the Target's distance from the camera plane.
    /// </summary>
    /// <returns>The Target's distance to the camera. Will be zero if not visible.</returns>
    protected virtual int EnableBasedOnDistanceToCamera() {
        int distanceToCamera = Constants.Zero;
        if (maxAnimateDistance == Constants.Zero) {
            D.Warn("{0}.maxAnimateDistance is 0 on {1}.", this.GetType().Name, gameObject.name);
        }

        bool toEnable = false;
        if (IsVisible) {
            distanceToCamera = Target.DistanceToCameraInt();
            D.Log("{0}.EnableBasedOnDistanceToCamera() called. Distance = {1}.", this.GetType().Name, distanceToCamera);
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

    private void OnAMeshVisibilityChanged(Transform sender, bool isVisible) {
        if (isVisible) {
            D.Assert(!_visibleMeshes.Contains(sender), "Sender is: {0}.".Inject(sender.name), pauseOnFail: true);
            _visibleMeshes.Add(sender);
        }
        else {
            if (!_visibleMeshes.Remove(sender)) {
                D.Warn("{0} was not removed from VisibleMeshes.", sender.name);
            }
        }

        if (IsVisible == (_visibleMeshes.Count == 0)) {
            // visibility state of this object should now change
            IsVisible = !IsVisible;
            D.Log("{0} isVisible changed to {1}.", gameObject.name, IsVisible);
        }
    }

    #region INotifyVisibilityChanged Members

    private bool _isVisible;
    public bool IsVisible {
        get { return _isVisible; }
        set { SetProperty<bool>(ref _isVisible, value, "IsVisible", OnIsVisibleChanged); }
    }

    public void NotifyVisibilityChanged(Transform sender, bool isVisible) {
        OnAMeshVisibilityChanged(sender, isVisible);
    }

    #endregion

}

