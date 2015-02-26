// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADiscernibleItemDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public abstract class ADiscernibleItemDisplayManager : APropertyChangeTracking, IDisposable {


    private bool _inCameraLOS = true;
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

    /// <summary>
    /// Property that allows each derived class to establish the radius of the sphericalHighlight.
    /// Default is twice the radius of the item.
    /// </summary>
    protected virtual float SphericalHighlightRadius { get { return Item.Radius * 2F; } }

    /// <summary>
    /// The radius of the smallest highlighting circle used by this Item.
    /// </summary>
    protected virtual float RadiusOfHighlightCircle { get { return Screen.height * Item.Radius * ItemTypeCircleScale; } }

    /// <summary>
    /// Circle scale factor specific to the derived type of the Item.
    /// e.g. ShipItem, CommandItem, StarItem, etc.
    /// </summary>
    protected virtual float ItemTypeCircleScale { get { return 3.0F; } }

    protected bool _isCirclesRadiusDynamic = true;
    private HighlightCircle _circles;



    //protected Animation _meshAnimation;
    protected ADiscernibleItem Item { get; private set; }

    protected bool _isMeshShowing;
    protected bool _isMeshInCameraLOS = true;

    protected MeshRenderer _primaryMeshRenderer;

    public ADiscernibleItemDisplayManager(ADiscernibleItem item) {
        Item = item;
        Initialize();
        //ShowMesh(true);
    }

    private void Initialize() {
        var itemGo = Item.gameObject;
        _primaryMeshRenderer = InitializePrimaryMesh(itemGo);
        //meshRenderer.castShadows = false;
        //meshRenderer.receiveShadows = false;
        _primaryMeshRenderer.enabled = true;

        var meshAnimation = _primaryMeshRenderer.gameObject.GetComponent<Animation>();
        meshAnimation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        meshAnimation.enabled = true;
        // TODO animation settings and distance controls

        var meshCameraLosChgdListener = _primaryMeshRenderer.gameObject.GetSafeMonoBehaviourComponent<CameraLosChangedListener>();
        meshCameraLosChgdListener.onCameraLosChanged += (go, isMeshInCameraLOS) => OnMeshInCameraLOSChanged(isMeshInCameraLOS);
        meshCameraLosChgdListener.enabled = true;

        InitializeSecondaryMeshes(itemGo);

        InitializeOther(itemGo);

        //ShowPrimaryMesh(true);
        AssessShowing();
    }
    //protected virtual void Initialize() {
    //    var itemGo = _item.gameObject;
    //    var meshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
    //    meshRenderer.castShadows = false;
    //    meshRenderer.receiveShadows = false;
    //    meshRenderer.enabled = true;

    //    _meshAnimation = meshRenderer.gameObject.GetComponent<Animation>();
    //    _meshAnimation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
    //    // TODO animation settings and distance controls

    //    var meshCameraLosChgdListener = meshRenderer.gameObject.GetSafeMonoBehaviourComponent<CameraLosChangedListener>();
    //    meshCameraLosChgdListener.onCameraLosChanged += (go, isMeshInCameraLOS) => OnMeshInCameraLOSChanged(isMeshInCameraLOS);
    //    meshCameraLosChgdListener.enabled = true;
    //}

    protected abstract MeshRenderer InitializePrimaryMesh(GameObject itemGo);

    protected virtual void InitializeSecondaryMeshes(GameObject itemGo) { }

    protected virtual void InitializeOther(GameObject itemGo) { }


    protected virtual void OnIsDisplayEnabledChanged() {
        AssessShowing();
        //if (!IsDisplayEnabled) {
        //    if (_isMeshShowing) {
        //        ShowPrimaryMesh(false);
        //    }
        //}
        //else {
        //    // Nothing to do when enabled. Mesh animation will show when determined by AssessInCameraLOS()
        //}
    }


    private void ShowPrimaryMesh(bool toShow) {
        // can't disable meshRenderer as lose OnMeshInCameraLOSChanged events
        //D.Log("{0}.ShowMesh({1}) called.", GetType().Name, toShow);
        if (_isMeshShowing == toShow) {
            D.Warn("{0} recording duplicate call to ShowMesh({1}).", GetType().Name, toShow);
            return;
        }
        if (toShow) {
            ShowPrimaryMesh();
        }
        else {
            HidePrimaryMesh();
        }
        _isMeshShowing = toShow;
    }

    protected virtual void ShowPrimaryMesh() {
        // does nothing as most primary meshes are always enabled once this display mgr is created
    }

    protected virtual void HidePrimaryMesh() {
        // does nothing as most primary meshes are always enabled once this display mgr is created
    }

    protected virtual void OnMeshInCameraLOSChanged(bool isMeshInCameraLOS) {
        D.Log("{0}.OnMeshInCameraLOSChanged({1}) called.", GetType().Name, isMeshInCameraLOS);
        _isMeshInCameraLOS = isMeshInCameraLOS;
        AssessInCameraLOS();
        AssessShowing();
        //if (IsDisplayEnabled) {
        //    if (!isMeshInCameraLOS) {
        //        // mesh moved beyond culling distance while on the screen or off the screen while within culling distance
        //        ShowPrimaryMesh(false);
        //    }
        //    else {
        //        // mesh moved within culling distance while on the screen or onto the screen while within culling distance
        //        ShowPrimaryMesh(true);
        //    }
        //}
    }

    protected virtual void AssessShowing() {
        bool toShow = IsDisplayEnabled && _isMeshInCameraLOS;
        ShowPrimaryMesh(toShow);
    }

    protected virtual void AssessInCameraLOS() {
        // one or the other inCameraLOS needs to be true for this to be set to true, both inCameraLOS needs to be false for this to trigger false
        InCameraLOS = _isMeshInCameraLOS;
    }

    public virtual void Highlight(Highlights highlight) {
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

    /// <summary>
    /// Shows or hides the highlighting circles around this item. Derived classes should override
    /// this if they wish to have the circles track a different transform besides the transform associated 
    /// with this item.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="highlight">The highlight.</param>
    protected virtual void ShowCircle(bool toShow, Highlights highlight) {
        ShowCircle(toShow, highlight, Item.Transform);
    }

    /// <summary>
    /// Shows or hides highlighting circles.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    /// <param name="highlight">The highlight.</param>
    /// <param name="transform">The transform the circles should track.</param>
    protected void ShowCircle(bool toShow, Highlights highlight, Transform transform) {
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            string circlesTitle = "{0} Circle".Inject(Item.DisplayName);
            _circles = new HighlightCircle(circlesTitle, transform, RadiusOfHighlightCircle, _isCirclesRadiusDynamic, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        //string showHide = toShow ? "showing" : "not showing";
        //D.Log("{0} {1} circle {2}.", gameObject.name, showHide, highlight.GetName());
        _circles.Show(toShow, (int)highlight);
    }

    public void ShowSphericalHighlight(bool toShow) {
        var sphericalHighlight = References.SphericalHighlight;
        if (sphericalHighlight != null) {  // allows deactivation of the SphericalHighlight gameObject
            if (toShow) {
                sphericalHighlight.SetTarget(Item, SphericalHighlightRadius);
            }
            sphericalHighlight.Show(toShow);
        }
    }

    protected virtual void Cleanup() {
        if (_circles != null) { _circles.Dispose(); }
        // other cleanup here including any tracking Gui2D elements
    }


    #region IDisposable
    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

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
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
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


    #region Nested Classes

    public enum Highlights {

        None = -1,
        /// <summary>
        /// The item is the focus.
        /// </summary>
        Focused = 0,
        /// <summary>
        /// The item is selected.
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

    #endregion


}

