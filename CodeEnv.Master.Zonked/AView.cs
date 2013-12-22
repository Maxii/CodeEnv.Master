// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AView.cs
// Abstract base class managing the UI for its object. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class managing the UI for its object. 
/// </summary>
public abstract class AView : AMonoBase, IViewable, ICameraLOSChangedClient, ICameraTargetable, IDisposable {

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

    protected float _radius;
    /// <summary>
    /// The [float] radius of this object in units measured as the distance from the 
    ///center to the min or max extent. As bounds is a bounding box it is the longest 
    /// diagonal from the center to a corner of the box. Most of the time, the collider can be
    /// used to calculate this size, assuming it doesn't change size dynmaically. 
    /// Alternatively, a mesh can be used.
    /// </summary>
    public virtual float Radius {
        get {
            if (_radius == Constants.ZeroF) {
                _radius = _collider.bounds.extents.magnitude;
            }
            return _radius;
        }
    }

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

    protected Collider _collider;

    private IList<Transform> _meshesInCameraLOS = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        UpdateRate = FrameUpdateFrequency.Seldom;
        enabled = false;
    }

    protected override void Start() {
        base.Start();
        RegisterComponentsToDisable();
    }

    public abstract void AssessHighlighting();

    /// <summary>
    /// Optional ability to register components and gameobjects to disable programatically.
    /// </summary>
    protected abstract void RegisterComponentsToDisable();

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        EnableBasedOnDistanceToCamera();
    }

    protected virtual void OnHover(bool isOver) {
        ShowHud(isOver);
    }

    protected virtual void OnPlayerIntelLevelChanged() {
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            ShowHud(true);
        }
    }

    protected virtual void OnInCameraLOSChanged() {
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
            distanceToCamera = _transform.DistanceToCameraInt();
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
            if (!disableComponentOnCameraDistance.IsNullOrEmpty()) {
                disableComponentOnCameraDistance.Where(c => c is Behaviour).ForAll(c => (c as Behaviour).enabled = toEnable);
                disableComponentOnCameraDistance.Where(c => c is Renderer).ForAll(c => (c as Renderer).enabled = toEnable);
                disableComponentOnCameraDistance.Where(c => c is Collider).ForAll(c => (c as Collider).enabled = toEnable);
            }
            if (!disableGameObjectOnCameraDistance.IsNullOrEmpty()) {
                disableGameObjectOnCameraDistance.ForAll(go => go.SetActive(toEnable));
            }
            _isPreviouslyEnabled = toEnable;
        }
    }

    private void OnMeshNotifingCameraLOSChanged(Transform sender, bool inLOS) {
        if (inLOS) {
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
            if (!_meshesInCameraLOS.Contains(sender)) {
                _meshesInCameraLOS.Add(sender);
            }
        }
        else {
            _meshesInCameraLOS.Remove(sender);
            // removed assertion tests and warnings as it will take a while to get the lists and state in sync
        }

        if (InCameraLOS == (_meshesInCameraLOS.Count == 0)) {
            // visibility state of this object should now change
            InCameraLOS = !InCameraLOS;
            D.Log("{0}.InCameraLOS changed to {1}.", gameObject.name, InCameraLOS);
        }
    }

    private void ShowHud(bool toShow) {
        if (HudPublisher != null) {
            HudPublisher.ShowHud(toShow, PlayerIntelLevel);
            return;
        }
        D.Warn("{0} HudPublisher is null.", gameObject.name);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    protected virtual void Cleanup() {
        if (HudPublisher != null) {
            (HudPublisher as IDisposable).Dispose();
        }
    }

    #region IViewable Members

    private IntelLevel _playerIntelLevel;
    public virtual IntelLevel PlayerIntelLevel {
        get {
            return _playerIntelLevel;
        }
        set {
            SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnPlayerIntelLevelChanged);
        }
    }

    public IGuiHudPublisher HudPublisher { get; set; }

    #endregion

    #region ICameraLOSChangedClient Members

    private bool _inCameraLOS = true; // everyone starts out thinking they are visible as it controls the enabled/activated state of key components
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        private set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnInCameraLOSChanged); }
    }

    public void NotifyCameraLOSChanged(Transform sender, bool inLOS) {
        OnMeshNotifingCameraLOSChanged(sender, inLOS);
    }

    #endregion

    #region ICameraTargetable Members

    public virtual bool IsEligible {
        get { return true; }
    }

    [SerializeField]
    protected float minimumCameraViewingDistanceMultiplier = 4.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = CalcMinimumCameraViewingDistance();
            }
            return _minimumCameraViewingDistance;
        }
    }

    /// <summary>
    /// One time calculation of the minimum camera viewing distance.
    /// </summary>
    /// <returns></returns>
    protected virtual float CalcMinimumCameraViewingDistance() {
        return Radius * minimumCameraViewingDistanceMultiplier;
    }

    #endregion

    #region IDisposable

    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

