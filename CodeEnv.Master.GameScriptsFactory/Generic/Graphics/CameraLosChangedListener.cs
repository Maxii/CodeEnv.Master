// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraLosChangedListener.cs
// Event Hook class lets you easily add remote Camera LineOfSight Changed event listener functions to an object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Event Hook class lets you easily add remote Camera LineOfSight Changed listener functions to an object.
/// Example usage: CameraLosChangedListener.Get(remoteGameObject).onCameraLosChanged += MyLocalMethodToExecute; 
/// Derived from Ngui's UIEventListener class.
/// </summary>
public class CameraLosChangedListener : AMonoBase, ICameraLosChangedListener {

    private const string DebugNameFormat = "{0}.{1}";

    private const string MeshGoNameFormat = "{0} Invisible Bounds";

    private static readonly Vector2 DefaultMeshSize = new Vector2(4F, 4F);

    private static readonly Vector3[] DefaultMeshLocalCorners = new Vector3[] { new Vector3(-2F, -2F),
                                                                                new Vector3(-2F, 2F),
                                                                                new Vector3(2F, 2F),
                                                                                new Vector3(2F, -2F)
                                                                              };

    /// <summary>
    /// Get or add an event listener to the specified game object.
    /// </summary>
    public static CameraLosChangedListener Get(GameObject go) {

        Profiler.BeginSample("Proper AddComponent allocation", go);
        CameraLosChangedListener listener = go.AddMissingComponent<CameraLosChangedListener>();
        Profiler.EndSample();

        return listener;
    }

    /// <summary>
    /// Occurs when Camera Line Of Sight state has changed on this GameObject.
    /// </summary>
    public event EventHandler inCameraLosChanged;

    private bool _inCameraLOS;
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        private set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS"); }
    }

    private string DebugName { get { return DebugNameFormat.Inject(name, typeof(CameraLosChangedListener).Name); } }

    private Renderer _renderer;
    private bool _hasInvisibleMesh;

    protected override void Awake() {
        base.Awake();
        enabled = false;
        Initialize();
    }

    private void Initialize() {

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        _renderer = GetComponent<Renderer>();
        Profiler.EndSample();

        if (_renderer == null) {
            InitializeInvisibleMesh();  // creates and assigns _renderer
        }
    }

    protected override void Start() {
        base.Start();
        _renderer.enabled = true;
        if (!_renderer.isVisible) {
            // Note: All subscribers begin with InCameraLOS = true, aka they think they are in the camera's line of sight. 
            // When the renderer first wakes up, it sends OnBecameInvisible if it is not visible. If this occurs before this listener 
            // is enabled, the listener's subscribers will not receive the notification. The following call makes sure the notification 
            // is repeated when this listener becomes enabled. It is not needed when using an invisible mesh as the newly installed 
            // invisible mesh renderer will immediately notify subscribers of its inCameraLOS state.
            OnBecameInvisible();
        }
    }

    #region Event and Property Change Handlers

    void OnBecameVisible() {
        if (enabled) {
            //D.LogContext(this, "{0} has received OnBecameVisible().", DebugName);
            InCameraLOS = true;
            OnInCameraLosChanged();
        }
        else {
            if (!_hasInvisibleMesh) {    // invisible mesh will always immediately generate an event as I can't instantiate it disabled
                D.WarnContext(this, "{0}.OnBecameVisible() called while not enabled. This is probably because the MeshRenderer on this object started enabled in the Inspector.",
                    DebugName);
            }
        }
    }

    void OnBecameInvisible() {
        if (IsApplicationQuiting || GameManager.Instance.IsSceneLoading) {
            // OnBecameInvisible called if already visible when application is quiting
            return;
        }

        if (enabled) {
            //D.LogContext(this, "{0} has received OnBecameInvisible().", DebugName);
            InCameraLOS = false;
            OnInCameraLosChanged();
        }
        else {
            if (!_hasInvisibleMesh) {  // invisible mesh will always immediately generate an event as I can't instantiate it disabled
                D.WarnContext(this, "{0}.OnBecameInvisible() called while not enabled. This is probably because the MeshRenderer on this object started enabled in the Inspector.",
                    DebugName);
            }
        }
    }

    private void OnInCameraLosChanged() {
        if (inCameraLosChanged != null) {
            inCameraLosChanged(this, EventArgs.Empty);
        }
        else {
            D.WarnContext(this, "{0} has no subscriber. InCameraLOS = {1}.", DebugName, InCameraLOS);
        }
    }

    #endregion

    #region Invisible Mesh System supporting OnBecameVisible/Invisible

    private static IDictionary<Vector2, Mesh> _meshCache = new Dictionary<Vector2, Mesh>(Vector2EqualityComparer.Default);

    /// <summary>
    /// Temporary workaround used to filter out all UIWidget.onChange events that aren't caused by a change in Widget Dimensions.
    /// </summary>
    private Vector2 __previousMeshSize;
    private UIWidget _widget;   // can be null
    private MeshFilter _meshFilter;

    /// <summary>
    /// Sets up an invisible bounds mesh that enables OnBecameVisible/Invisible to properly operate,
    /// even without having a pre-installed Renderer. Derived from Vectrosity.VectorManager.
    /// </summary>
    private void InitializeInvisibleMesh() {
        _hasInvisibleMesh = true;

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        _meshFilter = gameObject.AddMissingComponent<MeshFilter>();
        _renderer = gameObject.AddMissingComponent<MeshRenderer>();
        Profiler.EndSample();

        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        D.Assert(_renderer.enabled);    // renderers do not deliver OnBecameVisible() events if not enabled

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        _widget = gameObject.GetComponent<UIWidget>();
        Profiler.EndSample();

        // 1.14.17 Replaced subscription to widget.onChange with CheckForInvisibleMeshSizeChange()
        CheckInvisibleMeshSize();
    }

    /// <summary>
    /// Checks whether the InvisibleMesh's size should be changed if any is present.
    /// <remarks>Typically called after there is a change to the associated widget's mesh size.</remarks>
    /// </summary>
    public void CheckForInvisibleMeshSizeChange() {
        CheckInvisibleMeshSize();
    }

    /// <summary>
    /// Checks to see if a different mesh is needed to match Widget.size and if so, provides it. 
    /// This is typically called when the Widget's dimensions change.
    /// </summary>
    private void CheckInvisibleMeshSize() {
        Vector2 meshSize = _widget != null ? _widget.localSize : DefaultMeshSize;
        if (__previousMeshSize == meshSize) {
            // 1.14.17 Occurs infrequently, typically when a TrackingWidget's IconInfo changes after initially being set.
            // This is caused most commonly by the need for an icon color or composition change.
            //D.Warn("{0} invisible mesh size {1} check without a widget dimension change.", DebugName, meshSize);
            return;
        }
        //D.Log("{0} invisible mesh size changing from {1} to {2}.", DebugName, __previousMeshSize, meshSize);
        __previousMeshSize = meshSize;

        Vector2 cacheKey = meshSize; // meshSize.ToString();  // "[0.0, 1.0]"
        if (!_meshCache.ContainsKey(cacheKey)) {
            Vector3[] meshLocalCorners = _widget != null ? _widget.localCorners : DefaultMeshLocalCorners;
            _meshCache.Add(cacheKey, MakeBoundsMesh(UnityUtility.GetBounds(meshLocalCorners)));
            _meshCache[cacheKey].name = MeshGoNameFormat.Inject(cacheKey);
        }
        else {
            //D.Log("{0} is reusing {1} mesh.", DebugName, _meshCache[cacheKey].name);
        }
        _meshFilter.mesh = _meshCache[cacheKey];
    }

    /// <summary>
    /// Makes an invisible (as there are no triangles) mesh between the vertices of the provided bounds.
    /// Derived from Vectrosity.VectorManager.
    /// </summary>
    /// <param name="bounds">The bounds.</param>
    /// <returns></returns>
    private static Mesh MakeBoundsMesh(Bounds bounds) {
        var mesh = new Mesh();
        mesh.vertices = new[] {bounds.center + new Vector3(-bounds.extents.x,  bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x,  bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x,  bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x,  bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x, -bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x, -bounds.extents.y, -bounds.extents.z)};
        return mesh;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

